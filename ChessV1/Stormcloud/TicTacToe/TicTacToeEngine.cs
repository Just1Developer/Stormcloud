using System;
using System.Collections.Generic;

namespace ChessV1.Stormcloud.TicTacToe
{
	internal class TicTacToeEngine
	{

		public TicTacToeEngine(short myBoard, short opponentBoard)
		{
			CC_FinalDepth = 9;
			CC_Eval = 0;
			CC_IterativeDeepening(myBoard, opponentBoard);
		}

		public short BestMove
		{
			get
			{
				// Convert
				for (short i = 0; i < 9; i++)
				{
					if ((CC_Failsoft_BestMove >> i) == 1) return i;
				}

				return 9;	// Invalid move
			}
		}

		public string Evaluation
		{
			get
			{
				if (CC_Eval > WinValue - CC_FinalDepth) return $"M{WinValue - CC_Eval}";
				if (CC_Eval < LossValue + CC_FinalDepth) return $"-M{LossValue - CC_Eval}";
				else return CC_Eval <= 0 ? "" + CC_Eval : "+" + CC_Eval;
			}
		}


		// Stormcloud 3 AlphaBeta Algorithm but lighter

		private short CC_Failsoft_BestMove;
		private const int WinValue = 999999;
		private const int LossValue = -999999;
		private int CC_FinalDepth;
		private int CC_Eval;

		private void CC_IterativeDeepening(short myBoard, short opponentBoard)
		{
			for (int depth = 2; depth <= CC_FinalDepth; depth++)
			{
				DateTime before = DateTime.Now;
				CC_Eval = CC_FailsoftAlphaBeta(myBoard, opponentBoard, depth);
				DateTime after = DateTime.Now;
				//Console.WriteLine($"[{after}] Depth: {depth} | BestField: {Convert.ToString(CC_Failsoft_BestMove, 2)} (Eval: {Evaluation}) | Time: {(after-before).TotalSeconds}s");
			}
		}

		int CC_FailsoftAlphaBeta(short myBoard, short opponentBoard, int FinalDepth)
			=> CC_FailsoftAlphaBeta(LossValue - 1, WinValue + 1, myBoard, opponentBoard, FinalDepth, null, true);

		int CC_FailsoftAlphaBeta(double alpha, double beta, short myBoard, short opponentBoard, int depthLeft, List<short> AllLegalMoves = null, bool isRoot = false)
		{
			int bestscore = LossValue;    // This might still be glitchy, on insufficient moves it just caused an immediate return.

			if (AllLegalMoves == null)
			{
				AllLegalMoves = LegalMoves(myBoard, opponentBoard);
			}

			var result = playerWon(myBoard, opponentBoard);
			if (result == 1) return WinValue - CC_FinalDepth + depthLeft;	// Win, Quicker is better
			if (result == 2) return LossValue + CC_FinalDepth - depthLeft;	// Loss
			if (result == 0) return 0;   // Draw

			foreach (var move in AllLegalMoves)
			{
				// Do move
				myBoard ^= move;
				// Evaluate move
				int score = -CC_FailsoftAlphaBeta(-beta, - alpha, opponentBoard, myBoard, depthLeft);
				// Undo move
				myBoard ^= move;

				if (score >= beta)
				{
					if (isRoot)
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
						if (isRoot)
						{
							CC_Failsoft_BestMove = move;
						}
					}
				}
			}

			return bestscore;
		}

		private List<short> LegalMoves(short myBoard, short opponentBoard)
		{
			short squares = (short)((myBoard | opponentBoard) ^ 0x01FF); // Combine the boards and XOR with 111111111

			// Build a list of available moves
			List<short> moves = new List<short>();
			if (squares == 0) return moves;

			// For each bit position
			for (int i = 0; i < 9; i++)
			{
				short move = (short)(1 << i);
				// If the bit at position i is set (available move), add it
				if ((squares & move) != 0)
					moves.Add(move);
			}

			return moves;
		}


		#region Lazy Win Detection

		/// <summary>
		/// Values: <br/>
		/// -1: Game is still going <br/>
		/// 0: Draw <br/>
		/// 1: Player won <br/>
		/// 2: Opponent won <br/>
		/// </summary>
		/// <returns></returns>
		int playerWon(short myBoard, short opponentBoard)
		{
			// Check win for white
			bool maskApplies(short pos, short mask) => (pos & mask) == mask;

			foreach (var mask in masks) if (maskApplies(myBoard, mask)) return 1;
			foreach (var mask in masks) if (maskApplies(opponentBoard, mask)) return 2;

			// Check if board is full
			if (maskApplies((short) (myBoard | opponentBoard), 0x01FF)) return 0;
			return -1;
		}

		private const short mask_row1 = 0x0007; // 000000111 | 0 0000 0111
		private const short mask_row2 = 0x0038; // 000111000 | 0 0011 1000
		private const short mask_row3 = 0x01C0; // 111000000 | 1 1100 0000
		private const short mask_col1 = 0x0124; // 100100100 | 1 0010 0100
		private const short mask_col2 = 0x0092; // 010010010 | 0 1001 0010
		private const short mask_col3 = 0x0049; // 001001001 | 0 0100 1001
		private const short mask_diag1 = 0x0111; // 100010001 | 1 0001 0001
		private const short mask_diag2 = 0x0054; // 001010100 | 0 0101 0100

		private readonly short[] masks =
			{ mask_row1, mask_row2, mask_row3, mask_col1, mask_col2, mask_col3, mask_diag1, mask_diag2 };

		#endregion
	}
}
