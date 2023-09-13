using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ChessV1.Stormcloud.Connect4
{

	internal struct Move
	{
		public readonly int Column;
		public readonly int Row;
		public readonly double Eval;
		public readonly string EvaluationResult;
		public readonly long BinaryMove;
		public readonly double TimeSecs;

		public Move(int Column, int Row, double Eval, string EvaluationResult, long BinaryMove, double TimeSecs)
		{
			this.Column = Column;
			this.Row = Row;
			this.Eval = Eval;
			this.EvaluationResult = EvaluationResult;
			this.BinaryMove = BinaryMove;
			this.TimeSecs = TimeSecs;
		}
	}

	internal class Connect4Engine
	{

		private readonly int BOARD_MAX;
		private readonly int Rows, Columns;
		private readonly long MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2;

		public Connect4Engine(int Rows, int Columns, int maxDepth, long MASK_ROW, long MASK_COL, long MASK_DIAG1, long MASK_DIAG2)
		{
			this.Rows = Rows;
			this.Columns = Columns;
			this.BOARD_MAX = Rows * Columns - 1;
			this.CC_FinalDepth = maxDepth; //(maxDepth & 1) == 1 ? maxDepth + 1 : maxDepth;
			this.MASK_ROW = MASK_ROW;
			this.MASK_COL = MASK_COL;
			this.MASK_DIAG1 = MASK_DIAG1;
			this.MASK_DIAG2 = MASK_DIAG2;
		}

		private string Evaluation
		{
			get
			{
				if (CC_Eval > WinValue - CC_FinalDepth) return $"M{WinValue - CC_Eval}";
				if (CC_Eval < LossValue + CC_FinalDepth) return $"-M{LossValue - CC_Eval}";
				return CC_Eval <= 0 ? "" + CC_Eval : "+" + CC_Eval;
			}
		}

		public bool IsInUse = false;	// Add multithreading

		public Move BestMove(long myBoard, long opponentBoard, int maxDepth = -1, int maxMS = -1)
		{
			if(IsInUse)
			{
				Console.Error.WriteLine("Engine Object is in use right now, please wait until calculations are complete or create another instance.");
				return new Move();
			}
			DateTime before = DateTime.Now;
			int oldDepth = CC_FinalDepth;
			IsInUse = true;
			if(maxDepth >= 2) CC_FinalDepth = maxDepth; //(maxDepth & 1) == 1 ? maxDepth + 1 : maxDepth;
			CC_Eval = 0;
			CC_Failsoft_BestMove = 0L;
			//CC_IterativeDeepening(myBoard, opponentBoard);
			do
			{
				CC_FailsoftAlphaBeta(myBoard, opponentBoard, CC_FinalDepth);
				CC_FinalDepth++; // Repeat until time is reached
			} while ((DateTime.Now - before).TotalMilliseconds < maxMS);	// run once is maxMS == -1, so no time limit

			// Reset Data

			CC_FinalDepth = oldDepth;
			IsInUse = false;

			// Find column
			int col = -1, row = -1;
			for(int i = 0; i < Rows * Columns; i++)
			{
				if (((CC_Failsoft_BestMove >> i) & 1) == 0) continue;
				// Found the move, i is the index
				col = Columns - i % Columns;
				row = i / Columns + 1;
			}

			return new Move(col, row, CC_Eval, Evaluation, CC_Failsoft_BestMove, (DateTime.Now - before).TotalSeconds);
		}

		// Stormcloud 3 AlphaBeta Algorithm but lighter

		private long CC_Failsoft_BestMove;
		private const int WinValue = 999999;
		private const int LossValue = -999999;
		private int CC_FinalDepth;
		private double CC_Eval;

		private long calls;

		private void CC_IterativeDeepening(long myBoard, long opponentBoard)
		{
			for (int depth = 2; depth <= CC_FinalDepth; depth += 2)
			{
				DateTime before = DateTime.Now;
				calls = 0;
				CC_Eval = CC_FailsoftAlphaBeta(myBoard, opponentBoard, depth);
				DateTime after = DateTime.Now;
				Console.WriteLine($"[{after}] Depth: {depth} | BestField: {Convert.ToString(CC_Failsoft_BestMove, 2)} (Eval: {Evaluation}) | Time: {(after-before).TotalSeconds}s");
				System.Diagnostics.Debug.WriteLine($"[{after}] Calls: {calls} | Depth: {depth} | BestField: {Convert.ToString(CC_Failsoft_BestMove, 2)} (Eval: {Evaluation}) | Time: {(after-before).TotalSeconds}s");
			}
		}
		/*
		private const double WEIGHT_SCORE_OWN = 1.1;
		private const double WEIGHT_SCORE_OPPONENT = 1.03;
		private const double WEIGHT_HAMMINGDISTANCE_OWN = 0.9;
		private const double WEIGHT_HAMMINGDISTANCE_OPPONENT = 0.75;

		private const double WEIGHT_WALL_DISTANCE = 0.15;
		private const double WEIGHT_NEIGHBORS = 0.67;	// Overall neighbor weight
		private const double WEIGHT_NEIGHBOR_FREE = 1.0;	// Available
		private const double WEIGHT_NEIGHBOR_OWNED = 1.1;	// Owned by me
		private const double WEIGHT_NEIGHBOR_TAKEN = -0.9;  // Taken by opponent
															//*/
		//* I'm pretty sure there are fairly good weights
		private const double WEIGHT_SCORE_OWN = 1.2;                    // yes: 1.2
		private const double WEIGHT_SCORE_OPPONENT = 1.03;              // yes: 1.03
		private const double WEIGHT_HAMMINGDISTANCE_OWN = 0.9;          // yes: 0.9
		private const double WEIGHT_HAMMINGDISTANCE_OPPONENT = 1.35;	// yes: 1.05

		private const double WEIGHT_WALL_DISTANCE = 0.32;
		private const double WEIGHT_NEIGHBORS = 0.67;	// Overall neighbor weight
		private const double WEIGHT_NEIGHBOR_FREE = 1.0;	// Available
		private const double WEIGHT_NEIGHBOR_OWNED = 1.1;	// Owned by me
		private const double WEIGHT_NEIGHBOR_TAKEN = -0.9;	// Taken by opponent
		//*/
		private double Evaluate(long myBoard, long opponentBoard)
		{
			double myScore = 0, opponentScore = 0;

			// Use the win preset but count Hamming Distance
			// Mask Width is 4, number of transitions: columns - 4, number of runs: columns - 4 + 1 for run before first transition
			void ApplyMask(long position, long opponentBoardCurrent, long maskTemplate, int horizontalRuns, int verticalRuns, double weight, ref double score)
			{
				// horizontalRuns:	Columns -4 mask width +1 for initial run
				// verticalRuns:	usually 4-1, but now we need the height of the mask, which may be different.
				for (int i = 0; i < verticalRuns; i++)
				{
					long mask = maskTemplate << (i * Columns);  // Shift by 1 row, so 1 rowwidth, so amount of columns
					for (int i2 = 0; i2 < horizontalRuns; i2++)
					{
						long mash = (position & mask);
						long mashTotal = ((position | opponentBoardCurrent) & mask);
						int distance = HammingDistance(mash, mask);
						int distanceTotal = HammingDistance(mashTotal, mask);
						score += distance * weight;
						score -= distanceTotal-distance * 2 * weight;
						mask <<= 1;
					}
				}
			}

			void ApplyMasks(long boardstate, long opponentBoardCurrent, double weight, ref double score)
			{
				ApplyMask(boardstate, opponentBoardCurrent, MASK_ROW, Columns - 3, Rows, weight, ref score);
				ApplyMask(boardstate, opponentBoardCurrent, MASK_COL, Columns, Rows - 3, weight, ref score);
				ApplyMask(boardstate, opponentBoardCurrent, MASK_DIAG1, Columns - 3, Rows - 3, weight, ref score);
				ApplyMask(boardstate, opponentBoardCurrent, MASK_DIAG2, Columns - 3, Rows - 3, weight, ref score);
			}

			ApplyMasks(myBoard, opponentBoard, WEIGHT_HAMMINGDISTANCE_OWN, ref myScore);
			ApplyMasks(opponentBoard, myBoard, WEIGHT_HAMMINGDISTANCE_OPPONENT, ref opponentScore);

			double finalScore = 0;
			finalScore += myScore * WEIGHT_SCORE_OWN;
			finalScore += opponentScore * WEIGHT_SCORE_OPPONENT;

			for (var reverseIndex = 0; reverseIndex <= BOARD_MAX; reverseIndex++)
			{
				if (((myBoard >> reverseIndex) & 1) == 0) continue;	// Not my field
				finalScore += Score_WallDistance(reverseIndex, WEIGHT_WALL_DISTANCE);
				finalScore += WEIGHT_NEIGHBORS * Score_Neighbors(reverseIndex, myBoard, opponentBoard, WEIGHT_NEIGHBOR_FREE, WEIGHT_NEIGHBOR_OWNED, WEIGHT_NEIGHBOR_TAKEN);
			}

			return finalScore;
		}

		private const double OPTION_DISTANCE_TO_WALL = 0.1; // per fields away from the wall, adjacent fields are 0.1
		private const double OPTION_NEIGHBOR_IS_OWN = 0.9;
		private const double OPTION_NEIGHBOR_IS_EMPTY = 0.5;
		private const double OPTION_NEIGHBOR_IS_TAKEN = -0.2;

		private double Score_WallDistance(int reverseIndex, double WEIGHT, int col = -1)
		{
			if(col == -1) col = reverseIndex % (BOARD_MAX + 1);
			double wallDistance = Math.Min(col, Columns - 1 - col);     // Get distance to both walls and pick the nearest, minimum is 1

			return wallDistance * WEIGHT;
		}

		private double Score_Neighbors(int reverseIndex, long myBoard, long opponentBoard, double WEIGHT_NEIGHBOR_FREE, double WEIGHT_NEIGHBOR_OWNED, double WEIGHT_NEIGHBOR_OCCUPIED, int col = -1, int row = -1)
		{
			if(col == -1) col = reverseIndex % (BOARD_MAX + 1);
			if(row == -1) row = reverseIndex / (BOARD_MAX + 1);

			double score = 0.0;

			if (col > 0)
			{
				if ((myBoard >> (reverseIndex - 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OWNED;
				else if ((opponentBoard >> (reverseIndex - 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OCCUPIED;
				else score += WEIGHT_NEIGHBOR_FREE;


				if (row > 0)
				{
					if ((myBoard >> (reverseIndex + Columns - 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OWNED;
					else if ((opponentBoard >> (reverseIndex + Columns - 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OCCUPIED;
					else score += WEIGHT_NEIGHBOR_FREE;
				}
				if (row < Rows - 1) // Columns = Row Width
				{
					if ((myBoard >> (reverseIndex - Columns - 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OWNED;
					else if ((opponentBoard >> (reverseIndex - Columns - 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OCCUPIED;
					else score += WEIGHT_NEIGHBOR_FREE;
				}

			}

			if (col < Columns - 1)
			{
				if ((myBoard >> (reverseIndex + 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OWNED;
				else if ((opponentBoard >> (reverseIndex + 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OCCUPIED;
				else score += WEIGHT_NEIGHBOR_FREE;


				if (row > 0)
				{
					if ((myBoard >> (reverseIndex + Columns + 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OWNED;
					else if ((opponentBoard >> (reverseIndex + Columns + 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OCCUPIED;
					else score += WEIGHT_NEIGHBOR_FREE;
				}
				if (row < Rows - 1) // Columns = Row Width
				{
					if ((myBoard >> (reverseIndex - Columns + 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OWNED;
					else if ((opponentBoard >> (reverseIndex - Columns + 1) & 1) == 1) score += WEIGHT_NEIGHBOR_OCCUPIED;
					else score += WEIGHT_NEIGHBOR_FREE;
				}
			}

			if (row > 0)
			{
				if ((myBoard >> (reverseIndex + Columns) & 1) == 1) score += WEIGHT_NEIGHBOR_OWNED;
				else if ((opponentBoard >> (reverseIndex + Columns) & 1) == 1) score += WEIGHT_NEIGHBOR_OCCUPIED;
				else score += WEIGHT_NEIGHBOR_FREE;
			}
			if (row < Rows - 1) // Columns = Row Width
			{
				if ((myBoard >> (reverseIndex - Columns) & 1) == 1) score += WEIGHT_NEIGHBOR_OWNED;
				else if ((opponentBoard >> (reverseIndex - Columns) & 1) == 1) score += WEIGHT_NEIGHBOR_OCCUPIED;
				else score += WEIGHT_NEIGHBOR_FREE;
			}

			return score;
		}

		private List<long> OrderMoves(List<long> moves, long myBoard, long opponentBoard)
		{
			Dictionary<long, double> ScoreMap = new Dictionary<long, double>();

			foreach (var move in moves)
			{
				if (ScoreMap.ContainsKey(move)) continue;
				int reverseIndex = 0;
				for (int i = 0; i <= BOARD_MAX; i++)
				{
					if (((move >> i) & 1) == 1) break;	// So last BOARD_MAX == 42
					reverseIndex++;
				}

				// Now that we have the index, we can check wall
				int col = reverseIndex % (BOARD_MAX + 1); // 1 removed for 0-base, now we need 1-base though
				int row = reverseIndex / (BOARD_MAX + 1);

				double score = Score_WallDistance(reverseIndex, OPTION_DISTANCE_TO_WALL, col);

				score += Score_Neighbors(reverseIndex, myBoard, opponentBoard, OPTION_NEIGHBOR_IS_EMPTY,
					OPTION_NEIGHBOR_IS_OWN, OPTION_NEIGHBOR_IS_TAKEN, col, row);

				ScoreMap.Add(move, score);
			}

			return ScoreMap.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
		}

		double CC_FailsoftAlphaBeta(long myBoard, long opponentBoard, int FinalDepth)
			=> CC_FailsoftAlphaBeta(LossValue - 1, WinValue + 1, myBoard, opponentBoard, FinalDepth, null, true);
		
		double CC_FailsoftAlphaBeta(long myBoard, long opponentBoard)
			=> CC_FailsoftAlphaBeta(LossValue - 1, WinValue + 1, myBoard, opponentBoard, CC_FinalDepth, null, true);

		double CC_FailsoftAlphaBeta(double alpha, double beta, long myBoard, long opponentBoard, int depthLeft, List<long> AllLegalMoves = null, bool isRoot = false)
		{
			calls++;
			if (depthLeft < 0) return Evaluate(myBoard, opponentBoard);
			double bestscore = double.NegativeInfinity;    // This might still be glitchy, on insufficient moves it just caused an immediate return.

			if (AllLegalMoves == null)
			{
				AllLegalMoves = LegalMoves(myBoard, opponentBoard);
			}

			/*
			if (isRoot)
			{
				Console.WriteLine("Movelist pre-ordering:");
				for (int i = 0; i < AllLegalMoves.Count; i++)
				{
					Console.WriteLine($"{i + 1}/{AllLegalMoves.Count}. {Convert.ToString(AllLegalMoves[i], 2)}");
				}
			}*/
			AllLegalMoves = OrderMoves(AllLegalMoves, myBoard, opponentBoard);
			//*
			if (isRoot && Connect4UI.move == 35)
			{
				Console.WriteLine("Movelist post-ordering:");
				for (int i = 0; i < AllLegalMoves.Count; i++)
				{
					Console.WriteLine($"{i + 1}/{AllLegalMoves.Count}. {Convert.ToString(AllLegalMoves[i], 2)}");
				}
			}//*/

			if (Connect4UI.move == 35 && isRoot)
			{
				Console.WriteLine($"Calculating move 35... | Moves: {AllLegalMoves.Count}");
			}

			var result = PlayerWon(myBoard, opponentBoard);
			if (result == 1) return WinValue - CC_FinalDepth + depthLeft;	// Win, Quicker is better
			if (result == 2) return LossValue + CC_FinalDepth - depthLeft;	// Loss. Adding of FinalDepth is important because then LossValue is the minimum. when bestscore is initialized to LossValue and its forcedMate, this can cause issues with bestMove, as every move would be worse than doing nothing, so move stay 0 (invalid)
			if (result == 0) return 0;   // Draw

			if (AllLegalMoves.Count == 0) return 0; // Draw by no moves, do NOT evaluate. No moves = All tiles have been filled. This is a draw.

			//if (isRoot) CC_Failsoft_BestMove = AllLegalMoves[0];

			foreach (var move in AllLegalMoves)
			{
				//if(isRoot) Console.WriteLine($"1. Evaluating Binary Move: {Convert.ToString(move, 2)} | myBoard: {Convert.ToString(myBoard, 2)}");
				// Do move
				myBoard ^= move;
				//if (isRoot) Console.WriteLine($"2. Evaluating Binary Move: {Convert.ToString(move, 2)} | myBoard: {Convert.ToString(myBoard, 2)}");
				// Evaluate move
				double score = -CC_FailsoftAlphaBeta(-beta, -alpha, opponentBoard, myBoard, depthLeft-1);
				if(isRoot && Connect4UI.move == 35) Console.WriteLine($"Score: {score} | move: {Convert.ToString(move, 2)}");
				// Undo move
				myBoard ^= move;
				//if (isRoot) Console.WriteLine($"3. Evaluating Binary Move: {Convert.ToString(move, 2)} | myBoard: {Convert.ToString(myBoard, 2)}");

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


			if (Connect4UI.move == 35 && isRoot)
			{
				Console.WriteLine($"Calculating move 35... | moves: {AllLegalMoves.Count} | bestscore: {bestscore} | bestMove: {Convert.ToString(CC_Failsoft_BestMove)}");
			}


			return bestscore;
		}

		// code by GPT-4
		public static int HammingDistance(long a, long b)
		{
			long xorResult = a ^ b;
			int count = 0;
			while (xorResult != 0)
			{
				count += (int)(xorResult & 1);
				xorResult >>= 1;
			}
			return count;
		}


		// Bug: Row isn't properly checked for
		private List<long> LegalMoves(long myBoard, long opponentBoard, bool broadcast = false)
		{
			// Build a list of available moves
			List<long> moves = new List<long>();

			// Go through each column, then bottom-up and note the first move available
			long state = myBoard | opponentBoard;
			for (int col = 0; col < Columns; col++)
			{
				int row = -1, reverseIndex = -1;
				//for (int i = Rows - 1; i >= 0; i--)
				if(broadcast) Console.WriteLine("Testing the Rows...");
				for (int i = 0; i < Rows; i++)
				{
					// Slot
					reverseIndex = /*BOARD_MAX - */(i * Columns + col);    // Index is where it is, reverse it how much shift I need to bring it to the back
					if (broadcast) Console.WriteLine($"i: {i}, col: {col} | state: {Convert.ToString(myBoard, 2)} | {Convert.ToString(opponentBoard, 2)} >> {Convert.ToString(state, 2)}");
					if (broadcast) Console.WriteLine($"ReverseIndex: {reverseIndex}: {Convert.ToString(state >> reverseIndex, 2)} - {((state >> reverseIndex) & 1) == 1}");
					if (((state >> reverseIndex) & 1) == 1) continue;
					row = i;
					break;
				}

				if (row == -1) continue;
				// valid row -> valid move -> add it
				long binaryMove = 1L << reverseIndex;
				if (broadcast) Console.WriteLine($"[Final] row: {row}, col: {col} | reverse: {reverseIndex} | binaryMove: {Convert.ToString(binaryMove, 2)} | state: {Convert.ToString(myBoard, 2)} | {Convert.ToString(opponentBoard, 2)} >> {Convert.ToString(state, 2)}");
				moves.Add(binaryMove);
				
				//*
				if (broadcast)
				{
					Console.WriteLine($"Adding Legal Move {Convert.ToString(binaryMove, 2)} | Col: {col} Row: {row} | ReverseIndex {Convert.ToString(reverseIndex, 2)}");
					for (int i = 0; i < moves.Count; i++)
					{
						Console.WriteLine($"{(i + 1)}/{moves.Count} >> {Convert.ToString(moves[i], 2)}");
					}
				}//*/
			}

			return moves;
		}

		#region Sophisticated Win Detection

		int PlayerWon(long myBoard, long opponentBoard) => PlayerWon(myBoard, opponentBoard, Rows, Columns, MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2);

		/// <summary>
		/// Values: <br/>
		/// -1: Game is still going <br/>
		/// 0: Draw <br/>
		/// 1: Player 1 won <br/>
		/// 2: Player 2 won <br/>
		/// </summary>
		/// <returns></returns>
		public static int PlayerWon(long myBoard, long opponentBoard, int Rows, int Columns, long MASK_ROW, long MASK_COL, long MASK_DIAG1, long MASK_DIAG2)
		{
			// Mask Width is 4, number of transitions: columns - 4, number of runs: columns - 4 + 1 for run before first transition
			bool applyMask(long position, long maskTemplate, int horizontalRuns, int verticalRuns)
			{
				// horizontalRuns:	Columns -4 mask width +1 for initial run
				// verticalRuns:	usually 4-1, but now we need the height of the mask, which may be different.
				for (int i = 0; i < verticalRuns; i++)
				{
					long mask = maskTemplate << (i * Columns);  // Shift by 1 row, so 1 rowwidth, so amount of columns
					for (int i2 = 0; i2 < horizontalRuns; i2++)
					{
						if ((position & mask) == mask) return true;
						mask <<= 1;
					}
				}
				return false;
			}

			bool applyMasks(long boardstate)
			{
				if (applyMask(boardstate, MASK_ROW, Columns - 3, Rows)) return true;
				if (applyMask(boardstate, MASK_COL, Columns, Rows - 3)) return true;
				if (applyMask(boardstate, MASK_DIAG1, Columns - 3, Rows - 3)) return true;
				if (applyMask(boardstate, MASK_DIAG2, Columns - 3, Rows - 3)) return true;
				return false;
			}

			if (applyMasks(myBoard)) return 1;
			if (applyMasks(opponentBoard)) return 2;

			// Check if board is full
			if ((myBoard | opponentBoard) == (1L << (Rows * Columns))-1L) return 0;    // No more moves
			return -1;
		}

		public static int PlayerWon2(long myBoard, long opponentBoard, int Rows, int Columns, long MASK_ROW, long MASK_COL, long MASK_DIAG1, long MASK_DIAG2, ref long WinnerMatrix)
		{
			long matrix = 0L;
			// Mask Width is 4, number of transitions: columns - 4, number of runs: columns - 4 + 1 for run before first transition
			bool applyMask(long position, long maskTemplate, int horizontalRuns, int verticalRuns)
			{
				// horizontalRuns:	Columns -4 mask width +1 for initial run
				// verticalRuns:	usually 4-1, but now we need the height of the mask, which may be different.
				for (int i = 0; i < verticalRuns; i++)
				{
					long mask = maskTemplate << (i * Columns);  // Shift by 1 row, so 1 rowwidth, so amount of columns
					for (int i2 = 0; i2 < horizontalRuns; i2++)
					{
						if ((position & mask) == mask)
						{
							matrix = mask;
							return true;
						}
						mask <<= 1;
					}
				}
				return false;
			}

			bool applyMasks(long boardstate)
			{
				if (applyMask(boardstate, MASK_ROW, Columns - 3, Rows)) return true;
				if (applyMask(boardstate, MASK_COL, Columns, Rows - 3)) return true;
				if (applyMask(boardstate, MASK_DIAG1, Columns - 3, Rows - 3)) return true;
				if (applyMask(boardstate, MASK_DIAG2, Columns - 3, Rows - 3)) return true;
				return false;
			}

			if (applyMasks(myBoard)) { WinnerMatrix = matrix; return 1; }
			if (applyMasks(opponentBoard)) { WinnerMatrix = matrix; return 2; }

			// Check if board is full
			if ((myBoard | opponentBoard) == (1L << (Rows * Columns)) - 1L) return 0;    // No more moves
			return -1;
		}

		#endregion
	}
}
