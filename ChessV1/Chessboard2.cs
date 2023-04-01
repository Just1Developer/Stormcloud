using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChessV1
{
	internal class Chessboard2 : Label
	{

		public int DisplaySize;
		public Turn Turn { get; private set; } = Turn.White;
		public ChessMode ChessMode { get; private set; } = ChessMode.Normal;
		Brush LightColor, DarkColor, HighlightColor, LastMoveHighlightDark, LastMoveHighlightLight, LegalMoveColor, CheckColor, HighlightFieldColorLight, HighlightFieldColorDark;

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
		}
	}

	public class BoardPosition
	{
		internal Dictionary<int, Piece> Pieces = new Dictionary<int, Piece>();

		public int Value { get {
				return -999;	// Placeholder
			} }

		public int GetDirectValue(Turn PieceColor)
		{
			int val = 0;
			foreach (var piece in Pieces)
				if (piece.Value.PieceColor == PieceColor) val += piece.Value.PieceValue;
				else val -= piece.Value.PieceValue;
			return val;
		}

		public override string ToString()
		{
			return base.ToString();
		}
	}

	public class Calculation
	{
		// Every Pair of Type and Position (eg. Bishop E5) has it's legal moves calculated once(!) per calculation and stored
		// Not stored forever because well... ram
		internal Dictionary<Piece, int[]> LegalMovesDB = new Dictionary<Piece, int[]>();

		public void Dispose()
		{
			LegalMovesDB.Clear();
		}
	}
}
