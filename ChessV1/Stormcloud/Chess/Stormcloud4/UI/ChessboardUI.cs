using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using ChessUI;

namespace ChessV1.Stormcloud.Chess.Stormcloud4.UI
{
	internal class ChessboardUI : Panel
	{
		private bool DisregardTurnsDebug = false;
		private static bool Inited = false;

		public bool IsPlayerWhite { get; private set; } = true;

		private bool _const_EnableFlipBoard = true;
		public bool EnableFlipBoard { get => _const_EnableFlipBoard; set { if (_const_EnableFlipBoard && !IsPlayerWhite) FlipBoard(); _const_EnableFlipBoard = value; Refresh(); } }   // Flip the board if its still on black when we change the settings

		// 8x8 board
		Brush LightColor, DarkColor, HighlightColor, LastMoveHighlightDark, LastMoveHighlightLight, LegalMoveColor, CheckColor, HighlightFieldColorLight, HighlightFieldColorDark;

		// Bitboards
		private ulong[] Boardstate_WhiteBitboards;
		private ulong[] Boardstate_BlackBitboards;

		private ulong LastMoveBitboard = 0;
		private ulong BlockerBitboard = 0;
		private ulong HighlightBitboard = 0;
		private ulong MoveFrom = 0;
		private ulong SpecialHighlightBitboard = 0;

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
		private bool holding = false;

		public ChessboardUI(int DisplaySize, bool IsWhite = true) // 0 = white, 1 = black
		{
			if (!Inited) Init();
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			AutoSize = false;

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
			ResetBoard(!IsWhite);

			this.MouseDown += OnMouseDown;
			this.MouseUp += OnMouseUp;
			this.MouseMove += OnMouseMove;
			this.LostFocus += (s, e) => { if (holding) { holding = false; Refresh(); } };

			this.ResumeLayout();
			RegenerateLegalMoves();
		}
		
		static void printf(object s)
		{
			if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s.ToString());
			else Console.WriteLine(s.ToString());
		}
		static string String_square(int square) => $"{(char)('h' - (square % 8))}{(char)('1' + (square / 8))}";

		public void ResetBoard(bool IsWhite = true)
		{
			Form1.self.UndoButton.Enabled = false;
			Form1.self.tf_Result.Text = "";
			Form1.self.tf_Turn.Text = $"Current Turn: " + (IsWhite ? "White" : "Black");

			var defaultPos = Stormcloud4.GetStartingPositions();
			Boardstate_WhiteBitboards = defaultPos.Item1;
			Boardstate_BlackBitboards = defaultPos.Item2;

			// Reset Bitboards
			LastMoveBitboard = 0;
			BlockerBitboard = 0;
			HighlightBitboard = 0;

			Refresh();
		}

		const int MyPadding = 0;

		private Dictionary<int, HashSet<ushort>> AllLegalMoves = null;

		void RegenerateLegalMoves()
		{
			AllLegalMoves = new();
			Span<ushort> moves = stackalloc ushort[218];
			ulong combinedBoardstate = Boardstate_WhiteBitboards[6] | Boardstate_BlackBitboards[6];
			byte moveCount = 0;
			if (SelectedField >= 0)
			{
				// This can be made way more efficient but because movegen is *supposed* to be fast and this is a 1-per-frame operation, this is more compact
				if (IsPlayerWhite)
					moveCount = Stormcloud4.GenerateAllMoves(Boardstate_WhiteBitboards, Boardstate_BlackBitboards,
						moves, IsPlayerWhite);
				else
					moveCount = Stormcloud4.GenerateAllMoves(Boardstate_BlackBitboards, Boardstate_WhiteBitboards,
						moves, IsPlayerWhite);

				if (((Boardstate_WhiteBitboards[3] >> SelectedField) & 1) == 1)
					BlockerBitboard = MoveGen.RookBlockerBitboard((byte)SelectedField, combinedBoardstate);
				else if (((Boardstate_WhiteBitboards[2] >> SelectedField) & 1) == 1)
					BlockerBitboard = MoveGen.BishopBlockerBitboard((byte)SelectedField, combinedBoardstate);
				else BlockerBitboard = 0;
			}

			for (int i = 0; i < moveCount; i++)
			{
				int squareFrom = GetMoveSquareFrom(moves[i]);
				int squareTo = GetMoveSquareTo(moves[i]);

				if (!AllLegalMoves.ContainsKey(squareFrom)) AllLegalMoves.Add(squareFrom, new HashSet<ushort>());
				AllLegalMoves[squareFrom].Add(moves[i]);

				if (squareFrom == SelectedField)
				{
					MoveFrom |= 1UL << squareFrom;
					HighlightBitboard |= 1UL << squareTo;
					if (DataReferencesBitboard(GetMoveData(moves[i]))) SpecialHighlightBitboard |= 1UL << squareTo;
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			int Fieldsize = DisplaySize / 8;

			HighlightBitboard = 0;
			MoveFrom = 0;
			SpecialHighlightBitboard = 0;

			//if (AllLegalMoves == null)	// Not sure why but we need this to always happen
			{
				RegenerateLegalMoves();
			}

			if (SelectedField >= 0 && AllLegalMoves.ContainsKey(SelectedField))
			{
				var moves = AllLegalMoves[SelectedField];
				foreach (var move in moves)
				{
					int squareFrom = GetMoveSquareFrom(move);
					int squareTo = GetMoveSquareTo(move);
					if (squareFrom == SelectedField)
					{
						MoveFrom |= 1UL << squareFrom;
						HighlightBitboard |= 1UL << squareTo;
						if(DataReferencesBitboard(GetMoveData(move))) SpecialHighlightBitboard |= 1UL << squareTo;
					}
				}
			}


			Brush colorOf(sbyte reverseIndex)
			{
				bool light = (reverseIndex & 1) == ((reverseIndex / 8) & 1);

				if (reverseIndex == SelectedField) return light ? Brushes.Goldenrod : Brushes.DarkGoldenrod;
				if (((MoveFrom >> reverseIndex) & 1) == 1) return light ? Brushes.ForestGreen : Brushes.DarkGreen;
				if (((HighlightBitboard >> reverseIndex) & 1) == 1) return light ? Brushes.Crimson : Brushes.Brown;
				if (((SpecialHighlightBitboard >> reverseIndex) & 1) == 1) return light ? Brushes.Cyan : Brushes.DarkCyan;
				return light ? LightColor : DarkColor;
			}

			for (sbyte i = 63; i >= 0; i--)
			{
				// ReverseIndex doesnt matter
				g.FillRectangle(colorOf(i), new Rectangle(
					MyPadding + Fieldsize * (7 - (i % 8)),
					MyPadding + Fieldsize * (7 - (i / 8)),
					Fieldsize, Fieldsize));
			}

			byte SelectedPieceType = 128;

			// Draw Images
			for (byte index = 0; index < 6; index++)
			{
				ulong myPieces = Boardstate_WhiteBitboards[index];
				while (myPieces != 0)
				{
					ulong pieceOnlyBitboard = myPieces & (ulong)-(long)myPieces;
					int square = System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);
					if (square == SelectedField) SelectedPieceType = index;

					// Draw figure on given square
					if (square != SelectedField || !holding) g.DrawImage(PieceImages[index], new Rectangle(
						MyPadding + Fieldsize * (7 - (square % 8)),
						MyPadding + Fieldsize * (7 - (square / 8)),
						Fieldsize, Fieldsize));


					myPieces ^= pieceOnlyBitboard; // Clear the least significant bit set.
				}

				ulong opponentPieces = Boardstate_BlackBitboards[index];
				while (opponentPieces != 0)
				{
					ulong pieceOnlyBitboard = opponentPieces & (ulong)-(long)opponentPieces;
					int square = System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);
					if (square == SelectedField) SelectedPieceType = (byte) (index + 8);

					// Draw figure on given square
					if (square != SelectedField || !holding) g.DrawImage(PieceImages[(byte) (index + 8)], new Rectangle(
						MyPadding + Fieldsize * (7 - (square % 8)),
						MyPadding + Fieldsize * (7 - (square / 8)),
						Fieldsize, Fieldsize));

					opponentPieces ^= pieceOnlyBitboard; // Clear the least significant bit set.
				}
			}

			// Blocker Bitboard
			ulong blockers = BlockerBitboard;
			while (blockers != 0)
			{
				ulong pieceOnlyBitboard = blockers & (ulong)-(long)blockers;
				int square = System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

				g.DrawString("X", new Font(FontFamily.GenericMonospace, 64f, FontStyle.Bold), Brushes.Gold,
					new Point(MyPadding + Fieldsize * (7 - square % 8) + 6, MyPadding + Fieldsize * (7 - square / 8)));

				blockers ^= pieceOnlyBitboard; // Clear the least significant bit set.
			}

			if ((SelectedPieceType & 0b11110000) == 0 && holding)
			{
				g.DrawImage(PieceImages[SelectedPieceType],
					new RectangleF(
						new Point(CurrentMousePosition.X - Fieldsize / 2, CurrentMousePosition.Y - Fieldsize / 2),
						new Size(Fieldsize, Fieldsize)));
			}

			base.OnPaint(e);
		}

		// Stormcloud4 Helper methods for move reading
		static byte GetMoveSquareFrom(ushort move) => (byte)(move >> 10);
		static byte GetMoveSquareTo(ushort move) => (byte)((move >> 4) & 0x3F);
		static byte GetMoveData(ushort move) => (byte)(move & 0xF);
		static bool DataReferencesBitboard(byte data) => (data & 0b1000) == 0;

		#region Movemaking

		ushort GetMoveWhere(int SquareFrom, byte SquareTo)	// SquareFrom is probably SelectedField so leave as int
		{
			if (SquareFrom == SquareTo) return 0;
			if (!AllLegalMoves.ContainsKey(SquareFrom)) return 0;

			foreach (var move in AllLegalMoves[SquareFrom])
			{
				if (GetMoveSquareTo(move) == SquareTo) return move;
			}

			return 0;
		}

		bool ApplyMove(byte SquareTo)
		{
			// Move is always != 0
			ushort move = GetMoveWhere(SelectedField, SquareTo);
			if (move == 0) return false;
			ApplyMove(move);
			return true;
		}
		bool ApplyMove(int SquareFrom, byte SquareTo)
		{
			// Move is always != 0
			ushort move = GetMoveWhere(SquareFrom, SquareTo);
			if (move == 0) return false;
			ApplyMove(move);
			return true;
		}
		unsafe void ApplyMove(ushort move)
		{
			Span<(byte, ulong)> XOROperations = stackalloc (byte, ulong)[9];
			byte myOperationCount = 0, totalOperationCount = 0;

			if (IsPlayerWhite)
			{
				GenerateXORoperations(XOROperations, &myOperationCount, &totalOperationCount, Boardstate_WhiteBitboards, Boardstate_BlackBitboards, move, IsPlayerWhite);
				MakeMove(ref Boardstate_WhiteBitboards, ref Boardstate_BlackBitboards, XOROperations, myOperationCount, totalOperationCount);
			}
			else
			{
				GenerateXORoperations(XOROperations, &myOperationCount, &totalOperationCount, Boardstate_BlackBitboards, Boardstate_WhiteBitboards, move, IsPlayerWhite);
				MakeMove(ref Boardstate_BlackBitboards, ref Boardstate_WhiteBitboards, XOROperations, myOperationCount, totalOperationCount);
			}

			SelectedField = -1;

			MoveFrom = 0;
			SpecialHighlightBitboard = 0;
			BlockerBitboard = 0;	// Doesnt reset automatically
			
			AllLegalMoves = null;
			IsPlayerWhite = !IsPlayerWhite;
			Form1.self.newTurn(IsPlayerWhite ? "White" : "Black");
			Refresh();	// Generate all new legal moves + reset all other visible bitboards
		}

		#region Constants

		private const byte INDEX_PAWN_BITBOARD = 0;
		private const byte INDEX_KNIGHT_BITBOARD = 1;
		private const byte INDEX_BISHOP_BITBOARD = 2;
		private const byte INDEX_ROOK_BITBOARD = 3;
		private const byte INDEX_QUEEN_BITBOARD = 4;
		private const byte INDEX_KING_BITBOARD = 5;
		private const byte INDEX_FULL_BITBOARD = 6;
		private const byte INDEX_CASTLE_BITBOARD = 7;
		private const byte INDEX_EN_PASSANT_BITBOARD = 8;

		// Move Data (including piece types)
		// Basically, normal move data packs into 0xxx for xxx = Index of manipulated Bitboard
		// Additional Data like this may also hold data, but all move data has to have the 8-bit set to 1
		// The jumpstart data also indicates the pawn bitboard, but gives some *additional info*
		// Of course, we can just always pack the index like this, but that gives us only 1 bit for information. That is not a problem unless
		// we might want to pack more than a yes/no into movedata, for example the castle direction. Now, we can pack movedata into bits, but
		// if the 8-bit is set to 1, we interpret it differently, for example 1001 might be white short castle, and 1100 black long castle.
		// This way, we can (with a bit of if-else, fair) store more information.

		private const byte MOVEDATA_PAWN_JUMPSTART = 0b1000;

		private const byte MOVEDATA_CASTLE_SHORT = 0b1001;
		private const byte MOVEDATA_CASTLE_LONG = 0b1010;

		private const byte MOVEDATA_PROMOTION_N = 0b1011;
		private const byte MOVEDATA_PROMOTION_B = 0b1100;
		private const byte MOVEDATA_PROMOTION_R = 0b1101;
		private const byte MOVEDATA_PROMOTION_Q = 0b1110;

		private const byte MOVEDATA_EN_PASSANT_CAPTURE = 0b1111;

		// Masks for the King Position later
		private const ulong CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE = 0b00000010UL;
		private const ulong CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE = 0b00100000UL;
		private const ulong CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK = 0x2000000000000000UL;
		private const ulong CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK = 0x0200000000000000UL;

		// Squares for rook taking
		private const byte CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_WHITE = 0;
		private const byte CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_WHITE = 7;
		private const byte CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_BLACK = 56;
		private const byte CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_BLACK = 63;

		private static readonly ulong[] CASTLE_XOR_MASKS_KING = {	// Index = Move Data - 0b1001 since 0b1001 = 0
			0x000000000000000A,	// White castle Kingside, 0000 1010
			0x0000000000000028,	// White caslte Queenside, 0010 1000
			0x0A00000000000000, // Black castle Kingside, 0000 1010
			0x2800000000000000	// Black castle Queenside, 0010 1000
		};

		private static readonly ulong[] CASTLE_XOR_MASKS_ROOK = {	// Index = Move Data - 0b1001 since 0b1001 = 0, so Move Data - 9
			0x0000000000000005,	// White castle Kingside, 0000 0101
			0x0000000000000090,	// White caslte Queenside, 1001 0000
			0x0500000000000000, // Black castle Kingside, 0000 0101
			0x9000000000000000	// Black castle Queenside, 1001 0000
		};

		#endregion

		static unsafe void GenerateXORoperations(Span<(byte, ulong)> XORBitboardOperations, byte* myOperationCount, byte* OperationCount,
			ulong[] myBitboards, ulong[] opponentBitboards, ushort move, bool isWhite)
		{

			// Define Variables for Data processing
			var unpacked = Unpack(move);
			ulong unpacked_combined = unpacked.Item1 | unpacked.Item2;
			byte data = unpacked.Item3;

			byte fromSquare = GetMoveSquareFrom(move);
			byte toSquare = GetMoveSquareTo(move);

			#region Read Data and create all Operations

			XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked_combined);

			if (DataReferencesBitboard(data))
			{
				// Remove castle options if King/Rook
				if ((unpacked.Item1 & myBitboards[INDEX_KING_BITBOARD]) != 0)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, myBitboards[INDEX_CASTLE_BITBOARD]);
				}
				else if ((unpacked.Item1 & myBitboards[INDEX_ROOK_BITBOARD]) != 0)
				{
					// If could castle that way before, cannot castle anymore now
					if (unpacked.Item1 == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_WHITE && (CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE & myBitboards[INDEX_CASTLE_BITBOARD]) != 0)
						XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE);

					else if (unpacked.Item1 == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_WHITE && (CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE & myBitboards[INDEX_CASTLE_BITBOARD]) != 0)
						XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE);

					else if (unpacked.Item1 == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_BLACK && (CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK & myBitboards[INDEX_CASTLE_BITBOARD]) != 0)
						XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK);

					else if (unpacked.Item1 == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_BLACK && (CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK & myBitboards[INDEX_CASTLE_BITBOARD]) != 0)
						XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK);
				}
				XORBitboardOperations[(*OperationCount)++] = (data, unpacked_combined);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_EN_PASSANT_BITBOARD, myBitboards[INDEX_EN_PASSANT_BITBOARD]);
				(*myOperationCount) = *OperationCount;
			}
			// Put this here because this does not contain a clear en passant call
			else if (data == MOVEDATA_PAWN_JUMPSTART)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked_combined);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_EN_PASSANT_BITBOARD, GetMedianBitboard(fromSquare, toSquare));
				(*myOperationCount) = *OperationCount;
			}
			else
			{
				// Clear En Passants before putting the ball in the opponent's field, so no need to worry about opponent
				XORBitboardOperations[(*OperationCount)++] = (INDEX_EN_PASSANT_BITBOARD, myBitboards[INDEX_EN_PASSANT_BITBOARD]);

				if (data == MOVEDATA_CASTLE_SHORT)
				{
					int index = isWhite ? 0 : 2;
					XORBitboardOperations[(*OperationCount)++] = (INDEX_KING_BITBOARD, CASTLE_XOR_MASKS_KING[index]);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_ROOK_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
					// King move is added to full bitboard already, now apply the rookmove as well
					XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, myBitboards[INDEX_CASTLE_BITBOARD]);  // With whatever we have to set to 0 and restore to OG value
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_CASTLE_LONG)
				{
					int index = isWhite ? 1 : 3;
					XORBitboardOperations[(*OperationCount)++] = (INDEX_KING_BITBOARD, CASTLE_XOR_MASKS_KING[index]);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_ROOK_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
					// King move is added to full bitboard already, now apply the rookmove as well
					XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, CASTLE_XOR_MASKS_ROOK[index]);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD, myBitboards[INDEX_CASTLE_BITBOARD]);  // With whatever we have to set to 0 and restore to OG value
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_EN_PASSANT_CAPTURE)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked_combined);
					(*myOperationCount) = *OperationCount;
					// En Passant Pawn from Opponent
					byte pawnSquare = CombineSquareData(toSquare, fromSquare);
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, 1UL << pawnSquare);
				}
				else if (data == MOVEDATA_PROMOTION_N)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
					XORBitboardOperations[(*OperationCount)++] = (INDEX_KNIGHT_BITBOARD, unpacked.Item2); // Add to promoted piece bitboard instead
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_PROMOTION_B)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
					XORBitboardOperations[(*OperationCount)++] = (INDEX_BISHOP_BITBOARD, unpacked.Item2); // Add to promoted piece bitboard instead
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_PROMOTION_R)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
					XORBitboardOperations[(*OperationCount)++] = (INDEX_ROOK_BITBOARD, unpacked.Item2);   // Add to promoted piece bitboard instead
					(*myOperationCount) = *OperationCount;
				}
				else if (data == MOVEDATA_PROMOTION_Q)
				{
					XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item1);   // Only remove from pawn bitboard
					XORBitboardOperations[(*OperationCount)++] = (INDEX_QUEEN_BITBOARD, unpacked.Item2);  // Add to promoted piece bitboard instead
					(*myOperationCount) = *OperationCount;
				}
			}

			// If RookCapture, adjust CastleOptions if necessary
			if ((unpacked.Item2 & opponentBitboards[INDEX_ROOK_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_ROOK_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);

				ulong bitmask = 7;  // IMPOSSIBLE VALUE (3 incorrect squares marked)
				if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_WHITE) bitmask = CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE;
				else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_WHITE) bitmask = CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE;
				else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_BLACK) bitmask = CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK;
				else if (toSquare == CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_BLACK) bitmask = CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK;

				if (bitmask != 7) XORBitboardOperations[(*OperationCount)++] = (INDEX_CASTLE_BITBOARD,
					 opponentBitboards[INDEX_CASTLE_BITBOARD] & bitmask);   // Make XORable operation so it only has effect if it's original value is 1
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_PAWN_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_PAWN_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_KNIGHT_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_KNIGHT_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_BISHOP_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_BISHOP_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_QUEEN_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_QUEEN_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			else if ((unpacked.Item2 & opponentBitboards[INDEX_KING_BITBOARD]) != 0)
			{
				XORBitboardOperations[(*OperationCount)++] = (INDEX_KING_BITBOARD, unpacked.Item2);
				XORBitboardOperations[(*OperationCount)++] = (INDEX_FULL_BITBOARD, unpacked.Item2);
			}
			// King not necessary here since we check at the start

			#endregion
		}

		static void MakeMove(ref ulong[] myBitboards, ref ulong[] opponentBitboards, Span<(byte, ulong)> Operations, byte myOperationCount, byte totalOperationCount)
		{
			byte c;
			// Use this to update en passant takes and captures that impede castle
			for (c = 0; c < myOperationCount; ++c)
			{
				myBitboards[Operations[c].Item1] ^= Operations[c].Item2;
			}

			for (; c < totalOperationCount; ++c)
			{
				opponentBitboards[Operations[c].Item1] ^= Operations[c].Item2;
			}
		}
		static (ulong, ulong, byte) Unpack(ushort packedMove)
		{
			return (1UL << GetMoveSquareFrom(packedMove), 1UL << GetMoveSquareTo(packedMove), GetMoveData(packedMove));
		}
		static ulong GetMedianBitboard(byte squareFrom, byte squareTo)
		{
			return 1UL << ((squareFrom + squareTo) >> 1);
		}
		static byte CombineSquareData(byte squareFileData, byte squareRankData)
		{
			return (byte)((squareFileData & 0b111) | (squareRankData & 0b111000));
		}


		#endregion

		public void FlipBoard(bool immediateFlip = false)
		{
			return;
			if (!EnableFlipBoard) return;

			if (!immediateFlip)
			{
				Refresh();
				Sleep(200);
			}
		}

		Point CurrentMousePosition = new (0, 0);
		//PictureBox HeldPiecePictureBox;
		public void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (!holding) return;

			CurrentMousePosition = new Point(e.X, e.Y);
			Refresh();
		}

		private void SelectPiece(int field)
		{
			SelectedField = field;
			CurrentMousePosition = GetFieldPositionCenter(field);
			holding = true;

			Refresh();
		}

		// Press
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;  // Maybe add arrows later

			int field = GetField(e.X, e.Y);

			printf($"MouseDown >> Field: {String_square(field)}");

			if (!IsOwnPiece(field) && !DisregardTurnsDebug) return;

			// Clear Selection
			if (SelectedField == field)
			{
				holding = true;
				return;
			}

			SelectPiece(field);
			Refresh();
		}

		// Release
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			int field = GetField(e.X, e.Y);

			printf($"MouseUp >> Field: {String_square(field)}");

			if (e.Button == MouseButtons.Right)
			{
				// Highlight Fields
				if (field < 0) return;
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
			if (IsOwnPiece(field) && holding) holding = false;    // If dragged onto my piece let go
			if (!ApplyMove((byte) field))
			{
				if (holding) holding = false;
				SelectedField = -1;
				Refresh();
				return;
			}
			// Move applied successfully

			Refresh();
		}

		public bool IsOwnPiece(int field)
		{
			if (IsPlayerWhite && ((Boardstate_WhiteBitboards[6] >> field) & 1) != 0) return true;
			if (!IsPlayerWhite && ((Boardstate_BlackBitboards[6] >> field) & 1) != 0) return true;
			return false;
		}

		public int GetField(float X, float Y)
		{
			int x = 7 - (int) ((X - MyPadding) / DisplaySize * 8);
			int y = 7 - (int) ((Y - MyPadding) / DisplaySize * 8);
			return y * 8 + x;
		}
		public Point GetFieldPositionCenter(int field)
		{
			int delta = DisplaySize / 8;
			return new Point(
				(7 - field % 8) * /* per field */delta + delta / 2,
				(7 - field / 8) * /* per field */delta + delta / 2
			);
		}

		public static Image[] PieceImages = new Image[14];
		public static void Init()
		{
			Inited = true;
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

				PieceImages[0] = Image.FromFile(FileDirectory + "pawn.png");
				PieceImages[1] = Image.FromFile(FileDirectory + "knight.png");
				PieceImages[2] = Image.FromFile(FileDirectory + "bishop.png");
				PieceImages[3] = Image.FromFile(FileDirectory + "rook.png");
				PieceImages[4] = Image.FromFile(FileDirectory + "queen.png");
				PieceImages[5] = Image.FromFile(FileDirectory + "king.png");
				PieceImages[8] = Image.FromFile(FileDirectory + "_pawn.png");
				PieceImages[9] = Image.FromFile(FileDirectory + "_knight.png");
				PieceImages[10] = Image.FromFile(FileDirectory + "_bishop.png");
				PieceImages[11] = Image.FromFile(FileDirectory + "_rook.png");
				PieceImages[12] = Image.FromFile(FileDirectory + "_queen.png");
				PieceImages[13] = Image.FromFile(FileDirectory + "_king.png");
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

	public enum SoundType
	{
		GameStart, Draw, MateWin, MateLoss, Move, Castle, Capture, Check
	}
}
