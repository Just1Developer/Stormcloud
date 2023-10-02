using System;
using System.Numerics;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	partial class Stormcloud4
	{
		private ushort packed_Bestmove = 0;	// 0 means 0 movedata means no move

		internal unsafe double Failsoft_AlphaBeta(double alpha, double beta, ref ulong[] myBitboards, ref ulong[] opponentBitboards,
	int depthRemaining, bool isWhite, bool isRoot = false)
		{
			// Todo Quiesce
			if (depthRemaining == 0) return Evaluate(myBitboards, opponentBitboards, isWhite);

			double HighestScore = double.NegativeInfinity;

			// Somehow we need to save from and to, so we need to save
			Span<ushort> moves = stackalloc ushort[218];    // Does this make sense? This is 218 * 8 bytes + overhead for each node (But when one finishes the memory is released, so there is always just 1 path, meaning at most 218*8*depth, with probably >= 1MB stack size available
			int moveCount = 0;

			// Fill up moves
			moveCount = GenerateAllMoves(myBitboards, opponentBitboards, moves, isWhite);
			Span<(byte, ulong)> XORBitboardOperations = stackalloc (byte, ulong)[8]; // Max operations are 8, 1 for myFULL, 2 for myPIECE, 1 for EnPassant, 1 for my castle, and 2 for Opponent Full and Piece, and 1 for oppoent castle, hypothetically

			// Todo remove en passant

			for (byte i = 0; i < moveCount; i++)
			{
				// Process some data
				var unpacked = Unpack(moves[i]);
				//ulong NOT_MASK = ~unpacked.Item2;
				ulong unpacked_combined = unpacked.Item1 | unpacked.Item2;
				byte data = unpacked.Item3;

				byte fromSquare = GetMoveSquareFrom(moves[i]);
				byte toSquare = GetMoveSquareTo(moves[i]);

				// Save operations in (byte bitboardIndex, ulong xor_operation)
				byte OperationCount = 0, myOperationCount = 0;
				byte affectedBoardsCount = 0;

				#region Read Data and create all Operations

				XORBitboardOperations[OperationCount++] = (INDEX_FULL_BITBOARD, unpacked_combined);

				if (DataReferencesBitboard(data))
				{
					XORBitboardOperations[OperationCount++] = (data, unpacked_combined);
					XORBitboardOperations[OperationCount++] = (INDEX_EN_PASSANT_BITBOARD, myBitboards[INDEX_EN_PASSANT_BITBOARD]);
					myOperationCount = OperationCount;
				}
				// Put this here because this does not contain a clear en passant call
				else if (data == MOVEDATA_PAWN_JUMPSTART)
				{
					XORBitboardOperations[OperationCount++] = (INDEX_PAWN_BITBOARD, unpacked_combined);
					XORBitboardOperations[OperationCount++] = (INDEX_EN_PASSANT_BITBOARD, GetMedianBitboard(fromSquare, toSquare));
					myOperationCount = OperationCount;
				}
				else
				{
					// Clear En Passants before putting the ball in the opponent's field, so no need to worry about opponent
					XORBitboardOperations[OperationCount++] = (INDEX_EN_PASSANT_BITBOARD, myBitboards[INDEX_EN_PASSANT_BITBOARD]);

					if (data == MOVEDATA_CASTLE_SHORT)
					{
						int index = isWhite ? 0 : 2;
						XORBitboardOperations[OperationCount++] = (INDEX_KING_BITBOARD, CASTLE_XOR_MASKS_KING[index]);
						XORBitboardOperations[OperationCount++] = (INDEX_ROOK_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
						XORBitboardOperations[OperationCount++] = (INDEX_CASTLE_BITBOARD, myBitboards[INDEX_CASTLE_BITBOARD]);	// With whatever we have to set to 0 and restore to OG value
						myOperationCount = OperationCount;
					}
					else if (data == MOVEDATA_CASTLE_LONG)
					{
						int index = isWhite ? 1 : 3;
						XORBitboardOperations[OperationCount++] = (INDEX_KING_BITBOARD, CASTLE_XOR_MASKS_KING[index]);
						XORBitboardOperations[OperationCount++] = (INDEX_ROOK_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
						XORBitboardOperations[OperationCount++] = (INDEX_CASTLE_BITBOARD, myBitboards[INDEX_CASTLE_BITBOARD]);  // With whatever we have to set to 0 and restore to OG value
						myOperationCount = OperationCount;
					}
					else if (data == MOVEDATA_EN_PASSANT_CAPTURE)
					{
						XORBitboardOperations[OperationCount++] = (INDEX_PAWN_BITBOARD, unpacked_combined);
						myOperationCount = OperationCount;
						// En Passant Pawn from Opponent
						byte pawnSquare = CombineSquareData(toSquare, fromSquare);
						XORBitboardOperations[OperationCount++] = (INDEX_PAWN_BITBOARD, 1UL << pawnSquare);
					}
					else if (data == MOVEDATA_PROMOTION_N)
					{
						XORBitboardOperations[OperationCount++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
						XORBitboardOperations[OperationCount++] = (INDEX_KNIGHT_BITBOARD, unpacked.Item2); // Add to promoted piece bitboard instead
						myOperationCount = OperationCount;
					}
					else if (data == MOVEDATA_PROMOTION_B)
					{
						XORBitboardOperations[OperationCount++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
						XORBitboardOperations[OperationCount++] = (INDEX_BISHOP_BITBOARD, unpacked.Item2); // Add to promoted piece bitboard instead
						myOperationCount = OperationCount;
					}
					else if (data == MOVEDATA_PROMOTION_R)
					{
						XORBitboardOperations[OperationCount++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
						XORBitboardOperations[OperationCount++] = (INDEX_ROOK_BITBOARD, unpacked.Item2);   // Add to promoted piece bitboard instead
						myOperationCount = OperationCount;
					}
					else if (data == MOVEDATA_PROMOTION_Q)
					{
						XORBitboardOperations[OperationCount++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
						XORBitboardOperations[OperationCount++] = (INDEX_QUEEN_BITBOARD, unpacked.Item2);  // Add to promoted piece bitboard instead
						myOperationCount = OperationCount;
					}
				}

				// If RookCapture, adjust CastleOptions if necessary
				if ((unpacked.Item2 & opponentBitboards[INDEX_ROOK_BITBOARD]) != 0)
				{
					XORBitboardOperations[OperationCount++] = (INDEX_ROOK_BITBOARD, unpacked.Item2);
					XORBitboardOperations[OperationCount++] = (INDEX_FULL_BITBOARD, unpacked.Item2);

					ulong bitmask = 7;	// IMPOSSIBLE VALUE (3 incorrect squares marked)
					if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_WHITE) bitmask = CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE;
					else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_WHITE) bitmask = CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE;
					else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_BLACK) bitmask = CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK;
					else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_BLACK) bitmask = CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK;

					if(bitmask != 7) XORBitboardOperations[OperationCount++] = (INDEX_CASTLE_BITBOARD,
						opponentBitboards[INDEX_CASTLE_BITBOARD] & bitmask);	// Make XORable operation so it only has effect if it's original value is 1
				}
				else if ((unpacked.Item2 & opponentBitboards[INDEX_PAWN_BITBOARD]) != 0)
				{
					XORBitboardOperations[OperationCount++] = (INDEX_PAWN_BITBOARD, unpacked.Item2);
					XORBitboardOperations[OperationCount++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
				}
				else if ((unpacked.Item2 & opponentBitboards[INDEX_KNIGHT_BITBOARD]) != 0)
				{
					XORBitboardOperations[OperationCount++] = (INDEX_KNIGHT_BITBOARD, unpacked.Item2);
					XORBitboardOperations[OperationCount++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
				}
				else if ((unpacked.Item2 & opponentBitboards[INDEX_BISHOP_BITBOARD]) != 0)
				{
					XORBitboardOperations[OperationCount++] = (INDEX_BISHOP_BITBOARD, unpacked.Item2);
					XORBitboardOperations[OperationCount++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
				}
				else if ((unpacked.Item2 & opponentBitboards[INDEX_QUEEN_BITBOARD]) != 0)
				{
					XORBitboardOperations[OperationCount++] = (INDEX_QUEEN_BITBOARD, unpacked.Item2);
					XORBitboardOperations[OperationCount++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
				}
				else if ((unpacked.Item2 & opponentBitboards[INDEX_KING_BITBOARD]) != 0)
				{

					// TODO KING CAPTURED!!! IMMEDIATE LOSS

					XORBitboardOperations[OperationCount++] = (INDEX_KING_BITBOARD, unpacked.Item2);
					XORBitboardOperations[OperationCount++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
				}

				#endregion

				MakeMove(ref myBitboards, ref opponentBitboards, XORBitboardOperations, &myOperationCount, OperationCount);

				double score = -Failsoft_AlphaBeta(-beta, -alpha, ref myBitboards, ref opponentBitboards,
					depthRemaining - 1, !isWhite);

				// Unmake move, since this is just execution of XOR operations, we can execute it again
				MakeMove(ref myBitboards, ref opponentBitboards, XORBitboardOperations, &myOperationCount, OperationCount);

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

		// Pack and Unpack
		static ushort Pack(byte FromSquare, int ToSquare, byte data)
		{
			return (ushort)((FromSquare << 12) | (ToSquare << 4) | (data & 0xF));
		}
		static (ulong, ulong, byte) Unpack(ushort packedMove)
		{
			return (1UL << GetMoveSquareFrom(packedMove), 1UL << GetMoveSquareTo(packedMove), GetMoveData(packedMove));
		}

		static unsafe void MakeMove(ref ulong[] myBitboards, ref ulong[] opponentBitboards, Span<(byte, ulong)> Operations, byte* myOperationCount, byte totalOperationCount)
		{
			byte c;
			// Use this to update en passant takes and captures that impede castle
			for (c = 0; c < *myOperationCount; ++c)
			{
				myBitboards[Operations[c].Item1] ^= Operations[c].Item2;
			}

			for (; c < totalOperationCount; ++c)
			{
				// Todo perhaps updates before
				opponentBitboards[Operations[c].Item1] ^= Operations[c].Item2;
			}

		}

		static void GetMedianBitboard(ulong bitboard1, ulong bitboard2, out ulong result)
		{
			int index1 = BitOperations.TrailingZeroCount(bitboard1);
			int index2 = BitOperations.TrailingZeroCount(bitboard2);
			int medianIndex = (index1 + index2) >> 1;   // /2

			result = 1UL << medianIndex;
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

		static byte GetMoveSquareFrom(ushort move) => (byte)(move >> 12);
		static byte GetMoveSquareTo(ushort move) => (byte)((move >> 4) & 0x3F);
		static byte GetMoveData(ushort move) => (byte)(move & 0xF);
		static bool DataReferencesBitboard(byte data) => (data & 0b1000) == 0;
	}
}









/*

byte affectedOwnBitboardIndex;
// Consists of XOR Operation as ulong and byte as index of the affected bitboard
Span<(byte, ulong)> additionalAffectedBitboardsAndOperations = stackalloc (byte, ulong)[2]; // Max affected score


if (data == MOVEDATA_CASTLE_SHORT)
{
	affectedOwnBitboardIndex = INDEX_KING_BITBOARD;
	additionalAffectedBitboardsAndOperations[affectedBoardsCount++] = (INDEX_ROOK_BITBOARD,
		CASTLE_XOR_MASKS_ROOK[isWhite ? 0 : 2]);
}
else if (data == MOVEDATA_CASTLE_LONG)
{
	affectedOwnBitboardIndex = INDEX_KING_BITBOARD;
	additionalAffectedBitboardsAndOperations[affectedBoardsCount++] = (INDEX_ROOK_BITBOARD,
		CASTLE_XOR_MASKS_ROOK[isWhite ? 1 : 3]);
}
else if (data == MOVEDATA_EN_PASSANT_CAPTURE)
{
	affectedOwnBitboardIndex = INDEX_PAWN_BITBOARD;
	additionalAffectedBitboardsAndOperations[affectedBoardsCount++] = (INDEX_EN_PASSANT_BITBOARD,
		myBitboards[INDEX_EN_PASSANT_BITBOARD]);	// XOR with itself to remove/add pawn capturability
}
else if (data == MOVEDATA_PAWN_JUMPSTART)
{
	affectedOwnBitboardIndex = INDEX_PAWN_BITBOARD;
	additionalAffectedBitboardsAndOperations[affectedBoardsCount++] = (INDEX_EN_PASSANT_BITBOARD,
		GetMedianBitboard(moves[i]));	// XOR with itself to remove/add pawn capturability
}
else if (data == MOVEDATA_PROMOTION_N)
{
	affectedOwnBitboardIndex = INDEX_PAWN_BITBOARD;
	additionalAffectedBitboardsAndOperations[affectedBoardsCount++] = (INDEX_EN_PASSANT_BITBOARD,
		GetMedianBitboard(moves[i]));	// XOR with itself to remove/add pawn capturability
}
*/