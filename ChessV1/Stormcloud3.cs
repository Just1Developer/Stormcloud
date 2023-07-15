using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud
{

	// Todo rename file to Stormcloud3

	// Represent Moves as short or int instead of byte arrays?
	// Yes: Short: 16 bit -> 6 bit: from | 6 bit: to | 4 bit resulting piece.
	// Example: Short: 001000 010000 1001 => a7 -> a6, black pawn: move title: a6

	// Castle: Maybe first half of the byte means temporary no? (attacked)
	// Todo add castleOptions to position Key

	/*
	 * If we store at the back of the short
	 * From: byte GetFrom(byte move) => (byte) (move >> 10);
	 * To: byte GetTo(byte move) => (byte) ((move >> 4) & 0x3F);	// Mask for last 6 bit
	 * Piece: byte GetPiece(byte move) => (byte) (move & 0x0F);		// Mask for last 4 bit (obv)
	 * 
	 * Construction like this: (GPT-4 but i knew too):
	 * short GetMoveOf(byte fromIndex, byte toIndex, byte piece) => (short) ((fromIndex << 10) | (toIndex << 4) | (pieceType >> 4));	// Piece is usually stored in first half of the byte
	 */

	/*
	 * We definitely need a center heatmap / center matrix (8x8)
	 * 
	 * Example: Pawn Structure Heatmap:
	 * 
	 * (  1/4, -7/24, 1/4 )
	 * (  1/6, 1/4, 1/6 )
	 * (  1/4, -7/24, 1/4 )
	 * 
	 * Field Coverage Matrices for every piece type
	 */

	// Enqueue searchnodes in queue for recycling

	// Todo add ResultingCastle() into search algorithm to update temp castle variables (z. B. a bishop staring at a castle square)
	// Also add Constants for the Queen/Kingside castle stuff
	// Todo next: Faster algorithm

	/// <summary>
	/// Engine Calculation using the TQA Approach from Chessboard2.cs and Alpha-Beta Pruning
	/// </summary>
	internal partial class Stormcloud3	// Evaluation
	{
		#region Evaluation Weights



		#endregion

		#region Debug_DeleteMe_Unsafe

		public static Stormcloud3 search;

		public Stormcloud3()
		{
			search = this;
			/*
			byte[] position = {
				0xCA, 0xBD, 0xEB, 0xAC,
				0x99, 0x99, 0x99, 0x99,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x11, 0x11, 0x11, 0x11,
				0x42, 0x35, 0x63, 0x24
			};
			*/
			
			// Generated Testposition (by me on the board, whites queen is hanging, blacks queen too but protected and only attacked by queen)
			//byte[] position = new byte[] { 0xC0, 0x00, 0xE0, 0x0C, 0x99, 0x90, 0x09, 0x99, 0x00, 0xAB, 0xBA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD1, 0x91, 0x00, 0x00, 0x25, 0x30, 0x10, 0x11, 0x00, 0x00, 0x01, 0x40, 0x00, 0x60, 0x24, };
			
			// Scholar's mate execution Qxe8# is the best move
			//byte[] position = new byte[] { 0xCA, 0xBD, 0xEB, 0xAC, 0x99, 0x99, 0x00, 0x90, 0x00, 0x00, 0x09, 0x09, 0x00, 0x00, 0x90, 0x05, 0x00, 0x30, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x11, 0x01, 0x11, 0x42, 0x30, 0x60, 0x24 };

			byte[] position = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x90, 0x00, 0x00, 0x00, 0xE9, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x60, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0x0C };

			/*
			 * Depth: 8 | Time: 55,7723242s | Score: -2 | Move: Queen on d3 takes Queen on c4   ||   d3 -> c4   ||   Queen on d3 takes Queen on c4
			 * Stockfish: -4.3 | Best Move: Qxc4 => Same move
			 */


			double Test_Eval = PositionEvaluation(position, EvaluationResultWhiteTurn).Score;
			var moves = GetAllLegalMoves(position, true);
			System.Diagnostics.Debug.WriteLine("D >> Test Eval mat: " + Test_Eval + " | Moves: " + moves.Count);
			
			int i = 0;
			foreach (var move in moves)
			{
				System.Diagnostics.Debug.WriteLine($"Legal Move {++i} >> {MoveToStringPro1(position, move)}");
			}
			

			Debug_StartEvaluationTestSingleThread(position, true);
		}

		public Stormcloud3(bool ignored)
		{
			/*
			byte[] position = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x90, 0x00, 0x00, 0x00, 0xE9, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x60, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0x0C };
			position = new byte[] {
				0xCA, 0xBD, 0xEB, 0xAC,
				0x90, 0x09, 0x99, 0x99,
				0x00, 0x00, 0x00, 0x00,
				0x09, 0x90, 0x00, 0x00,
				0x01, 0x00, 0x00, 0x00,
				0x30, 0x00, 0x00, 0x00,
				0x10, 0x11, 0x11, 0x11,
				0x42, 0x05, 0x63, 0x24
			};

			var moves = GetAllLegalMoves(position, true);

			int i = 0;
			foreach (var move in moves)
			{
				if(move == 0x2005 || true)
				{
					System.Diagnostics.Debug.WriteLine($"Legal Move {++i} >> {MoveToStringPro1(position, move)} | Binary: {Convert.ToString(move, 2)}");
					System.Diagnostics.Debug.WriteLine($"Resulting Key: {i} >> {ResultingPosition(position, move, 0x00, null).Item2}");
				}
			}
			*/

			byte[] position = new byte[] {
				0xCA, 0xBD, 0xEB, 0xAC,
				0x90, 0x99, 0x99, 0x99,
				0x00, 0x00, 0x00, 0x00,
				0x09, 0x00, 0x00, 0x00,
				0x01, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x10, 0x11, 0x11, 0x11,
				0x42, 0x35, 0x63, 0x24
			};
			string key = GeneratePositionKey(position);
			//short m = (short) 0xEA80;
			//string move = MoveToStringPro1(position, m);

			//var result = ...

			System.Diagnostics.Debug.WriteLine($"Previous Key: {key} | Move: ");
		}

		public Stormcloud3(int GameDepth)	// 2nd constructor
		{
			// Play autonomous game
			//*
			byte[] position = {
				0xCA, 0xBD, 0xEB, 0xAC,
				0x99, 0x99, 0x99, 0x99,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x11, 0x11, 0x11, 0x11,
				0x42, 0x35, 0x63, 0x24
			};
			//*/

			//byte[] position = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x90, 0x00, 0x00, 0x00, 0xE9, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x60, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0x0C };

			bool TurnColorWhite = true;
			string key = GeneratePositionKey(position);
			byte castle = 0xFF;

			string move = null;
			List<string> moves = new List<string>();

			System.Diagnostics.Debug.WriteLine("Starting Game (" + DateTime.Now + ")...");
			while (true)
			{
				//Debug_StartEvaluationTestSingleThread(position, TurnColorWhite, 4);
				var score = CC_FailsoftAlphaBeta(position, TurnColorWhite, castle, key, GameDepth);
				byte to = position[MoveToIndex(CC_Failsoft_BestMove) >> 1];
				string moveString = MoveToStringPro1(position, CC_Failsoft_BestMove);

				System.Diagnostics.Debug.WriteLine($"Depth: {GameDepth} | Score: {score} | Move: {moveString}   ||   {MoveToString1(CC_Failsoft_BestMove)}   ||   {MoveToStringCas(position, CC_Failsoft_BestMove)}");

				var result = ResultingPosition(position, CC_Failsoft_BestMove, castle, key);
				System.Diagnostics.Debug.WriteLine($">>>>>   OldKey: {key} | Move: {moveString} | Move: {Convert.ToString(CC_Failsoft_BestMove, 2)} | NewKey: {result.Item2}");
				/*
				if(moves.Count >= 9)
				{
					System.Diagnostics.Debug.WriteLine($"Debug >> {moves.Count} | Move: {moveString} | Key: {key} | NewKey: {result.Item2}");
				}
				*/
				position = result.Item1;
				key = result.Item2;
				castle = result.Item3;

				if(TurnColorWhite)
				{
					if (move != null)
					{
						moves.Add(move);
						System.Diagnostics.Debug.WriteLine($"{moves.Count}. {move} | Key: {key}");
						if (moves.Count > 6)
						{
							/*	Idk if this is correct
							if (moves[moves.Count - 6] == moves[moves.Count - 3] &&
								moves[moves.Count - 5] == moves[moves.Count - 2] &&
								moves[moves.Count - 4] == move)
							{
								System.Diagnostics.Debug.WriteLine("Draw by Threefold repetition.");
								break;
							}
							else */if (moves[moves.Count - 6] == moves[moves.Count - 4] && moves[moves.Count - 4] == moves[moves.Count - 2] &&
								moves[moves.Count - 5] == moves[moves.Count - 3] && moves[moves.Count - 3] == move)	// move = Count - 1
							{
								System.Diagnostics.Debug.WriteLine("Draw by repetition.");
								break;
							}
						}
					}
					move = moveString;
				}
				else
				{
					move += " " + moveString;
				}
				TurnColorWhite = !TurnColorWhite;

				if ((to & 0x07) == 6 || (to & 0x70) == 6)
				{
					string KingColor = "-";

					if((to & 1) == 1)
					{
						// 2nd half
						if ((to & 0x0F) == 0x0E) KingColor = "Black";
						else if ((to & 0x0F) == 0x06) KingColor = "White";
					}
					else
					{
						// 2nd half
						if ((to & 0xF0) == 0xE0) KingColor = "Black";
						else if ((to & 0xF0) == 0x60) KingColor = "White";
					}

					if(KingColor != "-")
					{
						System.Diagnostics.Debug.WriteLine($"Win: The {KingColor} King has been captured.");
						break;
					}
				}
				if(!EnoughCheckmatingMaterial(position))
				{
					System.Diagnostics.Debug.WriteLine("Draw by Insufficient Checkmating Material.");
					break;
				}
			}
			System.Diagnostics.Debug.WriteLine("The Game:");
			for (int i = 0; i < moves.Count; i++)
			{
				System.Diagnostics.Debug.WriteLine($"{i+1}. {moves[i]}");
			}
		}

		// Todo castle, checks, mate
		// Out of memory at depth 10 (or 11 idk doesn't show) at around 3.5 GB ram usage
		void OldSearch_StartEvaluationTestSingleThread(byte[] startPosition, bool isWhitesTurn)
		{
			OldSearch_SearchNode startNode = new OldSearch_SearchNode(startPosition, new OldSearch_PositionData());
			OldSearch_SearchNodes.Enqueue(startNode);
			OldSearch_ProcessNextNode(true);
			OldSearch_StartProcessingNodesSingleThread();
			//StartProcessingMultiThread();
		}

		//System.Threading.Thread Debug_SearchThread = null;
		//int Debug_Depth = 2;

		// Though its a bugged position, pawn captures seem to ignore color (9 -> C), (black pawn -> black rook)

		public int[] Debug_StartEvaluationTestSingleThread(byte[] startPosition, bool isWhitesTurn, int Final_Depth = 8)
		{
			//if(Debug_SearchThread != null) Debug_SearchThread.Abort();
			IsRootTurnColorWhite = isWhitesTurn;
			string posKey = GeneratePositionKey(startPosition);
			//System.Diagnostics.Debug.WriteLine("posKey: " + posKey);
			DateTime start;
			//Debug_SearchThread = new System.Threading.Thread(() =>
			//{
			System.Diagnostics.Debug.WriteLine("Starting Stormcloud Calc...");
			int Debug_Depth = 2;
			while (Debug_Depth <= Final_Depth)	// Og: 8 but ingame i cant wait for that
			{
				start = DateTime.Now;
				double score = CC_FailsoftAlphaBeta(startPosition, isWhitesTurn, 0x00, posKey, Debug_Depth);
				string moveString = MoveToStringPro1(startPosition, CC_Failsoft_BestMove);
				Form1.bestMove = moveString;
				Form1.bestMoveScore = score;
				Form1.bestMoveDepth = Debug_Depth;
				System.Diagnostics.Debug.WriteLine($"Depth: {Debug_Depth++} | Time: {(DateTime.Now - start).TotalSeconds}s | Score: {score} | Move: {moveString}   ||   {MoveToString1(CC_Failsoft_BestMove)}   ||   {MoveToStringCas(startPosition, CC_Failsoft_BestMove)}");
			}
			return new int[] { MoveFromIndex(CC_Failsoft_BestMove), MoveToIndex(CC_Failsoft_BestMove) };
			//});
			//Debug_SearchThread.Start();
		}

		#endregion

		struct EvaluationResult
		{
			public double Score;
			public byte Result;
			public EvaluationResult(double score, byte evalResult)
			{
				this.Score = score;
				this.Result = evalResult;
			}
		}

		// Maybe Evaluate all legal moves too (or beforehand and enter them into the method as arg) to take them into account

		EvaluationResult PositionEvaluation(OldSearch_SearchNode Node) => PositionEvaluation(Node.Position, Node.PositionData.Turn);
		EvaluationResult PositionEvaluation(byte[] Position, OldSearch_PositionData PositionData) => PositionEvaluation(Position, PositionData.Turn);
		EvaluationResult PositionEvaluation(byte[] Position, bool IsTurnColorWhite) => PositionEvaluation(Position, IsTurnColorWhite ? EvaluationResultWhiteTurn : EvaluationResultBlackTurn);
		EvaluationResult PositionEvaluation(byte[] Position, byte Turn)
		{
			double score = 0;

			byte result = (byte)~Turn;	// Invert previous turn byte as default result

			bool GameOver = (result & EvalResultGameOverMask) == EvalResultGameOverMask;	// 0110 or 1001 is for turns, so we need to actually check for 1100
			bool Draw = GameOver && ((result & EvalResultDrawMask) == EvaluationResultDraw);
			if (Draw) result = EvaluationResultDraw;
			else if(GameOver) result = (result & EvalResultWhiteMask) != 0 ? EvaluationResultWhiteWon : EvaluationResultBlackWon;

			// ...

			// This is bad because here white is 1 but otherwise white is 0
			double materialAdvantage = (Turn & 0x0F) != 0 ? MaterialEvaluation(Position) : -MaterialEvaluation(Position);

			score += materialAdvantage;

			return new EvaluationResult(score, result);
		}

		// Todo remove not legal moves here

		/// <summary>
		/// Returns Tuple<Score, EvalResult, AllActuallyLegalMoves, (CastleOptionsActual)>
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="IsWhitesTurn"></param>
		/// <param name="CastleOptions"></param>
		/// <returns></returns>
		Tuple<double, byte, List<short>> AdvancedPositionEvaluation(byte[] Position, bool IsWhitesTurn, byte CastleOptions, List<short> AllLegalNextMoves)
		{
			double score = 0.0;
			byte result = IsWhitesTurn ? EvaluationResultBlackTurn : EvaluationResultWhiteTurn;     // Default Value

			double materialAdvantage = IsWhitesTurn ? Advanced_MaterialEvaluation(Position) : -Advanced_MaterialEvaluation(Position);

			score += materialAdvantage;
			score += (0.1 * (AllLegalNextMoves.Count - 8));    // less than 10 moves is negative, more than 10 is positive. Mobile positions are preferred

			// ...

			// Todo calculate result

			bool GameOver = (result & EvalResultGameOverMask) == EvalResultGameOverMask;    // 0110 or 1001 is for turns, so we need to actually check for 1100
			bool Draw = GameOver && ((result & EvalResultDrawMask) == EvaluationResultDraw);
			if (Draw) result = EvaluationResultDraw;
			else if (GameOver) result = (result & EvalResultWhiteMask) != 0 ? EvaluationResultWhiteWon : EvaluationResultBlackWon;

			return new Tuple<double, byte, List<short>>(score, result, AllLegalNextMoves);
		}
		public static double Advanced_MaterialEvaluation(byte[] Position)
		{
			double score = 0.0;
			foreach (byte doublePiece in Position)
			{
				if (IsWhitePieceFirstHalf(doublePiece)) score += BytePieceValue_Middlegame((byte)((doublePiece & firstHalfMask) >> 4));    // Shift by 4 to shift bits to second half
				else score -= BytePieceValue_Middlegame((byte)((doublePiece & firstHalfMask) >> 4));

				if (IsWhitePieceSecondHalf(doublePiece)) score += BytePieceValue_Middlegame((byte)(doublePiece & secondHalfMask));
				else score -= BytePieceValue_Middlegame((byte)(doublePiece & secondHalfMask));
			}
			return score;
		}
		private static double BytePieceValue_Middlegame(byte piece2ndHalf)
			=> BytePieceValues_Middlegame[piece2ndHalf & 0x07]; // Only last 3 bits => Mask 00000111 => 7
		private static double[] BytePieceValues_Middlegame =
		{
			// Value, Representation	| Hex  | 2nd Bit Half | Hex Value | Hex Value (Black)
			0,		// Empty			| 0x00 | 000		  | 0		  | (8)
			3,		// Pawn				| 0x01 | 001		  | 1		  | 9
			10,		// Knight			| 0x02 | 010		  | 2		  | A
			11,		// Bishop			| 0x03 | 011		  | 3		  | B
			23,		// Rook				| 0x04 | 100		  | 4		  | C
			54,		// Queen			| 0x05 | 101		  | 5		  | D
			9999,	// King				| 0x06 | 110		  | 6		  | E
			3,		// En Passant		| 0x07 | 111		  | 7		  | F
		};

		/**
		 * Evaluation Results work like this:
		 * First and second bit 1: Game Over
		 *   - Third and fourth bit 0 -> Game Over by draw
		 *   - Third and fourth bit 1 -> Game Over by win
		 * Else:
		 * First and Second bits are 0
		 * Last 4 bits 1: Whites Turn (Also for Game over -> White wins)
		 * Last 4 bits 0: Blacks Turn (Also for Game over -> Black wins)
		 */

		const byte EvalResultGameOverMask = 0xC0; /// 11000000 => 1100 => 12 => C
		const byte EvalResultDrawMask = 0xF0; /// 11110000, but result needs to be 1100 in the first 4 bits. Its just that all 4 bits matter here
		const byte EvalResultWhiteMask = 0x0F; /// 00001111 => 1111 => 15 => F

		public const byte EvaluationResultWhiteTurn = 0x8F;    /// 10011111	(bits 3 and 4 don't matter here, this is just for inverse)
		public const byte EvaluationResultBlackTurn = 0x40;    /// 01101111	(bits 3 and 4 don't matter here, this is just for inverse)
		const byte EvaluationResultWhiteWon = 0xFF;		/// 11111111
		const byte EvaluationResultBlackWon = 0xF0;		/// 11110000
		const byte EvaluationResultDraw = EvalResultGameOverMask;	/// 11000000, but result  Maybe add types of draw later, but it doesnt really matter

		const byte firstHalfMask = 0xF0;	/// First meaning the first 4 bits from the left
		const byte secondHalfMask = 0x0F;
		const short PieceMask = 0x000F;

		// Todo OG: private
		public static double MaterialEvaluation(byte[] Position)
		{
			double score = 0.0;
			foreach (byte doublePiece in Position)
			{
				if (IsWhitePieceFirstHalf(doublePiece)) score += BytePieceValue((byte) ((doublePiece & firstHalfMask) >> 4));    // Shift by 4 to shift bits to second half
				else score -= BytePieceValue((byte) ((doublePiece & firstHalfMask) >> 4));

				if (IsWhitePieceSecondHalf(doublePiece)) score += BytePieceValue((byte) (doublePiece & secondHalfMask));
				else score -= BytePieceValue((byte) (doublePiece & secondHalfMask));
			}
			return score;
		}

		/// <summary>
		/// Returns <Value, EnoughMaterial>
		/// </summary>
		/// <param name="Position"></param>
		/// <returns></returns>
		public static Tuple<double, bool> MaterialSumValue(byte[] Position)
		{
			double score = 0.0;
			bool enoughCheckmateMaterial = false;
			foreach (byte doublePiece in Position)
			{
				double val = BytePieceValue((byte)((doublePiece & firstHalfMask) >> 4));
				if(!enoughCheckmateMaterial) if (val < 10 && val != 3) enoughCheckmateMaterial = true;  // No King and no knight/bishop
				score += val;

				val = BytePieceValue((byte)(doublePiece & secondHalfMask));
				if (!enoughCheckmateMaterial) if (val < 10 && val != 3) enoughCheckmateMaterial = true; // No King and no knight/bishop
				score += val;
			}
			return new Tuple<double, bool>(score, enoughCheckmateMaterial);
		}

		private static bool EnoughCheckmatingMaterial(byte[] Position)
		{
			foreach (byte doublePiece in Position)
			{
				double val = BytePieceValue((byte)((doublePiece & firstHalfMask) >> 4));
				if (val < 10 && val != 3) return true;  // No King and no knight/bishop

				val = BytePieceValue((byte)(doublePiece & secondHalfMask));
				if (val < 10 && val != 3) return true; // No King and no knight/bishop
			}
			return false;
		}
	}

	partial class Stormcloud3 // Advanced Alpha Beta Pruning Search Algorithm
	{
		/*
		 * Fail-Soft Alpha Beta: https://www.chessprogramming.org/Alpha-Beta#Outside_the_Bounds
		 * cc = copycat = code copied / adjusted from pseudo-c-code from the internet
		int alphaBeta( int alpha, int beta, int depthleft ) {
			int bestscore = -oo;
			if( depthleft == 0 ) return quiesce( alpha, beta );
			for ( all moves)  {
				score = -alphaBeta( -beta, -alpha, depthleft - 1 );
				if( score >= beta )
					return score;  // fail-soft beta-cutoff
				if( score > bestscore ) {
					bestscore = score;
					if( score > alpha )
						alpha = score;
				}
			}
			return bestscore;
		}
		 */

		public static string MoveToString1(short value)
		{
			byte from = MoveFromIndex(value);
			byte to = MoveToIndex(value);
			char fromFile = (char) (from % 8 + 97);
			int fromRank = 8 - from / 8;
			char toFile = (char) (to % 8 + 97);
			int toRank = 8 - to / 8;
			return $"{fromFile}{fromRank} -> {toFile}{toRank}";
		}

		// Empty moves are possible

		public static string MoveToStringCas(byte[] position, short value)
		{
			byte from = MoveFromIndex(value);
			byte to = MoveToIndex(value);
			//System.Diagnostics.Debug.WriteLine($"Debug: from: {Convert.ToString(from, 2)} | to {Convert.ToString(to, 2)} | short: {Convert.ToString(value, 2)} | 0x{value.ToString("X2")}");
			byte pieceFrom = position[from >> 1];
			byte pieceTo = position[to >> 1];
			string nameFrom = (from & 1) == 1 ? PieceName(pieceFrom) : PieceName((byte) (pieceFrom >> 4));
			string nameTo = (to & 1) == 1 ? PieceName(pieceTo) : PieceName((byte) (pieceTo >> 4));
			char fromFile = (char)(from % 8 + 97);
			int fromRank = 8 - from / 8;	// 7 -> 1
			char toFile = (char)(to % 8 + 97);
			int toRank = 8 - to / 8;
			return $"{nameFrom} on {fromFile}{fromRank} {(nameTo == "Empty" ? "to" : "takes")} {nameTo} on {toFile}{toRank}";
		}

		public static string MoveToStringPro1(byte[] position, short value)
		{
			byte from = MoveFromIndex(value);
			byte to = MoveToIndex(value);
			//System.Diagnostics.Debug.WriteLine($"Debug: from: {Convert.ToString(from, 2)} | to {Convert.ToString(to, 2)} | short: {Convert.ToString(value, 2)} | 0x{value.ToString("X2")}");
			byte pieceFrom = position[from >> 1];
			byte pieceTo = position[to >> 1];
			string nameFrom = (from & 1) == 1 ? PieceNamePro(pieceFrom) : PieceNamePro((byte) (pieceFrom >> 4));
			string nameTo = (to & 1) == 1 ? PieceNamePro(pieceTo) : PieceNamePro((byte) (pieceTo >> 4));
			char fromFile = (char)(from % 8 + 97);
			int fromRank = 8 - from / 8;	// 7 -> 1
			char toFile = (char)(to % 8 + 97);
			int toRank = 8 - to / 8;
			string promotion = "";
			if ((value & PieceMask) != 0) promotion = "=" + PieceNamePro((byte) (value & 0x0007));
			string fromFile2 = /*fromRank == toRank ||*/ nameFrom == "" && nameTo != "-" /*pawn*/ ? "" + fromFile : "";	// This aint right
			string fromRank2 = fromFile == toFile ? "" + fromRank : "";	// This aint right
			return $"{nameFrom}{fromFile2}{/*fromRank2*/ ""}{(nameTo != "-" ? "x" : "")}{toFile}{toRank}{promotion}";
		}

		private static string PieceName(byte piece)
		{
			switch ((byte) (piece & 0x07))
			{
				case 0x01: return "Pawn";
				case 0x02: return "Knight";
				case 0x03: return "Bishop";
				case 0x04: return "Rook";
				case 0x05: return "Queen";
				case 0x06: return "King";
			}
			return "Empty";
		}

		private static string PieceNamePro(byte piece)
		{
			switch ((byte) (piece & 0x07))
			{
				case 0x01: return "";
				case 0x02: return "N";
				case 0x03: return "B";
				case 0x04: return "R";
				case 0x05: return "Q";
				case 0x06: return "K";
			}
			return "-";
		}


		#region Alpha Beta Pruning 1

		// History Heuristic: This can be implemented as a 2D table, indexed by the piece and the destination square, which keeps track of how often each move has caused a beta-cutoff.
		private int[][] HistoryHeuristic;	// Todo 2D table, I'm not satisfied with this

		// Killer Heuristic: You could keep an array of "killer moves" for each depth in the search tree. Each entry in the array can store
		// two moves, as it is commonly observed that there are seldom more than two distinct killer moves at each level of the tree.
		private List<short[]> KillerHeuristic = new List<short[]>();

		private Dictionary<string, double> TranspositionTable = new Dictionary<string, double>();

		List<short> OrderMoves(List<short> Moves)
		{
			Moves.OrderByDescending(x => scoreOf(x));	// Todo this

			double scoreOf(short x)
			{
				double score = 0;
				score += HistoryHeuristic[x].Length;
				return score;
			}

			return Moves;
		}

		short CC_Failsoft_BestMove = 0;

		// Todo re-do threefold repetition

		double CC_FailsoftAlphaBeta(byte[] position, bool isTurnColorWhite, byte castleOptions, string posKey, int depth)
			=> CC_FailsoftAlphaBeta(double.NegativeInfinity, double.PositiveInfinity, position, isTurnColorWhite, castleOptions, posKey, depth, new short[] { 0, 0, 0, 0, 0, 0 }, null, true);

		double CC_FailsoftAlphaBeta(double alpha, double beta, byte[] position, bool isTurnColorWhite, byte castleOptions, string posKey, int depthleft, short[] moveHistory, List<short> AllLegalMoves = null, bool isRoot = false)
		{
			double bestscore = int.MinValue;
			if (depthleft == 0) return CC_FailsoftQuiesce(alpha, beta, position, isTurnColorWhite, castleOptions, posKey, true);

			/*
			var moves = GetAllLegalMoves(position, isTurnColorWhite);
			System.Diagnostics.Debug.WriteLine("lne: " + moves.Count);
			*/

			if (AllLegalMoves == null) AllLegalMoves = GetAllLegalMoves(position, isTurnColorWhite);

			foreach (var move in AllLegalMoves)
			{
				var result = ResultingPosition(position, move, castleOptions, posKey);

				short[] newHistory = { moveHistory[1], moveHistory[2], moveHistory[3], moveHistory[4], moveHistory[5], move };

				// We don't have to pay attention to null moves since it fills up backwards and at least the last entry is not a null move. So, first move will almost be draw but the first move (current entry) prevents that
				if(newHistory[0] == newHistory[2] && newHistory[2] == newHistory[4] &&
					newHistory[1] == newHistory[3] && newHistory[3] == newHistory[5])
				{
					// Threefold repetition
					return 0.0;
				}

				// Check for King Captures (Checkmates)
				// 2nd Half
				if ((move & 1) == 1)
				{
					// King captured
					if ((position[MoveToIndex(move) >> 1] & 0x07) == 6)
					{
						return isTurnColorWhite ? double.PositiveInfinity : double.NegativeInfinity;
					}
				}
				// 1st Half
				else if ((position[MoveToIndex(move) >> 1] & 0x70) == 6)
				{
					// King captured
					return isTurnColorWhite ? double.PositiveInfinity : double.NegativeInfinity;
				}

				var followUpMoves = GetAllLegalMoves(result.Item1, !isTurnColorWhite);

				double score = -CC_FailsoftAlphaBeta(-beta, -alpha, result.Item1, !isTurnColorWhite, result.Item3, result.Item2, depthleft - 1, newHistory, followUpMoves);
				if (score >= beta)
				{
					if(isRoot)
					{
						CC_Failsoft_BestMove = move;
					}
					return score;
				}
				if (score > bestscore)
				{
					bestscore = score;
					if (score > alpha)
					{
						alpha = score;
						if(isRoot)
						{
							CC_Failsoft_BestMove = move;
						}
					}
				}
			}
			return bestscore;
		}

		/*
		 * -- Benchmarks --
		 * 
		 * -- Depth 4:
		 * 
		 * 
		 * No Quiesce (via || true statement in first if):
		 * Depth: 4 | Time: 0,0084995s | Score: 8 | Move: Pawn on c2 to Empty on c3   ||   c2 -> c3   ||   Pawn on c2 to Empty on c3
		 * 
		 * Old Quiesce:
		 * Depth 4 | ~380 secs
		 * 
		 * Improved Quiesce:
		 * Depth: 4 | Time: 0,0180641s | Score: 8 | Move: Pawn on c2 to Empty on c3   ||   c2 -> c3   ||   Pawn on c2 to Empty on c3
		 * 
		 * Improved Quiesce with post-capture-chain stuff:
		 * Depth: 4 | Time: 0,0198224s | Score: 8 | Move: Pawn on c2 to Empty on c3   ||   c2 -> c3   ||   Pawn on c2 to Empty on c3
		 * 
		 * 
		 * -- Depth 8:
		 * 
		 * 
		 * No Quiesce (via || true statement in first if):
		 * Depth: 8 | Time: 9,4162471s | Score: 1009 | Move: Pawn on c2 to Empty on c3   ||   c2 -> c3   ||   Pawn on c2 to Empty on c3
		 * 
		 * Improved Quiesce:
		 * Depth: 8 | Time: 16,9334008s | Score: 1009 | Move: Pawn on c2 to Empty on c3   ||   c2 -> c3   ||   Pawn on c2 to Empty on c3
		 * 
		 * Improved Quiesce with post-capture-chain stuff:
		 * Depth: 8 | Time: 27,7719654s | Score: 1009 | Move: Pawn on c2 to Empty on c3   ||   c2 -> c3   ||   Pawn on c2 to Empty on c3
		 * 
		 * Improved Quiesce with post-capture-chain stuff + post-chain-chain-length-threshold:
		 * Depth: 8 | Time: 16,8250764s | Score: 1009 | Move: Pawn on c2 to Empty on c3   ||   c2 -> c3   ||   Pawn on c2 to Empty on c3	(probably because the capture chains were all short)
		 * 
		 * Improved Quiesce with this: if ((captureChainLength >= 3 || captureChainLength % 2 == 1) && IsRootTurnColorWhite == isTurnColorWhite):
		 * Depth: 8 | Time: 17,3345857s | Score: 1009 | Move: Pawn on c2 to Empty on c3   ||   c2 -> c3   ||   Pawn on c2 to Empty on c3		(could work, lets try)
		 * 
		 * Old Quiesce:
		 * Unknown
		 * 
		 * => This is still way too slow, but it is a correct algorithm.
		 * 
		 */

		private bool IsRootTurnColorWhite;

		// The key if isRoot is that only the root call can edit capture chains
		// Since captures can't be repeats, repetition does not apply to this.
		double CC_FailsoftQuiesce(double alpha, double beta, byte[] position, bool isTurnColorWhite, byte castleOptions, string posKey, bool isRoot = false, int captureChainLength = 0, List<byte> captureChainFields = null)
		{
			// https://www.chessprogramming.org/Quiescence_Search
			List<short> AllCaptures = GetAllLegalMovesCapturesOnly(position, isTurnColorWhite, captureChainFields);
			double stand_pat = AdvancedPositionEvaluation(position, isTurnColorWhite, 0xFF /* Todo */, AllCaptures).Item1;
			if (stand_pat >= beta) return stand_pat;
			if (alpha < stand_pat) alpha = stand_pat;

			if(captureChainFields == null) captureChainFields = new List<byte>();   // Only look at capture chains
																					// Todo perhaps later add that it should always end on an opponentmove


			foreach (var capture in AllCaptures)
			{
				// Check for King Captures (Checkmates)
				// 2nd Half
				if ((capture & 1) == 1)
				{
					// King captured
					if ((position[MoveToIndex(capture) >> 1] & 0x07) == 6)
					{
						return isTurnColorWhite ? double.PositiveInfinity : double.NegativeInfinity;
					}
				}
				// 1st Half
				else if ((position[MoveToIndex(capture) >> 1] & 0x70) == 6)
				{
					// King captured
					return isTurnColorWhite ? double.PositiveInfinity : double.NegativeInfinity;
				}

				if (isRoot) captureChainFields.Add(MoveToIndex(capture));
				var result = ResultingPosition(position, capture, castleOptions, posKey);	// The point of MakeCapture() and TakeBack() is that we modify the same element and dont create a new one every time
				double score = -CC_FailsoftQuiesce(-beta, -alpha, result.Item1, !isTurnColorWhite, result.Item3, result.Item2, false, captureChainLength+1, captureChainFields);

				if (score >= beta) return beta;
				if (score > alpha) alpha = score;
			}

			//*
			// Last run
			if(AllCaptures.Count == 0)
			{
				// If no captures are available (finished), consider all resulting moves with the premise of stand_pat again, final capture
				if ((captureChainLength >= 3 || captureChainLength % 2 == 1) && IsRootTurnColorWhite == isTurnColorWhite)
				{
					// If its the final 
					foreach (var capture in GetAllLegalMovesCapturesOnly(position, isTurnColorWhite, null))
					{
						// Check for King Captures (Checkmates)
						// 2nd Half
						if ((capture & 1) == 1)
						{
							// King captured
							if ((position[MoveToIndex(capture) >> 1] & 0x07) == 6)
							{
								return isTurnColorWhite ? double.PositiveInfinity : double.NegativeInfinity;
							}
						}
						// 1st Half
						else if ((position[MoveToIndex(capture) >> 1] & 0x70) == 6)
						{
							// King captured
							return isTurnColorWhite ? double.PositiveInfinity : double.NegativeInfinity;
						}

						double score = PositionEvaluation(position, isTurnColorWhite).Score;

						if (score >= beta) return beta;
						if (score > alpha) alpha = score;
					}
				}
			}//*/

			return alpha;
		}


		/*
		protected class TranspositionTable
		{

		}*/

		#endregion
	}

	// Old Search Algorithm
	partial class Stormcloud3	// Search Algorithm
	{
		private ConcurrentQueue<OldSearch_SearchNode> OldSearch_SearchNodes = new ConcurrentQueue<OldSearch_SearchNode>();	// We're using a queue so that we can use just one, right?
		private ConcurrentDictionary<short, double> OldSearch_Temp_InitialMoveScores = new ConcurrentDictionary<short, double>();   // Calculating (Live) (just clone when necessary)

		private ConcurrentDictionary<string, double> OldSearch_PositionDataCacheDirectEvaluation = new ConcurrentDictionary<string, double>();

		private byte[] OldSearch_StartPosition;
		private Turn OldSearch_StartTurnColor;

		public Stormcloud3(byte[] Position, Turn CurrentTurnColor)
		{
			this.OldSearch_StartPosition = Position;
			this.OldSearch_StartTurnColor = CurrentTurnColor;
		}

		// ToDo Actually process position keys and values and stuff

		private short TargetDepth = 100, CurrentDepth = 1;

		private void OldSearch_StartProcessingInitialEntry(byte[] startPosition, bool isWhiteStart)
		{
			var InitialMovesWithPositions = GetAllLegalMoveAndResultingPositionPairs(startPosition, isWhiteStart, 0x0F);

			foreach (var v in InitialMovesWithPositions)
			{
				OldSearch_SearchNodes.Enqueue(new OldSearch_SearchNode(v.Result, v.Move));  //, new PositionData(isWhiteStart ? EvaluationResultWhiteTurn : EvaluationResultBlackTurn)
			}
		}

		private async void OldSearch_StartProcessingMultiThread()
		{
			OldSearch_ProcessNextNode(true);
			// (Temporary) Code from Stormcloud 2 (TQA)
			// Determine the number of worker tasks based on the available hardware resources
			int workerCount = Math.Min(Environment.ProcessorCount, 20);

			System.Diagnostics.Debug.WriteLine($"Launching Multithreaded Search with {workerCount} worker threads.");

			var tasks = new List<Task>();

			for (int i = 0; i < workerCount; i++)
			{
				tasks.Add(Task.Run(() => OldSearch_StartProcessingNodesSingleThread()));
			}

			await Task.WhenAll(tasks); // Wait for all tasks to complete

		}

		private void OldSearch_StartProcessingNodesSingleThread()
		{
			while (OldSearch_SearchNodes.Count > 0 && CurrentDepth <= TargetDepth)
			{
				OldSearch_ProcessNextNode();
			}
		}

		private void OldSearch_ProcessNextNode(bool isNodeOfStartPosition = false, OldSearch_SearchNode node = null)
		{
			if (node == null) OldSearch_SearchNodes.TryDequeue(out node);
			if (node == null) return;

			int depth = node.CurrentDepth;

			if(CurrentDepth <= depth)
			{
				OldSearch_ProcessNewDepth();
			}

			bool NodeTurnColorIsWhite = node.PositionData.Turn == EvaluationResultWhiteTurn;

			var AllNextOpponentMovesAndPositions = GetAllLegalMoveAndResultingPositionPairs(node.Position, !NodeTurnColorIsWhite, node.PositionData.Castle);
			var OpponentMoveScores = new Dictionary<short, double>();//new List<KeyValuePair<byte[], double>>();
			var OpponentMoveFollowUps = new Dictionary<short, List<short>>();


			if (isNodeOfStartPosition)
			{
				foreach (var move in AllNextOpponentMovesAndPositions)
				{
					OldSearch_Temp_InitialMoveScores.TryAdd(move.Move, PositionEvaluation(move.Result, new OldSearch_PositionData()).Score);
				}
			}

			// Evaluate all Opponent moves:

			foreach (var move in AllNextOpponentMovesAndPositions)
			{
				double score = 0;
				var moves = new List<short>();
				foreach (var pos in GetAllLegalMoveAndResultingPositionPairs(move.Result, NodeTurnColorIsWhite, move.CastleOptions))
				{
					score += PositionEvaluation(pos.Result, new OldSearch_PositionData()).Score;
					moves.Add(pos.Move);
				}
				OpponentMoveScores.Add(move.Move, score);
				OpponentMoveFollowUps.Add(move.Move, moves);
			}

			short bestMove = 0x0000;
			double maxScore = double.NegativeInfinity;

			foreach (var pair in OpponentMoveScores)
			{
				if (pair.Value > maxScore)
				{
					maxScore = pair.Value;
					bestMove = pair.Key;
				}
			}

			// Move with highest score is at index 0
			OldSearch_SearchNode OpponentNode = node.Result(bestMove);   // Perhaps use an implementation where the already saved new position is used.
			double opponentEval = PositionEvaluation(OpponentNode).Score;

			// Todo rework scores and 

			// Now get and enqueue all new stuff
			foreach (var moves in OpponentMoveFollowUps[bestMove])
			{
				OldSearch_SearchNode node2 = OpponentNode.Result(moves);
				OldSearch_SearchNodes.Enqueue(node2);
				if (node2.InitialMove != 0)
				{
					double thisScore = PositionEvaluation(node2).Score - opponentEval;
					if (OldSearch_Temp_InitialMoveScores.ContainsKey(node2.InitialMove))
						OldSearch_Temp_InitialMoveScores[node2.InitialMove] += thisScore;
					else OldSearch_Temp_InitialMoveScores.TryAdd(node2.InitialMove, thisScore);
				}
			}
		}

		/// <summary>
		/// When a checkmate is found, process search tree backwards to continue / search forced mate.
		/// </summary>
		/// <param name="kingTookNode"> The Searchnode in which the King was taken / could be taken. </param>
		/// <exception cref="NotImplementedException"> Yeah // Todo </exception>
		private void OldSearch_Wassereimer(OldSearch_SearchNode kingTookNode)
		{
			throw new NotImplementedException();
		}

		private void OldSearch_ProcessNewDepth()
		{
			short CurrentDepth = this.CurrentDepth;
			this.CurrentDepth++;
			var scores = OldSearch_Temp_InitialMoveScores.ToDictionary(entry => entry.Key, entry => entry.Value);
			if(OldSearch_Temp_InitialMoveScores.Count == 0)
			{
				System.Diagnostics.Debug.WriteLine($"Depth: {CurrentDepth} | Whoops, no moves | Nodes: {OldSearch_SearchNodes.Count}");
				return;
			}
			var bestMove = scores.OrderByDescending(value => value.Value).First();
			byte bestMoveFrom = MoveFromIndex(bestMove.Key);
			byte bestMoveTo = MoveToIndex(bestMove.Key);
			string move = bestMove.Key == 0 ? "null" :
				bestMoveFrom.ToString("X2") + " -> " + bestMoveTo.ToString("X2") + " | " +
				(int) bestMoveFrom + " -> " + (int)bestMoveTo + " | " +
				(char) (bestMoveFrom % 8 + 97) + (char)((64 - bestMoveFrom) / 8 + 49) + " -> " + (char)(bestMoveTo % 8 + 97) + (char)((64 - bestMoveTo) / 8 + 49);
			System.Diagnostics.Debug.WriteLine($"Depth: {CurrentDepth} | BestMove: [ {move} ] | Nodes: {OldSearch_SearchNodes.Count}");
		}
	}

	struct OldSearch_MoveResultingPositionPair
	{
		public short Move; // key
		public byte[] Result; // value
		public byte CastleOptions;	// Castle options

		public OldSearch_MoveResultingPositionPair(short move, byte[] result)
		{
			Move = move;
			Result = result;
			CastleOptions = 0x0F;
		}
		public OldSearch_MoveResultingPositionPair(short move, byte[] result, byte castleoptions)
		{
			Move = move;
			Result = result;
			CastleOptions = castleoptions;
		}
	}

	class OldSearch_SearchNode
	{
		internal byte[] Position;    // See position binary data docs
		// Position data
		OldSearch_SearchNode ParentNode;	// Save pointer to previous node in search tree for forced checkmate backtracking
		internal OldSearch_PositionData PositionData;
		public short InitialMove;
		public int CurrentDepth;
		public int FutureDepth = -2;

		public OldSearch_SearchNode(byte[] Position, OldSearch_SearchNode parentNode = null)
			: this(Position,
				  new OldSearch_PositionData(true, Stormcloud3.GeneratePositionKey(Position))
				{
					// ToDo Auto-Generate Position Data
				}, parentNode)
		{ }
		public OldSearch_SearchNode(byte[] Position, short initialMove)
			: this(Position,
				  new OldSearch_PositionData(true, Stormcloud3.GeneratePositionKey(Position))
				{
					// ToDo Auto-Generate Position Data
				}, initialMove)
		{ }
		public OldSearch_SearchNode(byte[] Position, OldSearch_PositionData PositionData, OldSearch_SearchNode parentNode = null)
		{
			if (Position == null) this.Position = new byte[32];
			else this.Position = Position;
			this.PositionData = PositionData;
			this.ParentNode = parentNode;
			SetInitialMove();
			if (ParentNode == null) CurrentDepth = 0;
			else
			{
				CurrentDepth = ParentNode.CurrentDepth + 1;
				FutureDepth = ParentNode.FutureDepth - 1;
			}
		}
		public OldSearch_SearchNode(byte[] Position, OldSearch_PositionData PositionData, short initialMove)
		{
			if (Position == null) this.Position = new byte[32];
			else this.Position = Position;
			this.PositionData = PositionData;
			this.ParentNode = null;
			SetInitialMove(initialMove);
			CurrentDepth = 0;
		}
		
		public OldSearch_SearchNode(byte[] Position, byte Turn, string PositionKey, OldSearch_SearchNode parentNode = null)
		: this(Position, Turn == Stormcloud3.EvaluationResultWhiteTurn, PositionKey, parentNode)
		{ }

		public OldSearch_SearchNode(byte[] Position, bool Turn, string PositionKey, OldSearch_SearchNode parentNode = null)
		{
			if (Position == null) this.Position = new byte[32];
			else this.Position = Position;
			this.PositionData = new OldSearch_PositionData(Turn, PositionKey);
			this.ParentNode = parentNode;
			SetInitialMove();
			if (ParentNode == null) CurrentDepth = 0;
			else
			{
				CurrentDepth = ParentNode.CurrentDepth + 1;
				FutureDepth = ParentNode.FutureDepth - 1;
			}
		}

		public OldSearch_SearchNode Result(short move)
		{
			var newPosition = Stormcloud3.ResultingPosition(Position, move, PositionData.Castle);
			OldSearch_PositionData newData = PositionData.Next(newPosition.Item2);
			newData.Castle = newPosition.Item3;	// New pos key
			return new OldSearch_SearchNode(newPosition.Item1, newData, this);
		}

		void SetInitialMove(short move = 0)	// Move = 0 represents 0x0000 meaning square 0 -> square 0
		{
			if (move == 0) InitialMove = move;
			else if (ParentNode != null) InitialMove = ParentNode.InitialMove;
			else InitialMove = 0;
		}
	}
	
	internal struct OldSearch_PositionData
	{
		public const byte defaultCastle = 0xFF;

		public byte Turn;
		public bool IsTurnWhite;
		public byte Castle;
		public string PositionKey;

		// Todo check if castle works as intended

		public bool WhiteCastleKingside() => (Castle & (1 << 0)) != 0; // Check if the 1st bit is set
		public bool WhiteCastleQueenside() => (Castle & (1 << 1)) != 0; // Check if the 2nd bit is set
		public bool BlackCastleKingside() => (Castle & (1 << 2)) != 0; // Check if the 3rd bit is set
		public bool BlackCastleQueenside() => (Castle & (1 << 3)) != 0; // Check if the 4th bit is set

		// Set castle
		public void SetWhiteCastleKingside(bool canCastle) => Castle = (byte)(canCastle ? (Castle | (1 << 0)) : (Castle & ~(1 << 0)));
		public void SetWhiteCastleQueenside(bool canCastle) => Castle = (byte)(canCastle ? (Castle | (1 << 1)) : (Castle & ~(1 << 1)));
		public void SetBlackCastleKingside(bool canCastle) => Castle = (byte)(canCastle ? (Castle | (1 << 2)) : (Castle & ~(1 << 2)));
		public void SetBlackCastleQueenside(bool canCastle) => Castle = (byte)(canCastle ? (Castle | (1 << 3)) : (Castle & ~(1 << 3)));

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Turn"></param>
		public OldSearch_PositionData(bool IsTurnWhite, string PositionKey, byte castle = defaultCastle)
		{
			this.IsTurnWhite = IsTurnWhite;
			this.Turn = IsTurnWhite ? Stormcloud3.EvaluationResultWhiteTurn : Stormcloud3.EvaluationResultBlackTurn;
			this.Castle = castle;
			this.PositionKey = PositionKey;
		}

		// Todo castle implement

		public OldSearch_PositionData Next(byte[] newPosition) => Next(Stormcloud3.GeneratePositionKey(newPosition));
		public OldSearch_PositionData Next(string newPositionKey)
		{
			OldSearch_PositionData data = new OldSearch_PositionData(!this.IsTurnWhite, newPositionKey);   // prev: (byte) ~this.Turn for Turn
			return data;
		}
	}

	partial class Stormcloud3	// Piece Values and Legal moves
	{


		#region Legal Moves

		#region Legal Moves [All]

		/// <summary>
		/// A list of all legal moves based on the provided position,<br/>
		/// paired with their respective resulting position. <br/> <br/>
		/// First is the move: [0] = From | [1] = To. <br/>
		/// Indexes are 64-based. <br/>
		/// Second is the new position: [0-31]: double-piece byte.
		/// </summary>
		/// <param name="Position"> The Position, a size 32 byte array. </param>
		/// <returns> List of all legal moves paired with their resulting position. </returns>
		private static List<OldSearch_MoveResultingPositionPair> GetAllLegalMoveAndResultingPositionPairs(byte[] Position, bool isTurnColorWhite, byte CastleOptions)
		{
			var movePairs = new List<OldSearch_MoveResultingPositionPair>();
			foreach (short move in GetAllLegalMoves(Position, isTurnColorWhite))
			{
				var _out = ResultingPosition(Position, move, CastleOptions);
				movePairs.Add(new OldSearch_MoveResultingPositionPair(move, _out.Item1, _out.Item3));
			}
			return movePairs;
		}

		/// <summary>
		/// A list of all legal moves based on the provided position. <br/>
		/// Each Move: [0] = From | [1] = To. <br/>
		/// Indexes are 64-based.
		/// </summary>
		/// <param name="Position"> The Position, a size 32 byte array. </param>
		/// <returns> List of all legal moves. </returns>
		private static List<short> GetAllLegalMoves(byte[] Position, bool isTurnColorWhite)              // Todo perhaps discard of this? Or move part of the other function in here
		{
			var moves = new List<short>();
			byte colorMask1 = isTurnColorWhite ? (byte) 0 : (byte) 0x80;
			byte colorMask2 = isTurnColorWhite ? (byte) 0 : (byte) 0x08;
			for (byte i = 0; i < 64; i += 2)
			{
				byte piece = Position[i >> 1];
				if((piece & 0x80) == colorMask1) moves.AddRange(GetLegalMovesPiece(Position, i, isTurnColorWhite));
				if ((piece & 0x08) == colorMask2) moves.AddRange(GetLegalMovesPiece(Position, (byte) (i+1), isTurnColorWhite));
			}
			return moves;
		}

		public static List<short> GetLegalMovesPiece(byte[] position, byte pieceLocationIndex, bool isTurnColorWhite)
		{
			byte piece = position[pieceLocationIndex >> 1];
			bool isPieceWhite;
			if ((pieceLocationIndex & 1) == 1)
			{
				isPieceWhite = (piece & 0x08) == 0;	// 4th bit is 0
				piece &= 0x07; // Uneven index => 2nd half
				piece = (byte) (piece << 4);	// Move to first half
			}
			else
			{
				isPieceWhite = (piece & 0x80) == 0; // 4th bit is 0
				piece &= 0x70; // Even index => 1st half
			}
			if(isPieceWhite != isTurnColorWhite) return new List<short>();

			// Todo look for checks
			if (piece == 0x10) return GetLegalMovesPawn(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x20) return GetLegalMovesKnight(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x30) return GetLegalMovesBishop(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x40) return GetLegalMovesRook(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x50) return GetLegalMovesQueen(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x60) return GetLegalMovesKing(position, pieceLocationIndex, isPieceWhite);

			return new List<short>();
		}

		/// <summary>
		/// All legal moves of a pawn.
		/// </summary>
		/// <param name="position"> The current position. </param>
		/// <param name="pawnLocationIndex"> The location index of the pawn (64-format). </param>
		/// <param name="isPieceWhite"> If the pawn is white or not. Not really necessary, but the method that calls this already knows so why calculate it again? </param>
		/// <returns> List of all legal moves of this pawn. </returns>
		public static List<short> GetLegalMovesPawn(byte[] position, byte pawnLocationIndex, bool isPieceWhite)	// 0x09 = black pawn (1001)
		{
			var legalMoves = new List<short>();
			
			// Check if field infront is clear
			byte fieldIndex = (byte) (isPieceWhite ? pawnLocationIndex - 8 : pawnLocationIndex + 8);
			if (IsValidIndex(fieldIndex))
			{
				if (IsFieldEmpty(position[fieldIndex >> 1], (fieldIndex & 1) == 1))
				{
					add(fieldIndex);
					// If first rank, add double
					if (pawnLocationIndex >> 3 == 0x06 && isPieceWhite || pawnLocationIndex >> 3 == 0x01 && !isPieceWhite)       // Loc index: White: 48, 49... - 55: 00110000, 00110001, 00110010,..., so 00110xxx >> 3 = 00000110 = 6   |   Black: 8,9,10,11... - 15 00001000, 00001001, 00001010 - 00001111 -> Mask of 00001xxx
					{
						fieldIndex = (byte)(isPieceWhite ? fieldIndex - 8 : fieldIndex + 8);
						if (IsFieldEmpty(position[fieldIndex >> 1], (fieldIndex & 1) == 1))
						{
							// No need for the add method because this is guaranteed to not be a promotion move or on the last rank
							legalMoves.Add(ToMove(pawnLocationIndex, fieldIndex));
						}
					}
				}
			}

			void diagonalMove(sbyte delta)
			{
				byte fieldIndex2 = (byte)(pawnLocationIndex + delta);
				if (!IsValidIndex(fieldIndex2)) return;
				bool isSecondHalf = (fieldIndex2 & 1) == 1;
				byte piece = position[fieldIndex2 >> 1];

				if(!IsFieldEmpty(piece, isSecondHalf))
				{
					if(isPieceWhite != IsWhitePiece(piece, isSecondHalf))
					{
						add(fieldIndex2);
					}
				}
			}

			void add(byte to)
			{
				if (IsEdgeRank64IndexFormat(to))   // We assume its the correct final rank since pawns shouldnt go backwards
				{
					byte colorMask = (byte) (isPieceWhite ? 0x00 : 0x08);
					legalMoves.Add(ToMove(pawnLocationIndex, to, (byte) (0x02 | colorMask)));	// Promotion to Knight
					legalMoves.Add(ToMove(pawnLocationIndex, to, (byte) (0x03 | colorMask)));	// Promotion to Bishop
					legalMoves.Add(ToMove(pawnLocationIndex, to, (byte) (0x04 | colorMask)));	// Promotion to Rook
					legalMoves.Add(ToMove(pawnLocationIndex, to, (byte) (0x05 | colorMask)));	// Promotion to Queen
				}
				else legalMoves.Add(ToMove(pawnLocationIndex, to));
			}

			if(isPieceWhite)
			{
				if (!IsFileH(pawnLocationIndex)) diagonalMove(-7);
				if (!IsFileA(pawnLocationIndex)) diagonalMove(-9);
			}
			else
			{
				if (!IsFileA(pawnLocationIndex)) diagonalMove(7);
				if (!IsFileH(pawnLocationIndex)) diagonalMove(9);
			}

			return legalMoves;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="knightLocationIndex">Location Index in 64-format.</param>
		/// <returns></returns>
		public static List<short> GetLegalMovesKnight(byte[] position, byte knightLocationIndex, bool isPieceWhite)
		{
			byte[] possibleMoves = KnightPossibleMoves[knightLocationIndex];
			List<short> legalMoves = new List<short>();

			foreach (byte possibleMove in possibleMoves)
			{
				// Check if piece is of own color

				bool isSecondHalf = (possibleMove & 1) == 1;
				// Check if the target field is empty
				if (IsFieldEmpty(position[knightLocationIndex >> 1], isSecondHalf))
				{
					legalMoves.Add(ToMove(knightLocationIndex, possibleMove));
				}
				// If not, check if the colors are different. This might not be the prettiest, but its the fastest
				else if (IsWhitePiece(position[possibleMove >> 1], isSecondHalf) != isPieceWhite)
				{
					legalMoves.Add(ToMove(knightLocationIndex, possibleMove));
				}
			}
			return legalMoves;
		}

		public static List<short> GetLegalMovesBishop(byte[] position, byte bishopLocationIndex, bool isPieceWhite)
			=> GetLegalMovesBishop(position, bishopLocationIndex, isPieceWhite, (bishopLocationIndex & 1) == 1);
		public static List<short> GetLegalMovesBishop(byte[] position, byte bishopLocationIndex, bool isPieceWhite, bool isSecondHalf)
		{
			List<short> legalMoves = new List<short>();

			// Up right
			byte index = bishopLocationIndex;
			bool isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileH(index)) break;
				if (IsRank8(index)) break;
				index -= 7;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			// Up left
			index = bishopLocationIndex;
			isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileA(index)) break;
				if (IsRank8(index)) break;
				index -= 9;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			// Down left
			index = bishopLocationIndex;
			isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileA(index)) break;
				if (IsRank1(index)) break;
				index += 7;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			// Down right
			index = bishopLocationIndex;
			isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileH(index)) break;
				if (IsRank1(index)) break;
				index += 9;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			bool addIfLegal()    // Returns if should break
			{
				byte piece = position[index >> 1];
				if (IsWhitePiece(piece, isSecondHalf2) != isPieceWhite)	// If empty & black: IsWhitePiece = false & isPieceWhite = false
				{
					legalMoves.Add(ToMove(bishopLocationIndex, index));
					return !IsFieldEmpty(piece, isSecondHalf2);    // If field is empty => dont break, if field is empty => break
				}
				else if(IsFieldEmpty(piece, isSecondHalf2))
				{
					legalMoves.Add(ToMove(bishopLocationIndex, index));
					return false;    // If field is empty => dont break, if field is empty => break
				}
				return true;
			}


			return legalMoves;
		}

		public static List<short> GetLegalMovesRook(byte[] position, byte rookLocationIndex, bool isPieceWhite)
			=> GetLegalMovesRook(position, rookLocationIndex, isPieceWhite, (rookLocationIndex & 1) == 1);
		public static List<short> GetLegalMovesRook(byte[] position, byte rookLocationIndex, bool isPieceWhite, bool isSecondHalf)
		{
			List<short> legalMoves = new List<short>();
			bool isSecondHalf2 = isSecondHalf;

			byte index = rookLocationIndex;
			while (true)
			{
				if (IsRank8(index)) break;
				index -= 8;
				if (addIfLegal()) break;
			}

			index = rookLocationIndex;
			while (true)
			{
				if (IsRank1(index)) break;
				index += 8;
				if (addIfLegal()) break;
			}

			index = rookLocationIndex;
			while (true)
			{
				if (IsFileA(index)) break;
				index -= 1;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			index = rookLocationIndex;
			isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileH(index)) break;
				index += 1;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			bool addIfLegal()    // Returns if should break
			{
				byte piece = position[index >> 1];
				if (IsWhitePiece(piece, isSecondHalf2) != isPieceWhite)
				{
					legalMoves.Add(ToMove(rookLocationIndex, index));
					return !IsFieldEmpty(piece, isSecondHalf2);    // If field is empty => dont break, if field is empty => break
				}
				else if (IsFieldEmpty(piece, isSecondHalf2))
				{
					legalMoves.Add(ToMove(rookLocationIndex, index));
					return false;    // If field is empty => dont break, if field is empty => break
				}
				return true;
			}

			return legalMoves;
		}

		public static List<short> GetLegalMovesQueen(byte[] position, byte queenLocationIndex, bool isPieceWhite)
		{
			List<short> legalMoves = new List<short>();
			bool isSecondHalf = (queenLocationIndex & 1) == 1;

			legalMoves.AddRange(GetLegalMovesBishop(position, queenLocationIndex, isPieceWhite, isSecondHalf));
			legalMoves.AddRange(GetLegalMovesRook(position, queenLocationIndex, isPieceWhite, isSecondHalf));

			return legalMoves;
		}

		public static List<short> GetLegalMovesKing(byte[] position, byte kingLocationIndex, bool isPieceWhite)
		{
			List<short> legalMoves = new List<short>();
			bool isSecondHalf = (kingLocationIndex & 1) == 1;

			if(!IsRank8(kingLocationIndex))
			{
				// Look for moves above
				deltaSameRankUp(9);

				byte i = (byte)(kingLocationIndex - 8);
				addIfLegal(i, isSecondHalf);

				deltaSameRankUp(7);
			}
			if(!IsRank1(kingLocationIndex))
			{
				// Look for moves below
				deltaSameRankDown(9);
				
				byte i = (byte)(kingLocationIndex + 8);
				addIfLegal(i, isSecondHalf);
				
				deltaSameRankDown(7);
			}
			if(!IsFileA(kingLocationIndex))
			{
				//System.Diagnostics.Debug.WriteLine($"Index");
				// Check move to the right
				byte i = (byte)(kingLocationIndex - 1);
				addIfLegal(i, !isSecondHalf);	// No need to check, since the corners are the ones we need to check
			}
			if(!IsFileH(kingLocationIndex))
			{
				// Check move to the right
				byte i = (byte)(kingLocationIndex + 1);
				addIfLegal(i, !isSecondHalf);	// No need to check, since the corners are the ones we need to check
			}

			if(kingLocationIndex == 4)
			{
				if(CanCastleBlackKingside(kingLocationIndex) && (position[2] & 0x0F) + (position[3] & 0xF0) == 0)		// Also check fields in between
				{
					addIfLegal((byte)(kingLocationIndex + 2), isSecondHalf);
				}
				if (CanCastleBlackQueenside(kingLocationIndex) && (position[0] & 0x0F) + (position[1] & 0xFF) == 0)
				{
					addIfLegal((byte)(kingLocationIndex - 2), isSecondHalf);
				}
			}
			else if (kingLocationIndex == 60)
			{
				if (CanCastleWhiteKingside(kingLocationIndex) && (position[30] & 0x0F) + (position[31] & 0xF0) == 0)
				{
					addIfLegal((byte)(kingLocationIndex + 2), isSecondHalf);
				}
				if (CanCastleWhiteQueenside(kingLocationIndex) && (position[28] & 0x0F) + (position[29] & 0xFF) == 0)
				{
					addIfLegal((byte)(kingLocationIndex - 2), isSecondHalf);
				}
			}

			void deltaSameRankUp(byte delta)
			{
				byte i = (byte) (kingLocationIndex - delta);
				if (IsSameRank64IndexFormat((byte) (kingLocationIndex - 8), i))
					addIfLegal(i, !isSecondHalf);
			}
			void deltaSameRankDown(byte delta)
			{
				byte i = (byte) (kingLocationIndex + delta);
				if (IsSameRank64IndexFormat((byte)(kingLocationIndex + 8), i))
					addIfLegal(i, !isSecondHalf);
			}

			void addIfLegal(byte index, bool secondHalf)
			{
				byte piece2 = position[index >> 1];
				if(isPieceWhite != IsWhitePiece(piece2, secondHalf))	// If opposite Color
				{
					legalMoves.Add(ToMove(kingLocationIndex, index));
				}
				else if(IsFieldEmpty(piece2, secondHalf))				// Or if Empty
				{
					legalMoves.Add(ToMove(kingLocationIndex, index));
				}
			}

			return legalMoves;
		}

		#endregion

		#region Legal Moves [Capture Only]

		/// <summary>
		/// A list of all legal moves based on the provided position. <br/>
		/// Each Move: [0] = From | [1] = To. <br/>
		/// Indexes are 64-based.
		/// </summary>
		/// <param name="Position"> The Position, a size 32 byte array. </param>
		/// <returns> List of all legal moves. </returns>
		private List<short> GetAllLegalMovesCapturesOnly(byte[] Position, bool isTurnColorWhite, List<byte> captureChainFields)              // Todo perhaps discard of this? Or move part of the other function in here
		{
			var moves = new List<short>();
			byte colorMask1 = isTurnColorWhite ? (byte)0 : (byte)0x80;
			byte colorMask2 = isTurnColorWhite ? (byte)0 : (byte)0x08;
			for (byte i = 0; i < 64; i += 2)
			{
				byte piece = Position[i >> 1];
				if ((piece & 0x80) == colorMask1) moves.AddRange(GetLegalMovesPieceCapturesOnly(Position, i, isTurnColorWhite, captureChainFields));
				if ((piece & 0x08) == colorMask2) moves.AddRange(GetLegalMovesPieceCapturesOnly(Position, (byte)(i + 1), isTurnColorWhite, captureChainFields));
			}
			return moves;
		}

		public static List<short> GetLegalMovesPieceCapturesOnly(byte[] position, byte pieceLocationIndex, bool isTurnColorWhite, List<byte> captureChainFields)
		{
			byte piece = position[pieceLocationIndex >> 1];
			bool isPieceWhite;
			if ((pieceLocationIndex & 1) == 1)
			{
				isPieceWhite = (piece & 0x08) == 0; // 4th bit is 0
				piece &= 0x07; // Uneven index => 2nd half
				piece = (byte)(piece << 4); // Move to first half
			}
			else
			{
				isPieceWhite = (piece & 0x80) == 0; // 4th bit is 0
				piece &= 0x70; // Even index => 1st half
			}
			if (isPieceWhite != isTurnColorWhite) return new List<short>();

			// Todo look for checks
			if (piece == 0x10) return GetLegalMovesPawnCapturesOnly(position, pieceLocationIndex, isPieceWhite, captureChainFields);
			if (piece == 0x20) return GetLegalMovesKnightCapturesOnly(position, pieceLocationIndex, isPieceWhite, captureChainFields);
			if (piece == 0x30) return GetLegalMovesBishopCapturesOnly(position, pieceLocationIndex, isPieceWhite, captureChainFields);
			if (piece == 0x40) return GetLegalMovesRookCapturesOnly(position, pieceLocationIndex, isPieceWhite, captureChainFields);
			if (piece == 0x50) return GetLegalMovesQueenCapturesOnly(position, pieceLocationIndex, isPieceWhite, captureChainFields);
			if (piece == 0x60) return GetLegalMovesKingCapturesOnly(position, pieceLocationIndex, isPieceWhite, captureChainFields);

			return new List<short>();
		}

		/// <summary>
		/// All legal moves of a pawn.
		/// </summary>
		/// <param name="position"> The current position. </param>
		/// <param name="pawnLocationIndex"> The location index of the pawn (64-format). </param>
		/// <param name="isPieceWhite"> If the pawn is white or not. Not really necessary, but the method that calls this already knows so why calculate it again? </param>
		/// <returns> List of all legal moves of this pawn. </returns>
		public static List<short> GetLegalMovesPawnCapturesOnly(byte[] position, byte pawnLocationIndex, bool isPieceWhite, List<byte> captureChainFields) // 0x09 = black pawn (1001)
		{
			var legalMoves = new List<short>();
			
			// Checking in front is not necessary since they def wont be captures
			void diagonalMove(sbyte delta)
			{
				byte fieldIndex2 = (byte)(pawnLocationIndex + delta);
				if (!IsValidIndex(fieldIndex2)) return;
				bool isSecondHalf = (fieldIndex2 & 1) == 1;
				byte piece = position[fieldIndex2 >> 1];

				if (!IsFieldEmpty(piece, isSecondHalf))
				{
					if (isPieceWhite != IsWhitePiece(piece, isSecondHalf))
					{
						add(fieldIndex2);
					}
				}
			}

			void add(byte to)
			{
				if (IsEdgeRank64IndexFormat(to))   // We assume its the correct final rank since pawns shouldnt go backwards
				{
					byte colorMask = (byte)(isPieceWhite ? 0x00 : 0x08);
					legalMoves.Add(ToMove(pawnLocationIndex, to, (byte)(0x02 | colorMask)));    // Promotion to Knight
					legalMoves.Add(ToMove(pawnLocationIndex, to, (byte)(0x03 | colorMask)));    // Promotion to Bishop
					legalMoves.Add(ToMove(pawnLocationIndex, to, (byte)(0x04 | colorMask)));    // Promotion to Rook
					legalMoves.Add(ToMove(pawnLocationIndex, to, (byte)(0x05 | colorMask)));    // Promotion to Queen
				}
				else legalMoves.Add(ToMove(pawnLocationIndex, to));
			}

			if (isPieceWhite)
			{
				if (!IsFileH(pawnLocationIndex)) diagonalMove(-7);
				if (!IsFileA(pawnLocationIndex)) diagonalMove(-9);
			}
			else
			{
				if (!IsFileA(pawnLocationIndex)) diagonalMove(7);
				if (!IsFileH(pawnLocationIndex)) diagonalMove(9);
			}

			return legalMoves;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="knightLocationIndex">Location Index in 64-format.</param>
		/// <returns></returns>
		public static List<short> GetLegalMovesKnightCapturesOnly(byte[] position, byte knightLocationIndex, bool isPieceWhite, List<byte> captureChainFields)
		{
			byte[] possibleMoves = KnightPossibleMoves[knightLocationIndex];
			List<short> legalMoves = new List<short>();

			foreach (byte possibleMove in possibleMoves)
			{
				// Only check the capture chain if it exists
				if (captureChainFields != null) if (!captureChainFields.Contains(possibleMove)) continue;
				// Check if piece is of own color

				bool isSecondHalf = (possibleMove & 1) == 1;
				// Check if the target field is capture
				if (IsWhitePiece(position[possibleMove >> 1], isSecondHalf) != isPieceWhite)
				{
					legalMoves.Add(ToMove(knightLocationIndex, possibleMove));
				}
			}
			return legalMoves;
		}

		public static List<short> GetLegalMovesBishopCapturesOnly(byte[] position, byte bishopLocationIndex, bool isPieceWhite, List<byte> captureChainFields)
			=> GetLegalMovesBishopCapturesOnly(position, bishopLocationIndex, isPieceWhite, (bishopLocationIndex & 1) == 1, captureChainFields);
		public static List<short> GetLegalMovesBishopCapturesOnly(byte[] position, byte bishopLocationIndex, bool isPieceWhite, bool isSecondHalf, List<byte> captureChainFields)
		{
			List<short> legalMoves = new List<short>();

			byte index = bishopLocationIndex;
			bool isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileH(index)) break;
				if (IsRank8(index)) break;
				index -= 7;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			index = bishopLocationIndex;
			isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileA(index)) break;
				if (IsRank8(index)) break;
				index -= 9;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			index = bishopLocationIndex;
			isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileA(index)) break;
				if (IsRank1(index)) break;
				index += 7;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			index = bishopLocationIndex;
			isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileH(index)) break;
				if (IsRank1(index)) break;
				index += 9;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			bool addIfLegal()    // Returns if should break
			{
				byte piece = position[index >> 1];
				if (IsWhitePiece(piece, isSecondHalf2) != isPieceWhite)
				{
					if (captureChainFields != null)
						if (captureChainFields.Contains(index))
							legalMoves.Add(ToMove(bishopLocationIndex, index));
					return !IsFieldEmpty(piece, isSecondHalf2);    // If field is empty => dont break, if field is empty => break
				}
				return true;
			}


			return legalMoves;
		}

		public static List<short> GetLegalMovesRookCapturesOnly(byte[] position, byte rookLocationIndex, bool isPieceWhite, List<byte> captureChainFields)
			=> GetLegalMovesRookCapturesOnly(position, rookLocationIndex, isPieceWhite, (rookLocationIndex & 1) == 1, captureChainFields);
		public static List<short> GetLegalMovesRookCapturesOnly(byte[] position, byte rookLocationIndex, bool isPieceWhite, bool isSecondHalf, List<byte> captureChainFields)
		{
			List<short> legalMoves = new List<short>();
			bool isSecondHalf2 = isSecondHalf;

			byte index = rookLocationIndex;
			while (true)
			{
				if (IsRank8(index)) break;
				index -= 8;
				if (addIfLegal()) break;
			}

			index = rookLocationIndex;
			while (true)
			{
				if (IsRank1(index)) break;
				index += 8;
				if (addIfLegal()) break;
			}

			index = rookLocationIndex;
			while (true)
			{
				if (IsFileA(index)) break;
				index -= 1;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			index = rookLocationIndex;
			isSecondHalf2 = isSecondHalf;
			while (true)
			{
				if (IsFileH(index)) break;
				index += 1;
				isSecondHalf2 = !isSecondHalf2;
				if (addIfLegal()) break;
			}

			bool addIfLegal()    // Returns if should break
			{
				byte piece = position[index >> 1];
				if (IsWhitePiece(piece, isSecondHalf2) != isPieceWhite)
				{
					// Only check the capture chain if it exists
					if (captureChainFields != null)
						if (captureChainFields.Contains(index))
							legalMoves.Add(ToMove(rookLocationIndex, index));
					return !IsFieldEmpty(piece, isSecondHalf2);    // If field is empty => dont break, if field is empty => break
				}
				return true;
			}

			return legalMoves;
		}

		public static List<short> GetLegalMovesQueenCapturesOnly(byte[] position, byte queenLocationIndex, bool isPieceWhite, List<byte> captureChainFields)
		{
			List<short> legalMoves = new List<short>();
			bool isSecondHalf = (queenLocationIndex & 1) == 1;

			legalMoves.AddRange(GetLegalMovesBishopCapturesOnly(position, queenLocationIndex, isPieceWhite, isSecondHalf, captureChainFields));
			legalMoves.AddRange(GetLegalMovesRookCapturesOnly(position, queenLocationIndex, isPieceWhite, isSecondHalf, captureChainFields));

			return legalMoves;
		}

		public static List<short> GetLegalMovesKingCapturesOnly(byte[] position, byte kingLocationIndex, bool isPieceWhite, List<byte> captureChainFields)
		{
			List<short> legalMoves = new List<short>();
			bool isSecondHalf = (kingLocationIndex & 1) == 1;

			if (!IsRank8(kingLocationIndex))
			{
				// Look for moves above
				deltaSameRankUp(9);

				byte i = (byte)(kingLocationIndex - 8);
				addIfLegal(i, isSecondHalf);

				deltaSameRankUp(7);
			}
			if (!IsRank1(kingLocationIndex))
			{
				// Look for moves below
				deltaSameRankDown(9);

				byte i = (byte)(kingLocationIndex + 8);
				addIfLegal(i, isSecondHalf);

				deltaSameRankDown(7);
			}
			if (!IsFileA(kingLocationIndex))
			{
				//System.Diagnostics.Debug.WriteLine($"Index");
				// Check move to the right
				byte i = (byte)(kingLocationIndex - 1);
				addIfLegal(i, !isSecondHalf);   // No need to check, since the corners are the ones we need to check
			}
			if (!IsFileH(kingLocationIndex))
			{
				// Check move to the right
				byte i = (byte)(kingLocationIndex + 1);
				addIfLegal(i, !isSecondHalf);   // No need to check, since the corners are the ones we need to check
			}

			if (kingLocationIndex == 4)
			{
				if (CanCastleBlackKingside(kingLocationIndex) && (position[2] & 0x0F) + (position[3] & 0xF0) == 0)      // Also check fields in between
				{
					addIfLegal((byte)(kingLocationIndex + 2), isSecondHalf);
				}
				if (CanCastleBlackQueenside(kingLocationIndex) && (position[0] & 0x0F) + (position[1] & 0xFF) == 0)
				{
					addIfLegal((byte)(kingLocationIndex - 2), isSecondHalf);
				}
			}
			else if (kingLocationIndex == 60)
			{
				if (CanCastleWhiteKingside(kingLocationIndex) && (position[30] & 0x0F) + (position[31] & 0xF0) == 0)
				{
					addIfLegal((byte)(kingLocationIndex + 2), isSecondHalf);
				}
				if (CanCastleWhiteQueenside(kingLocationIndex) && (position[28] & 0x0F) + (position[29] & 0xFF) == 0)
				{
					addIfLegal((byte)(kingLocationIndex - 2), isSecondHalf);
				}
			}

			void deltaSameRankUp(byte delta)
			{
				byte i = (byte)(kingLocationIndex - delta);
				if (IsSameRank64IndexFormat((byte)(kingLocationIndex - 8), i))
					addIfLegal(i, !isSecondHalf);
			}
			void deltaSameRankDown(byte delta)
			{
				byte i = (byte)(kingLocationIndex + delta);
				if (IsSameRank64IndexFormat((byte)(kingLocationIndex + 8), i))
					addIfLegal(i, !isSecondHalf);
			}

			void addIfLegal(byte index, bool secondHalf)
			{
				// Only check the capture chain if it exists
				if (captureChainFields != null) if (!captureChainFields.Contains(index)) return;
				if (IsWhitePiece(position[index >> 1], secondHalf) != isPieceWhite)
				{
					legalMoves.Add(ToMove(kingLocationIndex, index));
				}
			}

			return legalMoves;
		}

		#endregion

		#region Helper Methods | Board Index Analysis

		// Check if the first bit of each half is 0 (indicates white piece)
		static bool IsWhitePiece(byte piece) => (piece & 0x88) == 0 && (piece & 0xFF) != 0x00 /* not empty */;
		static bool IsWhitePiece(byte piece, bool isSecondHalf) => isSecondHalf ? IsWhitePieceSecondHalf(piece) : IsWhitePieceFirstHalf(piece);
		static bool IsWhitePieceFirstHalf(byte piece) => (piece & 0x80) == 0 && (piece & 0xF0) != 0;
		static bool IsWhitePieceSecondHalf(byte piece) => (piece & 0x08) == 0 && (piece & 0x0F) != 0;
		static bool IsBlackPiece(byte piece) => (piece & 0x88) != 0 && (piece & 0xFF) != 0x00 /* not empty */;
		static bool IsBlackPiece(byte piece, bool isSecondHalf) => isSecondHalf ? IsBlackPieceSecondHalf(piece) : IsBlackPieceFirstHalf(piece);
		static bool IsBlackPieceFirstHalf(byte piece) => (piece & 0x80) == 0x80;	// Per default, if there is a color bit 1 there is a piece
		static bool IsBlackPieceSecondHalf(byte piece) => (piece & 0x08) == 0x08;	// Aint to such thing as an empty black field
		static bool IsFieldEmpty(byte piece, bool isSecondHalf) => isSecondHalf ? IsFieldEmptySecondHalf(piece) : IsFieldEmptyFirstHalf(piece);
		static bool IsFieldEmptyFirstHalf(byte piece) => (piece & 0xF0) == 0;
		static bool IsFieldEmptySecondHalf(byte piece) => (piece & 0x0F) == 0;
		static bool IsOppositeColorOrEmpty(byte ogPiece, bool isOGsecondHalf, byte targetPiece, bool isTargetSecondHalf)
		{
			if (IsFieldEmpty(targetPiece, isTargetSecondHalf)) return true;
			if (IsWhitePiece(ogPiece, isOGsecondHalf) && IsWhitePiece(targetPiece, isTargetSecondHalf)) return false;
			if (IsBlackPiece(ogPiece, isOGsecondHalf) && IsBlackPiece(targetPiece, isTargetSecondHalf)) return false;
			return true;
		}

		private static bool IsSameRank64IndexFormat(byte i1, byte i2)
		{
			return i1 >> 3 == i2 >> 3;  // last 3 bits is position inside rank
		}

		private static bool IsEdgeFile64IndexFormat(byte posIndex)
		{
			// edge is 00000000 or 00001000 or 00010000 or 00011000 or 00100000 => xxxxx000 or xxxxx111 since 111 is position inside rank
			posIndex <<= 5; // no need to cast because posIndex is byte
							// Check if 11100000 or 00000000
			return posIndex == 0 || posIndex == 0xE0;   // last 3 bits is position inside rank
		}

		private static bool IsEdgeRank64IndexFormat(byte posIndex)
		{
			posIndex >>= 3;
			// Check if 00000000 or 00000111
			return posIndex == 0 || posIndex == 0x07;   // "first" 3 bits is rank
		}

		private static bool CanCastleWhiteQueenside(byte castleOptions) => (castleOptions & 0x88) == 0x88;
		private static bool CanCastleWhiteKingside(byte castleOptions) => (castleOptions & 0x44) == 0x44;
		private static bool CanCastleBlackQueenside(byte castleOptions) => (castleOptions & 0x22) == 0x22;
		private static bool CanCastleBlackKingside(byte castleOptions) => (castleOptions & 0x11) == 0x11;

		private static byte MoveFromIndex(short move) => (byte)((move >> 10) & 0x3F);            // Binary mask: 1111 1100 0000 0000 (not necessary) | Neutralize 128- and 64-bit since their default is 1 (we're shifting to the left)
		private static byte MoveToIndex(short move) => (byte)((move & 0x03F0) >> 4);    // Binary mask: 0000 0011 1111 (0000)
		private static byte MovePieceByte(short move) => (byte)(move & 0x000F);     // Binary mask: 0000 0000 0000 1111
		private static short ToMovePieceUpper(byte From, byte To, byte PieceType) => (short) ((From << 10) | (To << 4) | (PieceType >> 4));       // We trust that the upper most bits of To will always be 0 (128 and 64 bit)
		private static short ToMove(byte From, byte To, byte PieceType = 0x00) => (short) ((From << 10) | (To << 4) | PieceType);     // We trust that the upper most bits of To will always be 0 (128 and 64 bit)

		private static bool IsRank8(byte posIndex) => (posIndex >> 3) == 0;
		private static bool IsRank1(byte posIndex) => (posIndex >> 3) == 7;
		private static bool IsFileA(byte posIndex) => ((byte)(posIndex << 5)) == 0;
		private static bool IsFileH(byte posIndex) => ((byte)(posIndex << 5)) == 0xE0;
		private static bool IsValidIndex(byte posIndex) => (posIndex & 0xC0) == 0;      // Mask 1100 0000 != 0 means >= 64 means invalid index

		// Rank: index >> 3		| => First half
		// File: index & 0x07	| => Second (feinschliff) half, but index is only size 6 (0-64, byte is 0-256)

		#endregion

		#endregion


		internal static string GeneratePositionKey(byte[] position)
		{
			if (position == null) return "null";
			StringBuilder key = new StringBuilder(position.Length * 2);	// String += is inefficient bcs immutable strings
			foreach (byte b in position) key.Append(b.ToString("X2"));	// Since every square is accounted for and every piece has its own Hex character (4 bits), this should provide a unique key in the most effiecient way possible
			return key.ToString();
		}


		// Layout for adding pawn moves (pseudo code):
		//	if (inFront is Clear)
		//	{
		//		addMove (Walk_Forward);
		//		if (rank == 1 (0-based) AND inFront (2) is Clear)
		//		{
		//			addMove (Walk_Forward_Two);
		//		}
		//	}

		/// <summary>
		/// piece in Hex, first byte
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="move"></param>
		/// <param name="differentPiece0xX0"></param>
		/// <returns></returns>
		public static Tuple<byte[], string, byte> ResultingPosition(byte[] Position, short move, byte castleOptions, string currentKey = null)
		{
			// Input Validation not needed, as this is an internal process

			byte moveFrom = MoveFromIndex(move);
			byte moveTo = MoveToIndex(move);

			byte fromIndex = (byte) (moveFrom >> 1); // equivalent to moveFrom / 2
			byte toIndex = (byte) (moveTo >> 1);   // equivalent to moveTo / 2

			byte fromByte = Position[fromIndex];
			byte piece;

			byte[] newPosition = (byte[])Position.Clone();

			// toByte tries to keep the other half while its erased, so both are kept.
			// One could solve this by first merging the bytes, but the simpler solution is: separate case for moving inside 1 index

			if (fromIndex == toIndex)
			{
				// Just move it over
				if ((moveFrom & 1) == 1)
				{
					// 2nd Half will be empty, first half will be the piece
					if ((move & PieceMask) != 0)
					{
						piece = (byte) ((move & PieceMask) << 4);
						newPosition[toIndex] = (byte) ((move & PieceMask) << 4);
					}
					else
					{
						piece = (byte) ((fromByte & firstHalfMask) << 4);  // Move second half to first half
						newPosition[toIndex] = piece;
					}
				}
				else
				{
					// 1st Half will be empty, second half will be the piece
					if ((move & PieceMask) != 0)
					{
						piece = (byte) ((move & PieceMask) << 4);
						newPosition[toIndex] = (byte) (move & PieceMask);
					}
					else
					{
						piece = (byte) (fromByte & firstHalfMask);
						newPosition[toIndex] = (byte) (piece >> 4); // Move first half to second half
					}
				}
			}
			else
			{

				// V2: Immediate Transfer, no questions asked
				if ((moveFrom & 1) == 1)
				{
					newPosition[fromIndex] &= firstHalfMask;
					if ((move & PieceMask) != 0) piece = (byte) (((byte)move & secondHalfMask) << 4);
					else piece = (byte) ((fromByte & secondHalfMask) << 4);
					// 2nd Half
					if ((moveTo & 1) == 1)
					{
						// 2nd Half
						newPosition[toIndex] &= firstHalfMask;		// Erase second half
						newPosition[toIndex] |= (byte) (piece >> 4);	// Copy piece (loc in first half) into second half of toByte
					}
					else
					{
						// 1st Half
						newPosition[toIndex] &= secondHalfMask;		// Erase first half
						newPosition[toIndex] |= piece;	// Copy piece into first half
					}
				}
				else
				{
					newPosition[fromIndex] &= secondHalfMask;   // Erase old first half
					if ((move & PieceMask) != 0) piece = (byte) (((byte)move & secondHalfMask) << 4);
					else piece = (byte) (fromByte & secondHalfMask);

					// 2nd Half
					if ((moveTo & 1) == 1)
					{
						// 2nd Half
						newPosition[toIndex] &= firstHalfMask;
						newPosition[toIndex] |= (byte) (piece >> 4);    // Copy first half of piece into second half of toByte
					}
					else
					{
						// 1st Half
						newPosition[toIndex] &= secondHalfMask;     // Erase first half
						newPosition[toIndex] |= piece;   // Copy first half of piece into first half
					}
				}
			}

			// remove previous en passants

			// If first bit of piece is 1, its black. So, we need to check the white rank (3), since their en passant "expires" now.
			byte mask1, mask2, start;
			if ((piece & 0x80) == 0x80)	// If piece is 1xxx, meaning black
			{
				// Set values for white rank:
				mask1 = 0x70;
				mask2 = 0x07;
				// 3rd rank is en passant black, which is 1111 or 0x0F
				start = 20;
			}
			else
			{
				// Set values for black rank:
				mask1 = 0xF0;
				mask2 = 0x0F;
				// 3rd rank is en passant black, which is 1111 or 0x0F
				start = 8;
			}
			
			for (byte i = start; i < start+4; ++i)   // Indexes 8 - 11 and 20 - 23 (3rd rank and 3rd to last rank)
			{
				byte _piece = newPosition[i];
				if ((_piece & mask1) == mask1) { newPosition[i] &= secondHalfMask; break; }	// If first half is en passant, erase the first half
				if ((_piece & mask2) == mask2) { newPosition[i] &= firstHalfMask; break; } // If second half is en passant, erase the second half
			}

			char[] s = null;

			if (currentKey != null)
			{
				s = currentKey.ToCharArray();
				s[moveTo] = currentKey[moveFrom];
			}

			// Add en passant
			if ((piece & 0x70) == 1)	// Pawn
			{
				// Its always +/- 16
				// Check if they are the same without the 16er-bit => 11101111 => 0xEF
				if((moveFrom & 0xEF) == (moveTo & 0xEF) && moveFrom != moveTo)
				{
					// moved by 16 (2 ranks) and is a pawn => Insert en passant
					byte enPassantIndex = (byte) ((fromIndex + toIndex) >> 1);	// Average of the indexes
					
					// Mask is what we're going to keep, so if its the second half (uneven index) we want the first half masked
					if((moveFrom & 1) == 1)
					{
						// Clear Index not necessary as its guaranteed empty
						// add en passant to 2nd part of the byte and add the coloring bit if piece is also that color
						newPosition[enPassantIndex] += (byte) (0x07 + ((piece >> 4) & 0x08));
						if (s != null) s[(moveFrom + moveTo) >> 1] = '0';	// set to empty
					}
					else
					{
						newPosition[enPassantIndex] += (byte)(0x70 + (piece & 0x80));
						if (s != null) s[(moveFrom + moveTo) >> 1] = '0';   // set to empty
					}
				}
			}

			// Castle: Maybe first half of the byte means temporary no? (attacked)
			// Update Castle Options
			if ((piece & 0x70) == 6)    // Check if King
			{
				if (moveFrom == 60)
				{
					castleOptions &= 0x03;      // Remove bits for 4 and 8 (both white bits) | Erase Castle Options White
				}
				else if (moveFrom == 4)
				{
					castleOptions &= 0x0C;      // Remove bits for 1 and 2 (both black bits) | Erase Castle Options Black
				}
				// Its always +/- 2 if castles
				// Check if they are the same without the 2s-bit => 11111101 => 0xFD
				if ((moveFrom & 0xFD) == (moveTo & 0xFD) && moveFrom != moveTo)
				{
					// 1. move the rook
					// 2. Update castle options
					if (moveTo == 58)  // White Queenside
					{
						newPosition[28] = 0;		// Remove rook	(other field must have been empty for castle to be an option)
						newPosition[29] = 0x64;     // Set 29 to King-Rook combo
					}
					else if (moveTo == 62)  // White Kingside
					{
						newPosition[31] = 0x60;    // Remove rook and add King
						newPosition[30] = 0x04;    // Set 30 to empty-rook combo	(field was prev occupied by king so there's that
					}
					else if (moveTo == 2)  // Black Queenside
					{
						newPosition[0] = 0;        // Remove rook	(other field must have been empty for castle to be an option)
						newPosition[1] = 0x64;     // Set 1 to King-Rook combo
					}
					else if (moveTo == 6)  // Black Kingside
					{
						newPosition[3] = 0x60;    // Remove rook and add King
						newPosition[2] = 0x04;    // Set 2 to empty-rook combo	(field was prev occupied by king so there's that
					}
				}
			}
			// Else if rook
			else if ((piece & 0x70) == 4 && castleOptions != 0)
			{
				if (moveFrom == 56) // If from is white queenside rook
				{
					castleOptions &= 0x07;		// White queenside rook = 8er bit = 8
				}
				else if (moveFrom == 63) // If from is white kingside rook
				{
					castleOptions &= 0x0B;      // White kingside rook = 4er bit = 4 inverse 1011 = F-4 = B
				}
				else if (moveFrom == 0) // If from is black queenside rook
				{
					castleOptions &= 0x0D;      // Black queenside rook = 2er bit = 2 inverse 1101 = F-2 = D
				}
				else if (moveFrom == 7) // If from is black kingside rook
				{
					castleOptions &= 0x0E;      // Black kingside rook = 1er bit = 1 inverse 1110 = F-1 = E
				}
			}

			// Moves aren't applied correctly to the key -> pieces dont disappear

			if (currentKey == null || true)
			{
				currentKey = GeneratePositionKey(newPosition);
			}
			else
			{
				currentKey = new string(s);
			}

			return new Tuple<byte[], string, byte>(newPosition, currentKey, castleOptions);
		}

		public static byte ResultingCastle(byte[] position, byte oldCastle, bool isTurnColorWhite, List<short> AllCurrentLegalMoves = null)
		{
			if(AllCurrentLegalMoves == null) AllCurrentLegalMoves = GetAllLegalMoves(position, isTurnColorWhite);
			return oldCastle;
		}



		private static double BytePieceValue(byte piece2ndHalf)
			=> BytePieceValues[piece2ndHalf & 0x07];	// Only last 3 bits => Mask 00000111 => 7
		private static double[] BytePieceValues =
		{
			// Value, Representation	| Hex  | 2nd Bit Half | Hex Value | Hex Value (Black)
			0,		// Empty			| 0x00 | 000		  | 0		  | (8)
			1,		// Pawn				| 0x01 | 001		  | 1		  | 9
			3,		// Knight			| 0x02 | 010		  | 2		  | A
			3,		// Bishop			| 0x03 | 011		  | 3		  | B
			5,		// Rook				| 0x04 | 100		  | 4		  | C
			9,		// Queen			| 0x05 | 101		  | 5		  | D
			999,	// King				| 0x06 | 110		  | 6		  | E
			1,		// En Passant		| 0x07 | 111		  | 7		  | F
		};


		/// <summary>
		/// KnightPossibleMoves[KnightPositionByte as index] returns AllMoveDestinationsAsBytes[] <br/>
		/// Move Destinations are a 
		/// </summary>
		public static byte[][] KnightPossibleMoves =
		{
			// To override half a byte:
			// byte originalValue = ...; // some original value
			// byte newValueForHigher4Bits = ...; // This should have lower 4 bits as 0.
			// result = (byte)((originalValue & 0x0F) | newValueForHigher4Bits) or (byte)((originalValue & 0xF0) | newValueForLower4Bits)

			// A pinned knight has no moves, an unpinned knight will always have these moves (except if there is an own piece standing there)

			//{ (byte)((0xFF & 0x0F) | newValueForHigher4Bits) },
			// From Field 0: 10, 17
			new byte[2] { 10, 17 },
			// From Field 1: 11, 16, 18
			new byte[3] { 11, 16, 18 },
			// From Field 2: 8, 12, 17, 19
			new byte[4] { 8, 12, 17, 19 },
			// From Field 3: 9, 13, 18, 20
			new byte[4] { 9, 13, 18, 20 },
			// From Field 4: 10, 14, 19, 21
			new byte[4] { 10, 14, 19, 21 },
			// From Field 5: 11, 15, 20, 22
			new byte[4] { 11, 15, 20, 22 },
			// From Field 6: 12, 21, 23
			new byte[3] { 12, 21, 23 },
			// From Field 7: 13, 22
			new byte[2] { 13, 22 },
			// From Field 8: 2, 18, 25
			new byte[3] { 2, 18, 25 },
			// From Field 9: 3, 19, 24, 26
			new byte[4] { 3, 19, 24, 26 },
			// From Field 10: 0, 4, 16, 20, 25, 27
			new byte[6] { 0, 4, 16, 20, 25, 27 },
			// From Field 11: 1, 5, 17, 21, 26, 28
			new byte[6] { 1, 5, 17, 21, 26, 28 },
			// From Field 12: 2, 6, 18, 22, 27, 29
			new byte[6] { 2, 6, 18, 22, 27, 29 },
			// From Field 13: 3, 7, 19, 23, 28, 30
			new byte[6] { 3, 7, 19, 23, 28, 30 },
			// From Field 14: 4, 20, 29, 31
			new byte[4] { 4, 20, 29, 31 },
			// From Field 15: 5, 21, 30
			new byte[3] { 5, 21, 30 },
			// From Field 16: 1, 10, 26, 33
			new byte[4] { 1, 10, 26, 33 },
			// From Field 17: 0, 2, 11, 27, 32, 34
			new byte[6] { 0, 2, 11, 27, 32, 34 },
			// From Field 18: 1, 3, 8, 12, 24, 28, 33, 35
			new byte[8] { 1, 3, 8, 12, 24, 28, 33, 35 },
			// From Field 19: 2, 4, 9, 13, 25, 29, 34, 36
			new byte[8] { 2, 4, 9, 13, 25, 29, 34, 36 },
			// From Field 20: 3, 5, 10, 14, 26, 30, 35, 37
			new byte[8] { 3, 5, 10, 14, 26, 30, 35, 37 },
			// From Field 21: 4, 6, 11, 15, 27, 31, 36, 38
			new byte[8] { 4, 6, 11, 15, 27, 31, 36, 38 },
			// From Field 22: 5, 7, 12, 28, 37, 39
			new byte[6] { 5, 7, 12, 28, 37, 39 },
			// From Field 23: 6, 13, 29, 37
			new byte[4] { 6, 13, 29, 37 },
			// From Field 24: 9, 18, 34, 41
			new byte[4] { 9, 18, 34, 41 },
			// From Field 25: 8, 10, 19, 35, 40, 42
			new byte[6] { 8, 10, 19, 35, 40, 42 },
			// From Field 26: 9, 11, 16, 20, 32, 36, 41, 43
			new byte[8] { 9, 11, 16, 20, 32, 36, 41, 43 },
			// From Field 27: 10, 12, 17, 21, 33, 37, 42, 44
			new byte[8] { 10, 12, 17, 21, 33, 37, 42, 44 },
			// From Field 28: 11, 13, 18, 22, 34, 38, 43, 45
			new byte[8] { 11, 13, 18, 22, 34, 38, 43, 45 },
			// From Field 29: 12, 14, 19, 23, 35, 39, 44, 46
			new byte[8] { 12, 14, 19, 23, 35, 39, 44, 46 },
			// From Field 30: 13, 15, 20, 36, 45, 47
			new byte[6] { 13, 15, 20, 36, 45, 47 },
			// From Field 31: 14, 21, 37, 46
			new byte[4] { 14, 21, 37, 46 },
			// From Field 32: 17, 26, 42, 49
			new byte[4] { 17, 26, 42, 49 },
			// From Field 33: 16, 18, 27, 43, 48, 50
			new byte[6] { 16, 18, 27, 43, 48, 50 },
			// From Field 34: 17, 19, 24, 28, 40, 44, 49, 51
			new byte[8] { 17, 19, 24, 28, 40, 44, 49, 51 },
			// From Field 35: 18, 20, 25, 29, 41, 45, 50, 52
			new byte[8] { 18, 20, 25, 29, 41, 45, 50, 52 },
			// From Field 36: 19, 21, 26, 30, 42, 46, 51, 53
			new byte[8] { 19, 21, 26, 30, 42, 46, 51, 53 },
			// From Field 37: 20, 22, 27, 31, 43, 47, 52, 54
			new byte[8] { 20, 22, 27, 31, 43, 47, 52, 54 },
			// From Field 38: 21, 23, 28, 44, 53, 55
			new byte[6] { 21, 23, 28, 44, 53, 55 },
			// From Field 39: 22, 29, 45, 54
			new byte[4] { 22, 29, 45, 54 },
			// From Field 40: 25, 34, 50, 57
			new byte[4] { 25, 34, 50, 57 },
			// From Field 41: 24, 26, 35, 51, 56, 58
			new byte[6] { 24, 26, 35, 51, 56, 58 },
			// From Field 42: 25, 27, 32, 36, 48, 52, 57, 59
			new byte[8] { 25, 27, 32, 36, 48, 52, 57, 59 },
			// From Field 43: 26, 28, 33, 37, 49, 53, 58, 60
			new byte[8] { 26, 28, 33, 37, 49, 53, 58, 60 },
			// From Field 44: 27, 29, 34, 38, 50, 54, 59, 61
			new byte[8] { 27, 29, 34, 38, 50, 54, 59, 61 },
			// From Field 45: 28, 30, 35, 39, 51, 55, 60, 62
			new byte[8] { 28, 30, 35, 39, 51, 55, 60, 62 },
			// From Field 46: 29, 31, 36, 52, 61, 63
			new byte[6] { 29, 31, 36, 52, 61, 63 },
			// From Field 47: 30, 37, 53, 62
			new byte[4] { 30, 37, 53, 62 },
			// From Field 48: 33, 42, 58
			new byte[3] { 33, 42, 58 },
			// From Field 49: 32, 34, 43, 59
			new byte[4] { 32, 34, 43, 59 },
			// From Field 40: 33, 35, 40, 44, 56, 60
			new byte[6] { 33, 35, 40, 44, 56, 60 },
			// From Field 51: 34, 36, 41, 45, 57, 61
			new byte[6] { 34, 36, 41, 45, 57, 61 },
			// From Field 52: 35, 37, 42, 46, 58, 62
			new byte[6] { 35, 37, 42, 46, 58, 62 },
			// From Field 53: 36, 38, 43, 47, 59, 63
			new byte[6] { 36, 38, 43, 47, 59, 63 },
			// From Field 54: 37, 39, 44, 60
			new byte[4] { 37, 39, 44, 60 },
			// From Field 55: 38, 45, 61
			new byte[3] { 38, 45, 61 },
			// From Field 56: 41, 50
			new byte[2] { 41, 50 },
			// From Field 57: 40, 42, 51
			new byte[3] { 40, 42, 51 },
			// From Field 58: 41, 43, 48, 52
			new byte[4] { 41, 43, 48, 52 },
			// From Field 59: 42, 44, 49, 53
			new byte[4] { 42, 44, 49, 53 },
			// From Field 60: 43, 45, 50, 54
			new byte[4] { 43, 45, 50, 54 },
			// From Field 61: 44, 46, 51, 55
			new byte[4] { 44, 46, 51, 55 },
			// From Field 62: 45, 47, 52
			new byte[3] { 45, 47, 52 },
			// From Field 63: 46, 53
			new byte[2] { 46, 53 }
		};
	}
}