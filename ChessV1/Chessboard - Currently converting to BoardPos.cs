using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Net;

namespace ChessV1
{
	internal class Chessboard : Panel
	{
		// TODO Undo

		Turn Turn = Turn.White;
		ChessMode ChessMode = ChessMode.Normal;

		public bool LegalMovesEnabled = true, ScanForChecks = false, AllowSelfTakes = false, EnableFlipBoard = true;

		// 8x8 board
		Brush LightColor, DarkColor, HighlightColor, LastMoveHighlight, LegalMoveColor;

		// Keep, we can just use .Value and stuff
		int[] lastMove = { -1, -1 };

		public int DisplaySize;
		private BoardPosition SelectedField = BoardPosition.None;
		private List<BoardPosition> LegalMoves = new List<BoardPosition>();   // Notice if throwable (occupied)

		private bool holding = false;

		Dictionary<BoardPosition, PieceType> Pieces;

		public Chessboard(int DisplaySize, bool IsWhite = true) // 0 = white, 1 = black
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			this.Location = new Point(0, 0);
			this.SuspendLayout();
			this.DisplaySize = DisplaySize;
			this.Size = new Size(DisplaySize, DisplaySize);
			this.LightColor = Brushes.SandyBrown;
			this.DarkColor = Brushes.SaddleBrown;
			this.HighlightColor = Brushes.LightYellow;
			this.LastMoveHighlight = Brushes.Yellow;
			this.LegalMoveColor = Brushes.DimGray;
			this.Pieces = new Dictionary<BoardPosition, PieceType>();
			//ResetBoard(IsWhite);

			Pieces.Add(new BoardPosition(5, 5), PieceType.KNIGHT);
			Pieces.Add(new BoardPosition(4, 6), PieceType.queen);

			this.MouseDown += OnMouseDown;
			this.MouseUp += OnMouseUp;
			this.MouseMove += OnMouseMove;

			this.ResumeLayout();
		}

		public void ResetBoard(bool IsWhite)
		{
			// Kings
			Pieces.Add(new BoardPosition(4), IsWhite ? PieceType.king : PieceType.KING);
			Pieces.Add(new BoardPosition(60), IsWhite ? PieceType.KING : PieceType.king);
			// Queens
			Pieces.Add(new BoardPosition(3), IsWhite ? PieceType.queen : PieceType.QUEEN);
			Pieces.Add(new BoardPosition(59), IsWhite ? PieceType.QUEEN : PieceType.queen);
			// Bishops
			Pieces.Add(new BoardPosition(2), IsWhite ? PieceType.bishop : PieceType.BISHOP);
			Pieces.Add(new BoardPosition(5), IsWhite ? PieceType.bishop : PieceType.BISHOP);
			Pieces.Add(new BoardPosition(58), IsWhite ? PieceType.BISHOP : PieceType.bishop);
			Pieces.Add(new BoardPosition(61), IsWhite ? PieceType.BISHOP : PieceType.bishop);
			// Rooks
			Pieces.Add(new BoardPosition(0), IsWhite ? PieceType.rook : PieceType.ROOK);
			Pieces.Add(new BoardPosition(7), IsWhite ? PieceType.rook : PieceType.ROOK);
			Pieces.Add(new BoardPosition(56), IsWhite ? PieceType.ROOK : PieceType.rook);
			Pieces.Add(new BoardPosition(63), IsWhite ? PieceType.ROOK : PieceType.rook);
			// Knights
			Pieces.Add(new BoardPosition(1), IsWhite ? PieceType.knight : PieceType.KNIGHT);
			Pieces.Add(new BoardPosition(6), IsWhite ? PieceType.knight : PieceType.KNIGHT);
			Pieces.Add(new BoardPosition(57), IsWhite ? PieceType.KNIGHT : PieceType.knight);
			Pieces.Add(new BoardPosition(62), IsWhite ? PieceType.KNIGHT : PieceType.knight);
			// Pawns
			if (IsWhite)
			{
				for (int field = 8; field < 16; field++) Pieces.Add(new BoardPosition(field), PieceType.pawn);
				for (int field = 48; field < 56; field++) Pieces.Add(new BoardPosition(field), PieceType.PAWN);
			}
			else
			{
				for (int field = 8; field < 16; field++) Pieces.Add(new BoardPosition(field), PieceType.PAWN);
				for (int field = 48; field < 56; field++) Pieces.Add(new BoardPosition(field), PieceType.pawn);
			}
		}

		public void Checkmate()
		{
			PlaySound(SoundType.MateWin);
			Form1.self.tf_Turn.Text = $"Checkmate: {Turn} wins";
			Turn = Turn.Postgame;
		}

		public void Draw()
		{
			PlaySound(SoundType.Draw);
			Form1.self.tf_Turn.Text = $"It's a Draw!";
			Turn = Turn.Postgame;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Point current = new Point(0, 0);
			int delta = DisplaySize / 8;

			// 0-7 for each row
			for(int _field = 0; _field < 64; _field++)
			{
				BoardPosition field = new BoardPosition(_field);
				Rectangle rect = new Rectangle(current, new Size(delta, delta));

				// Draw Square
				if (field.Equals(SelectedField)) g.FillRectangle(HighlightColor, rect);
				else if (lastMove[0] == _field || lastMove[1] == _field) g.FillRectangle(LastMoveHighlight, rect);
				else g.FillRectangle((_field + _field / 8) % 2 == 0 ? DarkColor : LightColor, rect);

				if (Pieces.ContainsKey(field) && (!SelectedField.Equals(field) || !holding))
					g.DrawImage(PieceImages[Pieces[field]], new RectangleF(current, new Size(delta, delta)));

				if (LegalMoves != null && LegalMoves.Contains(field))
					if (IsOpponentPiece(field)) g.DrawEllipse(new Pen(LegalMoveColor, delta / 12), new Rectangle(new Point(current.X + delta / 2 - delta / 4, current.Y + delta / 2 - delta / 4), new Size(delta / 2, delta / 2)));
					else g.FillEllipse(LegalMoveColor, new Rectangle(new Point(current.X + delta / 2 - delta / 8, current.Y + delta / 2 - delta / 8), new Size(delta / 4, delta / 4)));

				if (field.Col == 7) current = new Point(0, delta * ((_field+1) / 8));	// 8 = next row: 8/8 = 1 threshold
				else current = new Point(current.X + delta, current.Y);
			}

			if (Pieces.ContainsKey(SelectedField) && holding) g.DrawImage(PieceImages[Pieces[SelectedField]], new RectangleF(new Point(CurrentMousePosition.X - delta / 2, CurrentMousePosition.Y - delta / 2), new Size(delta, delta)));
		}

		public void NextTurn()
		{
			if (Turn == Turn.White) Turn = Turn.Black;
			else if(Turn == Turn.Black) Turn = Turn.White;  // Also when starting a new game
			else
			{
				ResetBoard(true);
				Turn = Turn.White;
			}
			Form1.self.newTurn(Turn);
			FlipBoard();
		}

		public void FlipBoard()
		{
			if (!EnableFlipBoard) return;

			Refresh();
			Sleep(200);

			Dictionary<BoardPosition, PieceType> newPieces = new Dictionary<BoardPosition, PieceType>();
			foreach (BoardPosition i in Pieces.Keys)
			{
				// Swap 0 with 63
				newPieces.Add(new BoardPosition(63 - i.Value), Pieces[i]);
			}
			lastMove[0] = 63 - lastMove[0];
			lastMove[1] = 63 - lastMove[1];
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

		private void SelectPiece(BoardPosition field)
		{
			SelectedField = field;
			CurrentMousePosition = GetFieldPositionCenter(field);
			holding = true;

			LegalMoves.Clear();
			switch(ChessMode)
			{
				case ChessMode.Atomic:
					LegalMoves = GetLegalMovesAtomic(field);
					break;
				default:
					LegalMoves = GetLegalMovesNormal(field);
					break;
			}
			
		}

		// Adds the move under a given a condition but only when it's in bounds; Covers Self-Taking
		private List<int> AddLegalMove(List<int> CurrentLegalMoves, int field, Func<int, bool> Condition)
		{
			if(Condition(field) && field >= 0 && field < 64 && !(IsOwnPiece(field) && !AllowSelfTakes)) CurrentLegalMoves.Add(field);
			return CurrentLegalMoves;
		}
		private List<int> AddLegalMove(List<int> CurrentLegalMoves, int field)
		{
			if (field >= 0 && field < 64 && !(IsOwnPiece(field) && !AllowSelfTakes)) CurrentLegalMoves.Add(field);
			return CurrentLegalMoves;
		}

		private List<int> AddLegalMovesInDirection(List<int> Moves, int currentField, int delta)
		{
			while (currentField > 0 && currentField < 64)
			{
				Moves = AddLegalMove(Moves, currentField + delta, move => !IsOwnPiece(move));
				currentField += delta;	// Go to next field
				if (IsOwnPiece(currentField) || IsOpponentPiece(currentField) || currentField >= 64) currentField = -10; // Added it, but now stop
			}
			return Moves;
		}

		private List<BoardPosition> GetLegalMovesNormal(BoardPosition field)
		{
			List<BoardPosition> Moves = new List<BoardPosition>();
			if(!Pieces.ContainsKey(SelectedField) || !LegalMovesEnabled) return Moves;

			PieceType Piece = Pieces[SelectedField];
			string piecetype = Piece.ToString().ToLower();

			BoardPosition Up = !EnableFlipBoard && Turn == Turn.Black ? 8 : -8;
			BoardPosition Down = !EnableFlipBoard && Turn == Turn.Black ? -8 : 8;
			BoardPosition UpLeft = !EnableFlipBoard && Turn == Turn.Black ? 9 : -9;
			BoardPosition UpRight = !EnableFlipBoard && Turn == Turn.Black ? 7 : -7;
			BoardPosition DownLeft = !EnableFlipBoard && Turn == Turn.Black ? -9 : 9;
			BoardPosition DownRight = !EnableFlipBoard && Turn == Turn.Black ? -7 : 7;
			BoardPosition Left = !EnableFlipBoard && Turn == Turn.Black ? -1 : 1;
			BoardPosition Right = !EnableFlipBoard && Turn == Turn.Black ? 1 : -1;

			if (piecetype == "pawn")  // TODO pawns can queen
			{       // TODO en passant
				Moves = AddLegalMove(Moves, field + Up, move => GetPieceType(move) == PieceType.Empty);
				Moves = AddLegalMove(Moves, field + UpLeft, move => IsOpponentPiece(move));
				Moves = AddLegalMove(Moves, field + UpRight, move => IsOpponentPiece(move));

				Moves = AddLegalMove(Moves, field + Up + Up, move => { return GetPieceType(move + Down) == PieceType.Empty && GetPieceType(move) == PieceType.Empty && /*Pawn not moved*/((!EnableFlipBoard && Turn == Turn.Black && field / 8 == 1) || field / 8 == 6); });
			}
			else if (piecetype == "king")   // Todo get if king is in check, castle
			{
				Func<int, bool> Condition = move => { return (IsOpponentPiece(move) && ChessMode != ChessMode.Atomic) || GetPieceType(move) == PieceType.Empty; };

				Moves = AddLegalMove(Moves, field + UpLeft, Condition);
				Moves = AddLegalMove(Moves, field + Up, Condition);
				Moves = AddLegalMove(Moves, field + UpRight, Condition);
				Moves = AddLegalMove(Moves, field + Left, Condition);
				Moves = AddLegalMove(Moves, field + Right, Condition);
				Moves = AddLegalMove(Moves, field + DownLeft, Condition);
				Moves = AddLegalMove(Moves, field + Down, Condition);
				Moves = AddLegalMove(Moves, field + DownRight, Condition);
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
				//Moves = AddLegalMovesInDirection(Moves, field, Up);
				//Moves = AddLegalMovesInDirection(Moves, field, Down);
				Moves = AddLegalMovesInDirection(Moves, field, Left);
				//Moves = AddLegalMovesInDirection(Moves, field, Right);
				//Moves = AddLegalMovesInDirection(Moves, field, UpLeft);
				//Moves = AddLegalMovesInDirection(Moves, field, UpRight);
				//Moves = AddLegalMovesInDirection(Moves, field, DownLeft);
				//Moves = AddLegalMovesInDirection(Moves, field, DownRight);
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

			// TODO Legal Moves
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
				Moves = AddLegalMove(Moves, field + Up, move => GetPieceType(move) == PieceType.Empty);
				Moves = AddLegalMove(Moves, field + UpLeft, move => IsOpponentPiece(move));
				Moves = AddLegalMove(Moves, field + UpRight, move => IsOpponentPiece(move));

				Moves = AddLegalMove(Moves, field + Up + Up, move => { return GetPieceType(move + Down) == PieceType.Empty && GetPieceType(move) == PieceType.Empty && /*Pawn not moved*/((!EnableFlipBoard && Turn == Turn.Black && field / 8 == 1) || field / 8 == 6); });
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
				//Moves = AddLegalMovesInDirection(Moves, field, Down);
				//Moves = AddLegalMovesInDirection(Moves, field, Left);
				//Moves = AddLegalMovesInDirection(Moves, field, Right);
				//Moves = AddLegalMovesInDirection(Moves, field, UpLeft);
				//Moves = AddLegalMovesInDirection(Moves, field, UpRight);
				//Moves = AddLegalMovesInDirection(Moves, field, DownLeft);
				//Moves = AddLegalMovesInDirection(Moves, field, DownRight);
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

			// TODO Legal Moves
			return Moves;
		}


		// Press
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;	// Maybe add arrows later

			int field = GetField(e.X, e.Y);
			if (!IsOwnPiece(field)) return;

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

			if(SelectedField == field)
			{
				if(holding) holding = false;
				else SelectedField = -1;    // Remove Selection if not holding the piece currently, unnecessary as of now bcs its always "holding = true"
				Refresh();
				return;
			}
			// Different Field
			if(IsOwnPiece(field) && holding && !AllowSelfTakes) holding = false;    // If dragged onto my piece let go
			if (LegalMovesEnabled && !LegalMoves.Contains(field))
			{
				if(holding) holding = false;
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
			// Remove if not a pawn
			if (type > 1 && type != 11) Pieces.Remove(field);
			
			if (type == 10 || type == 20) return 1;
			return 0;
		}

		public void MovePiece(int from, int to)
		{
			if (from == -1) return; // Spawns happen with from=-2, this is simply missing selection

			bool EmptyField = IsFieldEmpty(to);

			LegalMoves.Clear();
			lastMove[0] = from;
			lastMove[1] = to;

			// If Atomic Take
			if (GetPieceType(to) != PieceType.Empty && ChessMode == ChessMode.Atomic)
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

				PlaySound(EmptyField ? SoundType.Move : SoundType.Capture);
				NextTurn();
				return;
			}

			bool is_mate = GetPieceType(to).ToString().ToLower() == "king";

			if(from >= 0)
			{
				Pieces[to] = Pieces[from];
				Pieces.Remove(from);
			}
			if (is_mate) { Checkmate(); return; }

			PlaySound(EmptyField ? SoundType.Move : SoundType.Capture);

			NextTurn();

			if (!ScanForChecks) { return; }

			// TODO Check Scans
		}

		public bool IsOwnPiece(int field)
		{
			if (!Pieces.ContainsKey(field)) return false;

			int Type = (int)Pieces[field]; // White is 1-10, black 11-20
			if (Turn == Turn.Black && Type > 10) return true;
			if (Turn == Turn.White && Type > 0 && Type <= 10) return true;
			return false;
		}

		public bool IsOpponentPiece(int field) => IsOpponentPiece(new BoardPosition(field));
		public bool IsOpponentPiece(BoardPosition field)
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
			if (!Pieces.ContainsKey(field)) return PieceType.Empty;
			return Pieces[field];
		}

		public bool IsFieldEmpty(int field)
			=> GetPieceType(field) == PieceType.Empty;

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
	}

	enum PieceType
	{
		KING = 10, QUEEN = 9, BISHOP = 3, ROOK = 5, KNIGHT = 2 /* treat as 3 */, PAWN = 1, Empty = 0,
		king = 20, queen = 19, bishop = 13, rook = 15, knight = 12, pawn = 11	// Black pieces, use values from white
	}

	public enum Turn
	{
		Pregame = 0, White = 1, Black = 2, Postgame = 3, Settings = 4 // <- Pregame actually
	}

	public enum SoundType
	{
		GameStart, Draw, MateWin, MateLoss, Move, Castle, Capture, Check
	}

	enum ChessMode
	{
		Normal, Blitz, Rapid, Atomic, Il_Vaticano
	}

	struct BoardPosition
	{
		public static BoardPosition None { get => new BoardPosition(-1, -1); }

		public int Row, Col;    // Row 1-2-3-4-..., Column a-b-c-d-...

		public int Value { get => Row * 8 + Col; }

		public BoardPosition(int row, int col)
		{
			this.Row = row % 8; // Allow 0-7
			this.Col = col % 8;
		}
		public BoardPosition(int field)
		{
			this.Row = field / 8; // Allow 0-7
			this.Col = field % 8;
		}

		public bool Equals(BoardPosition pos)
		{
			return this.Value == pos.Value;
		}
	}
}
