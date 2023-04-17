using System;
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


		public Dictionary<int[], PieceType> CurrentPosition;
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

		public int CurrentlyHolding = -1;
		public PieceType CurrentlyHoldingType;
		public Point CurrentMousePosition;

		public CastleOptions CastleOptionsWhite, CastleOptionsBlack;

		public Chessboard2(int DisplaySize)
		{
			Board = this;
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

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

			ResetBoard(Turn.Black);
			EventInit();
		}

		public void ResetBoard(Turn Color)
		{
			// Row, Col
			CurrentPosition = DefaultPosition();
			// Add pieces, Renderer works inverted

			this.Turn = Color;
			this.CurrentlyHoldingType = PieceType.None;
			this.CastleOptionsWhite = CastleOptions.Both;
			this.CastleOptionsBlack = CastleOptions.Both;

			Refresh();
		}

		public static Dictionary<int[], PieceType> DefaultPosition()
		{
			var BoardPosition = new Dictionary<int[], PieceType>();
			BoardPosition.Add(new int[2] { 0, 0 }, PieceType.rook);
			BoardPosition.Add(new int[2] { 0, 1 }, PieceType.knight);
			BoardPosition.Add(new int[2] { 0, 2 }, PieceType.bishop);
			BoardPosition.Add(new int[2] { 0, 3 }, PieceType.queen);
			BoardPosition.Add(new int[2] { 0, 4 }, PieceType.king);
			BoardPosition.Add(new int[2] { 0, 5 }, PieceType.bishop);
			BoardPosition.Add(new int[2] { 0, 6 }, PieceType.knight);
			BoardPosition.Add(new int[2] { 0, 7 }, PieceType.rook);

			BoardPosition.Add(new int[2] { 7, 0 }, PieceType.ROOK);
			BoardPosition.Add(new int[2] { 7, 1 }, PieceType.KNIGHT);
			BoardPosition.Add(new int[2] { 7, 2 }, PieceType.BISHOP);
			BoardPosition.Add(new int[2] { 7, 3 }, PieceType.QUEEN);
			BoardPosition.Add(new int[2] { 7, 4 }, PieceType.KING);
			BoardPosition.Add(new int[2] { 7, 5 }, PieceType.BISHOP);
			BoardPosition.Add(new int[2] { 7, 6 }, PieceType.KNIGHT);
			BoardPosition.Add(new int[2] { 7, 7 }, PieceType.ROOK);

			for (int i = 0; i < 8; i++)
			{
				BoardPosition.Add(new int[2] { 1, i }, PieceType.pawn);
				BoardPosition.Add(new int[2] { 6, i }, PieceType.PAWN);
			}

			return BoardPosition;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			int SquareSize = DisplaySize / 8;
			bool White = !(Turn == Turn.Black); // Also include Gamestart and everything if no Turn Color is set

			// Loop goes backwards if it's blacks turn
			for (int i = White ? 0 : 63; White && i <= 63 || !White && i >= 0; i += White ? 1 : -1)
			{
				int row = i / 8, col = i % 8;
				RectangleF PieceRectangleField = new RectangleF(SquareSize * col, SquareSize * row, SquareSize, SquareSize);
				Brush BackColor = (i + i/8) % 2 == 0 ? LightColor : DarkColor;
				string Brush = i % 2 == 0 ? "LightColor" : "DarkColor";
				Log($"Field: {i}, Brush: {Brush}, Rectangle pos: {PieceRectangleField.X}, {PieceRectangleField.Y} and Size: {PieceRectangleField.Width} x {PieceRectangleField.Height}");

				g.FillRectangle(BackColor, PieceRectangleField);
			}

			// Piece is KeyValuePair<int[] position, PieceType piecetype>
			foreach (var Piece in CurrentPosition)
			{
				Image _pieceImage = ChessGraphics.GetImage(Piece.Value);
				if (_pieceImage == null) continue;
				int row, col;
				if(this.Turn == Turn.White)
				{
					row = Piece.Key[0];
					col = Piece.Key[1];
				}
				else
				{
					row = 7 - Piece.Key[0];
					col = 7 - Piece.Key[1];
				}
				RectangleF PieceRectangleField = new RectangleF(SquareSize * col, SquareSize * row, SquareSize, SquareSize);
				g.DrawImage(_pieceImage, PieceRectangleField);
			}

			// Draw Currently Held Piece
			if (CurrentlyHolding < 0 && CurrentMousePosition != null) return;
			Image _piece = ChessGraphics.GetImage(CurrentlyHoldingType);
			if (_piece == null) return;
			RectangleF loc = new RectangleF(MousePosition.X - (SquareSize / 2), MousePosition.Y - (SquareSize / 2),
				SquareSize, SquareSize);
			g.DrawImage(_piece, loc);
		}

		public static void Log(string s)
		{
			if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s);
			else Console.WriteLine(s);
		}

		public string MoveToString(KeyValuePair<int[], int[]> Move, char MoveType)	// No own MoveType enum because a char takes up less bit (16) than an integer (64)
		{
			string move = "";
			PieceType PieceType = CurrentPosition.ContainsKey(Move.Key) ? CurrentPosition[Move.Key] : PieceType.None;
			if (PieceType == PieceType.None) return "None";

			switch (CurrentPosition[Move.Key])
			{
				case PieceType.PAWN:
				case PieceType.pawn:
					move += (char)(Move.Key[1] + 97);   // Lowercase 'a' + column, column 0 is 'a'
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
					move += 'K';   // Lowercase 'a' + column, column 0 is 'a'
					break;
			}

			// Find out if multiple pieces can go to that position
			List< KeyValuePair < KeyValuePair<int[], int[]>, char>> MovesFromDestination = Calculation.GetPieceLegalMoves(CurrentPosition, MoveHistory, Move.Value, PieceType, Calculation.GetColorOf(PieceType), false);
			
			foreach (var pair in MovesFromDestination)
			{
				var dest = pair.Key;
				if (CurrentPosition.ContainsKey(dest.Value) && CurrentPosition[dest.Value] == PieceType)
				{
					// There is another piece that can go there, so we need to distinguish
					if(dest.Value[0] == dest.Key[0])	// If the row is the same, we need to specify which column
					{
						move += (char)(Move.Key[1] + 97);
					}
					else if (dest.Value[1] == dest.Key[1])  // If the column is the same, we need to specify which row
					{
						move += Move.Key[0];
					}
					break;
				}
			}

			if (CurrentPosition.ContainsKey(Move.Value) && CurrentPosition[Move.Value] != PieceType.None)
			{
				move += 'x';
			}

			// Destination square
			if (!move.Equals("" + (char)(Move.Value[1] + 97))) move += (char)(Move.Value[1] + 97);
			move += Move.Value[0];

			// Add check or mate
			Calculation calc = new Calculation(MoveHistory.Branch(Move, MoveType), 1, Calculation.GetColorOf(PieceType));
			if (calc.IsCheckmate) move += '#';
			else if (calc.IsCheck) move += '+';

			return move;
		}
	}

	public partial class Calculation
	{
		private Dictionary<int[], PieceType> Position;	// Initial position; does not change
		private Turn TurnColor;
		private int Depth;
		public double BestScore { get; private set; } = 0;
		public KeyValuePair<int[], int[]> BestMove { get; private set; } = new KeyValuePair<int[], int[]>(new int[2] { -1, -1 }, new int[2] { -1, -1 });
		private Dictionary<KeyValuePair<int[], int[]>, double> Scores;

		public DateTime StartTime;
		public static int defaultMaxTimeMS = 10000;
		public int maxTimeMS;
		public bool AbortCalculation = false;
		public bool IsCheck = false;
		public bool IsCheckmate = false;

		public int FinalTimeMS = 0;
		public double FinalDepth = 0;

		public MoveHistory UpUntilPositionHistory = null;

		public Calculation(Dictionary<int[], PieceType> position, int depth, Turn turnColor)
		{
			ConstructorVoid(position, depth, turnColor);
		}

		public Calculation(MoveHistory MoveHistory, int depth, Turn turnColor)
		{
			UpUntilPositionHistory = MoveHistory;
			ConstructorVoid(MoveHistory.CalculatePosition(), depth, turnColor);
		}

		public void ConstructorVoid(Dictionary<int[], PieceType> position, int depth, Turn turnColor)
		{
			Position = position;
			TurnColor = turnColor;
			Depth = depth;
			maxTimeMS = defaultMaxTimeMS;
			StartTime = DateTime.Now;
			if (UpUntilPositionHistory == null) UpUntilPositionHistory = new MoveHistory();

			Scores = new Dictionary<KeyValuePair<int[], int[]>, double>();

			var allLegalMoves = GetAllLegalMoves(Position, turnColor, UpUntilPositionHistory);  // GetAllLegalMoves is Buggy, returns a list of size 0
			var lines = new List<MoveHistory>();
			foreach (var move in allLegalMoves)
			{
				Scores.Add(move.Key, 0);
				lines.Add(UpUntilPositionHistory.Branch(move));
			}

			// A Dictionary<int[], PieceType> Defines a BoardPosition
			// I don't like this, this way we are storing all the positions. I would like to only store the moves and thus a List<MoveHistory> for the lines instead, but that would not work with the legal moves method
			OldCalculateBestMove( /*new List<List<Dictionary<int[], PieceType>>>() { new List<Dictionary<int[], PieceType>>() { Position } }, */ lines, TurnColor, 0);
		}

		private void Finish(double Depth)
		{
			// TODO: MoveTypes and stuff

			// Determine Best Move
			foreach (var score in Scores)
			{
				if(score.Value > BestScore)
				{
					BestScore = score.Value;
					BestMove = score.Key;
				}
			}

			FinalTimeMS = (int) (DateTime.Now - StartTime).TotalMilliseconds;
			FinalDepth = Depth;
			Chessboard2.Log($"Calculation Complete: Time {FinalTimeMS} ms, Final Depth: {FinalDepth}. Best Move: " + MoveToString(Position, BestMove, 'n'));
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
			public List<double> Scores;

			public SearchNode(MoveHistory history, Turn turnColor, double depth, List<double> scores)
			{
				History = history;
				TurnColor = turnColor;
				Depth = depth;
				Scores = scores;
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
		/// <param name="initialHistory">The initial History of the position. </param>
		/// <param name="initialTurnColor"> Who's Turn it is. </param>
		/// <param name="initialDepth"> The initial depth. </param>
		private void CalculateBestMove(MoveHistory initialHistory, Turn initialTurnColor, double initialDepth)
		{
			/**
			 This segment defines the CalculateBestMove method and sets up the lineScores dictionary to store the scores of each line,
			a stack called searchStack to store the search nodes, and then pushes the initial node onto the stack.
			 */
			Dictionary<KeyValuePair<int[], int[]>, List<double>> lineScores = new Dictionary<KeyValuePair<int[], int[]>, List<double>>();
			Stack<SearchNode> searchStack = new Stack<SearchNode>();
			searchStack.Push(new SearchNode(initialHistory, initialTurnColor, initialDepth, null));

			/**
			 * This while loop iterates until the search stack is empty. It processes each node in the search tree.
			 */
			while (searchStack.Count > 0)
			{
				/**
				 * This segment pops a node from the stack, retrieves the depth, turn color, move history, and scores for the current line.
				 */
				SearchNode currentNode = searchStack.Pop();
				double currentDepth = currentNode.Depth;
				Turn currentTurnColor = currentNode.TurnColor;
				MoveHistory currentHistory = currentNode.History;
				List<double> currentScores = currentNode.Scores;

				/**
				 * Check if the maximum depth has been reached, if it's reached, the method calls Finish and returns.
				 */
				if (currentDepth > Depth)
				{
					Finish(currentDepth);
					return;
				}

				/**
				 * This segment calculates the current position, gets all legal moves, and then processes each move in a loop.
				 */
				var pos = currentHistory.CalculatePosition();
				var allLegalMoves = GetAllLegalMoves(Position, currentTurnColor, currentHistory);
				foreach (var move in allLegalMoves)
				{
					/**
					 * This part initializes a new list of scores for the current line, updates the scores using your custom score function,
					 * creates a new MoveHistory object by branching the current history with the move, and pushes a new node onto the search
					 * stack with the updated information.
					 */
					List<double> newScores;
					if (currentScores == null)
					{
						newScores = new List<double>();
						lineScores.Add(move.Key, newScores);
					}
					else
					{
						newScores = new List<double>(currentScores);
					}

					double MoveScore = GetScoreOf(move, currentHistory);
					if (currentTurnColor != initialTurnColor) MoveScore *= -1;
					// Update scores for the current line
					newScores.Add(MoveScore);

					MoveHistory newHistory = currentHistory.Branch(move);
					searchStack.Push(new SearchNode(newHistory, InvertColor(currentTurnColor), currentDepth + 0.5, newScores));
				}
			}

			/**
			 * This segment iterates through the lineScores dictionary, aggregates the scores for each line using the Sum function,
			 * and updates the Scores dictionary with the aggregated scores.
			 */
			// Aggregate the scores for each line and update the Scores dictionary
			foreach (var entry in lineScores)
			{
				KeyValuePair<int[], int[]> moveKey = entry.Key;
				List<double> scoresList = entry.Value;
				double aggregatedScore = scoresList.Sum();
				Scores.Add(moveKey, aggregatedScore);
			}
		}


		private double GetScoreOf(KeyValuePair<KeyValuePair<int[], int[]>, char> move, MoveHistory currentHistory)
		{
			Dictionary<int[], PieceType> Position = currentHistory.CalculatePosition();
			// First, just evaluate the capture of the piece
			double scoreOfPiececapture = Position.ContainsKey(move.Key.Value) ? GetPieceValue(Position[move.Key.Value]) : 0;
			double PieceCaptureWeight = 1.0;
			double ActivityWeight = 0.1;	// Row activity on the opponents side

			double score = (scoreOfPiececapture * PieceCaptureWeight);
			// Activity
			if(move.Key.Value[0] > 3) score += move.Key.Value[0] - 3 * ActivityWeight;

			return score;
		}






		private void OldCalculateBestMove(List<MoveHistory> currentLines, Turn turnColor, double currentDepth)  // List<List<Dictionary<int[], PieceType>>> currentLines
		{
			if (currentDepth > Depth || currentLines.Count == 0) { Finish(currentDepth); return; }	// Calculation complete

			// First, get all legal moves for current Color and do them
			foreach (var History in currentLines)
			{
				if (AbortCalculation) { Finish(currentDepth); return; }
				if ((DateTime.Now - StartTime).TotalMilliseconds > maxTimeMS) { Finish(currentDepth); return; }

				var pos = History.CalculatePosition();
				var allLegalMoves = GetAllLegalMoves(Position, turnColor, History);
				foreach (var move in allLegalMoves)
				{
					Scores.Add(move.Key, 0);
					currentLines.Add(History.Branch(move));
				}
			}

			OldCalculateBestMove(currentLines, InvertColor(turnColor), currentDepth + 0.5);
		}

		public static Turn InvertColor(Turn Turn)
		{
			return Turn == Turn.White ? Turn.Black : Turn.White;
		}

		public static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetAllLegalMoves(Dictionary<int[], PieceType> position, Turn turnColor, MoveHistory History = null)
		{
			if(History == null) History = new MoveHistory();
			var legalMoves = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();

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

			foreach (var piece in position)
			{
				if (IsPieceColor(piece.Value, turnColor))
				{
					legalMoves.AddRange(GetPieceLegalMoves(position, History, piece.Key, piece.Value, turnColor, TurnColorKingInCheck));
				}
			}

			return legalMoves;
		}

		public static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetPieceLegalMoves(Dictionary<int[], PieceType> position, MoveHistory History, int[] piecePos, PieceType pieceType, Turn turnColor, bool KingInCheck)
		{
			var legalMoves = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();

			switch (pieceType)
			{
				case PieceType.PAWN:
				case PieceType.pawn:
					legalMoves.AddRange(GetPawnLegalMoves(position, History, piecePos, turnColor));
					break;
				case PieceType.ROOK:
				case PieceType.rook:
					legalMoves.AddRange(GetRookLegalMoves(position, piecePos, turnColor));
					break;
				case PieceType.KNIGHT:
				case PieceType.knight:
					legalMoves.AddRange(GetKnightLegalMoves(position, piecePos, turnColor));
					break;
				case PieceType.BISHOP:
				case PieceType.bishop:
					legalMoves.AddRange(GetBishopLegalMoves(position, piecePos, turnColor));
					break;
				case PieceType.QUEEN:
				case PieceType.queen:
					legalMoves.AddRange(GetQueenLegalMoves(position, piecePos, turnColor));
					break;
				case PieceType.KING:
				case PieceType.king:
					legalMoves.AddRange(GetKingLegalMoves(position, piecePos, turnColor, History.GetCastleOptions(turnColor)));
					break;
			}

			return legalMoves;
		}
	}

	public class MoveHistory
	{
		public static Dictionary<int[], PieceType> DefaultInitialPosition { get; private set; } = Chessboard2.DefaultPosition();
		private List<KeyValuePair<KeyValuePair<int[], int[]>, char>> History { get; set; }
		public int Count { get => History.Count; }
		public Dictionary<int[], PieceType> CustomSetup = null;

		public CastleOptions WhiteCastleOptions, BlackCastleOptions;    // Keeps track of who (at the end) can castle
		public CastleOptions GetCastleOptions(Turn Color) => Color == Turn.Black ? BlackCastleOptions : WhiteCastleOptions;

		public KeyValuePair<int[], int[]> LastMove { get => History.Count > 0 ? History[History.Count - 1].Key : new KeyValuePair<int[], int[]>(new int[2] { 0, 0 }, new int[2] { 0, 0 }); }
		public KeyValuePair<KeyValuePair<int[], int[]>, char> LastMoveComplete { get => History.Count > 0 ? History[History.Count - 1] : new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(new int[2] { 0, 0 }, new int[2] { 0, 0 }), 'n'); }

		public MoveHistory(Dictionary<int[], PieceType> CustomSetup = null)
		{
			this.CustomSetup = CustomSetup;
			History = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();
			WhiteCastleOptions = CastleOptions.Both;
			BlackCastleOptions = CastleOptions.Both;
		}

		public Dictionary<int[], PieceType> CalculatePosition()
		{
			return CalculatePosition(History.Count);
		}

		public Dictionary<int[], PieceType> CalculatePosition(int maxmove)
		{
			Dictionary<int[], PieceType> position = CustomSetup ?? DefaultInitialPosition;  // Only use local position if we have one, otherwise use static default position. For most use cases, the local one will be null and not take up extra storage
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
					position.Remove(new int[2] { move.Key.Key[0], move.Key.Value[1] }); // Same row, different column as the start
					position.Remove(move.Key.Key);
				}
				else if (move.Value == 'c') // Castles. Move the rook as well.
				{
					position[move.Key.Value] = position[move.Key.Key];
					position.Remove(move.Key.Key);
					if (move.Key.Value[1] == 6)  // Kingside castle, King is now on column 6. Move Rook from Column 7 to Column 5.
					{
						position[new int[2] { move.Key.Value[0], 5 }] = position[new int[2] { move.Key.Value[0], 7 }];
						position.Remove(new int[2] { move.Key.Value[0], 7 });
					}
					else  // Queenside castle, King is now on column 2. Move Rook from Column 0 to Column 3.
					{
						position[new int[2] { move.Key.Value[0], 3 }] = position[new int[2] { move.Key.Value[0], 0 }];
						position.Remove(new int[2] { move.Key.Value[0], 0 });
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

		public void AddNormalMove(KeyValuePair<int[], int[]> Move)
		{
			if (Move.Key == new int[2] { 0, 4 } /* King position */) BlackCastleOptions = CastleOptions.None;
			else if (Move.Key == new int[2] { 7, 4 } /* King position */) WhiteCastleOptions = CastleOptions.None;
			else if (Move.Key == new int[2] { 0, 0 } /* King position */) BlackCastleOptions = BlackCastleOptions == CastleOptions.Short ? CastleOptions.None : CastleOptions.Short;
			else if (Move.Key == new int[2] { 0, 7 } /* King position */) BlackCastleOptions = BlackCastleOptions == CastleOptions.Long ? CastleOptions.None : CastleOptions.Long;
			else if (Move.Key == new int[2] { 7, 0 } /* King position */) WhiteCastleOptions = WhiteCastleOptions == CastleOptions.Short ? CastleOptions.None : CastleOptions.Short;
			else if (Move.Key == new int[2] { 7, 7 } /* King position */) WhiteCastleOptions = WhiteCastleOptions == CastleOptions.Long ? CastleOptions.None : CastleOptions.Long;
			AddMove(Move, 'n');
		}
		public void AddEnPassantMove(KeyValuePair<int[], int[]> Move) => AddMove(Move, 'e');
		public void AddCastlesKingMove(KeyValuePair<int[], int[]> Move)
		{
			if (Move.Key[0] == 0 /* Black (Row 0) */) BlackCastleOptions = CastleOptions.None;
			else WhiteCastleOptions = CastleOptions.None;
			AddMove(Move, 'c');
		}
		public void AddPromotionMoveQueen(KeyValuePair<int[], int[]> Move) => AddMove(Move, 'Q');
		public void AddPromotionMoveRook(KeyValuePair<int[], int[]> Move) => AddMove(Move, 'R');
		public void AddPromotionMoveBishop(KeyValuePair<int[], int[]> Move) => AddMove(Move, 'B');
		public void AddPromotionMoveKnight(KeyValuePair<int[], int[]> Move) => AddMove(Move, 'K');

		private void AddMove(KeyValuePair<int[], int[]> Move, char MoveType)
		{
			History.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(Move, MoveType));
		}

		public MoveHistory Branch(KeyValuePair<int[], int[]> Move, char MoveType)
		{
			MoveHistory newHistory = new MoveHistory(CustomSetup);
			newHistory.History = History;
			newHistory.WhiteCastleOptions = WhiteCastleOptions;
			newHistory.BlackCastleOptions = BlackCastleOptions;
			newHistory.AddMove(Move, MoveType);
			return newHistory;
		}
		public MoveHistory Branch(KeyValuePair<KeyValuePair<int[], int[]>, char> Move) => Branch(Move.Key, Move.Value);
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
			return char.IsUpper((char)Type) ? Turn.White : Turn.Black;
		}

		#region Legal Move Methods

		private static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetPawnLegalMoves(Dictionary<int[], PieceType> position, MoveHistory LineHistory, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();
			int pawnUp = turnColor == Turn.Black ? 1 : -1;

			// On the first rank, pawns can move two up
			int[] dest = new int[2] { piecePos[0] + pawnUp + pawnUp, piecePos[0] };
			if (IsLegalMove(position, dest, turnColor) && !position.ContainsKey(dest) && (piecePos[0] == 1 && pawnUp == 1 || piecePos[0] == 6 && pawnUp == -1))
				list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));

			// Pawns always move 1 up
			dest[0] = pawnUp;
			// The move forward is only allowed if there is noone there
			if (IsLegalMove(position, dest, turnColor) && !position.ContainsKey(dest)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));

			// En Passant Possible
			bool enPassantRight = false, enPassantLeft = false;
			int[] EnPassantRightDestination = new int[2] { piecePos[0], piecePos[1] + 1 };
			int[] EnPassantLeftDestination = new int[2] { piecePos[0], piecePos[1] - 1 };
			if (LineHistory.LastMove.Key == new int[2] { piecePos[0] - 2 * pawnUp, piecePos[1] + 1 } && LineHistory.LastMove.Value == EnPassantRightDestination
			&& position.ContainsKey(EnPassantRightDestination) && position[EnPassantRightDestination].ToString().ToUpper() == "PAWN")
				enPassantRight = true;
			if (LineHistory.LastMove.Key == new int[2] { piecePos[0] - 2 * pawnUp, piecePos[1] - 1 } && LineHistory.LastMove.Value == EnPassantLeftDestination
			&& position.ContainsKey(EnPassantLeftDestination) && position[EnPassantLeftDestination].ToString().ToUpper() == "PAWN")
				enPassantLeft = true;

			// Pawns can capture diagonally
			dest[1] = 1;
			if (IsLegalMove(position, dest, turnColor))
				if (position.ContainsKey(dest)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
				else if (enPassantRight) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(piecePos, dest), 'e'));
			dest[1] = -1;
			if (IsLegalMove(position, dest, turnColor))
				if (position.ContainsKey(dest)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
				else if (enPassantLeft) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(piecePos, dest), 'e'));

			return list;
		}

		private static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetRookLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, 0));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, 0));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 0, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 0, 1));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetKnightLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();
			int[] dest = new int[2] { piecePos[0] - 2, piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] - 2, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] + 2, piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] + 2, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] + 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] - 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] - 1, piecePos[0] + 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] - 1, piecePos[0] - 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetBishopLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, 1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, 1));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetQueenLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, 0));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, 1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, 0));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, 1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 0, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 0, 1));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetKingLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor, CastleOptions KingCastleOptions)
		{
			var list = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();
			int[] dest = new int[2] { piecePos[0] - 1, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] - 1, piecePos[0] };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] - 1, piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0], piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			dest = new int[2] { piecePos[0], piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char> (new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			// Castleing
			if(KingCastleOptions == CastleOptions.Both || KingCastleOptions == CastleOptions.Short)
			{
				dest = new int[2] { piecePos[0], piecePos[0] + 2 };
				if (IsLegalMove(position, dest, turnColor))
					list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			}
			dest = new int[2] { piecePos[0], piecePos[0] - 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(piecePos, dest), 'n'));
			return list;
		}

		private static List<KeyValuePair<KeyValuePair<int[], int[]>, char>> GetLegalMovesInDirection(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor, int rowDelta, int colDelta)
		{
			var list = new List<KeyValuePair<KeyValuePair<int[], int[]>, char>>();
			int row = piecePos[0], col = piecePos[1];
			// Continue in a straight line as long as there are free spaces. If you encounter your own piece, cancel before adding.
			//while (row > -1 && row < 8 && col > -1 && col < 8 && !IsPieceColor(position[new int[2] { row, col }], turnColor))
			while (IsLegalMove(position, new int[2] { row, col }, turnColor))
			{
				row += rowDelta;
				col += colDelta;
				list.Add(new KeyValuePair<KeyValuePair<int[], int[]>, char>(new KeyValuePair<int[], int[]>(piecePos, new int[2] { row, col }), 'n'));
				// If there is an opponent's piece on the current square, cancel the loop but still leave the move to capture
				if (position.ContainsKey(new int[2] { row, col }) && !IsPieceColor(position[new int[2] { row, col }], turnColor)) break;
			}
			return list;
		}

		private static bool IsLegalMove(Dictionary<int[], PieceType> position, int[] destination, Turn turnColor)
		{
			// Check color of destination piece, if there is one
			if (position.ContainsKey(destination) && IsPieceColor(position[destination], turnColor)) return false;
			// Check if destination is out of bounds
			if (destination[0] < 0 || destination[0] > 7 || destination[1] < 0 || destination[1] > 7) return false;
			// return true if everything checks out
			return true;
		}

		#endregion

		public string MoveToString(Dictionary<int[], PieceType> CurrentPosition, KeyValuePair<int[], int[]> Move, char MoveType)  // No own MoveType enum because a char takes up less bit (16) than an integer (64)
		{
			MoveHistory MoveHistory = new MoveHistory(CurrentPosition);
			string move = "";
			PieceType PieceType = CurrentPosition.ContainsKey(Move.Key) ? CurrentPosition[Move.Key] : PieceType.None;
			if (PieceType == PieceType.None) return "None";

			switch (CurrentPosition[Move.Key])
			{
				case PieceType.PAWN:
				case PieceType.pawn:
					move += (char)(Move.Key[1] + 97);   // Lowercase 'a' + column, column 0 is 'a'
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
					move += 'K';   // Lowercase 'a' + column, column 0 is 'a'
					break;
			}

			// Find out if multiple pieces can go to that position
			List<KeyValuePair<KeyValuePair<int[], int[]>, char>> MovesFromDestination = Calculation.GetPieceLegalMoves(CurrentPosition, MoveHistory, Move.Value, PieceType, Calculation.GetColorOf(PieceType), false);

			foreach (var pair in MovesFromDestination)
			{
				var dest = pair.Key;
				if (CurrentPosition.ContainsKey(dest.Value) && CurrentPosition[dest.Value] == PieceType)
				{
					// There is another piece that can go there, so we need to distinguish
					if (dest.Value[0] == dest.Key[0])   // If the row is the same, we need to specify which column
					{
						move += (char)(Move.Key[1] + 97);
					}
					else if (dest.Value[1] == dest.Key[1])  // If the column is the same, we need to specify which row
					{
						move += Move.Key[0];
					}
					break;
				}
			}

			if (CurrentPosition.ContainsKey(Move.Value) && CurrentPosition[Move.Value] != PieceType.None)
			{
				move += 'x';
			}

			// Destination square
			if (!move.Equals("" + (char)(Move.Value[1] + 97))) move += (char)(Move.Value[1] + 97);
			move += Move.Value[0];

			// Add check or mate
			Calculation calc = new Calculation(MoveHistory.Branch(Move, MoveType), 1, GetColorOf(PieceType));
			if (calc.IsCheckmate) move += '#';
			else if (calc.IsCheck) move += '+';

			return move;
		}

		public static int GetPieceValue(PieceType Piece)
		{
			int value = (int)Piece;
			if (value > 10) value -= 10;
			return value == 2 ? 3 : value;
		}
	}

	partial class Chessboard2	// Input / UI / Mouse Event Handling
	{
		int[] SelectedField;
		List<int[]> AllLegalMoves;

		void EventInit()
		{
			this.MouseMove += (s, e) => OnMouseMoved(e);
			this.MouseDown += (s, e) => MouseDownEvent(e);
			this.MouseUp += (s, e) => MouseUpEvent(e);
		}

		// Update Mouse Position
		public void OnMouseMoved(MouseEventArgs e)
		{
			if (CurrentlyHolding < 0) return;

			CurrentMousePosition = new Point(e.X, e.Y);
		}

		// Mouse Down
		public void MouseDownEvent(MouseEventArgs e)
		{

		}

		// Mouse Up
		public void MouseUpEvent(MouseEventArgs e)
		{

		}

		public void SelectField(int[] Field)
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
	/// GPT-4: The root cause of this issue is that you are using int[] as keys for your dictionaries. When comparing arrays in C#, <br/>
	/// the default comparison checks if the references are the same, not if the content is the same. As a result, your code does not <br/>
	/// find the correct keys in the dictionary, even if the contents of the arrays are the same. <br/> <br/>
	/// 
	/// To fix the issue I suggest creating a custom class for the coordinates instead of using int[]. This way, you can override the Equals <br/>
	/// and GetHashCode methods to provide proper comparison logic. Then, replace all instances of int[] used as keys in your dictionaries <br/>
	/// with the new Coordinate class. This should fix the System.Collections.Generic.KeyNotFoundException error you're encountering.
	/// 
	/// </summary>
	public class Coordinate
	{
		public int X { get; set; }
		public int Y { get; set; }

		public Coordinate(int x, int y)
		{
			X = x;
			Y = y;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			Coordinate other = (Coordinate)obj;
			return X == other.X && Y == other.Y;
		}

		/// <summary>
		/// GPT-4: This implementation [of the GetHashCode() method] combines the X and Y properties of the Coordinate class in a simple arithmetic <br/>
		/// operation, with a prime number (17) as a multiplier to reduce the likelihood of hash collisions. This is a basic example and <br/>
		/// might not be the most optimal implementation, but it should work well for most use cases.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return X * 17 + Y;
		}
	}
}



/*
 
 

	public class BoardPosition
	{
		internal Dictionary<int[], PieceType> Pieces = new Dictionary<int[], PieceType>();
		internal List<Piece> PieceList = new List<Piece>();

		public int Value
		{
			get
			{
				return -999;    // Placeholder
			}
		}

		public int GetDirectValue(Turn PieceColor)
		{
			int val = 0;
			foreach (var piece in PieceList)
				if (piece.PieceColor == PieceColor) val += piece.PieceValue;
				else val -= piece.PieceValue;
			return val;
		}

		public Image GetImage(int[] Position)
		{
			//return Chessboard.PieceImages[Pieces[Position]];
			// Yeah aight this is bad
			foreach (Piece p in PieceList)
			{
				if (p.Position.Equals(Position)) return Chessboard.PieceImages[p.PieceType];
			}
			return null;
		}

		public Image GetImage(int PositionValue)
		{
			//return Chessboard.PieceImages[Pieces[Position]];
			// Yeah aight this is bad
			foreach (Piece p in PieceList)
			{
				if (p.Position.Equals(PositionValue) && Chessboard.PieceImages.ContainsKey(p.PieceType)) return Chessboard.PieceImages[p.PieceType];
			}
			return null;
		}

		public override string ToString()
		{
			return base.ToString();
		}

		public static BoardPosition DefaultPosition(Turn Color)
		{
			Turn InverseColor = Color == Turn.White ? Turn.Black : Turn.White;
			return new BoardPosition()
			{
				PieceList = new List<Piece>
				{
					// Opponent Side
					new Pawn(new BoardLocation(0, 0)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(0, 1)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(0, 2)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(0, 3)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(0, 4)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(0, 5)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(0, 6)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(0, 7)) { PieceColor = InverseColor },
					// Own Side
					new Pawn(new BoardLocation(7, 0)) { PieceColor = Color },
					new Pawn(new BoardLocation(7, 1)) { PieceColor = Color },
					new Pawn(new BoardLocation(7, 2)) { PieceColor = Color },
					new Pawn(new BoardLocation(7, 3)) { PieceColor = Color },
					new Pawn(new BoardLocation(7, 4)) { PieceColor = Color },
					new Pawn(new BoardLocation(7, 5)) { PieceColor = Color },
					new Pawn(new BoardLocation(7, 6)) { PieceColor = Color },
					new Pawn(new BoardLocation(7, 7)) { PieceColor = Color },

					// Pawns - Opponent
                    new Pawn(new BoardLocation(1, 0)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(1, 1)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(1, 2)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(1, 3)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(1, 4)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(1, 5)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(1, 6)) { PieceColor = InverseColor },
					new Pawn(new BoardLocation(1, 7)) { PieceColor = InverseColor },
					// Own Pawns
					new Pawn(new BoardLocation(6, 0)) { PieceColor = Color },
					new Pawn(new BoardLocation(6, 1)) { PieceColor = Color },
					new Pawn(new BoardLocation(6, 2)) { PieceColor = Color },
					new Pawn(new BoardLocation(6, 3)) { PieceColor = Color },
					new Pawn(new BoardLocation(6, 4)) { PieceColor = Color },
					new Pawn(new BoardLocation(6, 5)) { PieceColor = Color },
					new Pawn(new BoardLocation(6, 6)) { PieceColor = Color },
					new Pawn(new BoardLocation(6, 7)) { PieceColor = Color },
				}
			};
		}
	}




	public class CalculationGPT4
	{
		public MoveHistory MoveHistory { get; private set; }
		private Dictionary<int[], PieceType> Position;
		private Turn TurnColor;
		private int Depth;
		public int BestScore { get; private set; }
		public KeyValuePair<int[], int[]> BestMove { get; private set; }
		private Dictionary<KeyValuePair<int[], int[]>, int> LegalMovesDB;

		internal CalculationGPT4(Dictionary<int[], PieceType> position, Turn turnColor, int depth)
		{
			MoveHistory = new MoveHistory(position);
			Position = position;
			TurnColor = turnColor;
			Depth = depth;

			LegalMovesDB = new Dictionary<KeyValuePair<int[], int[]>, int>();

			CalculateBestMove(Position, TurnColor, Depth);
		}

		private void CalculateBestMove(Dictionary<int[], PieceType> position, Turn turnColor, int depth)
		{
			if (depth <= 0) return;

			Dictionary<int[], PieceType> tempPosition = new Dictionary<int[], PieceType>(position);
			List<KeyValuePair<int[], int[]>> legalMoves = GetAllLegalMoves(tempPosition, MoveHistory /*TODO* /, turnColor);

		foreach (var move in legalMoves)
		{
			int score = 0;

			// Apply move
			PieceType capturedPiece = ApplyMove(tempPosition, move);
			if (capturedPiece != PieceType.None)
			{
				score += Chessboard.GetPieceValue(capturedPiece);
			}

			// Calculate opponent's best move
			CalculateBestMove(tempPosition, turnColor == Turn.White ? Turn.Black : Turn.White, depth - 1);
			if (LegalMovesDB.ContainsKey(move))
			{
				score -= LegalMovesDB[move];
			}

			// Update best move and score
			if (score > BestScore || BestMove.Key == null)
			{
				BestScore = score;
				BestMove = move;
			}

			// Revert move
			RevertMove(tempPosition, move, capturedPiece);
		}

		LegalMovesDB[BestMove] = BestScore;
				}

				private List<KeyValuePair<int[], int[]>> GetAllLegalMoves(Dictionary<int[], PieceType> position, MoveHistory History, Turn turnColor)
		{
			List<KeyValuePair<int[], int[]>> legalMoves = new List<KeyValuePair<int[], int[]>>();

			foreach (var piece in position)
			{
				if (IsPieceColor(piece.Value, turnColor))
				{
					legalMoves.AddRange(GetPieceLegalMoves(position, History, piece.Key, piece.Value, turnColor));
				}
			}

			return legalMoves;
		}

		private PieceType ApplyMove(Dictionary<int[], PieceType> position, KeyValuePair<int[], int[]> move)
		{
			int[] src = move.Key;
			int[] dst = move.Value;
			PieceType movedPiece = position[src];
			PieceType capturedPiece = position.ContainsKey(dst) ? position[dst] : PieceType.None;

			position.Remove(src);
			position[dst] = movedPiece;

			// Update castling options
			UpdateCastlingOptions(movedPiece, src);

			//MoveHistory.AddMove(move, capturedPiece);
			MoveHistory.AddNormalMove(move);
			return capturedPiece;
		}

		private void RevertMove(Dictionary<int[], PieceType> position, KeyValuePair<int[], int[]> move, PieceType capturedPiece)
		{
			int[] src = move.Key;
			int[] dst = move.Value;

			position.Remove(dst);

			if (capturedPiece != PieceType.None)
			{
				position[dst] = capturedPiece;
			}

			position[src] = position[dst];
		}

		private void UpdateCastlingOptions(PieceType movedPiece, int[] piecePos)
		{
			if (movedPiece == PieceType.KING || movedPiece == PieceType.king)
			{
				if (TurnColor == Turn.White)
				{
					Chessboard2.Board.CastleOptionsWhite = CastleOptions.None;
				}
				else
				{
					Chessboard2.Board.CastleOptionsBlack = CastleOptions.None;
				}
			}
			else if (movedPiece == PieceType.ROOK || movedPiece == PieceType.rook)
			{
				int row = piecePos[0];
				int col = piecePos[1];

				if (TurnColor == Turn.White && row == 7)
				{
					if (col == 0)
					{
						Chessboard2.Board.CastleOptionsWhite &= ~CastleOptions.Long;
					}
					else if (col == 7)
					{
						Chessboard2.Board.CastleOptionsWhite &= ~CastleOptions.Short;
					}
				}
				else if (TurnColor == Turn.Black && row == 0)
				{
					if (col == 0)
					{
						Chessboard2.Board.CastleOptionsBlack &= ~CastleOptions.Long;
					}
					else if (col == 7)
					{
						Chessboard2.Board.CastleOptionsBlack &= ~CastleOptions.Short;
					}
				}
			}
		}

		private bool IsPieceColor(PieceType piece, Turn color)
		{
			if (piece == PieceType.None) return false;
			return (color == Turn.White && char.IsUpper((char)piece)) || (color == Turn.Black && char.IsLower((char)piece));
		}
		private bool IsOppositePieceColor(PieceType piece, Turn color)
		{
			if (piece == PieceType.None) return false;
			return (color == Turn.White && char.IsUpper((char)piece)) || (color == Turn.Black && char.IsLower((char)piece));
		}

		private List<KeyValuePair<int[], int[]>> GetPieceLegalMoves(Dictionary<int[], PieceType> position, MoveHistory History, int[] piecePos, PieceType pieceType, Turn turnColor)
		{
			List<KeyValuePair<int[], int[]>> legalMoves = new List<KeyValuePair<int[], int[]>>();

			switch (pieceType)
			{
				case PieceType.PAWN:
				case PieceType.pawn:
					legalMoves.AddRange(GetPawnLegalMoves(position, History, piecePos, turnColor));
					break;
				case PieceType.ROOK:
				case PieceType.rook:
					legalMoves.AddRange(GetRookLegalMoves(position, piecePos, turnColor));
					break;
				case PieceType.KNIGHT:
				case PieceType.knight:
					legalMoves.AddRange(GetKnightLegalMoves(position, piecePos, turnColor));
					break;
				case PieceType.BISHOP:
				case PieceType.bishop:
					legalMoves.AddRange(GetBishopLegalMoves(position, piecePos, turnColor));
					break;
				case PieceType.QUEEN:
				case PieceType.queen:
					legalMoves.AddRange(GetQueenLegalMoves(position, piecePos, turnColor));
					break;
				case PieceType.KING:
				case PieceType.king:
					legalMoves.AddRange(GetKingLegalMoves(position, piecePos, turnColor));
					break;
			}

			return legalMoves;
		}

		private List<KeyValuePair<int[], int[]>> GetPawnLegalMoves(Dictionary<int[], PieceType> position, MoveHistory LineHistory, int[] piecePos, Turn turnColor)
		{

			// TODO Unfinished

			var list = new List<KeyValuePair<int[], int[]>>();
			// Pawns always move 1 up
			int[] dest = new int[2] { piecePos[0] - 1, piecePos[0] };
			// The move forward is only allowed if there is noone there
			if (IsLegalMove(position, dest, turnColor) && !position.ContainsKey(dest)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] - 2, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			return list;
		}

		private List<KeyValuePair<int[], int[]>> GetRookLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<int[], int[]>>();
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, 0));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, 0));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 0, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 0, 1));
			return list;
		}

		private List<KeyValuePair<int[], int[]>> GetKnightLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<int[], int[]>>();
			int[] dest = new int[2] { piecePos[0] - 2, piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] - 2, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] + 2, piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] + 2, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] + 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] - 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] - 1, piecePos[0] + 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] - 1, piecePos[0] - 2 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			return list;
		}

		private List<KeyValuePair<int[], int[]>> GetBishopLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<int[], int[]>>();
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, 1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, 1));
			return list;
		}

		private List<KeyValuePair<int[], int[]>> GetQueenLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<int[], int[]>>();
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, 0));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, -1, 1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, 0));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 1, 1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 0, -1));
			list.AddRange(GetLegalMovesInDirection(position, piecePos, turnColor, 0, 1));
			return list;
		}

		private List<KeyValuePair<int[], int[]>> GetKingLegalMoves(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor)
		{
			var list = new List<KeyValuePair<int[], int[]>>();
			int[] dest = new int[2] { piecePos[0] - 1, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] - 1, piecePos[0] };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] - 1, piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0] + 1, piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0], piecePos[0] - 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			dest = new int[2] { piecePos[0], piecePos[0] + 1 };
			if (IsLegalMove(position, dest, turnColor)) list.Add(new KeyValuePair<int[], int[]>(piecePos, dest));
			return list;
		}

		private List<KeyValuePair<int[], int[]>> GetLegalMovesInDirection(Dictionary<int[], PieceType> position, int[] piecePos, Turn turnColor, int rowDelta, int colDelta)
		{
			var list = new List<KeyValuePair<int[], int[]>>();
			int row = piecePos[0], col = piecePos[1];
			// Continue in a straight line as long as there are free spaces. If you encounter your own piece, cancel before adding.
			//while (row > -1 && row < 8 && col > -1 && col < 8 && !IsPieceColor(position[new int[2] { row, col }], turnColor))
			while (IsLegalMove(position, new int[2] { row, col }, turnColor))
			{
				row += rowDelta;
				col += colDelta;
				list.Add(new KeyValuePair<int[], int[]>(piecePos, new int[2] { row, col }));
				// If there is an opponent's piece on the current square, cancel the loop but still leave the move to capture
				if (position.ContainsKey(new int[2] { row, col }) && !IsPieceColor(position[new int[2] { row, col }], turnColor)) break;
			}
			return list;
		}

		private bool IsLegalMove(Dictionary<int[], PieceType> position, int[] destination, Turn turnColor)
		{
			// Check color of destination piece, if there is one
			if (position.ContainsKey(destination) && IsPieceColor(position[destination], turnColor)) return false;
			// Check if destination is out of bounds
			if (destination[0] < 0 || destination[0] > 7 || destination[1] < 0 || destination[1] > 7) return false;
			// return true if everything checks out
			return true;
		}

		public void Dispose()
		{
			LegalMovesDB.Clear();
		}
			}

			public class _MoveHistory
		{
			public List<KeyValuePair<KeyValuePair<int[], int[]>, PieceType>> History { get; private set; }

			public _MoveHistory()
			{
				History = new List<KeyValuePair<KeyValuePair<int[], int[]>, PieceType>>();
			}

			public void AddMove(KeyValuePair<int[], int[]> move, PieceType capturedPiece)
			{
				History.Add(new KeyValuePair<KeyValuePair<int[], int[]>, PieceType>(move, capturedPiece));
			}

			public void Clear()
			{
				History.Clear();
			}
		}



*/