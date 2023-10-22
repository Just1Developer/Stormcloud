using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	partial class Stormcloud4
	{

		public void Test_1(int FinalDepth)
		{
			// Lets get the best starting move without move sorting, quiesce search, killer heuristic, advanced evaluation or transposition table, so a slow version of the algorithm
			(ulong[], ulong[]) pos = GetStartingPositions();

			Log(String_square(0));
			Log(String_square(7));
			Log(String_square(56));
			Log(String_square(63));

			for (int depth = 2; depth <= FinalDepth; depth += 2)
			{
				this.FinalDepth = depth;
				DateTime start = DateTime.Now;
				Log($"Started with depth {depth} at {start}");
				Failsoft_AlphaBeta(pos.Item1, pos.Item2, true);
				var best = Unpack(packed_Bestmove);
				DateTime end = DateTime.Now;
				Log($"Finished at {end}. Time: {((end - start).TotalSeconds >= 1.5 ? (end - start).TotalSeconds + " s" : (end - start).TotalMilliseconds + " ms")}");
				Log($"Best Move: {String_square(GetMoveSquareFrom(packed_Bestmove))} to {String_square(GetMoveSquareTo(packed_Bestmove))} (Data: {Convert.ToString(best.Item3, 2)}) (Move: {Convert.ToString(packed_Bestmove, 2)})");
			}

			return;

			static void Log(object s)
			{
				if(System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s.ToString());
				else Console.WriteLine(s.ToString());
			}
			static string String_square(int square) => $"{(char)('h' - (square % 8))}{(char)('1' + (square / 8))}";
		}



		internal double Failsoft_AlphaBeta_Multithread(ulong[] myBitboards, ulong[] opponentBitboards, bool isWhite)
			//=> Failsoft_AlphaBeta(-ALGORITHM_CONSTANT_KING_CAPTUREVALUE - 1, ALGORITHM_CONSTANT_KING_CAPTUREVALUE + 1, ref myBitboards,
			//	ref opponentBitboards, FinalDepth, isWhite, true);
		{
			ConcurrentQueue<ushort> InitialMoves = new ConcurrentQueue<ushort>();

			int ThreadCount = Environment.ProcessorCount;
			Span<ushort> allCurrentMoves = stackalloc ushort[218];
			byte moveCount = GenerateAllMoves(myBitboards, opponentBitboards, allCurrentMoves, isWhite);
			(ushort, int)[] scores = new (ushort, int)[moveCount];

			ConcurrentDictionary<ushort, int> Scores = new ConcurrentDictionary<ushort, int>();

			for (byte i = 0; i < moveCount; ++i)
			{
				InitialMoves.Enqueue(allCurrentMoves[i]);
				scores[i] = (allCurrentMoves[i], 0);
				Scores.TryAdd(allCurrentMoves[i], 0);
			}

			// Create a task for each core in the system.
			Task[] threadPoolTasks = new Task[ThreadCount];

			for (int i = 0; i < ThreadCount; i++)
			{
				var task = Task.Run(() => RunEngineTasks());
				threadPoolTasks[i] = task;
			}


			void RunEngineTasks()
			{

			}

			// No need for Run = false since the Tasks only end when Run = false
			Task.WaitAll(threadPoolTasks);
			Console.WriteLine("All Finished.");

			// Now get the best out of the initial moves
			return 0;
		}



		private ushort packed_Bestmove = 0;	// 0 means 0 movedata means no move -> 0 means no move

		public ushort PackedBestmove
		{
			get => packed_Bestmove;
		}

		// Todo Multithread

		private int FinalDepth = 10;    // Todo

		internal (ushort, double) CalculateBestMove(ulong[] myBitboards, ulong[] opponentBitboards, bool isWhite,
			int Depth)
		{
			FinalDepth = Depth;
			double score = Failsoft_AlphaBeta(-ALGORITHM_CONSTANT_KING_CAPTUREVALUE - 1, ALGORITHM_CONSTANT_KING_CAPTUREVALUE + 1, ref myBitboards,
				ref opponentBitboards, FinalDepth, isWhite, true);
			return (packed_Bestmove, score);
		}


		internal double Failsoft_AlphaBeta(ulong[] myBitboards, ulong[] opponentBitboards, bool isWhite)
			=> Failsoft_AlphaBeta(-ALGORITHM_CONSTANT_KING_CAPTUREVALUE-1, ALGORITHM_CONSTANT_KING_CAPTUREVALUE+1, ref myBitboards,
				ref opponentBitboards, FinalDepth, isWhite, true);

		private unsafe double Failsoft_AlphaBeta(double alpha, double beta, ref ulong[] myBitboards, ref ulong[] opponentBitboards,
	int depthRemaining, bool isWhite, bool isRoot = false)
		{
			if (isRoot)
			{
				packed_Bestmove = 0;
			}

			if (NotEnoughCheckmatingMaterial(myBitboards, opponentBitboards)) return 0;

			if (depthRemaining == 0) return Failsoft_AlphaBeta_Quiesce(alpha, beta, ref myBitboards, ref opponentBitboards, isWhite, 0);

			double HighestScore = double.NegativeInfinity;

			// Somehow we need to save from and to, so we need to save
			Span<ushort> moves = stackalloc ushort[218];    // Does this make sense? This is 218 * 8 bytes + overhead for each node (But when one finishes the memory is released, so there is always just 1 path, meaning at most 218*8*depth, with probably >= 1MB stack size available
			int moveCount = 0;

			// Fill up moves
			if (isRoot) moveCount = GenerateAllMoves(myBitboards, opponentBitboards, moves, isWhite);
			else moveCount = GenerateAllMoves(myBitboards, opponentBitboards, moves, isWhite);
			// Todo generate only actually legal moves and add logic for No Moves (Checkmate) + In Check and No Moves + Not in check (stalemate)

			Span<(byte, ulong)> XORBitboardOperations = stackalloc (byte, ulong)[9]; // Max operations are 9, 1 for castleoptions removal, 1 for myFULL, 2 for myPIECE, 1 for EnPassant, 1 for my castle, and 2 for Opponent Full and Piece, and 1 for oppoent castle, hypothetically

			for (byte i = 0; i < moveCount; i++)
			{
				// Save operations in (byte bitboardIndex, ulong xor_operation)
				byte OperationCount = 0, myOperationCount = 0;
				
				double score = GenerateXORoperations(XORBitboardOperations, &myOperationCount, &OperationCount, myBitboards, opponentBitboards, moves[i], isWhite, -FinalDepth + depthRemaining);

				if (score == 0)
				{
					// Not set. Other possibility is KingLoss where we skip additional eval

					MakeMove(ref myBitboards, ref opponentBitboards, XORBitboardOperations, myOperationCount,
						OperationCount);

					score = -Failsoft_AlphaBeta(-beta, -alpha, ref myBitboards, ref opponentBitboards,
						depthRemaining - 1, !isWhite);

					// Unmake move, since this is just execution of XOR operations, we can execute it again
					MakeMove(ref myBitboards, ref opponentBitboards, XORBitboardOperations, myOperationCount,
						OperationCount);
				}

				// Make cutoffs / value updating
				if (score >= beta)
				{
					if (isRoot)
					{
						packed_Bestmove = moves[i];
					}
					// Todo if (!double.IsInfinity(score) && score < PositiveKingCaptureEvalValue && score > NegativeKingCaptureEvalValue) CC_ForcedMate = -1;  // Remove forced mate
					// Beta Cutoff. This is a killer.
					// Todo AddKiller(move, ref position, posKey);
					return score;
				}
				if (score > HighestScore)
				{
					HighestScore = score;
					if (score > alpha)
					{
						alpha = score;
						if (isRoot)
						{
							packed_Bestmove = moves[i];
						}
					}
				}
			}

			return HighestScore;
		}

		

		internal unsafe double Failsoft_AlphaBeta_Quiesce(double alpha, double beta, ref ulong[] myBitboards, ref ulong[] opponentBitboards, bool isWhite, int QuiesceDepth)
		{
			if (NotEnoughCheckmatingMaterial(myBitboards, opponentBitboards)) return 0;
			double HighestScore = double.NegativeInfinity;

			// Somehow we need to save from and to, so we need to save
			Span<ushort> moves = stackalloc ushort[218];    // Does this make sense? This is 218 * 8 bytes + overhead for each node (But when one finishes the memory is released, so there is always just 1 path, meaning at most 218*8*depth, with probably >= 1MB stack size available
			
			// Fill up moves
			ulong combinedBoardstate = myBitboards[INDEX_FULL_BITBOARD] | opponentBitboards[INDEX_FULL_BITBOARD];
			int moveCount = GenerateAllMoves_CapturesOnly(myBitboards, opponentBitboards, combinedBoardstate, moves, isWhite);
			
			if(moveCount == 0 || true) return Evaluate(myBitboards, opponentBitboards, combinedBoardstate, isWhite);

			Span<(byte, ulong)> XORBitboardOperations = stackalloc (byte, ulong)[9]; // Max operations are 9, 1 for castleoptions removal, 1 for myFULL, 2 for myPIECE, 1 for EnPassant, 1 for my castle, and 2 for Opponent Full and Piece, and 1 for oppoent castle, hypothetically

			for (byte i = 0; i < moveCount; i++)
			{
				// Save operations in (byte bitboardIndex, ulong xor_operation)
				byte OperationCount = 0, myOperationCount = 0;
				
				double score = GenerateXORoperations(XORBitboardOperations, &myOperationCount, &OperationCount, myBitboards, opponentBitboards, moves[i], isWhite, -FinalDepth - QuiesceDepth); // Depth for Mx is usually FinalDepth + depthRemaining

				if (score == 0)
				{
					// Not set. Other possibility is KingLoss where we skip additional eval

					MakeMove(ref myBitboards, ref opponentBitboards, XORBitboardOperations, myOperationCount,
						OperationCount);

					score = -Failsoft_AlphaBeta_Quiesce(-beta, -alpha, ref myBitboards, ref opponentBitboards, !isWhite, QuiesceDepth+1);

					// Unmake move, since this is just execution of XOR operations, we can execute it again
					MakeMove(ref myBitboards, ref opponentBitboards, XORBitboardOperations, myOperationCount,
						OperationCount);
				}

				// Make cutoffs / value updating
				if (score >= beta)
				{
					// Todo if (!double.IsInfinity(score) && score < PositiveKingCaptureEvalValue && score > NegativeKingCaptureEvalValue) CC_ForcedMate = -1;  // Remove forced mate
					// Beta Cutoff. This is a killer.
					// Todo AddKiller(move, ref position, posKey);
					return score;
				}
				if (score > HighestScore)
				{
					HighestScore = score;
					if (score > alpha)
					{
						alpha = score;
					}
				}
			}

			return HighestScore;
		}

		

		#region Helpers

		#region DHL Helpers

		// Pack and Unpack
		static ushort Pack(byte FromSquare, int ToSquare, byte data)
		{
			return (ushort)((FromSquare << 10) | (ToSquare << 4) | (data & 0xF));
		}
		static (ulong, ulong, byte) Unpack(ushort packedMove)
		{
			return (1UL << GetMoveSquareFrom(packedMove), 1UL << GetMoveSquareTo(packedMove), GetMoveData(packedMove));
		}

		#endregion

		static unsafe int GenerateXORoperations(Span<(byte, ulong)> XORBitboardOperations, byte* myOperationCount, byte* OperationCount,
			ulong[] myBitboards, ulong[] opponentBitboards, ushort move, bool isWhite, int DepthCaptureValue)
		{

			// Define Variables for Data processing
			var unpacked = Unpack(move);
			ulong unpacked_combined = unpacked.Item1 | unpacked.Item2;
			byte data = unpacked.Item3;

			byte fromSquare = GetMoveSquareFrom(move);
			byte toSquare = GetMoveSquareTo(move);



			// KING CAPTURED!!! IMMEDIATE LOSS
			if ((unpacked.Item2 & opponentBitboards[INDEX_KING_BITBOARD]) != 0)
			{

				return ALGORITHM_CONSTANT_KING_CAPTUREVALUE + DepthCaptureValue;

				// Hypothetical XOR ops:
				//XORBitboardOperations[OperationCount++] = (INDEX_KING_BITBOARD, unpacked.Item2);
				//XORBitboardOperations[OperationCount++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}

			// Not a King capture, continue as normal

			#region Read Data and create all Operations

			XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked_combined);

			if (DataReferencesBitboard(data))
			{
				// Remove castle options if King/Rook
				if ((unpacked.Item1 & myBitboards[INDEX_KING_BITBOARD]) != 0)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, myBitboards[INDEX_CASTLE_BITBOARD]);
				}
				else if ((unpacked.Item1 & myBitboards[INDEX_ROOK_BITBOARD]) != 0)
				{
					// If could castle that way before, cannot castle anymore now
					if (fromSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_WHITE && (CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE & myBitboards[INDEX_CASTLE_BITBOARD]) != 0)
						XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE);

					else if (fromSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_WHITE && (CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE & myBitboards[INDEX_CASTLE_BITBOARD]) != 0)
						XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE);

					else if (fromSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_BLACK && (CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK & myBitboards[INDEX_CASTLE_BITBOARD]) != 0)
						XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK);

					else if (fromSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_BLACK && (CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK & myBitboards[INDEX_CASTLE_BITBOARD]) != 0)
						XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK);
				}
				XORBitboardOperations[(*OperationCount)++] = (data, unpacked_combined);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_EN_PASSANT_BITBOARD, myBitboards[INDEX_EN_PASSANT_BITBOARD]);
				(*myOperationCount) = *OperationCount;
			}
			// Put this here because this does not contain a clear en passant call
			else if (data == MOVEDATA_PAWN_JUMPSTART)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked_combined);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_EN_PASSANT_BITBOARD, GetMedianBitboard(fromSquare, toSquare) | myBitboards[INDEX_EN_PASSANT_BITBOARD]);
				(*myOperationCount) = *OperationCount;
			}
			else
			{
				// Clear En Passants before putting the ball in the opponent's field, so no need to worry about opponent
				XORBitboardOperations[(*OperationCount)++] = (INDEX_EN_PASSANT_BITBOARD, myBitboards[INDEX_EN_PASSANT_BITBOARD]);

				if (data == MOVEDATA_CASTLE_SHORT)
				{
					int index = isWhite ? 0 : 2;
					XORBitboardOperations[(*OperationCount)++] = (INDEX_KING_BITBOARD, CASTLE_XOR_MASKS_KING[index]);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_ROOK_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
					// King move is added to full bitboard already, now apply the rookmove as well
					XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, myBitboards[INDEX_CASTLE_BITBOARD]);  // With whatever we have to set to 0 and restore to OG value
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_CASTLE_LONG)
				{
					int index = isWhite ? 1 : 3;
					XORBitboardOperations[(*OperationCount)++] = (INDEX_KING_BITBOARD, CASTLE_XOR_MASKS_KING[index]);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_ROOK_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
					// King move is added to full bitboard already, now apply the rookmove as well
					XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, myBitboards[INDEX_CASTLE_BITBOARD]);  // With whatever we have to set to 0 and restore to OG value
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_EN_PASSANT_CAPTURE)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked_combined);
					(*myOperationCount) = *OperationCount;
					// En Passant Pawn from Opponent
					byte pawnSquare = CombineSquareData(toSquare, fromSquare);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, 1UL << pawnSquare);
				}
				else if (data == MOVEDATA_PROMOTION_N)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
					XORBitboardOperations[(*OperationCount)++] = (INDEX_KNIGHT_BITBOARD, unpacked.Item2); // Add to promoted piece bitboard instead
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_PROMOTION_B)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
					XORBitboardOperations[(*OperationCount)++] = (INDEX_BISHOP_BITBOARD, unpacked.Item2); // Add to promoted piece bitboard instead
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_PROMOTION_R)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
					XORBitboardOperations[(*OperationCount)++] = (INDEX_ROOK_BITBOARD, unpacked.Item2);   // Add to promoted piece bitboard instead
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_PROMOTION_Q)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
					XORBitboardOperations[(*OperationCount)++] = (INDEX_QUEEN_BITBOARD, unpacked.Item2);  // Add to promoted piece bitboard instead
					(*myOperationCount) = *OperationCount;
				}
			}

			// If RookCapture, adjust CastleOptions if necessary
			if ((unpacked.Item2 & opponentBitboards[INDEX_ROOK_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_ROOK_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);

				ulong bitmask = 7;  // IMPOSSIBLE VALUE (3 incorrect squares marked)
				if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_WHITE) bitmask = CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE;
				else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_WHITE) bitmask = CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE;
				else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_BLACK) bitmask = CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK;
				else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_BLACK) bitmask = CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK;

				if (bitmask != 7) XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD,
					 opponentBitboards[INDEX_CASTLE_BITBOARD] & bitmask);   // Make XORable operation so it only has effect if it's original value is 1
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_PAWN_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_KNIGHT_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_KNIGHT_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_BISHOP_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_BISHOP_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_QUEEN_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_QUEEN_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			// King not necessary here since we check at the start

			#endregion

			return 0;	// score
		}

		static void MakeMove(ref ulong[] myBitboards, ref ulong[] opponentBitboards, Span<(byte, ulong)> Operations, byte myOperationCount, byte totalOperationCount)
		{
			byte c;
			// Use this to update en passant takes and captures that impede castle
			for (c = 0; c < myOperationCount; ++c)
			{
				myBitboards[Operations[c].Item1] ^= Operations[c].Item2;
			}

			for (; c < totalOperationCount; ++c)
			{
				opponentBitboards[Operations[c].Item1] ^= Operations[c].Item2;
			}
		}

		static ulong GetMedianBitboard(ushort move) =>
			GetMedianBitboard(GetMoveSquareFrom(move), GetMoveSquareTo(move));
		static ulong GetMedianBitboard(byte squareFrom, byte squareTo)
		{
			return 1UL << ((squareFrom + squareTo) >> 1);
		}

		// Combines a square's rank/file, used for finding the pawn for en passant by taking file from toSquare and rank from fromSquare
		static byte CombineSquareData(byte squareFileData, byte squareRankData)
		{
			return (byte) ((squareFileData & 0b111) | (squareRankData & 0b111000));
		}

		static byte GetMoveSquareFrom(ushort move) => (byte)(move >> 10);
		static byte GetMoveSquareTo(ushort move) => (byte)((move >> 4) & 0x3F);
		static byte GetMoveData(ushort move) => (byte)(move & 0xF);
		static bool DataReferencesBitboard(byte data) => (data & 0b1000) == 0;

		#endregion
	}
}
