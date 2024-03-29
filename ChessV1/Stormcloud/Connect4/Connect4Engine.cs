﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

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
		public readonly int FinalDepth;

		public Move(int Column, int Row, double Eval, string EvaluationResult, long BinaryMove, double TimeSecs, int FinalDepth)
		{
			this.Column = Column;
			this.Row = Row;
			this.Eval = Eval;
			this.EvaluationResult = EvaluationResult;
			this.BinaryMove = BinaryMove;
			this.TimeSecs = TimeSecs;
			this.FinalDepth = FinalDepth;
		}
	}

	internal class Connect4Engine
	{
		public bool Silenced = false;

		private static byte Evaluation_MThreshold = 100;	// This close to the absolute M0 value to be displayed as Mx
		private const double EVALUATION_DIVIDER = 2;	// Actual divider is *10, old eval uses 85 to achieve 850
		private readonly int BOARD_MAX;
		private readonly int Rows, Columns;
		private readonly long MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2;
		
		public Connect4Engine(int Rows, int Columns, int maxDepth, long MASK_ROW, long MASK_COL, long MASK_DIAG1, long MASK_DIAG2, EngineWeightMap WeightMap = null, bool Silenced = false)
		{
			this.Silenced = Silenced;
			this.Rows = Rows;
			this.Columns = Columns;
			this.BOARD_MAX = Rows * Columns - 1;
			this.CC_FinalDepth = maxDepth;
			this.MASK_ROW = MASK_ROW;
			this.MASK_COL = MASK_COL;
			this.MASK_DIAG1 = MASK_DIAG1;
			this.MASK_DIAG2 = MASK_DIAG2;
			this.WeightMap = WeightMap ?? EngineWeightMap.HighestEloEngineBoard;
			SetMasks();
		}

		private long MASK_FORK_ROW_L = 0b10001, MASK_FORK_COL_L, MASK_FORK_DIAG1_L, MASK_FORK_DIAG2_L,
			MASK_FORK_ROW_S = 0b01110, MASK_FORK_COL_S, MASK_FORK_DIAG1_S, MASK_FORK_DIAG2_S;

		private void SetMasks()
		{
			// Set Outer Masks, like 10001

			int cols = Columns; // Leave space for the 1 in the column
			// Basically: value is 1, shift so much across << that new line, then add 1 again, shift until 4
			MASK_FORK_COL_L = (1 << (cols * 4)) | 1;

			// Now do the same for +/- 1 each time for the diagnonal
			cols--; // Shift 1 less for the diagonal going upwards L-R. Only thing is this will need leading 0s, but it's got that. Just don't forget
			// We skipped 3 0s that are quite essential here (I think), so let's add them to the back
			MASK_FORK_DIAG1_L = (1 << (cols * 4)) | 1;

			cols += 2;
			// Now to +1, since Columns is +1, we can just use that instead
			MASK_FORK_DIAG2_L = (1 << (cols * 4)) | 1;

			// Set Inner Masks, like 01110

			cols = Columns; // Leave space for the 1 in the column
			// Basically: value is 1, shift so much across << that new line, then add 1 again, shift until 4
			MASK_FORK_COL_S = ((((1 << cols) | 1) << cols) | 1) << cols;

			// Now do the same for +/- 1 each time for the diagnonal
			cols--; // Shift 1 less for the diagonal going upwards L-R. Only thing is this will need leading 0s, but it's got that. Just don't forget
			// We skipped 3 0s that are quite essential here (I think), so let's add them to the back
			MASK_FORK_DIAG1_S = ((((1 << cols) | 1) << cols) | 1) << cols;

			cols += 2;
			// Now to +1, since Columns is +1, we can just use that instead
			MASK_FORK_DIAG2_S = ((((1 << cols) | 1) << cols) | 1) << cols;
		}


		private string GetEvaluationString(bool inverseEval)
		{
			if (CC_Eval > WinValue - Evaluation_MThreshold) return $"M{Math.Abs(WinValue - ((int)CC_Eval + 1))}";
			if (CC_Eval < LossValue + Evaluation_MThreshold) return $"-M{Math.Abs(LossValue - ((int)CC_Eval - 1))}";
			
			double eval = inverseEval ? -CC_Eval : CC_Eval;
			eval /= EVALUATION_DIVIDER;	// Preserve 1 Digit
			
			if (eval % 1.0 < 0.2 || eval % 1.0 > 0.8)
			{
				eval = Math.Round(eval) / 10;	// If its too close to being rounded, only preserve the one
			}
			else
			{
				// Otherwise, use 2 digits
				eval *= 10;
				eval = Math.Round(eval) / 100;
			}
			return eval <= 0 ? "" + eval : "+" + eval;
		}

		private string GetEvaluationStringAny(double CC_Eval, bool inverseEval)
		{
			if (CC_Eval > WinValue - Evaluation_MThreshold) return $"M{Math.Abs(WinValue - ((int)CC_Eval + 1))}";
			if (CC_Eval < LossValue + Evaluation_MThreshold) return $"-M{Math.Abs(LossValue - ((int)CC_Eval - 1))}";

			double eval = inverseEval ? -CC_Eval : CC_Eval;
			eval /= EVALUATION_DIVIDER; // Preserve 1 Digit

			if (eval % 1.0 < 0.2 || eval % 1.0 > 0.8)
			{
				eval = Math.Round(eval) / 10;   // If its too close to being rounded, only preserve the one
			}
			else
			{
				// Otherwise, use 2 digits
				eval *= 10;
				eval = Math.Round(eval) / 100;
			}
			return eval <= 0 ? "" + eval : "+" + eval;
		}

		public bool IsInUse;    // Perhaps add multithreading

		Dictionary<long, double> ScoreMap;
		public Move BestMove(long myBoard, long opponentBoard, bool inverseEval = false, int maxDepth = -1, int maxMS = -1)
		{
			if(IsInUse)
			{
				Console.Error.WriteLine("Engine Object is in use right now, please wait until calculations are complete or create another instance.");
				throw new Exception();
				return new Move();
			}
			DateTime before = DateTime.Now;
			int oldDepth = CC_FinalDepth;
			IsInUse = true;
			if(maxDepth >= 2) CC_FinalDepth = maxDepth;
			CC_Eval = 0;
			CC_Failsoft_BestMove = 0L;
			ScoreMap = new Dictionary<long, double>();
			do
			{
				// Or 3000ms + Depth 6 start
				ScoreMap.Clear();
				CC_Eval = CC_FailsoftAlphaBeta(myBoard, opponentBoard, CC_FinalDepth);
				CC_FinalDepth += 2; // Repeat until time is reached

				if (ScoreMap.Count <= 1) break;
				if (Math.Abs(ScoreMap.OrderByDescending(x => x.Value).Select(x => x.Value).ToList()[1]) >=
				    WinValue - Evaluation_MThreshold)
					break;  // If the second highest score is checkmate, the top score is either mate or not, but further looking doesn't make sense since we've seen all important there is to see now

			} while ((DateTime.Now - before).TotalMilliseconds < maxMS);    // run once is maxMS == -1, so no time limit
			//Console.WriteLine("-->>==...");
			int computedDepth = CC_FinalDepth - 2;

			if(!Silenced) Console.WriteLine($"Eval: {CC_Eval} >> {GetEvaluationString(false)}  |  Final Depth: {computedDepth}");
			int i2 = 1;
			foreach (var move in ScoreMap.OrderByDescending(x => x.Value).Select(x => x.Key))
			{
				if (!Silenced) Console.WriteLine($"{i2++}. Move: {Convert.ToString(move, 2)} | Score: {ScoreMap[move]}   (=> {GetEvaluationStringAny(ScoreMap[move], false)})");
			}
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

			return new Move(col, row, CC_Eval, GetEvaluationString(inverseEval), CC_Failsoft_BestMove, (DateTime.Now - before).TotalSeconds, computedDepth);
		}

		// Stormcloud 3 AlphaBeta Algorithm but lighter

		private long CC_Failsoft_BestMove;
		internal const int WinValue = 999999999;
		private const int LossValue = -WinValue;		// Jup, those all of a sudden high values are indeed Win/Loss value here, but not marked as Mx
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
				if (!Silenced) Console.WriteLine($"[{after}] Depth: {depth} | BestField: {Convert.ToString(CC_Failsoft_BestMove, 2)} (Eval: {GetEvaluationString(false)}) | Time: {(after-before).TotalSeconds}s");
				//System.Diagnostics.Debug.WriteLine($"[{after}] Calls: {calls} | Depth: {depth} | BestField: {Convert.ToString(CC_Failsoft_BestMove, 2)} (Eval: {GetEvaluationString(false)}) | Time: {(after-before).TotalSeconds}s");
			}
		}

		// I'm pretty sure there are fairly good weights
		public EngineWeightMap WeightMap;

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
						int distanceOpponent = HammingDistance((opponentBoardCurrent & mask), mask);
						int distanceTotal = HammingDistance(mashTotal, mask);

						// HammingDistance 0 is a win, 4 is nothing. This section is broken:
						if (distance < distanceOpponent)
						{
							score -= (distanceOpponent - distance) * weight;
						}
						if(distance == 2) score += (distanceTotal - distance) * weight;
						else if(distance == 1) score += (distanceTotal - distance) * weight * weight;

						//if(distanceOpponent == 2) score -= (distanceTotal - distance) * -WeightMap.WEIGHT_SCORE_OPPONENT;
						//else if(distanceOpponent == 1) score -= (distanceTotal - distance) * -(WeightMap.WEIGHT_SCORE_OPPONENT * WeightMap.WEIGHT_SCORE_OPPONENT);

						score -= distance / (weight * weight);	// High distance from the perfect mask is bad btw
						score += distanceTotal - distance * 2 * weight;
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

			ApplyMasks(myBoard, opponentBoard, WeightMap.WEIGHT_HAMMINGDISTANCE_OWN, ref myScore);
			ApplyMasks(opponentBoard, myBoard, WeightMap.WEIGHT_HAMMINGDISTANCE_OPPONENT, ref opponentScore);

			double finalScore = 0;
			finalScore += myScore * WeightMap.WEIGHT_SCORE_OWN;
			finalScore += opponentScore * WeightMap.WEIGHT_SCORE_OPPONENT;

			for (var reverseIndex = 0; reverseIndex <= BOARD_MAX; reverseIndex++)
			{
				if (((myBoard >> reverseIndex) & 1) == 0) continue;	// Not my field
				finalScore += Score_WallDistance(reverseIndex, WeightMap.WEIGHT_WALL_DISTANCE);
				finalScore += WeightMap.WEIGHT_NEIGHBORS * Score_Neighbors(reverseIndex, myBoard, opponentBoard, WeightMap.WEIGHT_NEIGHBOR_FREE, WeightMap.WEIGHT_NEIGHBOR_OWNED, WeightMap.WEIGHT_NEIGHBOR_TAKEN);
			}

			return finalScore;
		}

		private bool EVAL_DEBUG_FORKS = true;
		private bool EVAL_DEBUG_WALLDIST = true;
		private bool EVAL_DEBUG_NEIGHBORS = true;

		private double AdvancedEvaluation(long myBoard, long opponentBoard)	// Also perhaps evaluate moves that can arise from this position, or how many
		{

			double myScore = 0, opponentScore = 0;
			void ApplyMask(long myBoardstate, long opponentBoardCurrent, long maskTemplate, long forkMaskTemplateS, long forkMaskTemplateL, int horizontalRuns, int verticalRuns, double weight, ref double score)
			{
				for (int i = 0; i < verticalRuns; i++)
				{
					long mask = maskTemplate << (i * Columns);  // Shift by 1 row, so 1 rowwidth, so amount of columns
					long maskForkS = forkMaskTemplateS << (i * Columns);  // Shift by 1 row, so 1 rowwidth, so amount of columns
					long maskForkL = forkMaskTemplateL << (i * Columns);  // Shift by 1 row, so 1 rowwidth, so amount of columns

					for (int i2 = 0; i2 < horizontalRuns; i2++)
					{
						int distance = HammingDistance(myBoardstate & mask, mask);
						int distanceTotal = HammingDistance((myBoardstate | opponentBoardCurrent) & mask, mask);

						if (distance != distanceTotal)
						{
							// We don't solely occupy the mask, but does the opponent?
							if (distance == 0)
							{
								// Only let it count if we don't have a point here.
								// If we do, it's shared and neither can complete it. So, no points.
								// Otherwise, its a threat from the mate
								distance = HammingDistance(opponentBoardCurrent & mask, mask);
								if (distance <= 2)
								{
									int dist = 3 - distance;
									opponentScore += dist * WeightMap.WEIGHT_HAMMINGDISTANCE_OPPONENT;
									int MaskWallDistance = Math.Min(i2, horizontalRuns - 1 - i2);
									opponentScore += dist * MaskWallDistance * WeightMap.WEIGHT_WALL_DISTANCE;

									// Now Check for Potential Forks: How do S and L masks apply?: S >= 2/3 apply, L == 0 apply, otherwise its a fork
									// Since the mask is 1 larger, if it's the final run don't do anything
									if (i < verticalRuns - 1 && i < horizontalRuns - 1 && EVAL_DEBUG_FORKS)
									{
										int distanceS = HammingDistance(opponentBoardCurrent & maskForkS, maskForkS);
										if (distanceS <= 1)
										{
											opponentScore += (2 - distanceS) * WeightMap.WEIGHT_FORK_HAMMINGDISTANCE_S;
											// Check long
											int distanceL = HammingDistance(opponentBoardCurrent & maskForkL, maskForkL);
											if (distanceL <= 1)
											{
												// Higher Hamming distance is better for forks
												opponentScore += distanceL * WeightMap.WEIGHT_FORK_HAMMINGDISTANCE_L;
											}
										}
									}
								}
							}
							// No score for blocked Connect4s
							mask <<= 1;
							forkMaskTemplateS <<= 1;
							forkMaskTemplateL <<= 1;
							continue;
						}

						// We are the only ones in the mask, let's see how much we're in it to win it
						if (distance <= 2)
						{
							int dist = 3 - distance;
							myScore += dist * WeightMap.WEIGHT_HAMMINGDISTANCE_OWN;
							// Distance to the wall: i2 = 0 and i2 = horizontalRuns-1 => 0
							// Math.Min (distance left, distance right)
							int MaskWallDistance = Math.Min(i2, horizontalRuns - 1 - i2);
							myScore += dist * MaskWallDistance * WeightMap.WEIGHT_WALL_DISTANCE;

							// Now Check for Potential Forks: How do S and L masks apply?: S >= 2/3 apply, L == 0 apply, otherwise its a fork
							// Since the mask is 1 larger, if it's the final run don't do anything
							if (i < verticalRuns - 1 && i < horizontalRuns - 1 && EVAL_DEBUG_FORKS)
							{
								int distanceS = HammingDistance(myBoardstate & maskForkS, maskForkS);
								if (distanceS <= 1)
								{
									myScore += (2 - distanceS) * WeightMap.WEIGHT_FORK_HAMMINGDISTANCE_S;
									// Check long
									int distanceL = HammingDistance(myBoardstate & maskForkL, maskForkL);
									if (distanceL <= 1)
									{
										// Higher Hamming distance is better for forks
										myScore += distanceL * WeightMap.WEIGHT_FORK_HAMMINGDISTANCE_L;
									}
								}
							}
						}

						mask <<= 1;
						forkMaskTemplateS <<= 1;
						forkMaskTemplateL <<= 1;
					}
				}
			}

			void ApplyMasks(long myBoardstate, long opponentBoardCurrent, double weight, ref double score)
			{
				ApplyMask(myBoardstate, opponentBoardCurrent, MASK_ROW, MASK_FORK_ROW_S, MASK_FORK_ROW_L, Columns - 3, Rows, weight, ref score);
				ApplyMask(myBoardstate, opponentBoardCurrent, MASK_COL, MASK_FORK_COL_S, MASK_FORK_COL_L, Columns, Rows - 3, weight, ref score);
				ApplyMask(myBoardstate, opponentBoardCurrent, MASK_DIAG1, MASK_FORK_DIAG1_S, MASK_FORK_DIAG1_L, Columns - 3, Rows - 3, weight, ref score);
				ApplyMask(myBoardstate, opponentBoardCurrent, MASK_DIAG2, MASK_FORK_DIAG2_S, MASK_FORK_DIAG2_L, Columns - 3, Rows - 3, weight, ref score);
			}

			ApplyMasks(myBoard, opponentBoard, WeightMap.WEIGHT_HAMMINGDISTANCE_OWN, ref myScore);
			ApplyMasks(opponentBoard, myBoard, WeightMap.WEIGHT_HAMMINGDISTANCE_OPPONENT, ref opponentScore);

			//*
			for (var reverseIndex = 0; reverseIndex <= BOARD_MAX && EVAL_DEBUG_NEIGHBORS && EVAL_DEBUG_WALLDIST; reverseIndex++)
			{
				if (((myBoard >> reverseIndex) & 1) == 0)
				{
					if (((opponentBoard >> reverseIndex) & 1) == 0) continue;
					// Opponent has field
					if(EVAL_DEBUG_WALLDIST) opponentScore += Score_WallDistance(reverseIndex, WeightMap.WEIGHT_WALL_DISTANCE * WeightMap.WEIGHT_WALL_DISTANCE);
					if(EVAL_DEBUG_NEIGHBORS) opponentScore += WeightMap.WEIGHT_NEIGHBORS * Score_Neighbors(reverseIndex, opponentBoard, myBoard, WeightMap.WEIGHT_NEIGHBOR_FREE, WeightMap.WEIGHT_NEIGHBOR_OWNED, WeightMap.WEIGHT_NEIGHBOR_TAKEN);
					continue;
				}
				if(EVAL_DEBUG_WALLDIST) myScore += Score_WallDistance(reverseIndex, WeightMap.WEIGHT_WALL_DISTANCE * WeightMap.WEIGHT_WALL_DISTANCE);
				if(EVAL_DEBUG_NEIGHBORS) myScore += WeightMap.WEIGHT_NEIGHBORS * Score_Neighbors(reverseIndex, myBoard, opponentBoard, WeightMap.WEIGHT_NEIGHBOR_FREE, WeightMap.WEIGHT_NEIGHBOR_OWNED, WeightMap.WEIGHT_NEIGHBOR_TAKEN);
			}
			//*/

			// Apply factors and return
			myScore *= WeightMap.WEIGHT_SCORE_OWN;
			myScore -= opponentScore * WeightMap.WEIGHT_SCORE_OPPONENT;
			return myScore;
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

			// First: Check for a win
			var result = PlayerWon(myBoard, opponentBoard);
			if (result == 1) return WinValue - CC_FinalDepth + depthLeft;   // Win, Quicker is better
			if (result == 2) return LossValue + CC_FinalDepth - depthLeft;  // Loss. Adding of FinalDepth is important because then LossValue is the minimum. when bestscore is initialized to LossValue and its forcedMate, this can cause issues with bestMove, as every move would be worse than doing nothing, so move stay 0 (invalid)
			if (result == 0) return 0;   // Draw

			if (depthLeft < 0) return AdvancedEvaluation(myBoard, opponentBoard);
			double bestscore = double.NegativeInfinity;

			if (AllLegalMoves == null)
			{
				AllLegalMoves = LegalMoves(myBoard, opponentBoard);
			}

			AllLegalMoves = OrderMoves(AllLegalMoves, myBoard, opponentBoard);

			if (AllLegalMoves.Count == 0) return 0; // Draw by no moves, do NOT evaluate. No moves = All tiles have been filled. This is a draw.

			// Failsafe. AFAIK not needed but good to have.
			if (isRoot) CC_Failsoft_BestMove = AllLegalMoves[0];

			foreach (var move in AllLegalMoves)
			{
				// Do move
				myBoard ^= move;
				// Evaluate move
				double score = -CC_FailsoftAlphaBeta(-beta, -alpha, opponentBoard, myBoard, depthLeft-1);
				if (isRoot) ScoreMap.Add(move, score);
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
			//if(isRoot) { Console.WriteLine($"Returning score {bestscore}. Dict size: {ScoreMap.Count}"); }
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


		private List<long> LegalMoves(long myBoard, long opponentBoard)
		{
			// Build a list of available moves
			List<long> moves = new List<long>();

			// Go through each column, then bottom-up and note the first move available
			long state = myBoard | opponentBoard;
			for (int col = 0; col < Columns; col++)
			{
				int row = -1, reverseIndex = -1;
				for (int i = 0; i < Rows; i++)
				{
					// Slot
					reverseIndex = i * Columns + col;    // Index is where it is, reverse it how much shift I need to bring it to the back
					if (((state >> reverseIndex) & 1) == 1) continue;
					row = i;
					break;
				}

				if (row == -1) continue;
				// valid row -> valid move -> add it
				long binaryMove = 1L << reverseIndex;
				moves.Add(binaryMove);
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
