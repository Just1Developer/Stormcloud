using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChessV1
{
	internal class Chessboard2 : Label, IChessboard
	{

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

		public BoardPosition CurrentPosition;
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
		public Point CurrentMousePosition;

		public Chessboard2(int DisplaySize)
		{
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

			ResetBoard(Turn.White);
		}

		public void ResetBoard(Turn Color)
		{
			CurrentPosition = BoardPosition.DefaultPosition(Color);
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

			foreach (Piece piece in CurrentPosition.PieceList)
			{
				if (piece.PieceImage == null) continue;
				RectangleF PieceRectangleField = new RectangleF(SquareSize * piece.Position.Col, SquareSize * piece.Position.Row, SquareSize, SquareSize);
				g.DrawImage(piece.PieceImage, PieceRectangleField);
			}

			// Draw Currently Held Piece
			if (CurrentlyHolding < 0 && CurrentMousePosition != null) return;
			Image _piece = this.CurrentPosition.GetImage(CurrentlyHolding);
			if (_piece == null) return;
			RectangleF loc = new RectangleF(MousePosition.X - (SquareSize / 2), MousePosition.Y - (SquareSize / 2),
				SquareSize, SquareSize);
			g.DrawImage(_piece, loc);
		}

		public void OnMouseMoved(MouseEventArgs e)
		{
			if (CurrentlyHolding < 0) return;

			CurrentMousePosition = new Point(e.X, e.Y);
		}

		void Log(string s)
		{
			if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s);
			else Console.WriteLine(s);
		}
	}

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

	public class Calculation
	{
		// Every Pair of Type and Position (eg. Bishop E5) has it's legal moves calculated once(!) per calculation and stored
		// Not stored forever because well... ram
		internal Dictionary<Piece, Stack<int[,]>> LegalMovesDB = new Dictionary<Piece, Stack<int[,]>>();

		public void Dispose()
		{
			LegalMovesDB.Clear();
		}
	}
}
