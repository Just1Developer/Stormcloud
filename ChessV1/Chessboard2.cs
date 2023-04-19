﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using System.Linq;

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
		void IChessboard.Reset() {}
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
		private Turn TurnColor;
		private int Depth;
		public double BestScore { get; private set; } = 0;
		public KeyValuePair<Coordinate, Coordinate> _BestMove { get; private set; } = new KeyValuePair<Coordinate, Coordinate>(new Coordinate(-1, -1), new Coordinate(-1, -1));
		public KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> BestMove { get; private set; } = EmptyMove;
		private Dictionary<KeyValuePair<Coordinate, Coordinate>, double> Scores;

		public DateTime StartTime;
		public static int defaultMaxTimeMS = 10000;
		public int maxTimeMS;
		public void AbortCalculation() => Depth = -1; /* Causes cancellation on the next check */
		public bool IsCheck = false;
		public bool IsCheckmate = false;
		public bool IsStalemate = false;
		public bool IsDraw = false;

		public int FinalTimeMS = 0;
		public double FinalDepth = 0;

		public MoveHistory UpUntilPositionHistory = null;

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
			Depth = depth;
			maxTimeMS = defaultMaxTimeMS;
			StartTime = DateTime.Now;
			AllInitialMoveScores = new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>();

			Scores = new Dictionary<KeyValuePair<Coordinate, Coordinate>, double>();
			CalculateBestMove();
		}

		private void ProcessNewDepth(Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>> initialMoveScores, double Depth)
		{
			// GPT-4:

			// Sort the initialMoveScores dictionary based on the calculated move score
			var sortedInitialMoveScores = initialMoveScores.OrderByDescending(entry => CalculateMoveScore(entry.Value)).ToList();

			// To get the move with the highest overall score, you can simply access the first element of the sorted list:
			var bestMove = sortedInitialMoveScores.First();
			BestMove = bestMove.Key;
			BestScore = CalculateMoveScore(bestMove.Value);

			Form1.self.SetBestMove(MoveToString(UpUntilPositionHistory.CalculatePosition(), BestMove.Key, BestMove.Value), (int) (BestScore * 100), (int) Depth);
		}

		private const double AverageScoreWeight = 0.6, HighestScoreWeight = 0.25, LowestScoreWeight = 0.15, CheckmateLinePercentageWeight = 0.4 /* How many of the lines lead to checkmate */;
		private double CalculateMoveScore(KeyValuePair<List<double>, int> Score) => CalculateMoveScore(Score.Key, Score.Value);
		private double CalculateMoveScore(List<double> Scores, int CheckmateLines)
		{
			if (Scores.Count == 0) return 0;

			double score = 0;
			score += (Scores.Sum() / Scores.Count) * AverageScoreWeight;
			score += Scores.Max() * HighestScoreWeight;
			score += Scores.Min() * LowestScoreWeight;

			if (CheckmateLines > 0)
			{
				score += ((double)Scores.Count / CheckmateLines) * CheckmateLinePercentageWeight;
			}

			return score;
		}


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

		/// <summary>
		/// 
		/// Documentation provided by GPT-4. <br/> <br/>
		/// 
		/// The CalculateBestMove method iteratively explores the search tree, updates the scores for each line, and <br/>
		/// stores the aggregated scores for each line in the Scores dictionary to determine the best move.
		/// 
		/// </summary>
		private void CalculateBestMove()
		{
			Turn initialTurnColor = this.TurnColor;
			MoveHistory initialHistory = this.UpUntilPositionHistory.Clone();
			
			/**
			 This segment defines the CalculateBestMove method and sets up the lineScores dictionary to store the scores of each line,
			a stack called searchStack to store the search nodes, and then pushes the initial node onto the stack.
			 */

			Dictionary<KeyValuePair<Coordinate, Coordinate>, List<double>> lineScores = new Dictionary<KeyValuePair<Coordinate, Coordinate>, List<double>>();
			Stack<SearchNode> searchStack = new Stack<SearchNode>();
			searchStack.Push(new SearchNode(initialHistory, initialTurnColor, 0.0/*, EmptyMove*/, EmptyMove)); // Pass null as the initial InitialMoveIndex

			// Method Restructure
			double currentDepth = 0.0;

			var initialMoveScores = new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>();

			/**
			 * This while loop iterates until the search stack is empty. It processes each node in the search tree.
			 */
			while (searchStack.Count > 0)
			{
				/**
				 * This segment pops a node from the stack, retrieves the depth, turn color, move history, and scores for the current line.
				 */
				SearchNode currentNode = searchStack.Pop();

				if(currentNode.Depth > currentDepth && currentNode.Depth % 1.0 == 0.0 && initialMoveScores.Count > 0)
				{
					ProcessNewDepth(initialMoveScores, currentNode.Depth);
					// Clear Checkmate line amount
					foreach (var initialMoveSet in new Dictionary<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>, KeyValuePair<List<double>, int>>(initialMoveScores))
					{
						initialMoveScores[initialMoveSet.Key] = new KeyValuePair<List<double>, int>(initialMoveScores[initialMoveSet.Key].Key, 0);
					}
				}

				currentDepth = currentNode.Depth;
				Turn currentTurnColor = currentNode.TurnColor;
				MoveHistory currentHistory = currentNode.History;

				/**
				 * Check if the maximum depth has been reached, if it's reached, the method calls Finish and returns.
				 */
				if (currentDepth > Depth /* || (DateTime.Now - StartTime).TotalMilliseconds > this.maxTimeMS*/)
				{
					ProcessNewDepth(initialMoveScores, currentNode.Depth);
					return;
				}

				/**
				 * This segment calculates the current position, gets all legal moves, and then processes each move in a loop.
				 */
				var pos = currentHistory.CalculatePosition();
				var allLegalMoves = GetAllLegalMoves(pos, currentTurnColor, currentHistory);

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
					IsCheck = KingSafety2_IsKingSafe_IncludeFindKing(pos, initialTurnColor);
					if (allLegalMoves.Count == 0)
					{
						// No legal moves. Now its either Stalemate or Checkmate
						if (IsCheck) IsCheckmate = true;
						else IsStalemate = true;
						ProcessNewDepth(initialMoveScores, currentNode.Depth);
						return;
					}
					else if(pos.Count <= 2)
					{
						IsDraw = true;
						ProcessNewDepth(initialMoveScores, currentNode.Depth);
						return;
					}
				}

				foreach (var move in allLegalMoves)
				{
					// Calculate the move score before branching
					double MoveScore = GetScoreOf(move, currentHistory);
					if (currentTurnColor != initialTurnColor) MoveScore *= -1;

					// Branch the current history with the move
					MoveHistory newHistory = currentHistory.Branch(move);

					// Score stuff
					{
						// Check if the current node is a child of the root node (currentDepth == 0.5)
						var initMove = currentNode.InitialMove;


						if (!initMove.Key.Equals(EmptyMove.Key) && initialMoveScores.ContainsKey(initMove))
						{
							// If initial move is an actual move, add the score
							List<double> _ = new List<double>(initialMoveScores[initMove].Key);
							_.Add(MoveScore);
							KeyValuePair<List<double>, int> newValue;

							if (pos.ContainsKey(move.Key.Value) && pos[move.Key.Value].ToString().ToUpper() == "KING")
								newValue = new KeyValuePair<List<double>, int>(_, initialMoveScores[initMove].Value + 1);
							else
								newValue = new KeyValuePair<List<double>, int>(_, initialMoveScores[initMove].Value);
							initialMoveScores[initMove] = newValue;
						}
					}

					// Push the new node onto the search stack with the updated information
					searchStack.Push(new SearchNode(newHistory, InvertColor(currentTurnColor), currentDepth + 0.5, currentNode.InitialMove.Key.Equals(EmptyMove.Key) ? move : currentNode.InitialMove));
				}
			}
		}

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

		// Eval Weights
		private const double PieceCaptureWeight = 1.0;
		private const double ActivityWeight = 0.05;    // Row activity on the opponents side
		private const double BoardVisionActivityWeight = 0.03;    // Activity: How many Squares / Legal moves the piece has once it arrives
		private const double PromotionWeight = 0.9;
		private const double PositionMaterialAdvantageWeight = 0.65;
		/// <summary>
		/// Method Call BEFORE move is made. Evaluates a move based on Capture score, activity and general material advantage.
		/// </summary>
		/// <param name="move">The move to be evaluated.</param>
		/// <param name="currentHistory">The current MoveHistory of the Position.</param>
		/// <param name="turnColor">The Color of the current Turn.</param>
		private double GetScoreOf(KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> move, MoveHistory currentHistory, Turn turnColor = Turn.Pregame)
			=> GetScoreOf(move, currentHistory.CalculatePosition(), turnColor == Turn.Pregame ? currentHistory.Count % 2 == 0 ? Turn.White : Turn.Black : turnColor);
		/// <summary>
		/// Method Call BEFORE move is made. Evaluates a move based on Capture score, activity and general material advantage.
		/// </summary>
		/// <param name="move">The move to be evaluated.</param>
		/// <param name="Position">The Boardposition.</param>
		/// <param name="turnColor">The Color of the current Turn.</param>
		/// <returns></returns>
		private double GetScoreOf(KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char> move, Dictionary<Coordinate, PieceType> Position, Turn turnColor = Turn.Pregame)
		{
			PieceType PieceType = Position.ContainsKey(move.Key.Key) ? Position[move.Key.Key] : PieceType.None;
			// First, just evaluate the capture of the piece
			double scoreOfPiececapture = Position.ContainsKey(move.Key.Value) ? GetPieceValue(Position[move.Key.Value]) : 0;
			double PromotionScore = 0;

			switch (move.Value)
			{
				case 'Q': PromotionScore = GetPieceValue(PieceType.QUEEN) - 1; break;
				case 'R': PromotionScore = GetPieceValue(PieceType.ROOK) - 1; break;
				case 'B': PromotionScore = GetPieceValue(PieceType.BISHOP) - 1; break;
				case 'K': PromotionScore = GetPieceValue(PieceType.KNIGHT) - 1; break;
			}

			double PositionMaterialAdvantage = 0;
			if(turnColor == Turn.Pregame && Position.ContainsKey(move.Key.Value)) turnColor = GetColorOf(Position[move.Key.Key]);
			foreach (var piece in Position)
			{
				if (IsPieceColor(piece.Value, turnColor)) PositionMaterialAdvantage += GetPieceValue(piece.Value);
				else PositionMaterialAdvantage -= GetPieceValue(piece.Value);
			}

			double score = (scoreOfPiececapture * PieceCaptureWeight);
			// Activity
			if(turnColor == Turn.Black && move.Key.Value.Row > 3) score += move.Key.Value.Row - 3 * ActivityWeight;	// Max: 4 * Weight => 0.2
			else if(turnColor == Turn.White && move.Key.Value.Row < 4) score += Math.Abs(4 - move.Key.Value.Row) * ActivityWeight;

			// Also an Idea: Activity Eval: How many squares can the piece see -> GetPieceLegalMoves().Count;
			if(PieceType != PieceType.None)
			{
				score += GetPieceLegalMoves(Position, new MoveHistory(Position), move.Key.Value, PieceType, turnColor, false, false).Count * BoardVisionActivityWeight;
			}
			
			score += PromotionScore * PromotionWeight;
			score += PositionMaterialAdvantage * PositionMaterialAdvantageWeight;

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
		private List<KeyValuePair<KeyValuePair<Coordinate, Coordinate>, char>> History { get; set; }
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
				/**
				 
				GPT-4 wants this to catch the Castleing error:
				else if (move.Value == 'c') // Castles. Move the rook as well.
				{
					position[move.Key.Value] = position[move.Key.Key];
					position.Remove(move.Key.Key);
					if (move.Key.Value.Col == 6)  // Kingside castle, King is now on column 6. Move Rook from Column 7 to Column 5.
					{
						if (position.ContainsKey(new Coordinate(move.Key.Value.Row, 7))) // Check if Rook exists on Column 7
						{
							position[new Coordinate(move.Key.Value.Row, 5)] = position[new Coordinate(move.Key.Value.Row, 7)];
							position.Remove(new Coordinate(move.Key.Value.Row, 7));
						}
					}
					else  // Queenside castle, King is now on column 2. Move Rook from Column 0 to Column 3.
					{
						if (position.ContainsKey(new Coordinate(move.Key.Value.Row, 0))) // Check if Rook exists on Column 0
						{
							position[new Coordinate(move.Key.Value.Row, 3)] = position[new Coordinate(move.Key.Value.Row, 0)];
							position.Remove(new Coordinate(move.Key.Value.Row, 0));
						}
					}
				}
				 
				 */
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

			/*
			foreach (var piece in position)
			{
				if (GetColorOf(piece.Value) == turnColor) continue;
				var LegalMoves = GetPieceLegalMoves(position, History, piece.Key, piece.Value, turnColor, false, false);	// We dont actually need the History here because EnPassant is not important but w/e
				foreach (var move in LegalMoves)
				{
					if (position.ContainsKey(move.Key.Value) && position[move.Key.Value].ToString().ToUpper() == "KING")
						return false;	// When performing the given move, at least one move of the opponent can capture the King. Not a legal move.
				}
			}
			*/
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

			switch(MoveType)
			{
				case 'Q': move += "=Q"; break;
				case 'R': move += "=R"; break;
				case 'B': move += "=B"; break;
				case 'K': move += "=N"; break;
			}

			// Add check or mate
			//Calculation calc = new Calculation(MoveHistory.Branch(Move, MoveType), 1, GetColorOf(PieceType));
			//if (calc.IsCheckmate) move += '#';
			//else if (calc.IsCheck) move += '+';

			return move;
		}

		public static int GetPieceValue(PieceType Piece)
		{
			if (Piece.ToString().ToUpper() == "KING") return 999;
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

		Coordinate SelectedField;
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
					if (field == SelectedField) { CurrentlyHolding = false; Refresh(); return; }
					if (InvokeClickedOnFieldWhenSelected(field, FieldType)) return;
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
	}
}

/**
 	public static class KeyValuePairExtensions
	{
		public static bool Equals<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp1, KeyValuePair<TKey, TValue> kvp2)
		{
			return kvp1.Key.Equals(kvp2.Key) && kvp1.Value.Equals(kvp2.Value);
		}

		public static bool EqualTo<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp1, KeyValuePair<TKey, TValue> kvp2)
		{
			return kvp1.Equals(kvp2);
		}

		public static bool NotEqualTo<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp1, KeyValuePair<TKey, TValue> kvp2)
		{
			return !kvp1.Equals(kvp2);
		}



// These do not work:
		public static bool operator ==(KeyValuePair kvp1, KeyValuePair kvp2)
		{
			return kvp1.EqualTo(kvp2);
		}

		public static bool operator !=(KeyValuePair kvp1, KeyValuePair kvp2)
		{
			return kvp1.NotEqualTo(kvp2);
		}
	}
 
 
 */