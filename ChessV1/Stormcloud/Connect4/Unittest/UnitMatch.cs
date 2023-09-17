using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Connect4.Unittest
{
	internal static class UnitMatch
	{
		private static int Rows, Columns, EngineDepth;

		public static void Setup(int Rows, int Columns, int EngineDepth)
		{
			UnitMatch.Rows = Rows;
			UnitMatch.Columns = Columns;
			UnitMatch.EngineDepth = EngineDepth;
			Boardsize = Columns + " x " + Rows;
			SetMasks();
		}

		public static string Boardsize { get; private set; }
		public static int _EngineDepth { get => EngineDepth; }

		public static int PlayMatch(EngineWeightMap YellowMap, EngineWeightMap RedMap)
			=> PlayMatch(
				new Connect4Engine(Rows, Columns, EngineDepth, MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2, YellowMap,
					Silenced: true),
				new Connect4Engine(Rows, Columns, EngineDepth, MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2, RedMap,
					Silenced: true));
		public static int PlayMatch(Connect4Engine EngineYellow, Connect4Engine EngineRed)
		{
			long BoardstateYellow = 0L, BoardstateRed = 0L;
			bool IsYellow = true;
			int currentWinResult;
			do
			{
				//int isMate;
				if(IsYellow) /*isMate = */PlayMoveComputerNonRecursive(ref BoardstateYellow, ref BoardstateRed, EngineYellow);	// probably dont need ref for the engine
				else /*isMate = */ PlayMoveComputerNonRecursive(ref BoardstateRed, ref BoardstateYellow, EngineRed);
				IsYellow = !IsYellow;

				// Resign if Mx where x <= EngineDepth-2
				/*
				if (isMate != 0)
				{
					// Resignation, set win result
					currentWinResult = isMate;
					break;
				}
				//*/

				currentWinResult = PlayerWon(BoardstateYellow, BoardstateRed);
			} while (currentWinResult == -1);

			return currentWinResult;
		}

		static void/*int*/ PlayMoveComputerNonRecursive(ref long myBoard, ref long opponentBoard, Connect4Engine Engine/*, bool IsYellow*/)
		{
			Move bestMove = Engine.BestMove(myBoard, opponentBoard, maxDepth: EngineDepth);
			myBoard ^= bestMove.BinaryMove;
			/* Resign mechanics (buggy)
			bool mate = Math.Abs(bestMove.Eval) >= Connect4Engine.WinValue - EngineDepth + 2;   // 2 = Mate-Visibility-Threshold
			int result;
			if (IsYellow) result = mate ? bestMove.Eval < 0 ? 2 : 1 : 0;	// This needs to display in absolute who won
			else result = mate ? bestMove.Eval < 0 ? 1 : 2 : 0;
			return result;
			//*/
			// Return values: 0=Game ongoing, 1 = I will win, 2 = Opponent will win
		}

		#region WinDetection from UI class

		static int PlayerWon(long BoardstateYellow, long BoardstateRed) => Connect4Engine.PlayerWon(BoardstateYellow, BoardstateRed, Rows, Columns, MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2);

		private const short MASK_ROW = 0x000F; // 1111 | just shift around as needed
		private static long MASK_COL;
		private static long MASK_DIAG1;
		private static long MASK_DIAG2;

		// Masks need to be set because anything transcending own rows need to know how wide a row is
		static void SetMasks()
		{
			int cols = Columns; // Leave space for the 1 in the column
			// Basically: value is 1, shift so much across << that new line, then add 1 again, shift until 4
			MASK_COL = (((((1 << cols) | 1) << cols) | 1) << cols) | 1;

			// Now do the same for +/- 1 each time for the diagnonal
			cols--; // Shift 1 less for the diagonal going upwards L-R. Only thing is this will need leading 0s, but it's got that. Just don't forget
			// We skipped 3 0s that are quite essential here (I think), so let's add them to the back
			MASK_DIAG1 = ((((((1 << cols) | 1) << cols) | 1) << cols) | 1) << 3;

			cols += 2;
			// Now to +1, since Columns is +1, we can just use that instead
			MASK_DIAG2 = (((((1 << cols) | 1) << cols) | 1) << cols) | 1;
		}

		#endregion

	}
}
