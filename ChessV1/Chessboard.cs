using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Net;

namespace ChessV1
{
	internal class Chessboard : Panel, IChessboard
	{
		// TODO Black Laggs (freeze after turn switch) but white DOESN'T?!?!

		// TODO Undo -> Buggy (Should do normal moves just fine, but messes up in Atomic, castleing, en passant, etc.
		// Nvm it hella buggy; Imma leave it in but its hella buggy fr fr

		private bool DisregardTurnsDebug = false;

		public Turn Turn { get; private set; } = Turn.White;
		public ChessMode ChessMode { get; set; } = ChessMode.Normal;

		private bool _const_EnableFlipBoard = true;
		public bool LegalMovesEnabled { get; set; } = true;
		public bool ScanForChecks { get; set; } = false;
		public bool AllowSelfTakes { get; set; } = false;
		public bool EnableFlipBoard { get => _const_EnableFlipBoard; set { if (_const_EnableFlipBoard && Turn == Turn.Black) FlipBoard(); _const_EnableFlipBoard = value; Refresh(); } }   // Flip the board if its still on black when we change the settings

		// I KNOW I can do this with a Type like Castleing but I dont WANT to
		public int IsWhiteInCheck { get; private set; } = -1;
		public int IsBlackInCheck { get; private set; } = -1;

		public List<int> HighlightedFieldsManual = new List<int>();

		// 8x8 board
		Brush LightColor, DarkColor, HighlightColor, LastMoveHighlightDark, LastMoveHighlightLight, LegalMoveColor, CheckColor, HighlightFieldColorLight, HighlightFieldColorDark;

		int[] lastMove = { -1, -1 };

		private int displaySize;
		public int DisplaySize
		{
			get => displaySize; set
			{
				this.Size = new Size(value, value); displaySize = value; Refresh();
				Form1.self.RefreshSizeButton.Location = new Point(value + 50, 50);
			}
		}
		private int SelectedField = -1;
		private List<int> LegalMoves = new List<int>();   // Notice if throwable (occupied)

		private bool holding = false;

		public Dictionary<int, PieceType> Pieces { get; private set; }
		public List<int> EnPassantWhite { get; private set; } = new List<int>();
		public List<int> EnPassantBlack { get; private set; } = new List<int>();
		public Dictionary<Turn, CastleOptions> CastleAvailability { get; private set; } = new Dictionary<Turn, CastleOptions>();

		public Chessboard(int DisplaySize, bool IsWhite = true) // 0 = white, 1 = black
		{
			LocalEngine = new Stormcloud.Stormcloud3(false);
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			this.Location = new Point(0, 0);
			this.SuspendLayout();
			this.DisplaySize = DisplaySize;
			this.LightColor = Brushes.SandyBrown;
			this.DarkColor = Brushes.SaddleBrown;
			this.HighlightColor = Brushes.LightYellow;  // Peru, I like PowderBlue
			this.LastMoveHighlightLight = Brushes.Yellow;
			this.LastMoveHighlightDark = Brushes.Gold;
			this.LegalMoveColor = Brushes.DimGray;
			this.HighlightFieldColorLight = Brushes.PowderBlue;
			this.HighlightFieldColorDark = Brushes.Turquoise;
			this.CheckColor = Brushes.Red;
			this.Pieces = new Dictionary<int, PieceType>();
			ResetBoard(IsWhite);

			//Pieces.Add(35, PieceType.QUEEN);
			//Pieces.Add(28, PieceType.knight);
			//Pieces.Add(27, PieceType.knight);

			this.MouseDown += OnMouseDown;
			this.MouseUp += OnMouseUp;
			this.MouseMove += OnMouseMove;
			this.LostFocus += (s, e) => { if (holding) { holding = false; Refresh(); } };

			this.ResumeLayout();
		}

		public void Reset()
		{
			Turn = Turn.Pregame;
			NextTurn(true);
		}

		public void ResetBoard(bool IsWhite = true)
		{
			/*
			// For King's Knight Swap Puzzle
			Form1.self.UndoButton.Enabled = false;
			Form1.self.tf_Result.Text = "";
			Form1.self.tf_Turn.Text = $"Current Turn: " + (IsWhite ? "White" : "Black");

			Pieces[19] = PieceType.KNIGHT;
			Pieces[36] = PieceType.KNIGHT;
			Pieces[42] = PieceType.knight;
			Pieces[44] = PieceType.knight;

			for(int i = 0; i < 64; i++)
			{
				if (i != 19 && i != 27 && i != 28 && i != 35 && i != 36 && i != 37 && i != 42 && i != 43 && i != 44 && i != 45) HighlightedFieldsManual.Add(i);
			}

			/** /

			// Hikaru Atomic Missed Checkmate Position
			// Normal Stuff Copy Paste
			HighlightedFieldsManual.Clear();
			Form1.self.UndoButton.Enabled = false;
			Form1.self.tf_Result.Text = "";
			Form1.self.tf_Turn.Text = $"Current Turn: " + (IsWhite ? "White" : "Black");
			Pieces.Clear();
			// Settings
			EnableFlipBoard = false;
			ChessMode = ChessMode.Atomic;
			//Position
			Pieces[2] = PieceType.king;
			Pieces[6] = PieceType.rook;
			Pieces[8] = PieceType.pawn;
			Pieces[15] = PieceType.pawn;
			Pieces[17] = PieceType.pawn;
			Pieces[18] = PieceType.pawn;
			Pieces[20] = PieceType.pawn;
			Pieces[27] = PieceType.pawn;

			Pieces[14] = PieceType.ROOK;
			Pieces[44] = PieceType.PAWN;
			Pieces[45] = PieceType.PAWN;
			Pieces[48] = PieceType.PAWN;
			Pieces[49] = PieceType.PAWN;
			Pieces[51] = PieceType.PAWN;
			Pieces[55] = PieceType.PAWN;
			Pieces[60] = PieceType.KING;
			// Normal Stuff Copy Paste
			lastMove[0] = -1;
			lastMove[1] = -1;
			EnPassantWhite.Clear();
			EnPassantBlack.Clear();
			Moves.Clear();
			MovesIndex = -1;
			CastleAvailability.Clear();
			CastleAvailability.Add(Turn.White, CastleOptions.Both);
			CastleAvailability.Add(Turn.Black, CastleOptions.Both);

			/**/

			HighlightedFieldsManual.Clear();
			Form1.self.UndoButton.Enabled = false;
			Form1.self.tf_Result.Text = "";
			Form1.self.tf_Turn.Text = $"Current Turn: " + (IsWhite ? "White" : "Black");
			Pieces.Clear();
			// Kings
			Pieces[4] = IsWhite ? PieceType.king : PieceType.KING;
			Pieces[60] = IsWhite ? PieceType.KING : PieceType.king;
			// Queens
			Pieces[3] = IsWhite ? PieceType.queen : PieceType.QUEEN;
			Pieces[59] = IsWhite ? PieceType.QUEEN : PieceType.queen;
			// Bishops
			Pieces[2] = IsWhite ? PieceType.bishop : PieceType.BISHOP;
			Pieces[5] = IsWhite ? PieceType.bishop : PieceType.BISHOP;
			Pieces[58] = IsWhite ? PieceType.BISHOP : PieceType.bishop;
			Pieces[61] = IsWhite ? PieceType.BISHOP : PieceType.bishop;
			// Rooks
			Pieces[0] = IsWhite ? PieceType.rook : PieceType.ROOK;
			Pieces[7] = IsWhite ? PieceType.rook : PieceType.ROOK;
			Pieces[56] = IsWhite ? PieceType.ROOK : PieceType.rook;
			Pieces[63] = IsWhite ? PieceType.ROOK : PieceType.rook;
			// Knights
			Pieces[1] = IsWhite ? PieceType.knight : PieceType.KNIGHT;
			Pieces[6] = IsWhite ? PieceType.knight : PieceType.KNIGHT;
			Pieces[57] = IsWhite ? PieceType.KNIGHT : PieceType.knight;
			Pieces[62] = IsWhite ? PieceType.KNIGHT : PieceType.knight;
			// Pawns
			if (IsWhite)
			{
				for (int field = 8; field < 16; field++) Pieces[field] = PieceType.pawn;
				for (int field = 48; field < 56; field++) Pieces[field] = PieceType.PAWN;
			}
			else
			{
				for (int field = 8; field < 16; field++) Pieces[field] = PieceType.PAWN;
				for (int field = 48; field < 56; field++) Pieces[field] = PieceType.pawn;
			}
			//*/

			lastMove[0] = -1;
			lastMove[1] = -1;
			EnPassantWhite.Clear();
			EnPassantBlack.Clear();
			Moves.Clear();
			MovesIndex = -1;
			CastleAvailability.Clear();
			CastleAvailability.Add(Turn.White, CastleOptions.Both);
			CastleAvailability.Add(Turn.Black, CastleOptions.Both);//*/

			Refresh();
		}

		public void Checkmate()
		{
			PlaySound(SoundType.MateWin);
			Form1.self.tf_Turn.Text = $"Checkmate: {Turn} wins";
			Form1.self.tf_Result.Text = $"Checkmate: {Turn} wins";
			Turn = Turn.Postgame;
		}

		public void Draw()
		{
			PlaySound(SoundType.Draw);
			Form1.self.tf_Turn.Text = $"It's a Draw!";
			Form1.self.tf_Result.Text = $"It's a Draw!";
			Turn = Turn.Postgame;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Point current = new Point(0, 0);
			int delta = DisplaySize / 8;

			List<int> legalKnightMoves = new List<int>();
			if (StormcloudPosition == null) StormcloudPosition = ConvertToHexPositionArray(this.Pieces, Turn);
			if(SelectedField >= 0)
			{
				foreach (short mov in Stormcloud.Stormcloud3.GetLegalMovesPiece(StormcloudPosition, (byte) SelectedField, Turn == Turn.White))
				{
					legalKnightMoves.Add((mov >> 4) & 0x003F);	// Last 6 bits
				}
			}

			// 0-7 for each row
			for (int field = 0; field < 64; field++)
			{

				Rectangle rect = new Rectangle(current, new Size(delta, delta));

				// Draw Square
				if (field == SelectedField) g.FillRectangle(HighlightColor, rect);
				else if (HighlightedFieldsManual.Contains(field)) g.FillRectangle((field + field / 8) % 2 == 0 ? HighlightFieldColorLight : HighlightFieldColorDark, rect);
				else if (IsWhiteInCheck == field) g.FillRectangle(CheckColor, rect);
				else if (IsBlackInCheck == field) g.FillRectangle(CheckColor, rect);
				else if (lastMove[0] == field || lastMove[1] == field)
					g.FillRectangle((field + field / 8) % 2 == 0 ? LastMoveHighlightLight : LastMoveHighlightDark, rect);
				else g.FillRectangle((field + field / 8) % 2 == 0 ? LightColor : DarkColor, rect);

				if (Pieces.ContainsKey(field) && (SelectedField != field || !holding))
					g.DrawImage(PieceImages[Pieces[field]], new RectangleF(current, new Size(delta, delta)));

				//if (LegalMoves != null && LegalMoves.Contains(field))
				//	if (IsOpponentPiece(field) || (Turn == Turn.White ? EnPassantBlack : EnPassantWhite).Contains(field)) g.DrawEllipse(new Pen(LegalMoveColor, delta / 12), new Rectangle(new Point(current.X + delta / 2 - delta / 4, current.Y + delta / 2 - delta / 4), new Size(delta / 2, delta / 2)));
				//	else g.FillEllipse(LegalMoveColor, new Rectangle(new Point(current.X + delta / 2 - delta / 8, current.Y + delta / 2 - delta / 8), new Size(delta / 4, delta / 4)));

				g.DrawString($"{field}", new Font(Font.FontFamily, 13f, FontStyle.Bold), new SolidBrush(Color.Red), rect);


				// remove this
				if(legalKnightMoves.Contains(field))
				{
					g.FillEllipse(new SolidBrush(Color.Red), new Rectangle(new Point(current.X + delta / 2 - delta / 8, current.Y + delta / 2 - delta / 8), new Size(delta / 4, delta / 4)));
				}

				if (field % 8 == 7) current = new Point(0, delta * ((field + 1) / 8));  // 8 = next row: 8/8 = 1 threshold
				else current = new Point(current.X + delta, current.Y);
			}

			if (Pieces.ContainsKey(SelectedField) && holding)
				g.DrawImage(PieceImages[Pieces[SelectedField]], new RectangleF(new Point(CurrentMousePosition.X - delta / 2, CurrentMousePosition.Y - delta / 2), new Size(delta, delta)));
		}

		public void NextTurn(bool immediateBoardFlip = false)
		{

			/*
			Turn CheckColor = Turn == Turn.White ? Turn.Black : Turn.White;
			// 1. Is he (who is about to be able to play) in Check? 2. If so, check all moves and check if they are in check
			if (CheckColor == Turn.Black && IsBlackInCheck >= 0 || CheckColor == Turn.White && IsWhiteInCheck >= 0)
			{
				int AllLegalMoves = 0;
				foreach (int pieceField in Pieces.Keys)
				{
					if (GetPieceColor(pieceField) != CheckColor) continue;
					AllLegalMoves += GetLegalMovesNormal(pieceField).Count;
					// Collect Amount of all legal moves
				}
				if(AllLegalMoves == 0)
				{
					Checkmate();	// Same as if he just took the King, so before the Color Change
					return;
				}

			// From further down:
			
			//Form1.self.SetScore(Stormcloud.Stormcloud3.MaterialEvaluation(StormcloudPosition));
			}*/
			if (Turn == Turn.White)
			{
				Turn = Turn.Black;
				FlipBoard(immediateBoardFlip);
			}
			else if (Turn == Turn.Black)
			{
				Turn = Turn.White;
				FlipBoard(immediateBoardFlip);
			}
			else
			{
				ResetBoard(true);
				Turn = Turn.White;
			}
			Form1.self.newTurn(Turn);
			SelectedField = -1;
			Refresh();
			StormcloudPosition = ConvertToHexPositionArray(this.Pieces, Turn);
			Form1.self.SetPosKey(ConvertToHexPositionArrayString(StormcloudPosition));
			var bestmove = LocalEngine.Debug_StartEvaluationTestSingleThread(StormcloudPosition, Turn == Turn.White);
			if (Turn == Turn.Black)
			{
				MovePiece(63-bestmove[0], 63-bestmove[1]);
			}
			else if(Turn == Turn.White)
			{
				MovePiece(bestmove[0], bestmove[1]);
			}
		}

		Stormcloud.Stormcloud3 LocalEngine;

		#region Stormcloud Conversion

		byte[] StormcloudPosition = null;

		private static byte[] ConvertToHexPositionArray(Dictionary<int, PieceType> pieces, Turn turn)
		{
			int start, delta, delta2;
			Func<int, bool> func;
			if (turn == Turn.White)
			{
				start = 0;
				func = new Func<int, bool>((i) => { return i < 64; });
				delta = 2;
				delta2 = 1;
			}
			else
			{
				start = 63;
				func = new Func<int, bool>((i) => { return i >= 0; });
				delta = -2;
				delta2 = -1;
			}
			byte[] pos = new byte[32];
			byte index = 0;
			// Process array backwards if color is black
			for (int i = start; func(i); i+=delta)
			{
				PieceType type1 = PieceType.None, type2 = PieceType.None;
				if (pieces.ContainsKey(i)) type1 = pieces[i];
				if (pieces.ContainsKey(i + delta2)) type2 = pieces[i + delta2];
				pos[index >> 1] = (byte)((PieceHexValue[type1] & 0xF0) + (PieceHexValue[type2] & 0x0F));
				index += 2;
			}
			System.Diagnostics.Debug.WriteLine($">>> Position Converted: Turn: {turn} | Key: {Stormcloud.Stormcloud3.GeneratePositionKey(pos, 0xFF)}");
			return pos;
		}

		private static string ConvertToHexPositionArrayString(Dictionary<int, PieceType> pieces, Turn turn) => ConvertToHexPositionArrayString(ConvertToHexPositionArray(pieces, turn));
		private static string ConvertToHexPositionArrayString(byte[] position)
		{
			System.Text.StringBuilder b = new System.Text.StringBuilder("byte[] testPos = new byte[] { ");
			foreach (byte b2 in position)
			{
				b.Append("0x");
				b.Append(b2.ToString("X2"));
				b.Append(", ");
			}
			b.Append("};");
			return b.ToString();
		}

		private static Dictionary<PieceType, byte> PieceHexValue = new Dictionary<PieceType, byte>()
		{
			{ PieceType.None, 0x00 },
			{ PieceType.PAWN, 0x11 },
			{ PieceType.KNIGHT, 0x22 },
			{ PieceType.BISHOP, 0x33 },
			{ PieceType.ROOK, 0x44 },
			{ PieceType.QUEEN, 0x55 },
			{ PieceType.KING, 0x66 },	// 7 is en passant and 8 is empty (1000)
			{ PieceType.pawn, 0x99 },
			{ PieceType.knight, 0xAA },
			{ PieceType.bishop, 0xBB },
			{ PieceType.rook, 0xCC },
			{ PieceType.queen, 0xDD },
			{ PieceType.king, 0xEE },
		};

		#endregion

		public void FlipBoard(bool immediateFlip = false)
		{
			if (!EnableFlipBoard) return;

			if (!immediateFlip)
			{
				Refresh();
				Sleep(200);
			}

			Dictionary<int, PieceType> newPieces = new Dictionary<int, PieceType>();
			foreach (int i in Pieces.Keys)
			{
				// Swap 0 with 63
				newPieces.Add(63 - i, Pieces[i]);
			}

			List<int> newEnPassantWhite = new List<int>();
			foreach (int i in EnPassantWhite)
			{
				newEnPassantWhite.Add(63 - i);
			}
			EnPassantWhite = newEnPassantWhite;

			List<int> newEnPassantBlack = new List<int>();
			foreach (int i in EnPassantBlack)
			{
				newEnPassantBlack.Add(63 - i);
			}
			EnPassantBlack = newEnPassantBlack;

			List<int> newHighlightedFields = new List<int>();
			foreach (int i in HighlightedFieldsManual)
			{
				newHighlightedFields.Add(63 - i);
			}
			HighlightedFieldsManual = newHighlightedFields;

			lastMove[0] = 63 - lastMove[0];
			lastMove[1] = 63 - lastMove[1];
			if (IsWhiteInCheck >= 0) IsWhiteInCheck = 63 - IsWhiteInCheck;
			if (IsBlackInCheck >= 0) IsBlackInCheck = 63 - IsBlackInCheck;
			Pieces = newPieces;
		}

		Point CurrentMousePosition = new Point(0, 0);
		//PictureBox HeldPiecePictureBox;
		public void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (!holding || !Pieces.ContainsKey(SelectedField)) return;

			//HeldPiecePictureBox.Location = new Point(e.X - HeldPiecePictureBox.Width / 2, e.Y - HeldPiecePictureBox.Height / 2);
			CurrentMousePosition = new Point(e.X, e.Y);
			Refresh();
		}

		private void SelectPiece(int field)
		{
			SelectedField = field;
			CurrentMousePosition = GetFieldPositionCenter(field);
			holding = true;

			LegalMoves.Clear();
			switch (ChessMode)
			{
				case ChessMode.Atomic:
					LegalMoves = GetLegalMovesNormal(field);    // TODO Make atomic again
					break;
				default:
					LegalMoves = GetLegalMovesNormal(field);
					break;
			}

		}

		// Adds the move under a given a condition but only when it's in bounds; Covers Self-Taking
		private List<int> AddLegalMove(List<int> CurrentLegalMoves, int field, Func<int, bool> Condition)
		{
			if (Condition(field) && field >= 0 && field < 64 && !(IsOwnPiece(field) && !AllowSelfTakes)) CurrentLegalMoves.Add(field);
			return CurrentLegalMoves;
		}
		private List<int> AddLegalMove(List<int> CurrentLegalMoves, int field)
		{
			if (field >= 0 && field < 64 && !(IsOwnPiece(field) && !AllowSelfTakes)) CurrentLegalMoves.Add(field);
			return CurrentLegalMoves;
		}
		private List<int> AddLegalMove(List<int> CurrentLegalMoves, BoardLocation BoardPosition, BoardLocation DeltaBoardPosition)
		{
			BoardPosition.Add(DeltaBoardPosition);
			if (!BoardPosition.Illegal && !(IsOwnPiece(BoardPosition.Value) && !AllowSelfTakes)) CurrentLegalMoves.Add(BoardPosition.Value);
			return CurrentLegalMoves;
		}
		private List<int> AddLegalMove(List<int> CurrentLegalMoves, BoardLocation NewBoardPosition)
		{
			if (!NewBoardPosition.Illegal && !(IsOwnPiece(NewBoardPosition.Value) && !AllowSelfTakes)) CurrentLegalMoves.Add(NewBoardPosition.Value);
			return CurrentLegalMoves;
		}

		private List<int> AddLegalMovesInDirection(List<int> Moves, int currentField, int delta)
			=> AddLegalMovesInDirection(Moves, currentField, new BoardLocation(delta));
		private List<int> AddLegalMovesInDirection(List<int> Moves, int currentField, BoardLocation deltaPos)
		{
			BoardLocation currentPosition = new BoardLocation(currentField);

			while (currentField > 0 && currentField < 64)
			{
				// First next field
				currentPosition.Add(deltaPos);
				// Check if it's legal, if not cancel the loop
				if (IsOwnPiece(currentPosition.Value) || currentPosition.Illegal) break;
				// Now add the move
				Moves = AddLegalMove(Moves, currentPosition.Value, move => !IsOwnPiece(move));
				// If there is an opponent piece on there, cancel now
				if (IsOpponentPiece(currentPosition.Value)) break;
			}
			return Moves;
		}

		private List<int> GetLegalMovesNormal(int field)
		{
			List<int> Moves = new List<int>();
			if (!Pieces.ContainsKey(field) || !LegalMovesEnabled) return Moves;

			PieceType Piece = Pieces[field];
			string piecetype = Piece.ToString().ToLower();

			bool invert = !EnableFlipBoard && Turn == Turn.Black;
			int Up = invert ? 8 : -8;
			int Down = invert ? -8 : 8;
			//int UpLeft = invert ? 9 : -9;
			BoardLocation UpLeft = invert ? new BoardLocation(1, 1) : new BoardLocation(-1, -1);
			//int UpRight = invert ? 7 : -7;
			BoardLocation UpRight = invert ? new BoardLocation(1, -1) : new BoardLocation(-1, 1);
			//int DownLeft = invert ? -7 : 7;
			BoardLocation DownLeft = invert ? new BoardLocation(-1, 1) : new BoardLocation(1, -1);
			//int DownRight = invert ? -9 : 9;
			BoardLocation DownRight = invert ? new BoardLocation(-1, -1) : new BoardLocation(1, 1);
			int Left = invert ? 1 : -1;
			int Right = invert ? -1 : 1;

			if (piecetype == "pawn")  // TODO pawns can queen
			{       // TODO en passant
				Moves = AddLegalMove(Moves, field + Up, move => GetPieceType(move) == PieceType.None);
				Moves = AddLegalMove(Moves, field + UpLeft.Value, move => IsOpponentPiece(move) || (Turn == Turn.White ? EnPassantBlack : EnPassantWhite).Contains(move));
				Moves = AddLegalMove(Moves, field + UpRight.Value, move => IsOpponentPiece(move) || (Turn == Turn.White ? EnPassantBlack : EnPassantWhite).Contains(move));

				Moves = AddLegalMove(Moves, field + Up + Up, move => { return GetPieceType(move + Down) == PieceType.None && GetPieceType(move) == PieceType.None && /*Pawn not moved*/((!EnableFlipBoard && Turn == Turn.Black && field / 8 == 1) || field / 8 == 6); });
			}
			else if (piecetype == "king")   // Todo get if king is in check, castle
			{
				Func<int, bool> Condition = move => { return (IsOpponentPiece(move) && ChessMode != ChessMode.Atomic) || GetPieceType(move) == PieceType.None; };

				Moves = AddLegalMove(Moves, field + UpLeft.Value, Condition);
				Moves = AddLegalMove(Moves, field + Up, Condition);
				Moves = AddLegalMove(Moves, field + UpRight.Value, Condition);
				Moves = AddLegalMove(Moves, field + Left, Condition);
				Moves = AddLegalMove(Moves, field + Right, Condition);
				Moves = AddLegalMove(Moves, field + DownLeft.Value, Condition);
				Moves = AddLegalMove(Moves, field + Down, Condition);
				Moves = AddLegalMove(Moves, field + DownRight.Value, Condition);

				int CastleShort = Turn == Turn.White ? Right : Left;
				if ((CastleAvailability[Turn] == CastleOptions.Short || CastleAvailability[Turn] == CastleOptions.Both) &&
					GetPieceType(field + CastleShort) == PieceType.None && GetPieceType(field + CastleShort * 2) == PieceType.None) Moves = AddLegalMove(Moves, field + CastleShort * 2);
				// CastleLong = -CastleShort
				if ((CastleAvailability[Turn] == CastleOptions.Long || CastleAvailability[Turn] == CastleOptions.Both) &&
					GetPieceType(field - CastleShort) == PieceType.None && GetPieceType(field - CastleShort * 2) == PieceType.None && GetPieceType(field - CastleShort * 3) == PieceType.None)
					Moves = AddLegalMove(Moves, field - CastleShort * 2);
			}
			else if (piecetype == "rook")
			{
				Moves = AddLegalMovesInDirection(Moves, field, Up);
				Moves = AddLegalMovesInDirection(Moves, field, Down);
				Moves = AddLegalMovesInDirection(Moves, field, Left);
				Moves = AddLegalMovesInDirection(Moves, field, Right);
			}
			else if (piecetype == "bishop")
			{
				Moves = AddLegalMovesInDirection(Moves, field, UpLeft);
				Moves = AddLegalMovesInDirection(Moves, field, UpRight);
				Moves = AddLegalMovesInDirection(Moves, field, DownLeft);
				Moves = AddLegalMovesInDirection(Moves, field, DownRight);
			}
			else if (piecetype == "queen")
			{
				Moves = AddLegalMovesInDirection(Moves, field, Up);
				Moves = AddLegalMovesInDirection(Moves, field, Down);
				Moves = AddLegalMovesInDirection(Moves, field, Left);
				Moves = AddLegalMovesInDirection(Moves, field, Right);
				Moves = AddLegalMovesInDirection(Moves, field, UpLeft);
				Moves = AddLegalMovesInDirection(Moves, field, UpRight);    // Upright = Special
				Moves = AddLegalMovesInDirection(Moves, field, DownLeft);
				Moves = AddLegalMovesInDirection(Moves, field, DownRight);
			}
			else if (piecetype == "knight")
			{
				BoardLocation current = new BoardLocation(field);
				Moves = AddLegalMove(Moves, current, new BoardLocation(-2, 1));
				Moves = AddLegalMove(Moves, current, new BoardLocation(-2, -1));
				Moves = AddLegalMove(Moves, current, new BoardLocation(2, 1));
				Moves = AddLegalMove(Moves, current, new BoardLocation(2, -1));
				Moves = AddLegalMove(Moves, current, new BoardLocation(1, 2));
				Moves = AddLegalMove(Moves, current, new BoardLocation(1, -2));
				Moves = AddLegalMove(Moves, current, new BoardLocation(-1, 2));
				Moves = AddLegalMove(Moves, current, new BoardLocation(-1, -2));
			}

			if (ScanForChecks)
			{
				// If he is in Check and has no moves that dont result in a check, its mate because the other can then take
				// (nvm this is just for this piece)
				// Checkmate check gonna be it's own thing
				// Oh and of course remove moves that result in a Check
				foreach (int move in Moves)
				{
					if (Catfish.LookForChecks(CloneWithMove(field, move), Turn) >= 0)
						// If there is a Check after this move is played, don't allow it
						Moves.Remove(move);
				}
			}

			return Moves;
		}

		private List<int> GetLegalMovesAtomic(int field)
		{
			List<int> Moves = new List<int>();
			if (!Pieces.ContainsKey(SelectedField) || !LegalMovesEnabled) return Moves;

			PieceType Piece = Pieces[SelectedField];
			string piecetype = Piece.ToString().ToLower();

			int Up = !EnableFlipBoard && Turn == Turn.Black ? 8 : -8;
			int Down = !EnableFlipBoard && Turn == Turn.Black ? -8 : 8;
			int UpLeft = !EnableFlipBoard && Turn == Turn.Black ? 9 : -9;
			int UpRight = !EnableFlipBoard && Turn == Turn.Black ? 7 : -7;
			int DownLeft = !EnableFlipBoard && Turn == Turn.Black ? -9 : 9;
			int DownRight = !EnableFlipBoard && Turn == Turn.Black ? -7 : 7;
			int Left = !EnableFlipBoard && Turn == Turn.Black ? -1 : 1;
			int Right = !EnableFlipBoard && Turn == Turn.Black ? 1 : -1;

			if (piecetype == "pawn")  // TODO pawns can queen
			{       // TODO en passant
				Moves = AddLegalMove(Moves, field + Up, move => GetPieceType(move) == PieceType.None);
				Moves = AddLegalMove(Moves, field + UpLeft, move => IsOpponentPiece(move));
				Moves = AddLegalMove(Moves, field + UpRight, move => IsOpponentPiece(move));

				Moves = AddLegalMove(Moves, field + Up + Up, move => { return GetPieceType(move + Down) == PieceType.None && GetPieceType(move) == PieceType.None && /*Pawn not moved*/((!EnableFlipBoard && Turn == Turn.Black && field / 8 == 1) || field / 8 == 6); });
			}
			else if (piecetype == "rook")
			{
				Moves = AddLegalMovesInDirection(Moves, field, Up);
				Moves = AddLegalMovesInDirection(Moves, field, Down);
				Moves = AddLegalMovesInDirection(Moves, field, Left);
				Moves = AddLegalMovesInDirection(Moves, field, Right);
			}
			else if (piecetype == "bishop")
			{
				Moves = AddLegalMovesInDirection(Moves, field, UpLeft);
				Moves = AddLegalMovesInDirection(Moves, field, UpRight);
				Moves = AddLegalMovesInDirection(Moves, field, DownLeft);
				Moves = AddLegalMovesInDirection(Moves, field, DownRight);
			}
			else if (piecetype == "queen")
			{
				Moves = AddLegalMovesInDirection(Moves, field, Up);
				Moves = AddLegalMovesInDirection(Moves, field, Down);
				Moves = AddLegalMovesInDirection(Moves, field, Left);
				Moves = AddLegalMovesInDirection(Moves, field, Right);
				Moves = AddLegalMovesInDirection(Moves, field, UpLeft);
				Moves = AddLegalMovesInDirection(Moves, field, UpRight);
				Moves = AddLegalMovesInDirection(Moves, field, DownLeft);
				Moves = AddLegalMovesInDirection(Moves, field, DownRight);
			}
			else if (piecetype == "knight")
			{
				Moves = AddLegalMove(Moves, field + Up + UpLeft);
				Moves = AddLegalMove(Moves, field + Up + UpRight);
				Moves = AddLegalMove(Moves, field + Right + UpRight);
				Moves = AddLegalMove(Moves, field + Right + DownRight);
				Moves = AddLegalMove(Moves, field + Down + DownRight);
				Moves = AddLegalMove(Moves, field + Down + DownLeft);
				Moves = AddLegalMove(Moves, field + Left + DownLeft);
				Moves = AddLegalMove(Moves, field + Left + UpLeft);
			}

			if (ScanForChecks)
			{
				// If he is in Check and has no moves that dont result in a check, its mate because the other can then take
				// (nvm this is just for this piece)
				// Checkmate check gonna be it's own thing
				// Oh and of course remove moves that result in a Check
				foreach (int move in Moves)
				{
					if (Catfish.LookForChecks(CloneWithMove(field, move), Turn) >= 0)
						// If there is a Check after this move is played, don't allow it
						Moves.Remove(move);
				}
			}

			return Moves;
		}


		// Press
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;  // Maybe add arrows later

			int field = GetField(e.X, e.Y);
			if (!IsOwnPiece(field) && !DisregardTurnsDebug) return;

			// Clear Selection
			if (SelectedField == field)
			{
				holding = true;
				/*
				holding = false;    // Shouldnt be possible but still
				SelectedField = -1;    // Remove Selection if not holding the piece currently
				if (LegalMoves != null) LegalMoves.Clear();
				Refresh();*/
				return;
			}

			SelectPiece(field);
			Refresh();

			/**
			 * What needs to be done here (in order)
			 * - Halten = unmöglich
			 */
		}
		// Release
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			int field = GetField(e.X, e.Y);
			if (e.Button == MouseButtons.Right)
			{
				// Highlight Fields
				if (field < 0) return;
				if (HighlightedFieldsManual.Contains(field)) HighlightedFieldsManual.Remove(field);
				else HighlightedFieldsManual.Add(field);
				Refresh();
				return;
			}  // Maybe add arrows later
			if (e.Button != MouseButtons.Left) return;  // Maybe add arrows later

			if (SelectedField == field)
			{
				if (holding) holding = false;
				else SelectedField = -1;    // Remove Selection if not holding the piece currently, unnecessary as of now bcs its always "holding = true"
				Refresh();
				return;
			}
			// Different Field
			if (IsOwnPiece(field) && holding && !AllowSelfTakes) holding = false;    // If dragged onto my piece let go
			if (LegalMovesEnabled && !LegalMoves.Contains(field))
			{
				if (holding) holding = false;
				Refresh();
				return;
			}

			MovePiece(SelectedField, field);
			SelectedField = -1;
			Refresh();
		}

		// Returns if mate
		private int RemoveIfNotPawnAtomic(int field)
		{
			int type = (int)GetPieceType(field);

			// Removed Castle Bug | Geht nich, scheiß egal lol
			if (GetPieceValue(field) == 5)
				if (field == 7 || field == 63 /*short corners*/) CastleAvailability[Turn] = CastleAvailability[Turn] == CastleOptions.Both ? CastleOptions.Long : CastleOptions.None;
				else if (field == 0 || field == 56 /*long corners*/) CastleAvailability[Turn] = CastleAvailability[Turn] == CastleOptions.Both ? CastleOptions.Short : CastleOptions.None;

			// Remove if not a pawn
			if (type > 1 && type != 11) Pieces.Remove(field);

			if (type == 10 || type == 20) return 1;
			return 0;
		}

		private void MoveEnPassant(int from, int to)
		{
			int Down = !EnableFlipBoard && Turn == Turn.Black ? -8 : 8; // Down for calculating from "to" to the pawn square to remove
			Pieces.Remove(to + Down);
			Pieces[to] = Pieces[from];
			Pieces.Remove(from);

			PlaySound(SoundType.Capture);
			if (Pieces.Count == 1) { Checkmate(); return; }
			// Check for Draw
			if (Pieces.Count <= 2) { Draw(); return; }
			NextTurn();
		}

		public void MovePiece(int from, int to, bool AddMoveToList = true)
		{
			if (from == -1) return; // Spawns happen with from=-2, this is simply missing selection

			bool EmptyField = IsFieldEmpty(to);

			if (AddMoveToList) AddMove(new Move(new BoardLocation(from), new BoardLocation(to), this));

			LegalMoves.Clear();
			// TODO HighlightedFieldsManual.Clear();
			lastMove[0] = from;
			lastMove[1] = to;

			// En Passant
			if (Turn == Turn.White && EnPassantBlack.Contains(to)) { MoveEnPassant(from, to); EnPassantBlack.Clear(); return; }
			if (Turn == Turn.Black && EnPassantWhite.Contains(to)) { MoveEnPassant(from, to); EnPassantWhite.Clear(); return; }

			// Remove old EnPassant options
			if (Turn == Turn.White) EnPassantWhite.Clear();
			else if (Turn == Turn.Black) EnPassantBlack.Clear();

			// Add En Passant
			if (GetPieceType(from).ToString().ToLower() == "pawn" && Math.Abs(from - to) == 16 /* => moved 2 rows*/)
				if (Turn == Turn.White) EnPassantWhite.Add((from + to) / 2);    // Add field in between (where the pawn would have been)
				else if (Turn == Turn.Black) EnPassantBlack.Add((from + to) / 2);    // Add field in between (where the pawn would have been)

			if (GetPieceType(from).ToString().ToLower() == "king") CastleAvailability[Turn] = CastleOptions.None;
			if (GetPieceType(from).ToString().ToLower() == "rook")
				if (from == 7 || from == 63 /*short corners*/) CastleAvailability[Turn] = CastleAvailability[Turn] == CastleOptions.Both ? CastleOptions.Long : CastleOptions.None;
				else if (from == 0 || from == 56 /*long corners*/) CastleAvailability[Turn] = CastleAvailability[Turn] == CastleOptions.Both ? CastleOptions.Short : CastleOptions.None;

			// Castleing: Passive move so we don't need a WinCheck ==> Wrong, Bring the rook for mate is possible
			if (GetPieceType(from).ToString().ToLower() == "king" && Math.Abs(from - to) == 2)  // Castle, diagonal is >= 7 and L/R is 1
			{
				int rookplace = (from + to) / 2;    // Average of both positions is the field in between here
				int oldRookPlace = to == 2 ? 0 : to == 6 ? 7 : to == 57 ? 56 : 63;
				Pieces.Remove(oldRookPlace);
				Pieces[rookplace] = Turn == Turn.White ? PieceType.ROOK : PieceType.rook;
				Pieces[to] = Pieces[from];
				Pieces.Remove(from);

				PlaySound(SoundType.Castle);
				NextTurn();
				return;
			}

			// If Atomic Take
			if (GetPieceType(to) != PieceType.None && ChessMode == ChessMode.Atomic)
			{

				int type = (int)GetPieceType(to);
				int mate = type == 10 || type == 20 ? 1 : 0;

				// Do sumn else
				Pieces.Remove(from);
				Pieces.Remove(to);

				mate += RemoveIfNotPawnAtomic(to - 1);
				mate += RemoveIfNotPawnAtomic(to + 1);
				mate += RemoveIfNotPawnAtomic(to - 9);
				mate += RemoveIfNotPawnAtomic(to - 8);
				mate += RemoveIfNotPawnAtomic(to - 7);
				mate += RemoveIfNotPawnAtomic(to + 7);
				mate += RemoveIfNotPawnAtomic(to + 8);
				mate += RemoveIfNotPawnAtomic(to + 9);

				if (mate > 0) { Checkmate(); return; }

				IsWhiteInCheck = Catfish.LookForChecks(this, Turn.White);
				IsBlackInCheck = Catfish.LookForChecks(this, Turn.Black);

				bool isInCheck = Turn == Turn.White && IsWhiteInCheck >= 0 || Turn == Turn.Black && IsBlackInCheck >= 0;

				PlaySound(isInCheck ? SoundType.Check : EmptyField ? SoundType.Move : SoundType.Capture);
				NextTurn();
				return;
			}

			bool is_mate = GetPieceType(to).ToString().ToLower() == "king";

			if (from >= 0)
			{
				Pieces[to] = Pieces[from];
				Pieces.Remove(from);
			}

			if (is_mate || Pieces.Count == 1) { Checkmate(); return; }
			// Check for Draw
			if (Pieces.Count <= 2) { Draw(); return; }

			// Check for Checks
			IsWhiteInCheck = Catfish.LookForChecks(this, Turn.White);
			IsBlackInCheck = Catfish.LookForChecks(this, Turn.Black);

			bool _isInCheck = Turn == Turn.White && IsWhiteInCheck >= 0 || Turn == Turn.Black && IsBlackInCheck >= 0;

			PlaySound(_isInCheck ? SoundType.Check : EmptyField ? SoundType.Move : SoundType.Capture);

			NextTurn();
		}

		public void AddMove(Move move)
		{
			// Form1 Access
			Form1.self.UndoButton.Enabled = true;

			MovesIndex++;
			// Remove all possible redos (new chain of events)
			if (Moves.Count >= MovesIndex) Moves.RemoveRange(MovesIndex, Moves.Count - MovesIndex);

			Moves.Add(move);
		}

		public int MovesIndex;
		public List<Move> Moves = new List<Move>();
		public bool UndoLastMove()
		{
			if (MovesIndex <= 0 || Moves.Count == 0) return false;
			MovesIndex--;
			UndoMove(Moves[MovesIndex]);
			return true;
		}
		public bool RedoLastMove()
		{
			if (Moves.Count == MovesIndex + 1) return false;    // max number reached

			MovesIndex++;
			Move move = Moves[MovesIndex];
			MovePiece(move.FromPosition.Value, move.ToPosition.Value, false);

			return true;
		}

		// TODO Undo doesn't remember En Passant or Castle

		// Reverts a given move (to -> from)
		public void UndoMove(Move move)
		{
			// Before the flip
			if (MovesIndex > 0)
			{
				lastMove[0] = Moves[MovesIndex - 1].FromPosition.Value;
				lastMove[1] = Moves[MovesIndex - 1].ToPosition.Value;
			}
			else
			{
				lastMove[0] = -1;
				lastMove[1] = -1;
			}

			if (EnableFlipBoard) NextTurn(true);

			bool EmptyField = move.ToPositionPiece == PieceType.None;

			Pieces[move.FromPosition.Value] = move.FromPositionPiece;
			if (EmptyField) Pieces.Remove(move.ToPosition.Value);
			else Pieces[move.ToPosition.Value] = move.ToPositionPiece;

			LegalMoves.Clear();

			PlaySound(EmptyField ? SoundType.Move : SoundType.Capture);
			Refresh();
		}

		public bool IsOwnPiece(int field)
		{
			if (!Pieces.ContainsKey(field)) return false;

			int Type = (int)Pieces[field]; // White is 1-10, black 11-20
			if (Turn == Turn.Black && Type > 10) return true;
			if (Turn == Turn.White && Type > 0 && Type <= 10) return true;
			return false;
		}

		public bool IsOpponentPiece(int field)
		{
			if (!Pieces.ContainsKey(field)) return false;

			int Type = (int)Pieces[field]; // White is 1-10, black 11-20
			if (Turn == Turn.White && Type > 10) return true;
			if (Turn == Turn.Black && Type > 0 && Type <= 10) return true;
			return false;
		}

		public Turn GetPieceColor(int field)
		{
			if (!Pieces.ContainsKey(field)) return Turn.Pregame;

			int Type = (int)Pieces[field]; // White is 1-10, black 11-20
			if (Type > 10) return Turn.Black;
			return Turn.White;
		}

		public PieceType GetPieceType(int field)
		{
			if (!Pieces.ContainsKey(field)) return PieceType.None;
			return Pieces[field];
		}

		public bool IsFieldEmpty(int field)
			=> GetPieceType(field) == PieceType.None;

		public int GetField(float X, float Y)
		{
			int x = (int)(X / DisplaySize * 8);
			int y = (int)(Y / DisplaySize * 8);
			return y * 8 + x;
		}
		public Point GetFieldPosition(int field)
		{
			int delta = DisplaySize / 8;
			return new Point(
				field % 8 * /* per field */delta,
				field / 8 * /* per field */delta
				);
		}
		public Point GetFieldPositionAbsolute(int field)
		{
			Point pos = GetFieldPosition(field);
			return new Point(Location.X + pos.X, Location.Y + pos.Y);
		}
		public Point GetFieldPositionCenter(int field)
		{
			int delta = DisplaySize / 8;
			return new Point(
				field % 8 * /* per field */delta + delta / 2,
				field / 8 * /* per field */delta + delta / 2
				);
		}
		public Point GetFieldPositionAbsoluteCenter(int field)
		{
			Point pos = GetFieldPositionCenter(field);
			return new Point(Location.X + pos.X, Location.Y + pos.Y);
		}

		// Todo Cloning the black board triggers flip method and manual delay, thats why it's lagging
		public Chessboard Clone()
		{
			Chessboard board = new Chessboard(DisplaySize) { Turn = Turn.White };
			if (this.Turn == Turn.Black) board.NextTurn();
			board.Pieces.Clear();
			foreach (int i in Pieces.Keys)
			{
				board.Pieces.Add(i, Pieces[i]);
			}
			board.ChessMode = ChessMode;
			board.EnableFlipBoard = EnableFlipBoard;
			board.LegalMovesEnabled = LegalMovesEnabled;
			board.AllowSelfTakes = AllowSelfTakes;
			board.IsWhiteInCheck = IsWhiteInCheck;
			board.IsBlackInCheck = IsBlackInCheck;
			board.CastleAvailability = CastleAvailability;
			board.EnPassantBlack = EnPassantBlack;
			board.EnPassantWhite = EnPassantWhite;
			board.ScanForChecks = ScanForChecks;
			return board;
		}

		public Chessboard CloneWithMove(Move m)
		{
			Chessboard clone = Clone();
			clone.Pieces[m.ToPosition.Value] = m.FromPositionPiece;
			clone.Pieces.Remove(m.FromPosition.Value);
			return clone;
		}
		public Chessboard CloneWithMove(int from, int to)
		{
			Chessboard clone = Clone();
			if (!clone.Pieces.ContainsKey(from)) return clone;

			clone.Pieces[to] = clone.Pieces[from];
			clone.Pieces.Remove(from);
			return clone;
		}

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

		private static string FileDirectory = System.IO.Directory.GetCurrentDirectory() + "/Chessfiles/";
		public static void Sleep(int millis)
		{
			System.Threading.Thread.Sleep(millis);
		}

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

		public int GetPieceValue(int field)
		{
			return GetPieceValue(GetPieceType(field));
		}

		public static int GetPieceValue(PieceType Piece)
		{
			int value = (int)Piece;
			if (value > 10) value -= 10;
			return value == 2 ? 3 : value;
		}

		public bool IsTypeOf(int field, PieceType Type)
		{
			return GetPieceType(field).ToString().ToLower() == Type.ToString().ToLower();
		}
		public static bool IsTypeOf(PieceType PieceType, PieceType Type)
		{
			return PieceType.ToString().ToLower() == Type.ToString().ToLower();
		}
	}

	public enum Turn
	{
		Pregame = 0, White = 1, Black = 2, Postgame = 3, Settings = 4 // <- Pregame actually
	}

	public enum SoundType
	{
		GameStart, Draw, MateWin, MateLoss, Move, Castle, Capture, Check
	}

	public enum ChessMode
	{
		Normal, Blitz, Rapid, Atomic, Il_Vaticano
	}

	public enum CastleOptions
	{
		None, Long, Short, Both
	}

	struct BoardLocation
	{
		public static BoardLocation None { get => new BoardLocation(-1, -1); }

		public int Row, Col;    // Row 1-2-3-4-..., Column a-b-c-d-...

		public int Value { get => Row * 8 + Col; }

		public BoardLocation(int row, int col)
		{
			this.Row = row % 8; // Allow 0-7
			this.Col = col % 8;
		}
		public BoardLocation(int field)
		{
			bool negative = field < 0;
			field = Math.Abs(field);
			this.Row = field / 8; // Allow 0-7
			this.Col = field % 8;

			if (!negative) return;
			this.Row *= -1;
			this.Col *= -1;
		}

		public bool Equals(BoardLocation pos)
		{
			return this.Value == pos.Value;
		}

		public bool Equals(int[] RowColInt)
		{
			return this.Row == RowColInt[0] && this.Col == RowColInt[1];
		}

		public bool Equals(int FieldValue)
		{
			return this.Value == FieldValue;
		}

		public bool Illegal { get => this.Row > 7 || this.Row < 0 || this.Col > 7 || this.Col < 0; }

		public void Add(BoardLocation pos)
		{
			Row += pos.Row;
			Col += pos.Col;
		}
		public void Add(int field)
		{
			this.Row += field / 8; // Allow 0-7
			this.Col += field % 8;
		}

		/// <summary>
		/// For absolute positions.
		/// </summary>
		public void Invert()
		{
			this.Row = 8 - Row;
			this.Col = 8 - Col;
		}

		/// <summary>
		/// For relative positions. <br/>
		/// Does not edit this location but instead returns a new, mirrored BoardLocation.
		/// </summary>
		/// <returns> The new relative position. </returns>
		public BoardLocation Mirror()
		{
			return new BoardLocation(-Row, -Col);
		}

		public BoardLocation GetInverted()
		{
			BoardLocation clone = new BoardLocation(this.Row, this.Col);
			clone.Invert();
			return clone;
		}
	}

	class Move
	{
		public BoardLocation FromPosition, ToPosition;
		public PieceType FromPositionPiece, ToPositionPiece;

		public Move(BoardLocation From, BoardLocation To, PieceType FromType, PieceType ToType)
		{
			this.FromPosition = From;
			this.ToPosition = To;
			this.FromPositionPiece = FromType;
			this.ToPositionPiece = ToType;
		}
		public Move(BoardLocation From, BoardLocation To, Chessboard Board)
		{
			this.FromPosition = From;
			this.ToPosition = To;
			this.FromPositionPiece = Board.GetPieceType(From.Value);
			this.ToPositionPiece = Board.GetPieceType(To.Value);
		}
	}
}
