using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChessV1
{
	internal partial class Chessboard2 : Label, IChessboard
	{

		public static Chessboard2 Board;

		#region IChessboard Implementations

		// Variable Implementations
		public bool LegalMovesEnabled { get; set; }
		public bool ScanForChecks { get; set; }
		public bool AllowSelfTakes { get; set; }
		public bool EnableFlipBoard { get; set; }
		ChessMode IChessboard.ChessMode { get; set; }

		// Method Implementations, not yet functional
		void IChessboard.Reset() { ResetBoard(Turn.White); }
		bool IChessboard.UndoLastMove() => false;
		bool IChessboard.Focus() => base.Focus();

		#endregion


		public Dictionary<Coordinate, PieceType> CurrentPosition;
		public MoveHistory MoveHistory { get; private set; }
		// DisplaySize from Chessboard.cs
		private int displaySize;
		public int DisplaySize
		{
			get => displaySize; set
			{
				this.Size = new Size(value, value); displaySize = value; Refresh();
				Form1.self.RefreshSizeButton.Location = new Point((int) (value * 1.036), 50);
			}
		}
		public Turn Turn { get; private set; } = Turn.White;
		public ChessMode ChessMode { get; private set; } = ChessMode.Normal;
		Brush LightColor, DarkColor, HighlightColor, LastMoveHighlightDark, LastMoveHighlightLight, LegalMoveColor, CheckColor, HighlightFieldColorLight, HighlightFieldColorDark;
		//SolidBrush LightColor, DarkColor, HighlightColor, LastMoveHighlightDark, LastMoveHighlightLight, LegalMoveColor, CheckColor, HighlightFieldColorLight, HighlightFieldColorDark;

		public CastleOptions CastleOptionsWhite, CastleOptionsBlack;

		public Chessboard2(int DisplaySize)
		{
			Board = this;
			// Prevent Flickering
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			AllLegalMoves = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			SelectedLegalMoves = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			HighlightedFields = new List<Coordinate>();

			this.Location = new Point(0, 0);
			this.SuspendLayout();
			this.DisplaySize = DisplaySize;
			this.Size = new Size(DisplaySize, DisplaySize);
			this.LightColor = Brushes.SandyBrown;
			this.DarkColor = Brushes.SaddleBrown;
			this.HighlightColor = Brushes.LightYellow;  // Peru, I like PowderBlue
			this.LastMoveHighlightLight = Brushes.Yellow;
			this.LastMoveHighlightDark = Brushes.Gold;
			this.LegalMoveColor = Brushes.DimGray;
			this.HighlightFieldColorLight = Brushes.PowderBlue;
			this.HighlightFieldColorDark = Brushes.Turquoise;
			this.CheckColor = Brushes.Red;

			ResetBoard(Turn.White);
			EventInit();
		}

		public void ResetBoard(Turn Color)
		{
			// Update UI
			DeselectCurrentField();
			AllLegalMoves.Clear();

			// Row, Col
			MoveHistory = new MoveHistory();
			CurrentPosition = DefaultPosition();
			// Add pieces, Renderer works inverted

			this.Turn = Color;
			this.CurrentlyHoldingType = PieceType.None;
			this.CastleOptionsWhite = CastleOptions.Both;
			this.CastleOptionsBlack = CastleOptions.Both;

			AllLegalMoves.AddRange(Calculation.GetAllLegalMoves(CurrentPosition, Turn, MoveHistory));

			Refresh();
			CurrentCalculation = new Calculation(MoveHistory, MaxEngineDepth, Turn) { maxTimeMS = this.MaxEngineTimeMS };
		}

		public void CalculationUpdateReceived()
		{

		}

		public static Dictionary<Coordinate, PieceType> DefaultPosition()
		{
			var BoardPosition = new Dictionary<Coordinate, PieceType>();
			BoardPosition.Add(new Coordinate(0, 0 ), PieceType.rook);
			BoardPosition.Add(new Coordinate(0, 1 ), PieceType.knight);
			BoardPosition.Add(new Coordinate(0, 2 ), PieceType.bishop);
			BoardPosition.Add(new Coordinate(0, 3 ), PieceType.queen);
			BoardPosition.Add(new Coordinate(0, 4 ), PieceType.king);
			BoardPosition.Add(new Coordinate(0, 5 ), PieceType.bishop);
			BoardPosition.Add(new Coordinate(0, 6), PieceType.knight);
			BoardPosition.Add(new Coordinate(0, 7), PieceType.rook);

			BoardPosition.Add(new Coordinate(7, 0), PieceType.ROOK);
			BoardPosition.Add(new Coordinate(7, 1), PieceType.KNIGHT);
			BoardPosition.Add(new Coordinate(7, 2), PieceType.BISHOP);
			BoardPosition.Add(new Coordinate(7, 3), PieceType.QUEEN);
			BoardPosition.Add(new Coordinate(7, 4), PieceType.KING);
			BoardPosition.Add(new Coordinate(7, 5), PieceType.BISHOP);
			BoardPosition.Add(new Coordinate(7, 6), PieceType.KNIGHT);
			BoardPosition.Add(new Coordinate(7, 7), PieceType.ROOK);

			for (int i = 0; i < 8; i++)
			{
				BoardPosition.Add(new Coordinate(1, i), PieceType.pawn);
				BoardPosition.Add(new Coordinate(6, i), PieceType.PAWN);
			}

			/*
			BoardPosition.Clear();

			BoardPosition.Add(new Coordinate(1, 3), PieceType.rook);
			BoardPosition.Add(new Coordinate(1, 6), PieceType.ROOK);
			BoardPosition.Add(new Coordinate(2, 2), PieceType.king);
			BoardPosition.Add(new Coordinate(3, 4), PieceType.bishop);
			BoardPosition.Add(new Coordinate(3, 6), PieceType.PAWN);
			BoardPosition.Add(new Coordinate(3, 7), PieceType.PAWN);
			BoardPosition.Add(new Coordinate(4, 0), PieceType.rook);
			BoardPosition.Add(new Coordinate(5, 4), PieceType.pawn);
			BoardPosition.Add(new Coordinate(4, 5), PieceType.PAWN);
			BoardPosition.Add(new Coordinate(4, 7), PieceType.pawn);
			BoardPosition.Add(new Coordinate(5, 1), PieceType.PAWN);
			BoardPosition.Add(new Coordinate(5, 2), PieceType.pawn);
			BoardPosition.Add(new Coordinate(5, 3), PieceType.PAWN);
			BoardPosition.Add(new Coordinate(6, 5), PieceType.KING);
			BoardPosition.Add(new Coordinate(7, 4), PieceType.ROOK);
			*/

			return BoardPosition;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			int SquareSize = DisplaySize / 8;
			bool WhiteBottom = !(Turn == Turn.Black); // Also include Gamestart and everything if no Turn Color is set

			// Loop goes backwards if it's blacks turn
			for (int i = WhiteBottom ? 0 : 63; WhiteBottom && i <= 63 || !WhiteBottom && i >= 0; i += WhiteBottom ? 1 : -1)
			{
				int row = i / 8, col = i % 8;
				RectangleF PieceRectangleField = new RectangleF(SquareSize * col, SquareSize * row, SquareSize, SquareSize);
				Brush BackColor;
				
				bool IsLightSquare = (i + i / 8) % 2 == 0;
				Coordinate _ = Turn == Turn.White ? new Coordinate(row, col) : new Coordinate(Math.Abs(7 - row), Math.Abs(7 - col)); ;

				if (SelectedField == _)
					BackColor = HighlightColor;
				else if(HighlightedFields.Contains(_))
							BackColor = IsLightSquare ? HighlightFieldColorLight : HighlightFieldColorDark;
				else if(MoveHistory.LastMove.Key == _ || MoveHistory.LastMove.Value == _)
					BackColor = IsLightSquare ? LastMoveHighlightLight : LastMoveHighlightDark;
				else BackColor = IsLightSquare ? LightColor : DarkColor;

				g.FillRectangle(BackColor, PieceRectangleField);
			}

			// Piece is KeyValuePair<Coordinate position, PieceType piecetype>
			foreach (var Piece in CurrentPosition)
			{
				if (Piece.Key == SelectedField && CurrentlyHolding) continue;

				Image _pieceImage = ChessGraphics.GetImage(Piece.Value);
				if (_pieceImage == null) continue;
				int row, col;
				if(WhiteBottom)
				{
					row = Piece.Key.Row;
					col = Piece.Key.Col;
				}
				else
				{
					row = 7 - Piece.Key.Row;
					col = 7 - Piece.Key.Col;
				}
				RectangleF PieceRectangleField = new RectangleF(SquareSize * col, SquareSize * row, SquareSize, SquareSize);
				g.DrawImage(_pieceImage, PieceRectangleField);
			}

			foreach (var move in SelectedLegalMoves)
			{
				int row, col;
				if (WhiteBottom) // Gotta invert idk why but it doesnt work otherwise
				{
					row = move.Key.Value.Row;
					col = move.Key.Value.Col;
				}
				else
				{
					row = 7 - move.Key.Value.Row;
					col = 7 - move.Key.Value.Col;
				}
				PointF FieldPos = new PointF(SquareSize * col, SquareSize * row);
				float BigCircleDiameter = SquareSize * 0.95f, SmallCircleDiameter = SquareSize / 6;
				if(CurrentPosition.ContainsKey(new Coordinate(move.Key.Value.Row, move.Key.Value.Col)))
				{
					// Big circle
					g.DrawEllipse(new Pen(LegalMoveColor, SquareSize / 11), new RectangleF(FieldPos.X + ((SquareSize - BigCircleDiameter) / 2),
																							FieldPos.Y + ((SquareSize - BigCircleDiameter) / 2),
																							BigCircleDiameter,
																							BigCircleDiameter));
				}
				else
				{
					// Small point, Width: SquareSize / 8
					g.FillEllipse(LegalMoveColor, new RectangleF(FieldPos.X + ((SquareSize - SmallCircleDiameter) / 2),
																							FieldPos.Y + ((SquareSize - SmallCircleDiameter) / 2),
																							SmallCircleDiameter,
																							SmallCircleDiameter));	// new RectangleF((SquareSize * (col+0.5f)) /*- (SquareSize / 3)*/, (SquareSize * (row+0.5f))/* - (SquareSize / 3)*/, SquareSize / 6, SquareSize / 6));
				}
			}

			// Draw Currently Held Piece
			if (!CurrentlyHolding || CurrentMousePosition == null) return;
			Image _piece = ChessGraphics.GetImage(CurrentlyHoldingType);
			if (_piece == null) return;
			RectangleF loc = new RectangleF(CurrentMousePosition.X - (SquareSize / 2), CurrentMousePosition.Y - (SquareSize / 2),
				SquareSize, SquareSize);
			g.DrawImage(_piece, loc);
		}

		public static void Log(string s)
		{
			if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s);
			else Console.WriteLine(s);
		}

		public string MoveToString(KeyValuePair<Coordinate, Coordinate> Move, char MoveType)	// No own MoveType enum because a char takes up less bit (16) than an integer (64)
		{
			string move = "";
			PieceType PieceType = CurrentPosition.ContainsKey(Move.Key) ? CurrentPosition[Move.Key] : PieceType.None;
			if (PieceType == PieceType.None) return "None";

			switch (CurrentPosition[Move.Key])
			{
				case PieceType.PAWN:
				case PieceType.pawn:
					move += (char)(Move.Key.Col + 97);   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.KNIGHT:
				case PieceType.knight:
					move += 'N';   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.BISHOP:
				case PieceType.bishop:
					move += 'B';   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.ROOK:
				case PieceType.rook:
					move += 'R';   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.QUEEN:
				case PieceType.queen:
					move += 'Q';   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.KING:
				case PieceType.king:
					if(Move.Key.Row == Move.Value.Row && Math.Abs(Move.Key.Col - Move.Value.Col) == 2)
					{
						if (Move.Key.Col > Move.Value.Col) return "O-O-O";
						else return "O-O";
					}
					move += 'K';   // Lowercase 'a' + column, column 0 is 'a'
					break;
			}

			// Find out if multiple pieces can go to that position
			List< KeyValuePair < KeyValuePair<Coordinate, Coordinate>, char>> MovesFromDestination = Calculation.GetPieceLegalMoves(CurrentPosition, MoveHistory, Move.Value, PieceType, Calculation.GetColorOf(PieceType), false);
			
			foreach (var pair in MovesFromDestination)
			{
				var dest = pair.Key;
				if (CurrentPosition.ContainsKey(dest.Value) && CurrentPosition[dest.Value] == PieceType && dest.Value != Move.Key /* "Duplicate" right now since move has not yet been applied maybe */ )
				{
					// There is another piece that can go there, so we need to distinguish
					if(dest.Value.Row == dest.Key.Row)	// If the row is the same, we need to specify which column
					{
						move += (char)(Move.Key.Col + 97);
					}
					else if (dest.Value.Col == dest.Key.Col)  // If the column is the same, we need to specify which row
					{
						move += Move.Key.Row;
					}
					break;
				}
			}

			if (CurrentPosition.ContainsKey(Move.Value) && CurrentPosition[Move.Value] != PieceType.None)
			{
				move += 'x';
			}

			// Destination square
			if (!move.Equals("" + (char)(Move.Value.Col + 97))) move += (char)(Move.Value.Col + 97);
			move += Move.Value.Row;

			// Add check or mate
			Calculation calc = new Calculation(MoveHistory.Branch(Move, MoveType), 1, Calculation.GetColorOf(PieceType));
			if (calc.IsCheckmate) move += '#';
			else if (calc.IsCheck) move += '+';

			return move;
		}
	}

	public partial class Calculation
	{

		#region Static Provider
		
		// Separate Thread for every initialMove, and for the search in general
		// Then, store positions not moves. seems counter-intuitive

		#endregion

		private Turn TurnColor;
		/// <summary>
		/// Normal MaxDepth, -1 is Abort, 0 is unlimited
		/// </summary>
		protected int MaxDepth;
		public double BestScore { get; private set; } = 0;
		public KeyValuePair<Coordinate, Coordinate> _BestMove { get; private set; } = new KeyValuePair<Coordinate, Coordinate>(new Coordinate(-1, -1), new Coordinate(-1, -1));
		public KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> BestMove { get; private set; } = EmptyMove;
		private Dictionary<KeyValuePair<Coordinate, Coordinate>, double> Scores;

		public DateTime StartTime;
		public static int defaultMaxTimeMS = 10000;
		public int maxTimeMS;
		public void AbortCalculation() => MaxDepth = -1; /* Causes cancellation on the next check */
		public bool IsCheck = false;
		public bool IsCheckmate = false;
		public bool IsStalemate = false;
		public bool IsDraw = false;

		public int FinalTimeMS = 0;
		public double FinalDepth = 0;

		public MoveHistory UpUntilPositionHistory = null;


		#region Evaluation Weights

		private const double PieceCaptureWeight = 1.0;
		private const double ActivityWeight = 0.03;    // Row activity on the opponents side
		private const double PieceHomestuckActivityKnightWeight = 0.35;    // Row activity of Knights on the two home rows
		private const double PieceHomestuckActivityBishopWeight = 0.15;    // Row activity of Bishops on the two home rows
		private const double PieceHomestuckPieceBeingMovedWeight = 0.4;    // Bonus if a homestuck piece is being moved, multiplied by Knight/BishopWeight
		private const double HomestuckDepthWeightDepthDecreaseDivider = 10;    // The higher the depth, DecreaseWeight divided by this is added to the Homestuck bonus
		private const double DepthScoreDecreaseWeight = 0.15;    // MAXIMUM 1.0!! Row activity of Bishops on the two home rows
		private const double DepthScoreDecreaseSteps = 5;    // Example for 5: Every 3 Depth it increases by one: ((int) depth / steps) * DecreaseWeight
		private const double DepthScoreMaximum = 13;    // Maximum amount for Depth / DecreaseSteps
		private const double BoardVisionActivityWeight = 0.08;    // Activity: How many Squares / Legal moves the piece has once it arrives
		private const double PromotionWeight = 0.45;
		private const double PositionMaterialAdvantageWeight = 0.65;
		private const double AverageScoreWeight = 0.6;
		private const double HighestScoreWeight = 0.25;
		private const double LowestScoreWeight = 0.15;
		private const double CheckmateLinePercentageWeight = 0.4; // How many of the lines lead to checkmate
		private const double ScoreBoostViaCheckmate = 9999;
		private const double ScoreBoostViaCheckmateSequence = 9999;
		private const double RepeatMoveDecreaseMultiplier = 0.8;    // Score is multiplied with this, 1 means no issue, 0 is score = 0
		private const double RepeatMoveDecreaseMultiplierHistory = 0.4;    // Going further back, this is multiplied with the repeatMoveDecreaseMultiplier and the amount were going back
		private const double HistoryRepeatLimiter = 13;    // How many steps it may go back to check for repetition. 6 represents 3 white and 3 black moves, so 3 in total, 13 means 12 but no error in <=

		#endregion

		public Calculation(Dictionary<Coordinate, PieceType> position, int depth, Turn turnColor)
		{
			UpUntilPositionHistory = new MoveHistory(position);
			ConstructorVoid(depth, turnColor);
		}

		public Calculation(MoveHistory MoveHistory, int depth, Turn turnColor)
		{
			UpUntilPositionHistory = MoveHistory;
			ConstructorVoid(depth, turnColor);
		}

		public void ConstructorVoid(int depth, Turn turnColor)
		{
			TurnColor = turnColor;
			MaxDepth = depth;
			maxTimeMS = defaultMaxTimeMS;
			StartTime = DateTime.Now;
			AllInitialMoveScores = new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>();

			Scores = new Dictionary<KeyValuePair<Coordinate, Coordinate>, double>();
			CalculateBestMoveParentThread();
		}

		private void ProcessNewDepth(Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>> initialMoveScores, double Depth)
		{
			if(Depth >= 2)
			{
				TimeSpan Calc = DateTime.Now - StartTime;
				int mins = (int) Calc.TotalMinutes;
				string _mins = mins < 10 ? "0" + mins : "" + mins;
				string time = mins >= 2 ? $"{_mins}:{Calc.Seconds}" : $"00:{(int) Calc.TotalSeconds}";
				Chessboard2.Log($"Depth {Depth} Reached! Time: {time},{Calc.Milliseconds}s");
			}

			// GPT-4:
			// Sort the initialMoveScores dictionary based on the calculated move score
			var sortedInitialMoveScores = initialMoveScores.OrderByDescending(entry => CalculateMoveScore(entry.Value)).ToList();

			if (sortedInitialMoveScores.Count == 0 || Depth < 50) return;

			// To get the move with the highest overall score, you can simply access the first element of the sorted list:
			var bestMove = sortedInitialMoveScores.First();
			BestMove = bestMove.Key;
			BestScore = CalculateMoveScore(bestMove.Value);

			Form1.self.SetBestMove(MoveToString(UpUntilPositionHistory.CalculatePosition(), BestMove.Key, BestMove.Value), (int) (BestScore * 100), (int) Depth);
		}

		private double CalculateMoveScore(KeyValuePair<List<double>, int> Score) => CalculateMoveScore(Score.Key, Score.Value);
		private double CalculateMoveScore(List<double> Scores, int CheckmateLines)
		{
			if (Scores.Count == 0) return -1;

			double score = 0;
			score += (Scores.Sum() / Scores.Count) * AverageScoreWeight;
			score += Scores.Max() * HighestScoreWeight;
			score += Scores.Min() * LowestScoreWeight;

			if (CheckmateLines > 0)
			{
				score += ((double)Scores.Count / CheckmateLines) * CheckmateLinePercentageWeight;
				if (Scores.Count == CheckmateLines) score += ScoreBoostViaCheckmateSequence;
			}

			return score;
		}



		#region Two-Queue-Approach

		/**
		 * The Two-Queue-Approach is a multithreaded approach to the Seach-problem. It has two queues for depth that store Search Nodes. The premise
		 * is that we want to use multiple Threads but dont want one thread working on depth 8 while another is still at depth 5 to get an efficient
		 * method and goes through all depths at the highest speed. A depth is only evaluated and pushed as recommended 'best move' when every option
		 * has been explored, so we want all threads to work at the lowest depth available. The problem with one queue used by multiple threads is that
		 * the queues are not necessesarily sorted by depth, at least not to 100%. So, we make a two queues, one that stores the lower current depth
		 * value and one that stores nodes with a depth at one above it.
		 * The Threads get an element from the 'lower' depth queue and process it, then push resulting nodes in the 'higher queue'. When the 'lower
		 * queue' is empty, the Threads grab elements from the higher queue, process the new depth, and push new nodes in the now empty queue, thus
		 * making the former 'lower queue' now the 'higher queue'. The method FetchNewNode() looks at both queues and returns the node from the stack
		 * with a lower depth and processes it. This way, all threads coordinate with the Search-depth.
		 * If a Node tries to be enqueued but does not match either depth and no queue is free, it is added to the Backlog to be dealth with later
		 * when a queue frees up.
		 */

		private ConcurrentQueue<TQA_SearchNode> TQA_SearchQueueOne = new ConcurrentQueue<TQA_SearchNode>();
		private ConcurrentQueue<TQA_SearchNode> TQA_SearchQueueTwo = new ConcurrentQueue<TQA_SearchNode>();
		private ConcurrentBag<TQA_SearchNode> TQA_Backlog = new ConcurrentBag<TQA_SearchNode>();
		private SemaphoreSlim BacklogSemaphore = new SemaphoreSlim(1, 1);   // Limits how many threads can access a thing
		// Stores the following:<PositionKey, Pair<Pair<Pair<Score of the move that lead to this position, multiplier how often this position came up>, List<string> of Sub-position keys in cache (follow-up move position keys)>, Bool if this position is mate to add up lines later>
		// OLD: private ConcurrentDictionary<string, KeyValuePair<KeyValuePair<KeyValuePair<double, int>, List<string>>, bool>> TQA_PositionDataCache = new ConcurrentDictionary<string, KeyValuePair<KeyValuePair<KeyValuePair<double, int>, List<string>>, bool>>();
		private ConcurrentDictionary<string, KeyValuePair<KeyValuePair<Dictionary<string, double>, List<string>>, bool>> TQA_PositionDataCache = new ConcurrentDictionary<string, KeyValuePair<KeyValuePair<Dictionary<string, double>, List<string>>, bool>>();
		// But instead of KVP<double, int> that counts it needs to store a Dictionary of <string, double> that saves the beforePositionString and the score of that move. Because, when in one sequence I capture the queen and land there and in the other I capture a knight and land there, the scores are not the same
		// Alternative: Save THAT score somewhere else??
		private ConcurrentDictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, string> TQA_InitialMoves = new ConcurrentDictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, string>();

		private double TQA_QueueOneDepth { get => TQA_SearchQueueOne.TryPeek(out TQA_SearchNode node) ? node.Depth : -1; }
		private double TQA_QueueTwoDepth { get => TQA_SearchQueueTwo.TryPeek(out TQA_SearchNode node) ? node.Depth : -1; }

		// The calculation problem:
		// Store position key as key and as value: int Score, List<otherPosKeys that directly follow> -> linked tree, bool checkmate => top-down
		// Or: Store all position keys that led up to this and do it bottom-up.
		// Later calculate other stuff

		// Depth within queues stays the same. TQA is short for Two-Queue-Approach. Maybe change this up to remember the low and high queue
		private TQA_SearchNode TQA_FetchNewNode(TQA_SearchNode DefaultNode = null)
		{
			// Debug: Chessboard2.Log("Function Call: TQA_FetchNewNode");
			double One = TQA_QueueOneDepth;
			double Two = TQA_QueueTwoDepth;
			TQA_SearchNode node;

			if (One < 0) return Two < 0 ? DefaultNode : TQA_SearchQueueTwo.TryDequeue(out node) ? node : DefaultNode;
			if (Two < 0) return One < 0 ? DefaultNode : TQA_SearchQueueOne.TryDequeue(out node) ? node : DefaultNode;

			return One < Two ? (TQA_SearchQueueOne.TryDequeue(out node) ? node : DefaultNode) :
							   (TQA_SearchQueueTwo.TryDequeue(out node) ? node : DefaultNode);
		}
		private void TQA_EnqueueToSeachQueue(TQA_SearchNode SearchNode)
		{
			Chessboard2.Log("Qing new Element. New Queue Sizes: One: " + TQA_SearchQueueOne.Count + " | Two: " + TQA_SearchQueueTwo.Count);

			// Debug: Chessboard2.Log("Function Call: TQA_EnqueueToSeachQueue");
			double Depth = SearchNode.Depth;
			if (TQA_QueueOneDepth == Depth) { TQA_SearchQueueOne.Enqueue(SearchNode); return; }
			if (TQA_QueueTwoDepth == Depth) { TQA_SearchQueueTwo.Enqueue(SearchNode); return; }
			// Depth does not match either queue.
			if (TQA_QueueOneDepth == -1) { TQA_SearchQueueOne.Enqueue(SearchNode); return; }
			if (TQA_QueueTwoDepth == -1) { TQA_SearchQueueTwo.Enqueue(SearchNode); return; }
			// If neither queue is finished (empty) and the depths do not match, add it to the leftovers to be sorted out when a queue frees up.
			TQA_Backlog.Add(SearchNode);
		}

		// Threadsafe Backlog Handling by GPT-4, The ConcurrentBag and SemaphoreSlim too:
		// This method happens when a queue is empty
		private async Task ProcessBacklogAsync()
		{
			// Debug: Chessboard2.Log("Function Call: ProcessBacklogAsync");
			await BacklogSemaphore.WaitAsync(); // Acquire the semaphore
			try
			{
				// One of those is -1 bcs queue is empty
				double CurrentQDepth = Math.Max(TQA_QueueOneDepth, TQA_QueueTwoDepth);
				// If both are empty
				if(CurrentQDepth < 0) CurrentQDepth = -0.5;
				int i = TQA_Backlog.Count;
				while (i > 0)	// Go through every element
				{
					TQA_Backlog.TryTake(out TQA_SearchNode node);

					if (node.Depth <= CurrentQDepth)
					{
						// Old and forgotten Node
						// ToDo Add Backlog Handling
					} else if(node.Depth == CurrentQDepth + 0.5)
					{
						TQA_EnqueueToSeachQueue(node);
						i--;
					}
					else
					{
						// Add it back for future handling
						TQA_Backlog.Add(node);
					}
				}
			}
			finally
			{
				BacklogSemaphore.Release(); // Release the semaphore
			}
		}

		private int ProcessedDepth = 0;
		private async Task TQA_ProcessNewDepthAsync(double MaxDepth)
		{
			if (MaxDepth % 1.0 == 0.5) return;

			if ((int)MaxDepth <= ProcessedDepth) return;
			ProcessedDepth = (int) MaxDepth;

			if (MaxDepth >= 2)
			{
				TimeSpan Calc = DateTime.Now - StartTime;
				int mins = (int)Calc.TotalMinutes;
				string _mins = mins < 10 ? "0" + mins : "" + mins;
				string time = mins >= 2 ? $"{_mins}:{Calc.Seconds}" : $"00:{(int)Calc.TotalSeconds}";
				Chessboard2.Log($"Depth {MaxDepth} Reached! Time: {time},{Calc.Milliseconds}s");
			}


			// Debug: Chessboard2.Log("Function Call: TQA_ProcessNewDepthAync");
			// One queue is empty
			// Handle the backlog
			await ProcessBacklogAsync();

			// Stores the Following: <The initial move, KVP< TotalScore, KVP <Number of Total Scores, Number of Checkmates>>>
			// No, actually the score is calculated immediately so it just stores the final score
			var Scores = new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, double>();

			// Add all values
			foreach (var initialMove in TQA_InitialMoves)
			{
				// Maybe store in KVP
				string oldDepthPosKey = "";
				string posKey = initialMove.Value;

				// Stores <posKey#oldPosKey, depth>
				Queue<KeyValuePair<string, double>> posKeyQueue = new Queue<KeyValuePair<string, double>>();
				posKeyQueue.Enqueue(new KeyValuePair<string, double>(initialMove.Value + "#" + initialMove.Value, 0.0));

				double TotalScore = 0.0, HighestScore = 0.0, LowestScore = 0.0;
				int ScoreAmounts = 0, CheckmateAmounts = 0;
				double CurrentDepth = 0;
				while(TQA_PositionDataCache.ContainsKey(posKey) && CurrentDepth <= MaxDepth)
				{
					//CurrentDepth += 0.5;
					// Fill up fill up lol
					if (TQA_PositionDataCache[posKey].Key.Key.ContainsKey(oldDepthPosKey))
					{
						double score = TQA_PositionDataCache[posKey].Key.Key[oldDepthPosKey];
						TotalScore += score;
						// Highest/Lowest Score for weights
						if (score < LowestScore) LowestScore = score;
						if (score > HighestScore) HighestScore = score;
						ScoreAmounts++;
					}
					if (TQA_PositionDataCache[posKey].Value)
					{
						CheckmateAmounts++;
					}
					else
					{
						// If not checkmate, add all children to the queue
						foreach (var ChildPos in TQA_PositionDataCache[posKey].Key.Value)
						{
							if(CurrentDepth < MaxDepth) // Leave room for increment with < instead of <=
								posKeyQueue.Enqueue(new KeyValuePair<string, double>($"{ChildPos}#{posKey}", CurrentDepth + 0.5));
						}
					}

					if (posKeyQueue.Count == 0) break;

					// Get next position Key
					var next = posKeyQueue.Dequeue();   // Key = new child, Value = oldKey
					var split = next.Key.Split('#');
					posKey = split[0];
					oldDepthPosKey = split[1];
					CurrentDepth = next.Value;
				}

				// We accumulated all scores for this initMove, nice!
				Scores.Add(initialMove.Key, TQA_CalculateMoveScore(TotalScore, HighestScore, LowestScore, ScoreAmounts, CheckmateAmounts));
			}

			// Now we have all the scores, lets get the best move, using GPT-4 ofc
			// Sort the dictionary by value in descending order and take the top 3 key-value pairs
			var top3 = Scores.OrderByDescending(kv => kv.Value).Take(3).ToList();

			// Extract the keys from the top 3 key-value pairs
			var top3Keys = top3.Select(kv => kv.Key).ToList();

			// Print the keys for demonstration purposes
			/* For some reason it either takes very long or they arent printed at depth != 1
			int d = 1;
			foreach (var key in top3Keys)
			{
				Console.WriteLine($"Depth {MaxDepth}, {d++}.Best Move: ({key.Key.Key}, {key.Key.Value}), {key.Value}");
			}
			*/
		}

		private double TQA_CalculateMoveScore(double TotalScore, double HighestScore, double LowestScore, int ScoreAmounts, int CheckmateAmounts)
		{
			// Debug: Chessboard2.Log("Function Call: TQA_CalculateMoveScore");
			// Code copied from CalculateMoveScore(List<int>, int)

			if (ScoreAmounts == 0) return -1;

			double score = 0;
			score += (TotalScore / (double) ScoreAmounts) * AverageScoreWeight;
			score += HighestScore * HighestScoreWeight;
			score += LowestScore * LowestScoreWeight;

			if (CheckmateAmounts > 0)
			{
				score += ((double)Scores.Count / CheckmateAmounts) * CheckmateLinePercentageWeight;
				if (Scores.Count == CheckmateAmounts) score += ScoreBoostViaCheckmateSequence;
			}

			return score;
		}

		private async Task TQA_CalcMoveAsync2(TQA_SearchNode Node)
		{
			// Debug: Chessboard2.Log("Function Call: TQA_CalcMoveAsync2");
			// Your search logic for a single initial move
			MoveHistory newHistory = Node.CurrentMoveHistory;
			var newPosition = newHistory.CalculatePosition();
			string posKey = Node.CurrentPositionKey;
			string oldPosKey = Node.OldPositionKey;
			var Move = Node.CurrentMoveHistory.LastMoveComplete;

			/* This is now in the starter method
			if (IsInitialMove && !Move.Key.Key.Equals(Coordinate.NullCoord))
			{
				TQA_InitialMoves.TryAdd(Move, posKey);
			}
			*/

			double Score = GetScoreOf(Move, newHistory, Node.Depth);
			if (Node.Depth % 1.0 == 0.5) Score *= -1;	// Invert score if its for the opponent

			// Calculate the actual score, but only if this is not being calculated yet
			if (!TQA_PositionDataCache.ContainsKey(posKey))
			{
				// If this does not work, we might just want to score a bool in the SearchNode that remembers if this move is a king capture
				bool IsCheckmate = !KingSafety2_IsKingSafe_IncludeFindKing(newPosition, newHistory.Count % 2 == 0 ? Turn.White : Turn.Black);

				// Calculate score add add to the Cache
				var allLegalFollowUpMovesList = new List<string>();
				
				var AllNewNodes = TQA_SearchNode.AllNewNodesFromPosition(newPosition, newHistory, Node.Depth, this.MaxDepth);

				if(AllNewNodes == null)
				{
					// Game is over by Draw, Stalemate or king capture
					
				}

				// No new nodes if depth is reached
				foreach (var node in AllNewNodes)
				{
					allLegalFollowUpMovesList.Add(node.CurrentPositionKey);
					if(!TQA_PositionDataCache.ContainsKey(node.CurrentPositionKey))
					{
						// If its not in the cache, add the new node to the calculation processing queue
						TQA_EnqueueToSeachQueue(node);
					}
				}

				// Stores the scores in combination with the move/position that lead up to this score. For example, if the queen was captures to get here the score would differ from a knight capture so we save the previous pos as well
				Dictionary<string, double> ScoreDict = new Dictionary<string, double>();
				ScoreDict.Add(oldPosKey, Score);

				TQA_PositionDataCache.TryAdd(posKey, new KeyValuePair<KeyValuePair<Dictionary<string, double>, List<string>>, bool>(new KeyValuePair<Dictionary<string, double>/*KeyValuePair<double, int> */, List<string>>( ScoreDict /*new KeyValuePair<double, int>(Score, 1) */, allLegalFollowUpMovesList), IsCheckmate));

				// Add all new Nodes. If they already exist, they will be ignored on the next move
				// => Even better, dont even add them to the queue, already implemented in loop just leaving this comment here

			}
			else	// Already being handled, position trace; We still need to add our oldPosKey with move value
			{
				if (TQA_PositionDataCache[posKey].Key.Key.ContainsKey(oldPosKey)) return;
				TQA_PositionDataCache[posKey].Key.Key.Add(oldPosKey, Score);
			}
		}

		// I want a method that receives: The (new) position that just branched, the new History, the old position, the old history (for score) and the newPositionString and ofc the Move, the current Depth and bool of if initial move

		private class TQA_SearchNode
		{
			public static List<TQA_SearchNode> AllNewNodesFromPosition(Dictionary<Coordinate, PieceType> Position, MoveHistory History, double currentDepth, double MaxDepth)
			{
				// Debug: Chessboard2.Log("Function Call: AllNewNodesFromPosition");
				Turn TurnColor = History.Count % 2 == 1 ? Turn.White : Turn.Black; // include invert through == 1 and not == 0 bcs thats the color of the last 
				var AllFollowUpMoves = GetAllLegalMoves(Position, TurnColor, History);
				var Nodelist = new List<TQA_SearchNode>();
				string oldPositionKey = History.GeneratePositionKey(Position);

				if (currentDepth > MaxDepth && MaxDepth != 0 /*unlimited*/) return Nodelist;	// Cancel
				
				// King captures are dealt with when checking new legal moves and adding them, because checkmates have extra value, in contrast to draws
				if (TQA_IsDraw(Position))
				{
					return null;
				}
				
				foreach (var move in AllFollowUpMoves)
				{
					MoveHistory newHistory = History.Branch(move);
					var newPosition = History.CalculatePosition();
					string positionKey = newHistory.GeneratePositionKey(newPosition);

					TQA_SearchNode Node = new TQA_SearchNode(newPosition, newHistory, positionKey, move, Position, History, oldPositionKey, currentDepth + 0.5);
					Nodelist.Add(Node);
				}
				return Nodelist;
			}

			public MoveHistory CurrentMoveHistory { get; private set; }
			public MoveHistory OldMoveHistory { get; private set; }
			public Dictionary<Coordinate, PieceType> CurrentPosition { get; private set; }
			public Dictionary<Coordinate, PieceType> OldPosition { get; private set; }
			public KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> CurrentMove { get; private set; }
			public string CurrentPositionKey { get; private set; }
			public string OldPositionKey { get; private set; }
			public double Depth { get; private set; }

			public TQA_SearchNode(
				Dictionary<Coordinate, PieceType> newPosition,
				MoveHistory newHistory,
				string newPositionKey,
				KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> currentMove,
				Dictionary<Coordinate, PieceType> oldPosition,
				MoveHistory oldHistory,
				string oldPositionKey,
				double newDepth)
			{
				// Debug: Chessboard2.Log("Function Call: TQA_SearchNode Constructor");
				CurrentPosition = newPosition;
				CurrentMoveHistory = newHistory;
				CurrentPositionKey = newPositionKey;
				CurrentMove = currentMove;
				OldPosition = oldPosition;
				OldMoveHistory = oldHistory;
				OldPositionKey = oldPositionKey;
				Depth = newDepth;
			}
		}

		/// <summary>
		/// This is Async
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="History"></param>
		/// <param name="MaxDepth"></param>
		public Calculation(MoveHistory History, double MaxDepth, Turn InitialTurnColor, Dictionary<Coordinate, PieceType> Position = null)
		{
			StartTime = DateTime.Now;
			Chessboard2.Log("Function Call: Calculation Async Constructor");
			this.TurnColor = InitialTurnColor;
			// ToDo Set global Check, Checkmate and Stalemate variables
			if (Position == null) Position = History.CalculatePosition();
			TQA_SearchAsyncTwoQueueApproach(Position, History, 0.0, MaxDepth);
		}

		private async void TQA_SearchAsyncTwoQueueApproach(Dictionary<Coordinate, PieceType> Position, MoveHistory History, double currentDepth, double MaxDepth)
		{
			// Debug: Chessboard2.Log("Function Call: TQA_SearchAsyncTwoQueueApproach");
			List<TQA_SearchNode> initialNodes = TQA_SearchNode.AllNewNodesFromPosition(Position, History, currentDepth, MaxDepth);
			if(initialNodes == null || initialNodes.Count == 0)
			{
				Chessboard2.Log("Position given to SearchAsyncTwoQueueApproach has zero legal moves.");
				return;
			}
			// Add initialMoves
			foreach (TQA_SearchNode node in initialNodes)
			{
				if(!TQA_InitialMoves.ContainsKey(node.CurrentMove))
					TQA_InitialMoves.TryAdd(node.CurrentMove, node.CurrentPositionKey);
			}
			// Get all initial SearchNodes
			await TQA_RunSearchForAllInitialMovesAsync2(initialNodes);
		}

		#region Async Search By GPT-4, Optimized after request for while implementation

		/**
		 * Comment by GPT-4:
		 * 
		 * In this example, the while loop continues fetching and processing nodes as long as TQA_FetchNewNode returns a non-null value.
		 * When TQA_FetchNewNode returns null, it means there are no more nodes to process, and the loop breaks. Please note that since
		 * TQA_FetchNewNode may return null due to the queues being empty momentarily, you may want to consider adding a small delay
		 * (e.g., using await Task.Delay(10)) before fetching the next node to avoid busy-waiting and reduce CPU usage. In this updated
		 * version, the initial moves are enqueued before starting the worker tasks. Each worker task will run the RunSearchAsync method,
		 * which will fetch and process nodes as long as there are nodes available. The number of worker tasks is determined based on the
		 * number of available processor cores and the number of initial moves, ensuring a balance between parallelism and computational
		 * resources.
		 * 
		 * > Is Math.Max for workerCount correct or should it be Math.Min?
		 * 
		 * You are right, I apologize for the confusion. It should be Math.Min instead of Math.Max. The idea is to limit the number of
		 * worker tasks to the minimum between the number of initial moves and the number of available processor cores. Here's the
		 * corrected version: [...] Using Math.Min ensures that you don't create more worker tasks than there are initial moves or
		 * processor cores, which would be inefficient and could potentially lead to performance issues.
		 */

		private async Task TQA_RunSearchForAllInitialMovesAsync2(List<TQA_SearchNode> initialMoves)
		{
			// Debug: Chessboard2.Log("Function Call: TQA_RunSearchForAllInitialMovesAsync2");
			// Enqueue initial moves
			foreach (var initialMove in initialMoves)
			{
				TQA_EnqueueToSeachQueue(initialMove);
			}

			// Determine the number of worker tasks based on the available hardware resources
			int workerCount = Math.Min(Environment.ProcessorCount, initialMoves.Count);

			var tasks = new List<Task>();

			for (int i = 0; i < workerCount; i++)
			{
				tasks.Add(Task.Run(() => TQA_RunSearchAsync2()));
			}

			await Task.WhenAll(tasks); // Wait for all tasks to complete
		}

		private async Task TQA_RunSearchAsync2()
		{
			// Debug: Chessboard2.Log("Function Call: TQA_RunSearchAsync2");
			while (true)
			{
				TQA_SearchNode currentNode = TQA_FetchNewNode();

				if (currentNode == null)
				{
					break; // Exit the loop when there are no more nodes to process
				}

				if(currentNode.Depth == 0 && TQA_DetermineCheckZeroDepth(currentNode.CurrentPosition))
				{
					// Game Over
					// ToDo maybe
					break;
				}

				// Your search logic for the current node
				await TQA_CalcMoveAsync2(currentNode);
				// :) Maybe there needs to be some logic here too idk
				// Oh yeah check if either queue is empty
				
				if(TQA_SearchQueueOne.Count == 0 || TQA_SearchQueueTwo.Count == 0)
				{
					TQA_SearchNode node;
					TQA_SearchQueueOne.TryPeek(out node);
					if (node == null) TQA_SearchQueueTwo.TryPeek(out node);

					// Check that not both Queues are not empty, if so process the last known depth
					if ((TQA_SearchQueueOne.Count == 0 && TQA_SearchQueueTwo.Count == 0) || node == null)
					{
						// Remember current and last processed depth?
						break;
					}
					// Assert node is some node

					double maxDepth = node.Depth - 0.5;	// Node is of the new stack. The depth we want to process, aka the finished calculated depth, is 0.5 (1 level) lower.
					await TQA_ProcessNewDepthAsync(maxDepth);
				}
			}
		}

		private bool TQA_DetermineCheckZeroDepth(Dictionary<Coordinate, PieceType> Position)
		{
			// Debug: Chessboard2.Log("Function Call: TQA_DetermineCheckZeroDepth");
			//IsCheck = IsTurnColorKingInCheck(pos, UpUntilPositionHistory, initialTurnColor);
			IsCheck = KingSafety2_IsKingSafe_IncludeFindKing(Position, TurnColor);
			if (TQA_InitialMoves.Count == 0)
			{
				// No legal moves. Now its either Stalemate or Checkmate
				if (IsCheck) IsCheckmate = true;
				else IsStalemate = true;
				TQA_GameOver(IsCheck ? GameOver.Checkmate : GameOver.Stalemate);
				return true;
			}
			else if (TQA_IsDraw(Position))
			{
				IsDraw = true;
				TQA_GameOver(GameOver.Draw);
				return true;
			}
			return false;
		}

		private static bool TQA_IsDraw(Dictionary<Coordinate, PieceType> Position)
		{
			// Debug: Chessboard2.Log("Function Call: TQA_IsDraw");
			if (Position.Count <= 2) return true;

			int MaterialValueWhite = 0, MaterialValueBlack = 0;
			// Since you cant checkmate with a 3-point piece, we ask how many there are, in addition to the king
			foreach (var piece in Position)
			{
				int val = GetPieceValue(piece.Value);
				Turn color = GetColorOf(piece.Value);
				if (val == 1) if (color == Turn.White) MaterialValueWhite += 9; else MaterialValueBlack += 9;	// Pawns are worth 9 because they _can_ queen and thats important
				else if (val != 10) if (color == Turn.White) MaterialValueWhite += val; else MaterialValueBlack += val;
			}
			if (MaterialValueWhite <= 3 && MaterialValueBlack <= 3) return true;	// Draw by Insufficient Material because they have no pawns and max one bishop/knight on the board, which is a draw

			// No Draw
			return false;
		}

		private void TQA_GameOver(GameOver GameOver)
		{
			// Debug: Chessboard2.Log("Function Call: TQA_GameOver");
			// ToDo
			Chessboard2.Log($"The Game is over: ToDo - {GameOver}");
		}

		public enum GameOver
		{
			Draw, Stalemate, Checkmate
		}

		#endregion


		#endregion


		/*
		 Auch möglich: Positionen und so werden erst ab move 3 gespeichert. Da move 1 und 2 noch keine Duplikationen hervorrufen können werden diese im OG dictionary gespeichert.
		 */


		// GPT-4 Implementation for efficiency and StackOverflow prevention

		/// <summary>
		/// This defines the SearchNode class, which represents a node in the search tree. It contains a MoveHistory, the current Turn, the search Depth, and a list of Scores for the current line.
		/// </summary>
		private class SearchNode
		{
			public MoveHistory History;
			public Turn TurnColor;
			public double Depth;
			//public KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> ParentMove;		// Scratch this maybe, maybe keep this to later print the line
			public KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> InitialMove;

			public SearchNode(MoveHistory history, Turn turnColor, double depth/*, KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> parentMove*/, KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> initialMove)
			{
				History = history;
				TurnColor = turnColor;
				Depth = depth;
				//ParentMove = parentMove;
				InitialMove = initialMove;
			}
		}

		private static KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> EmptyMove = new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(Coordinate.NullCoord, Coordinate.NullCoord), '-');

		public Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>> AllInitialMoveScores;
		ConcurrentDictionary<string, KeyValuePair<MoveHistory, List<double>>> positionDataCache = new ConcurrentDictionary<string, KeyValuePair<MoveHistory, List<double>>>();

		private void CalculateBestMoveParentThread()
		{
			Turn initialTurnColor = this.TurnColor;
			MoveHistory initialHistory = this.UpUntilPositionHistory.Clone();

			var positionDataCache = new ConcurrentDictionary<string, KeyValuePair<MoveHistory, List<double>>>();

			Queue<SearchNode> searchQueue = new Queue<SearchNode>();
			searchQueue.Enqueue(new SearchNode(initialHistory, initialTurnColor, 0.0, EmptyMove));
			List<Thread> SearchThreads = new List<Thread>();

			var pos = initialHistory.CalculatePosition();
			var allLegalMoves = GetAllLegalMoves(pos, initialTurnColor, initialHistory);

			foreach (var move in allLegalMoves)
			{
				Thread Thread = new Thread(() =>
				{
					CalculateBestMove(ref searchQueue, ref positionDataCache, move);
				});
				Thread.Start();
				SearchThreads.Add(Thread);
			}
			
		}

		/// <summary>
		/// 
		/// Documentation provided by GPT-4. <br/> <br/>
		/// 
		/// The CalculateBestMove method iteratively explores the search tree, updates the scores for each line, and <br/>
		/// stores the aggregated scores for each line in the Scores dictionary to determine the best move.
		/// 
		/// </summary>
		private void CalculateBestMove(ref Queue<SearchNode> searchQueue, ref ConcurrentDictionary<string, KeyValuePair<MoveHistory, List<double>>> PositionDataCache, KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> InitialMove)
		{
			// We now instead receive an initial move. Thus, the starting turn color must invert the starting turn color as one move has already been applied

			MoveHistory InitialHistory = this.UpUntilPositionHistory.Branch(InitialMove);
			var SecondInitialPosition = InitialHistory.CalculatePosition();
			var AllSecondInitialLegalMoves = GetAllLegalMoves(SecondInitialPosition, InvertColor(TurnColor), InitialHistory);

			// Method Restructure
			double currentDepth = 0.0;

			var initialMoveScores = new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>();

			/**
			 * This while loop iterates until the search stack is empty. It processes each node in the search tree.
			 */
			while (searchQueue.Count > 0)	// Übeltäter?
			{
				/**
				 * This segment pops a node from the stack, retrieves the depth, turn color, move history, and scores for the current line.
				 */
				SearchNode currentNode = searchQueue.Dequeue();
				//Chessboard2.Log($"Depth: {currentNode.Depth} Current Queue Size: {searchQueue.Count}");

				if (currentNode.Depth > currentDepth && currentNode.Depth % 1.0 == 0.0 && initialMoveScores.Count > 0)
				{
					if(currentDepth >= 15)
					{
						Chessboard2.Log("Processing Depth 50...");
						int i = 0;
						foreach (var move in initialMoveScores)
						{
							Chessboard2.Log($"Move {++i}: Total Score Amount: {move.Value.Key.Count}, Checkmate Lines: {move.Value.Value}");
						}
					}

					ProcessNewDepth(new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores), currentNode.Depth);

					// Clear Checkmate line amount
					// => No. Since Checkmate lines do not Branch further, we need to keep the score as is.
					/*foreach (var initialMoveSet in new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores))
					{
						initialMoveScores[initialMoveSet.Key] = new KeyValuePair<List<double>, int>(initialMoveScores[initialMoveSet.Key].Key, 0);
					}
					*/

					// Do we clear the lists? I think so because new depth = new Branches
					/*foreach (var initialMoveSet in new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores))
					{
						initialMoveScores[initialMoveSet.Key] = new KeyValuePair<List<double>, int>(new List<double>(), initialMoveScores[initialMoveSet.Key].Value);
					}//*/
				}

				currentDepth = currentNode.Depth;
				Turn currentTurnColor = currentNode.TurnColor;
				MoveHistory currentHistory = currentNode.History;

				/**
				 * Check if the maximum depth has been reached, if it's reached, the method calls Finish and returns.
				 */
				if (currentDepth > MaxDepth /* || (DateTime.Now - StartTime).TotalMilliseconds > this.maxTimeMS*/)
				{
					ProcessNewDepth(new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores), currentNode.Depth);
					return;
				}

				/**
				 * This segment calculates the current position, gets all legal moves, and then processes each move in a loop.
				 */
				var pos = currentHistory.CalculatePosition();
				var allLegalMoves = GetAllLegalMoves(pos, currentTurnColor, currentHistory);

				// Bug: Only for the first legal move are all following scores calculated

				// Determine Check
				if(currentDepth == 0)
				{
					// We have all Initial Legal Moves
					if(initialMoveScores.Count == 0)
					{
						// Add all initial moves
						foreach (var move in allLegalMoves)
						{
							if (initialMoveScores.ContainsKey(move)) continue;
							initialMoveScores.Add(move, new KeyValuePair<List<double>, int>(new List<double>(), 0));
						}
					}

					//IsCheck = IsTurnColorKingInCheck(pos, UpUntilPositionHistory, initialTurnColor);
					IsCheck = KingSafety2_IsKingSafe_IncludeFindKing(pos, TurnColor);
					if (allLegalMoves.Count == 0)
					{
						// No legal moves. Now its either Stalemate or Checkmate
						if (IsCheck) IsCheckmate = true;
						else IsStalemate = true;
						ProcessNewDepth(new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores), currentNode.Depth);
						return;
					}
					else if(pos.Count <= 2)
					{
						IsDraw = true;
						ProcessNewDepth(new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores), currentNode.Depth);
						return;
					}
				}

				foreach (var move in allLegalMoves)
				{
					// Calculate the move score before branching
					double MoveScore = GetScoreOf(move, currentHistory, currentDepth);
					if (currentTurnColor != TurnColor) MoveScore *= -1;

					// Branch the current history with the move
					MoveHistory newHistory = currentHistory.Branch(move);

					// Score stuff
					{
						// Check if the current node is a child of the root node (currentDepth == 0.5)
						var initMove = currentNode.InitialMove;


						if (!initMove.Key.Equals(EmptyMove.Key) && initialMoveScores.ContainsKey(initMove))
						{
							// GPT-4-Improved Code:
							// If initial move is an actual move, add the score
							initialMoveScores[initMove].Key.Add(MoveScore);

							// Check if the move results in a checkmate
							if (pos.ContainsKey(move.Key.Value) && pos[move.Key.Value].ToString().ToUpper() == "KING")
							{
								initialMoveScores[initMove] = new KeyValuePair<List<double>, int>(initialMoveScores[initMove].Key, initialMoveScores[initMove].Value + 1);
							}
						}
					}

					// Push the new node onto the search stack with the updated information
					if (currentDepth < MaxDepth)
					{
						// ZielArray ist nicht lang genug exception like ?? searchQueue.Enqueue(new SearchNode(newHistory, InvertColor(currentTurnColor), currentDepth + 0.5, currentNode.InitialMove.Key.Equals(EmptyMove.Key) ? move : currentNode.InitialMove));
					}
				}
			}
		}

		/**private async Task CalculateBestMoveAsync()
		{
			Turn initialTurnColor = this.TurnColor;
			MoveHistory initialHistory = this.UpUntilPositionHistory.Clone();

			/**
			 This segment defines the CalculateBestMove method and sets up the lineScores dictionary to store the scores of each line,
			a stack called searchStack to store the search nodes, and then pushes the initial node onto the stack.
			 * /

			Dictionary<KeyValuePair<Coordinate, Coordinate>, List<double>> lineScores = new Dictionary<KeyValuePair<Coordinate, Coordinate>, List<double>>();
			Queue<SearchNode> searchQueue = new Queue<SearchNode>();
			searchQueue.Enqueue(new SearchNode(initialHistory, initialTurnColor, 0.0, EmptyMove));

			// Method Restructure
			double currentDepth = 0.0;

			var initialMoveScores = new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>();

			/**
			 * This while loop iterates until the search stack is empty. It processes each node in the search tree.
			 * /
			while (searchQueue.Count > 0)   // Übeltäter?
			{
				/**
				 * This segment pops a node from the stack, retrieves the depth, turn color, move history, and scores for the current line.
				 * /
				SearchNode currentNode = searchQueue.Dequeue();
				//Chessboard2.Log($"Depth: {currentNode.Depth} Current Queue Size: {searchQueue.Count}");

				if (currentNode.Depth > currentDepth && currentNode.Depth % 1.0 == 0.0 && initialMoveScores.Count > 0)
				{
					ProcessNewDepth(new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores), currentNode.Depth);
				}

				currentDepth = currentNode.Depth;
				Turn currentTurnColor = currentNode.TurnColor;
				MoveHistory currentHistory = currentNode.History;

				/**
				 * Check if the maximum depth has been reached, if it's reached, the method calls Finish and returns.
				 * /
				if (currentDepth > MaxDepth /* || (DateTime.Now - StartTime).TotalMilliseconds > this.maxTimeMS* /)
				{
					ProcessNewDepth(new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores), currentNode.Depth);
					return;
				}

				/**
				 * This segment calculates the current position, gets all legal moves, and then processes each move in a loop.
				 * /
				var pos = currentHistory.CalculatePosition();
				var allLegalMoves = GetAllLegalMoves(pos, currentTurnColor, currentHistory);

				// Determine Check
				if (currentDepth == 0)
				{
					// We have all Initial Legal Moves
					if (initialMoveScores.Count == 0)
					{
						// Add all initial moves
						foreach (var move in allLegalMoves)
						{
							if (initialMoveScores.ContainsKey(move)) continue;
							initialMoveScores.Add(move, new KeyValuePair<List<double>, int>(new List<double>(), 0));
						}
					}

					//IsCheck = IsTurnColorKingInCheck(pos, UpUntilPositionHistory, initialTurnColor);
					IsCheck = KingSafety2_IsKingSafe_IncludeFindKing(pos, initialTurnColor);
					if (allLegalMoves.Count == 0)
					{
						// No legal moves. Now its either Stalemate or Checkmate
						if (IsCheck) IsCheckmate = true;
						else IsStalemate = true;
						ProcessNewDepth(new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores), currentNode.Depth);
						return;
					}
					else if (pos.Count <= 2)
					{
						IsDraw = true;
						ProcessNewDepth(new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores), currentNode.Depth);
						return;
					}
				}

				List<Task> tasks = new List<Task>();

				foreach (var move in allLegalMoves)
				{
					tasks.Add(Task.Run(() => ProcessMove(move, initialHistory, initialTurnColor, positionScoresCache)));
				}

				await Task.WhenAll(tasks);
			}
		}
		*/

		private static bool _IsTurnColorKingInCheck(Dictionary<Coordinate, PieceType> position, MoveHistory History, Turn turnColor)
		{
			// Is King in Check?
			bool TurnColorKingInCheck = false;
			foreach (var piece in position)
			{
				if (TurnColorKingInCheck) break;
				// For including PieceType.None, IsPieceColor would not do the trick here.
				if (!IsOppositePieceColor(piece.Value, turnColor)) continue;
				var LegalMoves = GetPieceLegalMoves(position, History, piece.Key, piece.Value, GetColorOf(piece.Value), false);
				foreach (var legalMove in LegalMoves)
				{
					// legalMove.Key.Value is the destination of the legal move of the opponent. Just check if our king is there
					if (position.ContainsKey(legalMove.Key.Value) && (position[legalMove.Key.Value] == PieceType.KING || position[legalMove.Key.Value] == PieceType.king))
					{
						TurnColorKingInCheck = true;
						break;
					}
				}
			}
			return TurnColorKingInCheck;
		}

		/// <summary>
		/// Method Call BEFORE move is made. Evaluates a move based on Capture score, activity and general material advantage.
		/// </summary>
		/// <param name="move">The move to be evaluated.</param>
		/// <param name="currentHistory">The current MoveHistory of the Position.</param>
		/// <param name="turnColor">The Color of the current Turn.</param>
		private double GetScoreOf(KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> move, MoveHistory currentHistory, double depth, Turn turnColor = Turn.Pregame)
			=> GetScoreOf(move, currentHistory.CalculatePosition(), depth, turnColor == Turn.Pregame ? currentHistory.Count % 2 == 0 ? Turn.White : Turn.Black : turnColor, currentHistory);
		/// <summary>
		/// Method Call BEFORE move is made. Evaluates a move based on Capture score, activity and general material advantage.
		/// </summary>
		/// <param name="move">The move to be evaluated.</param>
		/// <param name="Position">The Boardposition.</param>
		/// <param name="turnColor">The Color of the current Turn.</param>
		/// <returns></returns>
		private double GetScoreOf(KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> move, Dictionary<Coordinate, PieceType> Position, double depth, Turn turnColor = Turn.Pregame, MoveHistory currentHistory = null)
		{
			PieceType PieceType = Position.ContainsKey(move.Key.Key) ? Position[move.Key.Key] : PieceType.None;
			string TypeString = PieceType.ToString().ToUpper();
			// First, just evaluate the capture of the piece
			double scoreOfPiececapture = Position.ContainsKey(move.Key.Value) ? GetPieceValue(Position[move.Key.Value]) : 0;
			double PromotionScore = 0;
			double HomestuckScore = 0;

			switch (move.Value)
			{
				case 'Q': PromotionScore = GetPieceValue(PieceType.QUEEN) - 1; break;
				case 'R': PromotionScore = GetPieceValue(PieceType.ROOK) - 1; break;
				case 'B': PromotionScore = GetPieceValue(PieceType.BISHOP) - 1; break;
				case 'K': PromotionScore = GetPieceValue(PieceType.KNIGHT) - 1; break;
			}

			double PositionMaterialAdvantage = 0;
			double HomestuckDepthBonusMultiplier = 1 + ((((int)depth) / DepthScoreDecreaseSteps) * DepthScoreDecreaseWeight) / HomestuckDepthWeightDepthDecreaseDivider;    // Increase a bit with score
			if (turnColor == Turn.Pregame && Position.ContainsKey(move.Key.Value)) turnColor = GetColorOf(Position[move.Key.Key]);
			foreach (var piece in Position)
			{
				if (IsPieceColor(piece.Value, turnColor)) PositionMaterialAdvantage += GetPieceValue(piece.Value);
				else PositionMaterialAdvantage -= GetPieceValue(piece.Value);

				// Homestuck EarlyGame pieces weight

				double PieceWeight = TypeString == "KNIGHT" ? PieceHomestuckActivityKnightWeight : TypeString == "BISHOP" ? PieceHomestuckActivityBishopWeight : -1;

				if (PieceWeight < 0) continue;

				if(turnColor == Turn.Black && piece.Key.Row <= 2)	// Bonus still applies and multiplier for negative is zero
				{
					if (piece.Key == move.Key.Key) HomestuckScore += PieceHomestuckPieceBeingMovedWeight * PieceWeight * move.Key.Value.Row;
					else HomestuckScore -= (2 - piece.Key.Row) * PieceWeight * HomestuckDepthBonusMultiplier;
				}
				else if (turnColor == Turn.White && move.Key.Value.Row >= 5)
				{
					if (piece.Key == move.Key.Key) HomestuckScore += PieceHomestuckPieceBeingMovedWeight * PieceWeight * (7 - move.Key.Value.Row);
					else HomestuckScore -= (piece.Key.Row - 5) * PieceWeight * HomestuckDepthBonusMultiplier;
				}
			}

			double score = HomestuckScore + (scoreOfPiececapture * PieceCaptureWeight);
			// Activity
			if(turnColor == Turn.Black && move.Key.Value.Row > 3) score += move.Key.Value.Row - 3 * ActivityWeight;	// Max: 4 * Weight => 0.2
			else if(turnColor == Turn.White && move.Key.Value.Row < 4) score += Math.Abs(4 - move.Key.Value.Row) * ActivityWeight;

			// Activity Eval: How many squares can the piece see -> GetPieceLegalMoves().Count;
			if (TypeString == "QUEEN" || TypeString == "ROOK" || TypeString == "BISHOP")
			{
				score += GetPieceLegalMoves(Position, new MoveHistory(Position), move.Key.Value, PieceType, turnColor, false, false).Count * BoardVisionActivityWeight;
			}

			// Depth Decrease
			if (depth > 0)
			{
				score -= Math.Max(score / (Math.Min(((int) depth) / DepthScoreDecreaseSteps, DepthScoreMaximum) * DepthScoreDecreaseWeight), 0);	// Don't allow adding
			}

			score += PromotionScore * PromotionWeight;
			score += PositionMaterialAdvantage * PositionMaterialAdvantageWeight;
			
			// Repetition Prevention
			if(currentHistory != null)
			{
				int Back = 2;
				KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> LastMove;
				while (currentHistory.Count >= Back && Back <= HistoryRepeatLimiter)
				{
					LastMove = currentHistory.History[currentHistory.Count - Back];
					if (move.Key.Equals(LastMove.Key) && move.Value == LastMove.Value)
						score *= RepeatMoveDecreaseMultiplier * (RepeatMoveDecreaseMultiplierHistory * Back/2);
					Back += 2;
				}
			}

			// Checkmate stuff
			if (depth < 1 && scoreOfPiececapture == (int) PieceType.KING /* works for PieceType.KING and PieceType.king */)
			{
				score += ScoreBoostViaCheckmate;
			}
			
			return score;
		}

		public static Turn InvertColor(Turn Turn)
		{
			return Turn == Turn.White ? Turn.Black : Turn.White;
		}

		public static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetAllLegalMoves(Dictionary<Coordinate, PieceType> position, Turn turnColor, MoveHistory History = null)
		{
			if(History == null) History = new MoveHistory();
			var legalMoves = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();

			// Is King in Check?
			//bool TurnColorKingInCheck = IsTurnColorKingInCheck(position, History, turnColor);
			bool TurnColorKingInCheck = KingSafety2_IsKingSafe_IncludeFindKing(position, turnColor);

			foreach (var piece in position)	// piece.Key = Coordinate, piece.Value = 
			{
				if (IsPieceColor(piece.Value, turnColor))	// Check if the piece is one's own color
				{
					legalMoves.AddRange(GetPieceLegalMoves(position, History, piece.Key, piece.Value, turnColor, TurnColorKingInCheck));
				}
			}

			return legalMoves;
		}

		public static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetPieceLegalMoves(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate piecePos, PieceType pieceType, Turn turnColor, bool KingInCheck, bool CheckForIfMoveLegal = true)
		{
			var legalMoves = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();

			switch (pieceType)
			{
				case PieceType.PAWN:
				case PieceType.pawn:
					legalMoves.AddRange(GetPawnLegalMoves(position, History, piecePos, turnColor, CheckForIfMoveLegal));
					break;
				case PieceType.ROOK:
				case PieceType.rook:
					legalMoves.AddRange(GetRookLegalMoves(position, History, piecePos, turnColor, CheckForIfMoveLegal));
					break;
				case PieceType.KNIGHT:
				case PieceType.knight:
					legalMoves.AddRange(GetKnightLegalMoves(position, History, piecePos, turnColor, CheckForIfMoveLegal));
					break;
				case PieceType.BISHOP:
				case PieceType.bishop:
					legalMoves.AddRange(GetBishopLegalMoves(position, History, piecePos, turnColor, CheckForIfMoveLegal));
					break;
				case PieceType.QUEEN:
				case PieceType.queen:
					legalMoves.AddRange(GetQueenLegalMoves(position, History, piecePos, turnColor, CheckForIfMoveLegal));
					break;
				case PieceType.KING:
				case PieceType.king:
					legalMoves.AddRange(GetKingLegalMoves(position, History, piecePos, turnColor, History.GetCastleOptions(turnColor), CheckForIfMoveLegal));
					break;
			}

			return legalMoves;
		}
	}

	// By GPT-4
	public static class KeyValuePairExtensions
	{
		public static bool Equals<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp1, KeyValuePair<TKey, TValue> kvp2)
			where TKey : IEquatable<TKey>
			where TValue : IEquatable<TValue>
		{
			return kvp1.Key.Equals(kvp2.Key) && kvp1.Value.Equals(kvp2.Value);
		}
	}

	public class MoveHistory
	{
		public static Dictionary<Coordinate, PieceType> DefaultInitialPosition { get; private set; } = Chessboard2.DefaultPosition();
		internal List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> History { get; set; }
		public int Count { get => History.Count; }
		public Dictionary<Coordinate, PieceType> CustomSetup = null;

		public CastleOptions WhiteCastleOptions, BlackCastleOptions;    // Keeps track of who (at the end) can castle
		public CastleOptions GetCastleOptions(Turn Color) => Color == Turn.Black ? BlackCastleOptions : WhiteCastleOptions;

		public KeyValuePair<Coordinate, Coordinate> LastMove { get => History.Count > 0 ? History[History.Count - 1].Key : new KeyValuePair<Coordinate, Coordinate>(Coordinate.NullCoord, Coordinate.NullCoord); }
		public KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> LastMoveComplete { get => History.Count > 0 ? History[History.Count - 1] : new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(new Coordinate(0, 0), new Coordinate(0, 0)), 'n'); }

		public MoveHistory(Dictionary<Coordinate, PieceType> CustomSetup = null)
		{
			this.CustomSetup = CustomSetup;
			History = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			WhiteCastleOptions = CastleOptions.Both;
			BlackCastleOptions = CastleOptions.Both;
		}

		public Dictionary<Coordinate, PieceType> CalculatePosition()
		{
			return CalculatePosition(History.Count);
		}

		public Dictionary<Coordinate, PieceType> CalculatePosition(int maxmove)
		{
			//Dictionary<Coordinate, PieceType> position = CustomSetup ?? DefaultInitialPosition;  // Only use local position if we have one, otherwise use static default position. For most use cases, the local one will be null and not take up extra storage
			Dictionary<Coordinate, PieceType> position = new Dictionary<Coordinate, PieceType>(CustomSetup ?? DefaultInitialPosition);
			
			
			if (maxmove == 0) return position;
			if (maxmove > History.Count) maxmove = History.Count;
			int currentmove = 0;
			// Apply moves
			foreach (var move in History)
			{
				currentmove++;

				// Normal Move
				if (move.Value == 'n')
				{
					position[move.Key.Value] = position[move.Key.Key];
					position.Remove(move.Key.Key);
				}
				else if (move.Value == 'e') // En Passant Take. move.Key.Key = position array.
				{
					position[move.Key.Value] = position[move.Key.Key];
					position.Remove(new Coordinate(move.Key.Key.Row, move.Key.Value.Col)); // Same row, different column as the start
					position.Remove(move.Key.Key);
				}
				else if (move.Value == 'c') // Castles. Move the rook as well.
				{
					position[move.Key.Value] = position[move.Key.Key];
					position.Remove(move.Key.Key);
					if (move.Key.Value.Col == 6)  // Kingside castle, King is now on column 6. Move Rook from Column 7 to Column 5.
					{
						position[new Coordinate(move.Key.Value.Row, 5)] = position[new Coordinate(move.Key.Value.Row, 7)];
						position.Remove(new Coordinate(move.Key.Value.Row, 7));
					}
					else  // Queenside castle, King is now on column 2. Move Rook from Column 0 to Column 3.
					{
						position[new Coordinate(move.Key.Value.Row, 3)] = position[new Coordinate(move.Key.Value.Row, 0)];
						position.Remove(new Coordinate(move.Key.Value.Row, 0));
					}
				}
				else if (move.Value == 'Q') // Promotion of a pawn to a queen
				{
					position[move.Key.Value] = currentmove % 2 == 0 /* White */ ? PieceType.QUEEN : PieceType.queen;
					position.Remove(move.Key.Key);
				}
				else if (move.Value == 'R') // Promotion of a pawn to a rook
				{
					position[move.Key.Value] = currentmove % 2 == 0 /* White */ ? PieceType.ROOK : PieceType.rook;
					position.Remove(move.Key.Key);
				}
				else if (move.Value == 'B') // Promotion of a pawn to a bishop
				{
					position[move.Key.Value] = currentmove % 2 == 0 /* White */ ? PieceType.BISHOP : PieceType.bishop;
					position.Remove(move.Key.Key);
				}
				else if (move.Value == 'K') // Promotion of a pawn to a knight
				{
					position[move.Key.Value] = currentmove % 2 == 0 /* White */ ? PieceType.KNIGHT : PieceType.knight;
					position.Remove(move.Key.Key);
				}

				if (currentmove >= maxmove) break;
			}
			// Return new position
			return position;
		}

		// '-' = error / no move, 'n' = normal move, 'e' = en passant, 'c' = castle; Q, K, B, R are for the promotion moves, when a pawn promotes on the 8th rank
		public char GetMoveType(int i)
		{
			if (i >= History.Count) return '-';
			return History[i].Value;
		}

		public void AddNormalMove(KeyValuePair<Coordinate, Coordinate> Move)
		{
			if (Move.Key == new Coordinate(0, 4) /* Black King position */) BlackCastleOptions = CastleOptions.None;
			else if (Move.Key == new Coordinate(7, 4) /* White King position */) WhiteCastleOptions = CastleOptions.None;
			else if (Move.Key == new Coordinate(0, 0) /* Black Queenside Rook position */) BlackCastleOptions = BlackCastleOptions == CastleOptions.Short ? CastleOptions.None : CastleOptions.Short;
			else if (Move.Key == new Coordinate(0, 7) /* Black Kingside Rook position */) BlackCastleOptions = BlackCastleOptions == CastleOptions.Long ? CastleOptions.None : CastleOptions.Long;
			else if (Move.Key == new Coordinate(7, 0) /* White Queenside Rook position */) WhiteCastleOptions = WhiteCastleOptions == CastleOptions.Short ? CastleOptions.None : CastleOptions.Short;
			else if (Move.Key == new Coordinate(7, 7) /* White Kingside Rook position */) WhiteCastleOptions = WhiteCastleOptions == CastleOptions.Long ? CastleOptions.None : CastleOptions.Long;
			AddMove(Move, 'n');
		}
		public void AddEnPassantMove(KeyValuePair<Coordinate, Coordinate> Move) => AddMove(Move, 'e');
		public void AddCastlesKingMove(KeyValuePair<Coordinate, Coordinate> Move)
		{
			if (Move.Key.Row == 0 /* Black (Row 0) */) BlackCastleOptions = CastleOptions.None;
			else WhiteCastleOptions = CastleOptions.None;
			AddMove(Move, 'c');
		}
		public void AddPromotionMoveQueen(KeyValuePair<Coordinate, Coordinate> Move) => AddMove(Move, 'Q');
		public void AddPromotionMoveRook(KeyValuePair<Coordinate, Coordinate> Move) => AddMove(Move, 'R');
		public void AddPromotionMoveBishop(KeyValuePair<Coordinate, Coordinate> Move) => AddMove(Move, 'B');
		public void AddPromotionMoveKnight(KeyValuePair<Coordinate, Coordinate> Move) => AddMove(Move, 'K');

		private void AddMove(KeyValuePair<Coordinate, Coordinate> Move, char MoveType)
		{
			History.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(Move, MoveType));
		}

		public MoveHistory Branch(KeyValuePair<Coordinate, Coordinate> Move, char MoveType)
		{
			MoveHistory newHistory = Clone();
			switch(MoveType)
			{
				case 'n':
					newHistory.AddNormalMove(Move);
					break;
				case 'c':
					newHistory.AddCastlesKingMove(Move);
					break;
				default:
					newHistory.AddMove(Move, MoveType);
					break;
			}
			return newHistory;
		}
		public MoveHistory Branch(KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> Move) => Branch(Move.Key, Move.Value);

		public MoveHistory Clone()
		{
			MoveHistory newHistory = new MoveHistory(CustomSetup);
			newHistory.History = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>(History);
			newHistory.WhiteCastleOptions = WhiteCastleOptions;
			newHistory.BlackCastleOptions = BlackCastleOptions;
			return newHistory;
		}

		// https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
		public string GeneratePositionKey() => GeneratePositionKey(this, CalculatePosition());
		public string GeneratePositionKey(Dictionary<Coordinate, PieceType> PositionTrust) => GeneratePositionKey(this, PositionTrust);
		public static string GeneratePositionKey(MoveHistory History, Dictionary<Coordinate, PieceType> Position = null)
		{
			if (Position == null) Position = History.CalculatePosition();
			string key = "";
			int empty = 0;
			for (int i = 0; i < 64; i++)
			{
				Coordinate coord = new Coordinate(i / 8, i % 8);
				if (Position.ContainsKey(coord) && Position[coord] != PieceType.None)
				{
					if (empty != 0) key += empty;
					empty = 0;
					key += GetPieceChar(Position[coord]);
				}
				else empty++;

				if(i % 8 == 7)
				{
					if (empty != 0) key += empty;
					empty = 0;
					key += '/';
				}
			}

			// Turn Color
			key += " " + ((History.Count == 0 || History.Count % 2 == 0) ? "w" : "b") + " ";
			// Castleing Options
			if (History.WhiteCastleOptions == CastleOptions.Both) key += "KQ";
			else if (History.WhiteCastleOptions == CastleOptions.Short) key += "K";
			else if (History.WhiteCastleOptions == CastleOptions.Long) key += "Q";
			else key += "-";

			if (History.BlackCastleOptions == CastleOptions.Both) key += "kq";
			else if (History.BlackCastleOptions == CastleOptions.Short) key += "k";
			else if (History.BlackCastleOptions == CastleOptions.Long) key += "q";
			else key += "-";
			// En passant
			// Just look at last move
			var LastMove = History.LastMove;
			if(LastMove.Key.Col == LastMove.Value.Col && Math.Abs(LastMove.Key.Row - LastMove.Value.Row) == 2
				&& Position.ContainsKey(LastMove.Value) && Position[LastMove.Value].ToString().ToUpper() == "PAWN")
				key += " " + ((char)(LastMove.Value.Col + 97)) + ((LastMove.Key.Row + LastMove.Value.Row) / 2);	// Either from 1 -> 3 => 4/2=2 or 6 -> 4 => 10/2=5

			// This should generate a key that is unique to its position but not unique to its move order (except en passant and castle)

			return key;
		}
		private static string GetPieceChar(PieceType type)
		{
			if (type == PieceType.None) return "";
			if (type.ToString().ToUpper() == "KNIGHT") return "" + type.ToString()[1];
			return "" + type.ToString()[0];
		}

		/// <summary>
		/// TODO WIP
		/// </summary>
		/// <param name="Key"></param>
		/// <returns></returns>
		public Dictionary<Coordinate, PieceType> GeneratePositionFromKey(string Key)
		{
			return new Dictionary<Coordinate, PieceType>();
		}
	}

	partial class Calculation
	{
		// Implementation of Basic methods that are not directly calculation methods; helper methods, if you will

		public static bool IsPieceColor(PieceType piece, Turn color)
		{
			if (piece == PieceType.None) return false;
			return (color == Turn.White && char.IsUpper(piece.ToString()[0])) || (color == Turn.Black && char.IsLower(piece.ToString()[0]));
		}
		public static bool IsOppositePieceColor(PieceType piece, Turn color)
		{
			if (piece == PieceType.None) return false;
			return (color == Turn.White && char.IsUpper(piece.ToString()[0])) || (color == Turn.Black && char.IsLower(piece.ToString()[0]));
		}

		public static Turn GetColorOf(PieceType Type)
		{
			if (Type == PieceType.None) return Turn.Pregame;
			return char.IsUpper((char)Type.ToString()[0]) ? Turn.White : Turn.Black;
		}

		#region Legal Move Methods

		private static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetPawnLegalMovesDiagonalCapture(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate piecePos, Turn turnColor, Coordinate dest, int pawnUp, bool enPassant, bool CheckForIfMoveLegal)
		{
			var list = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor))
			{
				if (position.ContainsKey(dest))
				{
					if (dest.Row == 0 && pawnUp == -1 || dest.Row == 7 && pawnUp == -1)
					{
						// Promotion
						list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'Q'));
						list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'R'));
						list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'B'));
						list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'K'));
					}
					else   // Normal stuff
					{
						list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
					}
				}   // Obviously En passant can't happen on the back rank so we dont need to consider it when calculating promotion
				else if (enPassant)
				{
					list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'e'));
				}
			}
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetPawnLegalMoves(Dictionary<Coordinate, PieceType> position, MoveHistory LineHistory, Coordinate piecePos, Turn turnColor, bool CheckForIfMoveLegal = true)
		{
			var list = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			int pawnUp = turnColor == Turn.Black ? 1 : -1;

			// On the first rank, pawns can move two up
			Coordinate dest = new Coordinate(piecePos.Row + pawnUp, piecePos.Col);

			// Since this part of the if-statement can also fail because of IsOutOfBounds(), the CheckForIfMoveLegal has to be inserted twice. When it's false we really dont want to call the method
			if ((!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, LineHistory, dest, turnColor)) && !position.ContainsKey(dest))
			{
				if(dest.Row == 0 && pawnUp == -1 || dest.Row == 7 && pawnUp == -1)
				{
					// Promotion
					list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'Q'));
					list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'R'));
					list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'B'));
					list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'K'));
				}
				else   // Normal stuff
				{
					list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
				}
			}

			// Pawns always move 1 up, except on the 'first' rank
			dest = new Coordinate(piecePos.Row + (2 * pawnUp), piecePos.Col);
			// The move forward is only allowed if there is noone there
			if ((!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, LineHistory, dest, turnColor)) && !position.ContainsKey(dest)
				 && (piecePos.Row == 1 && pawnUp == 1 || piecePos.Row == 6 && pawnUp == -1) && list.Count > 0 /* Move before possible, nothing is blocking */)
				list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));

			// En Passant Possible
			bool enPassantRight = false, enPassantLeft = false;
			Coordinate EnPassantRightDestination = new Coordinate(piecePos.Row, piecePos.Col + 1);
			Coordinate EnPassantLeftDestination = new Coordinate(piecePos.Row, piecePos.Col - 1);
			if (LineHistory.LastMove.Key == new Coordinate(piecePos.Row - 2 * pawnUp, piecePos.Col + 1) && LineHistory.LastMove.Value == EnPassantRightDestination
			&& position.ContainsKey(EnPassantRightDestination) && position[EnPassantRightDestination].ToString().ToUpper() == "PAWN")
				enPassantRight = true;
			if (LineHistory.LastMove.Key == new Coordinate(piecePos.Row - 2 * pawnUp, piecePos.Col - 1) && LineHistory.LastMove.Value == EnPassantLeftDestination
			&& position.ContainsKey(EnPassantLeftDestination) && position[EnPassantLeftDestination].ToString().ToUpper() == "PAWN")
				enPassantLeft = true;

			// Pawns can capture diagonally
			dest = new Coordinate(piecePos.Row + pawnUp, piecePos.Col + 1);
			list.AddRange(GetPawnLegalMovesDiagonalCapture(position, LineHistory, piecePos, turnColor, dest, pawnUp, enPassantRight, CheckForIfMoveLegal));
			dest = new Coordinate(piecePos.Row + pawnUp, piecePos.Col - 1);
			list.AddRange(GetPawnLegalMovesDiagonalCapture(position, LineHistory, piecePos, turnColor, dest, pawnUp, enPassantLeft, CheckForIfMoveLegal));


			return list;
		}

		private static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetRookLegalMoves(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate piecePos, Turn turnColor, bool CheckForIfMoveLegal = true)
		{
			var list = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, -1, 0, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 1, 0, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 0, -1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 0, 1, CheckForIfMoveLegal));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetKnightLegalMoves(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate piecePos, Turn turnColor, bool CheckForIfMoveLegal = true)
		{
			var list = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			Coordinate dest = new Coordinate(piecePos.Row - 2, piecePos.Col + 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row - 2, piecePos.Col - 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row + 2, piecePos.Col + 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row + 2, piecePos.Col - 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row + 1, piecePos.Col + 2);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row + 1, piecePos.Col - 2);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row - 1, piecePos.Col + 2);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row - 1, piecePos.Col - 2);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetBishopLegalMoves(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate piecePos, Turn turnColor, bool CheckForIfMoveLegal = true)
		{
			var list = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, -1, -1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, -1, 1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 1, -1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 1, 1, CheckForIfMoveLegal));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetQueenLegalMoves(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate piecePos, Turn turnColor, bool CheckForIfMoveLegal = true)
		{
			var list = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, -1, -1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, -1, 0, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, -1, 1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 1, -1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 1, 0, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 1, 1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 0, -1, CheckForIfMoveLegal));
			list.AddRange(GetLegalMovesInDirection(position, History, piecePos, turnColor, 0, 1, CheckForIfMoveLegal));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetKingLegalMoves(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate piecePos, Turn turnColor, CastleOptions KingCastleOptions, bool CheckForIfMoveLegal = true)
		{
			var list = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			Coordinate dest = new Coordinate(piecePos.Row - 1, piecePos.Col - 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row - 1, piecePos.Col);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row - 1, piecePos.Col + 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row + 1, piecePos.Col - 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row + 1, piecePos.Col);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row + 1, piecePos.Col + 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row, piecePos.Col - 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			dest = new Coordinate(piecePos.Row, piecePos.Col + 1);
			if (!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> (new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'n'));
			// Castleing
			if(KingCastleOptions == CastleOptions.Both || KingCastleOptions == CastleOptions.Short)
			{
				dest = new Coordinate(piecePos.Row, piecePos.Row + 2);
				Coordinate RookPos = new Coordinate(piecePos.Row, 7);
				// Rook-Check as suggested by GPT-4, PieceType Check by me
				if ((!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) &&
					position.ContainsKey(RookPos) && position[RookPos] == GetPieceOfColor(PieceType.ROOK, turnColor)) // Check if Rook exists on Column 7
				{
					list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'c'));
				}
			}
			if (KingCastleOptions == CastleOptions.Both || KingCastleOptions == CastleOptions.Long)
			{
				dest = new Coordinate(piecePos.Row, piecePos.Row - 2);
				Coordinate RookPos = new Coordinate(piecePos.Row, 0);
				// Rook-Check as suggested by GPT-4, PieceType Check by me
				if ((!CheckForIfMoveLegal && !IsOutOfBounds(dest) || CheckForIfMoveLegal && IsLegalMove(position, History, dest, turnColor)) &&
					position.ContainsKey(RookPos) && position[RookPos] == GetPieceOfColor(PieceType.ROOK, turnColor)) // Check if Rook exists on Column 0
				{
					list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, dest), 'c'));
				}
			}
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> GetLegalMovesInDirection(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate piecePos, Turn turnColor, int rowDelta, int colDelta, bool CheckForIfMoveLegal = true)
		{
			var list = new List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>>();
			int row = piecePos.Row + rowDelta, col = piecePos.Col + colDelta;
			Coordinate coord = new Coordinate(row, col);
			// Continue in a straight line as long as there are free spaces. If you encounter your own piece, cancel before adding.
			while (!CheckForIfMoveLegal && !IsOutOfBounds(coord) || CheckForIfMoveLegal && IsLegalMove(position, History, coord, turnColor))
			{
				list.Add(new KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>(new KeyValuePair<Coordinate, Coordinate>(piecePos, coord), 'n'));
				// If there is an opponent's piece on the current square, cancel the loop but still leave the move to capture
				if (position.ContainsKey(coord) && !IsPieceColor(position[coord], turnColor)) break;
				/**
				 * GPT-4: The issue could be with the Coordinate struct's properties Row and Col, which are mutable.
				 * When you update the values of coord.Row and coord.Col inside the while loop, you're directly
				 * changing the Coordinate object's properties.
				 * To fix this issue, you can create a new Coordinate object with the updated row and col values inside the loop:
				 */
				row += rowDelta;
				col += colDelta;
				coord = new Coordinate(row, col);
			}
			return list;
		}

		private static bool IsLegalMove(Dictionary<Coordinate, PieceType> position, MoveHistory History, Coordinate destination, Turn turnColor)
		{
			// Check color of destination piece, if there is one
			if (position.ContainsKey(destination) && IsPieceColor(position[destination], turnColor)) return false;
			// Check if out of bounds
			if (IsOutOfBounds(destination)) return false;
			// Check if None of the pieces could now take the king
			//Chessboard2.Log($"The move that Lands on {(char)(destination.Col + 97)}{Math.Abs(8 - destination.Row)} is not out of bounds and does not capture {turnColor}'s own piece.");

			// Different Approach:
			if (!KingSafety2_IsKingSafe_IncludeFindKing(position, turnColor)) return false;	// I know I can just return this but this is for clarity.

			//Chessboard2.Log($"No piece can take the King, everything checks out. The move that Lands on {(char)(destination.Col + 97)}{Math.Abs(8 - destination.Row)} is Legal.");
			// return true if everything checks out
			return true;
		}

		#region King Safety 2

		// TODO/Idea: Just get Knight/QueenLegalMoves with false as last parameter instead of doing this manually with KingPosition

		private static bool KingSafety2_IsKingSafe_IncludeFindKing(Dictionary<Coordinate, PieceType> Position, Turn KingColor)
		{
			PieceType King = GetPieceOfColor(PieceType.KING, KingColor);
			Coordinate KingCoordinate = Coordinate.NullCoord;
			foreach (var piece in Position)
			{
				if(piece.Value == King)
				{
					KingCoordinate = piece.Key;
					break;
				}
			}
			if (KingCoordinate == Coordinate.NullCoord) return false;
			return KingSafety2_IsKingSafe(Position, KingCoordinate, InvertColor(KingColor));
		}
		private static bool KingSafety2_IsKingSafe(Dictionary<Coordinate, PieceType> Position, Coordinate KingCoordinate, Turn OpponentColor)
		{
			if (!KingSafety2_IsKingSafe_CheckKnightMoves(Position, KingCoordinate, OpponentColor)) return false;
			if (!KingSafety2_IsKingSafe_CheckLine(Position, KingCoordinate, OpponentColor, 1, 0)) return false;
			if (!KingSafety2_IsKingSafe_CheckLine(Position, KingCoordinate, OpponentColor, -1, 0)) return false;
			if (!KingSafety2_IsKingSafe_CheckLine(Position, KingCoordinate, OpponentColor, 0, 1)) return false;
			if (!KingSafety2_IsKingSafe_CheckLine(Position, KingCoordinate, OpponentColor, 0, -1)) return false;
			if (!KingSafety2_IsKingSafe_CheckLine(Position, KingCoordinate, OpponentColor, 1, 1)) return false;
			if (!KingSafety2_IsKingSafe_CheckLine(Position, KingCoordinate, OpponentColor, 1, -1)) return false;
			if (!KingSafety2_IsKingSafe_CheckLine(Position, KingCoordinate, OpponentColor, -1, 1)) return false;
			if (!KingSafety2_IsKingSafe_CheckLine(Position, KingCoordinate, OpponentColor, -1, -1)) return false;
			return true;
		}

		private static bool KingSafety2_IsKingSafe_CheckKnightMoves(Dictionary<Coordinate, PieceType> Position, Coordinate KingCoordinate, Turn KnightColor)
		{
			Coordinate dest = new Coordinate(KingCoordinate.Row + 2, KingCoordinate.Col + 1);
			if (Position.ContainsKey(dest) && Position[dest] == GetPieceOfColor(PieceType.KNIGHT, KnightColor)) return false;
			dest = new Coordinate(KingCoordinate.Row + 2, KingCoordinate.Col - 1);
			if (Position.ContainsKey(dest) && Position[dest] == GetPieceOfColor(PieceType.KNIGHT, KnightColor)) return false;
			dest = new Coordinate(KingCoordinate.Row + 1, KingCoordinate.Col + 2);
			if (Position.ContainsKey(dest) && Position[dest] == GetPieceOfColor(PieceType.KNIGHT, KnightColor)) return false;
			dest = new Coordinate(KingCoordinate.Row + 1, KingCoordinate.Col - 2);
			if (Position.ContainsKey(dest) && Position[dest] == GetPieceOfColor(PieceType.KNIGHT, KnightColor)) return false;
			dest = new Coordinate(KingCoordinate.Row - 2, KingCoordinate.Col + 1);
			if (Position.ContainsKey(dest) && Position[dest] == GetPieceOfColor(PieceType.KNIGHT, KnightColor)) return false;
			dest = new Coordinate(KingCoordinate.Row - 2, KingCoordinate.Col - 1);
			if (Position.ContainsKey(dest) && Position[dest] == GetPieceOfColor(PieceType.KNIGHT, KnightColor)) return false;
			dest = new Coordinate(KingCoordinate.Row - 1, KingCoordinate.Col + 2);
			if (Position.ContainsKey(dest) && Position[dest] == GetPieceOfColor(PieceType.KNIGHT, KnightColor)) return false;
			dest = new Coordinate(KingCoordinate.Row - 1, KingCoordinate.Col - 2);
			if (Position.ContainsKey(dest) && Position[dest] == GetPieceOfColor(PieceType.KNIGHT, KnightColor)) return false;
			return true;
		}
		private static bool KingSafety2_IsKingSafe_CheckLine(Dictionary<Coordinate, PieceType> Position, Coordinate KingCoordinate, Turn OpponentColor, int rowDelta, int colDelta)
		{
			int row = KingCoordinate.Row + rowDelta, col = KingCoordinate.Col + colDelta;

			if (row == 0 && col == 0) return false;
			bool IsRow = row == 0 ^ col == 0;	// Exclusive or symbol '^'

			Coordinate coord = new Coordinate(row, col);
			
			while (!IsOutOfBounds(coord))
			{
				if (Position.ContainsKey(coord))
				{
					if (GetColorOf(Position[coord]) == OpponentColor) return true;
					// Check if on the diagonal there is a Queen or, depending on diagonal or not, a bishop/rook. When we hit one of our own pieces though, its immediately return true since something is blocking it
					if ((Position[coord] == GetPieceOfColor(PieceType.QUEEN, OpponentColor) ||
						Position[coord] == GetPieceOfColor(IsRow ? PieceType.ROOK : PieceType.BISHOP, OpponentColor))) return false;
				}
				row += rowDelta;
				col += colDelta;
				coord = new Coordinate(row, col);
			}
			return true;
		}
		private static PieceType GetPieceOfColor(PieceType Type, Turn Color)
		{
			switch (Color)
			{
				case Turn.White:
					switch (Type)
					{
						// It's also possible to just use parsing and ToUpper() and ToLower()
						case PieceType.PAWN:
						case PieceType.pawn:
							return PieceType.PAWN;
						case PieceType.ROOK:
						case PieceType.rook:
							return PieceType.ROOK;
						case PieceType.KNIGHT:
						case PieceType.knight:
							return PieceType.KNIGHT;
						case PieceType.BISHOP:
						case PieceType.bishop:
							return PieceType.BISHOP;
						case PieceType.QUEEN:
						case PieceType.queen:
							return PieceType.QUEEN;
						case PieceType.KING:
						case PieceType.king:
							return PieceType.KING;
					}
					break;
				case Turn.Black:
					switch (Type)
					{
						// It's also possible to just use parsing and ToUpper() and ToLower()
						case PieceType.PAWN:
						case PieceType.pawn:
							return PieceType.pawn;
						case PieceType.ROOK:
						case PieceType.rook:
							return PieceType.rook;
						case PieceType.KNIGHT:
						case PieceType.knight:
							return PieceType.knight;
						case PieceType.BISHOP:
						case PieceType.bishop:
							return PieceType.bishop;
						case PieceType.QUEEN:
						case PieceType.queen:
							return PieceType.queen;
						case PieceType.KING:
						case PieceType.king:
							return PieceType.king;
					}
					break;
			}
			return PieceType.None;
		}

		#endregion

		private static bool IsOutOfBounds(Coordinate destination)
		{
			// Check if destination is out of bounds
			return (destination.Row < 0 || destination.Row > 7 || destination.Col < 0 || destination.Col > 7);
		}

		#endregion

		public static string MoveToString(Dictionary<Coordinate, PieceType> CurrentPosition, KeyValuePair<Coordinate, Coordinate> Move, char MoveType)  // No own MoveType enum because a char takes up less bit (16) than an integer (64)
		{
			MoveHistory MoveHistory = new MoveHistory(CurrentPosition);
			string move = "";
			PieceType PieceType = CurrentPosition.ContainsKey(Move.Key) ? CurrentPosition[Move.Key] : PieceType.None;
			if (PieceType == PieceType.None) return "None";

			switch (CurrentPosition[Move.Key])
			{
				case PieceType.PAWN:
				case PieceType.pawn:
					move += (char)(Move.Key.Col + 97);   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.KNIGHT:
				case PieceType.knight:
					move += 'N';   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.BISHOP:
				case PieceType.bishop:
					move += 'B';   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.ROOK:
				case PieceType.rook:
					move += 'R';   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.QUEEN:
				case PieceType.queen:
					move += 'Q';   // Lowercase 'a' + column, column 0 is 'a'
					break;
				case PieceType.KING:
				case PieceType.king:
					if (Move.Key.Row == Move.Value.Row && Math.Abs(Move.Key.Col - Move.Value.Col) == 2)
					{
						if (Move.Key.Col > Move.Value.Col) return "O-O-O";
						else return "O-O";
					}
					move += 'K';   // Lowercase 'a' + column, column 0 is 'a'
					break;
			}

			// Find out if multiple pieces can go to that position
			List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> MovesFromDestination = GetPieceLegalMoves(CurrentPosition, MoveHistory, Move.Value, PieceType, GetColorOf(PieceType), false);

			foreach (var pair in MovesFromDestination)
			{
				var dest = pair.Key;
				if (CurrentPosition.ContainsKey(dest.Value) && CurrentPosition[dest.Value] == PieceType && dest.Value != Move.Key /* "Duplicate" right now since move has not yet been applied maybe */ )
				{
					// There is another piece that can go there, so we need to distinguish
					if (dest.Value.Row == dest.Key.Row)   // If the row is the same, we need to specify which column
					{
						move += (char)(Move.Key.Col + 97);
					}
					else if (dest.Value.Col == dest.Key.Col)  // If the column is the same, we need to specify which row
					{
						move += Move.Key.Row;
					}
					break;
				}
			}

			if (CurrentPosition.ContainsKey(Move.Value) && CurrentPosition[Move.Value] != PieceType.None)
			{
				move += 'x';
			}

			// Destination square
			if (!move.Equals("" + (char)(Move.Value.Col + 97))) move += (char)(Move.Value.Col + 97);	// TODO Col can be -1 to 8 instead of 0 to 7
			move += Math.Abs(8 - Move.Value.Row);   // Move.Value.Row = 0 - 7 but opposite order.	7 -> 1, 6 -> 2, 5 -> 3... 0 -> 8

			/*
			switch(MoveType)
			{
				case 'Q': move += "=Q"; break;
				case 'R': move += "=R"; break;
				case 'B': move += "=B"; break;
				case 'K': move += "=N"; break;
			}*/
			// Other way:
			if (char.IsUpper(MoveType)) move += '=' + MoveType;


			// Add check or mate
			//Calculation calc = new Calculation(MoveHistory.Branch(Move, MoveType), 1, GetColorOf(PieceType));
			//if (calc.IsCheckmate) move += '#';
			//else if (calc.IsCheck) move += '+';

			return move;
		}

		public static int GetPieceValue(PieceType Piece)
		{
			int value = (int)Piece;
			if (value > 10) value -= 10;
			return value == 2 ? 3 : value;
		}
	}

	partial class Chessboard2   // Input / UI / Mouse Event Handling
	{
		public bool CurrentlyHolding = false;
		public PieceType CurrentlyHoldingType;
		public Point CurrentMousePosition;
		public Calculation CurrentCalculation;

		Coordinate _SelectedField;
		Coordinate SelectedField { get => _SelectedField; set { if (value != Coordinate.NullCoord) GrabbedPieceTime = DateTime.Now; _SelectedField = value; } }
		List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> AllLegalMoves, SelectedLegalMoves;
		List<Coordinate> HighlightedFields;

		public int MaxEngineDepth = 50, MaxEngineTimeMS = 100000;

		public const int MoveDelayMS = 200;

		private string lastMove = "";
		public void ApplyMove(KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> move)
		{
			// Evaluate Sound
			SoundType Sound = SoundType.Move;

			switch (move.Value)
			{
				case 'n': MoveHistory.AddNormalMove(move.Key); break;
				case 'e': MoveHistory.AddEnPassantMove(move.Key); break;
				case 'c': MoveHistory.AddCastlesKingMove(move.Key); break;
				case 'Q': MoveHistory.AddPromotionMoveQueen(move.Key); break;
				case 'R': MoveHistory.AddPromotionMoveRook(move.Key); break;
				case 'B': MoveHistory.AddPromotionMoveBishop(move.Key); break;
				case 'K': MoveHistory.AddPromotionMoveKnight(move.Key); break;
			}

			AllLegalMoves.Clear();
			Turn = Calculation.InvertColor(Turn);
			HighlightedFields.Clear();
			DeselectCurrentField();

			CurrentPosition = MoveHistory.CalculatePosition();
			AllLegalMoves.AddRange(Calculation.GetAllLegalMoves(CurrentPosition, Turn, MoveHistory));
			if (CurrentCalculation != null) CurrentCalculation.AbortCalculation();

			if (MoveHistory.Count > 0 && Turn == Turn.White)
			{
				Log($"{MoveHistory.Count % 2}. {lastMove}   {Calculation.MoveToString(CurrentPosition, move.Key, move.Value)}");
			}
			else if(Turn == Turn.Black)
			{
				lastMove = Calculation.MoveToString(CurrentPosition, move.Key, move.Value);
			}
			
			ChessGraphics.PlaySound(Sound);
			
			Refresh();
			Sleep();
			Refresh();

			CurrentCalculation = new Calculation(MoveHistory, MaxEngineDepth, Turn) { maxTimeMS = this.MaxEngineTimeMS };
		}
		public static void Sleep(int millis = MoveDelayMS)
		{
			System.Threading.Thread.Sleep(millis);
		}

		void EventInit()
		{
			this.MouseMove += (s, e) => OnMouseMoved(e);
			this.MouseDown += (s, e) => MouseDownEvent(e);
			this.MouseUp += (s, e) => MouseUpEvent(e);
		}

		private void DeselectCurrentField()
		{
			CurrentlyHolding = false;
			SelectedField = Coordinate.NullCoord;
			SelectedLegalMoves.Clear();
			CurrentlyHoldingType = PieceType.None;
		}

		// Update Mouse Position
		public void OnMouseMoved(MouseEventArgs e)
		{
			if (!CurrentlyHolding) return;
			
			CurrentMousePosition = new Point(e.X, e.Y);
			Refresh();
		}

		private Coordinate Event_GetFieldByMouseLocation(Point MouseLocation)
		{
			int DisplayFieldSize = DisplaySize / 8;
			int Row = MouseLocation.Y / DisplayFieldSize;
			int Col = MouseLocation.X / DisplayFieldSize;
			// Invert when Blacks turn
			return Turn == Turn.White ? new Coordinate(Row, Col) :
				new Coordinate(Math.Abs(7 - Row), Math.Abs(7 - Col));
		}

		private DateTime GrabbedPieceTime;

		// Mouse Down
		public void MouseDownEvent(MouseEventArgs e)
		{
			Coordinate field = Event_GetFieldByMouseLocation(e.Location);

			if (e.Button == MouseButtons.Left)
			{
				if(field == SelectedField && CurrentlyHolding /* Otherwise we might be wanting to pick it up */)
				{
					// Deselect
					DeselectCurrentField();
					return;
				}

				// Get FieldType
				PieceType FieldType;// = CurrentPosition.ContainsKey(field) ? CurrentPosition[field] : PieceType.None;
				if (!CurrentPosition.TryGetValue(field, out FieldType)) FieldType = PieceType.None;

				// If no field is selected and we click on one that isn't ours, nothing happens except possible deselect
				if(SelectedField == Coordinate.NullCoord && !Calculation.IsPieceColor(FieldType, Turn))
				{
					DeselectCurrentField();
					return;
				}

				// Clicked on Field that isnt ours
				if (InvokeClickedOnFieldWhenSelected(field, FieldType)) return;

				// Clicked on Field that is ours
				this.CurrentMousePosition = e.Location;
				SelectedField = field;
				CurrentlyHoldingType = CurrentPosition[field];
				CurrentlyHolding = true;
				UpdateSelectedLegalMoves();

				Refresh();
			}
		}

		private void UpdateSelectedLegalMoves()
		{
			SelectedLegalMoves.Clear();
			// Get all legal moves
			foreach (var move in AllLegalMoves)
			{
				if (move.Key.Key == SelectedField) SelectedLegalMoves.Add(move);
			}
		}

		/// <summary>
		/// Invoked when clicked on a field. Also invoked when held figure is dropped on the field. <br/>
		/// Returns false when the clicked field contains a Piece of the own color. Returns true if <br/>
		/// the Field was not an own field, action has been taken accordingly.
		/// </summary>
		/// <param name="field"></param>
		/// <returns>False, True if action has been taken.</returns>
		public bool InvokeClickedOnFieldWhenSelected(Coordinate field, PieceType FieldType)
		{
			if (FieldType != PieceType.None && Calculation.GetColorOf(FieldType) == Turn)
			{
				Refresh();
				return false;
			}

			// Update our legal moves
			UpdateSelectedLegalMoves();

			// Contains doenst work here
			foreach (var move in SelectedLegalMoves)
			{
				if (move.Key.Value == field)
				{
					// Move is legal, make the move
					ApplyMove(move);
					Refresh();
					return true;
				}
			}
			// Not in legal moves (cant be applied)
			DeselectCurrentField();
			Refresh();
			return true;
		}

		// Mouse Up
		public void MouseUpEvent(MouseEventArgs e)
		{
			Coordinate field = Event_GetFieldByMouseLocation(e.Location);

			if (e.Button == MouseButtons.Left)
			{
				if (CurrentlyHolding)
				{
					// Get FieldType
					PieceType FieldType;
					if (!CurrentPosition.TryGetValue(field, out FieldType)) FieldType = PieceType.None;

					// Dropped somewhere
					if (field == SelectedField) { if ((DateTime.Now - GrabbedPieceTime).Milliseconds > 500) DeselectCurrentField(); }
					else if (InvokeClickedOnFieldWhenSelected(field, FieldType)) return;
					CurrentlyHolding = false;
				}

				Refresh();
			}
		}

		public void SelectField(Coordinate Field)
		{
			SelectedField = Field;
			// Paint All Legal Moves

		}
	}

	public static class ChessGraphics
	{
		public static Dictionary<PieceType, Image> PieceImages = new Dictionary<PieceType, Image>();

		public static void Init()
		{
			using (WebClient client = new WebClient())
			{
				System.Diagnostics.Debug.WriteLine($"Loading Images... ({FileDirectory})");

				if (!System.IO.Directory.Exists(FileDirectory)) System.IO.Directory.CreateDirectory(FileDirectory);
				// normal names = white pieces, _ in front = black pieces, in code its WHITE and black
				if (!System.IO.File.Exists(FileDirectory + "pawn.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075486831514226769/Pawn.png"), FileDirectory + "pawn.png");
				if (!System.IO.File.Exists(FileDirectory + "_pawn.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075487044278689872/Pawn.png"), FileDirectory + "_pawn.png");
				if (!System.IO.File.Exists(FileDirectory + "knight.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075486817207459953/Knight.png"), FileDirectory + "knight.png");
				if (!System.IO.File.Exists(FileDirectory + "_knight.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075487026868129893/Knight.png"), FileDirectory + "_knight.png");
				if (!System.IO.File.Exists(FileDirectory + "bishop.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075486806977556500/Bishop.png"), FileDirectory + "bishop.png");
				if (!System.IO.File.Exists(FileDirectory + "_bishop.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075487016898269307/Bishop.png"), FileDirectory + "_bishop.png");
				if (!System.IO.File.Exists(FileDirectory + "rook.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075486824635576400/Rook.png"), FileDirectory + "rook.png");
				if (!System.IO.File.Exists(FileDirectory + "_rook.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075487034380124161/Rook.png"), FileDirectory + "_rook.png");
				if (!System.IO.File.Exists(FileDirectory + "queen.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075486847037345881/Queen.png"), FileDirectory + "queen.png");
				if (!System.IO.File.Exists(FileDirectory + "_queen.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075486788837195876/Queen.png"), FileDirectory + "_queen.png");
				if (!System.IO.File.Exists(FileDirectory + "king.png")) client.DownloadFile(new Uri("https://cdn.discordapp.com/attachments/987855802905817098/1075486771191758888/King.png"), FileDirectory + "king.png");
				if (!System.IO.File.Exists(FileDirectory + "_king.png")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/987855802905817098/1075486840771055667/King.png"), FileDirectory + "_king.png");

				System.Diagnostics.Debug.WriteLine($"Loading Audio files... ({FileDirectory})");

				if (!System.IO.File.Exists(FileDirectory + "GameStart.wav")) client.DownloadFile(new Uri("https://cdn.discordapp.com/attachments/1075559670598598747/1075867514065653760/GameStart.wav"), FileDirectory + "GameStart.wav");
				if (!System.IO.File.Exists(FileDirectory + "Draw.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867516670316574/Draw.wav"), FileDirectory + "Draw.wav");
				if (!System.IO.File.Exists(FileDirectory + "MateWin.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867537948037270/MateWin.wav"), FileDirectory + "MateWin.wav");
				if (!System.IO.File.Exists(FileDirectory + "MateLoss.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867537637650442/MateLoss.wav"), FileDirectory + "MateLoss.wav");
				if (!System.IO.File.Exists(FileDirectory + "Move1.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867514409590925/Move1.wav"), FileDirectory + "Move1.wav");
				if (!System.IO.File.Exists(FileDirectory + "Move2.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867514673844424/Move2.wav"), FileDirectory + "Move2.wav");
				if (!System.IO.File.Exists(FileDirectory + "Castle1.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867514967429200/Castle1.wav"), FileDirectory + "Castle1.wav");
				if (!System.IO.File.Exists(FileDirectory + "Castle2.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867515277811753/Castle2.wav"), FileDirectory + "Castle2.wav");
				if (!System.IO.File.Exists(FileDirectory + "Capture1.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867515533676544/Capture1.wav"), FileDirectory + "Capture1.wav");
				if (!System.IO.File.Exists(FileDirectory + "Capture2.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867515823067216/Capture2.wav"), FileDirectory + "Capture2.wav");
				if (!System.IO.File.Exists(FileDirectory + "Check1.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867516078915765/Check1.wav"), FileDirectory + "Check1.wav");
				if (!System.IO.File.Exists(FileDirectory + "Check2.wav")) client.DownloadFile(new Uri(@"https://cdn.discordapp.com/attachments/1075559670598598747/1075867516397703289/Check2.wav"), FileDirectory + "Check2.wav");

				PieceImages.Add(PieceType.PAWN, Image.FromFile(FileDirectory + "pawn.png"));
				PieceImages.Add(PieceType.pawn, Image.FromFile(FileDirectory + "_pawn.png"));
				PieceImages.Add(PieceType.KNIGHT, Image.FromFile(FileDirectory + "knight.png"));
				PieceImages.Add(PieceType.knight, Image.FromFile(FileDirectory + "_knight.png"));
				PieceImages.Add(PieceType.BISHOP, Image.FromFile(FileDirectory + "bishop.png"));
				PieceImages.Add(PieceType.bishop, Image.FromFile(FileDirectory + "_bishop.png"));
				PieceImages.Add(PieceType.ROOK, Image.FromFile(FileDirectory + "rook.png"));
				PieceImages.Add(PieceType.rook, Image.FromFile(FileDirectory + "_rook.png"));
				PieceImages.Add(PieceType.QUEEN, Image.FromFile(FileDirectory + "queen.png"));
				PieceImages.Add(PieceType.queen, Image.FromFile(FileDirectory + "_queen.png"));
				PieceImages.Add(PieceType.KING, Image.FromFile(FileDirectory + "king.png"));
				PieceImages.Add(PieceType.king, Image.FromFile(FileDirectory + "_king.png"));
			}
		}

		public static Image GetImage(PieceType PieceType)
		{
			if(PieceImages.ContainsKey(PieceType)) return PieceImages[PieceType];
			return null;
		}

		private static string FileDirectory = System.IO.Directory.GetCurrentDirectory() + "/Chessfiles/";

		public static void PlaySound(SoundType t)
		{
			string name = $"{t}";
			if (t != SoundType.Draw && !t.ToString().Contains("Mate") && t != SoundType.GameStart)
				name += (new Random().Next(2) + 1); // 1 / 2
			name = FileDirectory + name + ".wav";

			if (!System.IO.File.Exists(name)) { System.Diagnostics.Debug.WriteLine("Could not find file " + name); return; }

			System.Media.SoundPlayer Player = new System.Media.SoundPlayer(name);
			Player.Play();
		}
	}

	public enum PieceType
	{
		KING = 10, QUEEN = 9, BISHOP = 3, ROOK = 5, KNIGHT = 2 /* treat as 3 */, PAWN = 1, None = 0,
		king = 20, queen = 19, bishop = 13, rook = 15, knight = 12, pawn = 11   // Black pieces, use values from white
	}

	// Suggested by GPT-4 in response to a KeyNotFoundException is MoveHistory:
	/// <summary>
	/// ---==={ GPT-4 Implementation }===--- <br/> <br/>
	/// 
	/// GPT-4: The root cause of this issue is that you are using Coordinate as keys for your dictionaries. When comparing arrays in C#, <br/>
	/// the default comparison checks if the references are the same, not if the content is the same. As a result, your code does not <br/>
	/// find the correct keys in the dictionary, even if the contents of the arrays are the same. <br/> <br/>
	/// 
	/// To fix the issue I suggest creating a custom class for the coordinates instead of using Coordinate. This way, you can override the Equals <br/>
	/// and GetHashCode methods to provide proper comparison logic. Then, replace all instances of Coordinate used as keys in your dictionaries <br/>
	/// with the new Coordinate class. This should fix the System.Collections.Generic.KeyNotFoundException error you're encountering.
	/// 
	/// </summary>
	public class Coordinate
	{
		public static Coordinate NullCoord = new Coordinate(-1, -1);
		public int Row { get; set; }
		public int Col { get; set; }

		public Coordinate(int row, int col)
		{
			Row = row;
			Col = col;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			Coordinate other = (Coordinate)obj;
			return Row == other.Row && Col == other.Col;
		}

		/// <summary>
		/// GPT-4: This implementation [of the GetHashCode() method] combines the X and Y properties of the Coordinate class in a simple arithmetic <br/>
		/// operation, with a prime number (17) as a multiplier to reduce the likelihood of hash collisions. This is a basic example and <br/>
		/// might not be the most optimal implementation, but it should work well for most use cases.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Row * 17 + Col;
		}

		// Overload == and != operators
		public static bool operator ==(Coordinate c1, Coordinate c2)
		{
			if (ReferenceEquals(c1, null))
			{
				return ReferenceEquals(c2, null);
			}

			return c1.Equals(c2);
		}

		public static bool operator !=(Coordinate c1, Coordinate c2)
		{
			return !(c1 == c2);
		}

		public override string ToString()
		{
			return $"({Row}, {Col})";
		}
	}
}