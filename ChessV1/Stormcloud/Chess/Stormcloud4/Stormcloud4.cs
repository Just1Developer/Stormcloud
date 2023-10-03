using System;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	internal partial class Stormcloud4
	{

		/**
		 * Some Links that carry core ideas for enhancing the Stormcloud Engine:
		 * https://www.chessprogramming.org/Zobrist_Hashing
		 * https://www.chessprogramming.org/Transposition_Table
		 * https://www.chessprogramming.org/Bitboards
		 * https://www.chessprogramming.org/Magic_Bitboards
		 * https://www.chessprogramming.org/Bit-Twiddling
		 * https://www.chessprogramming.org/Futility_Pruning
		 */

		/*
		 * Board
		 * 00000000		Rook a8 mask: 01111111 10000000 1000... => 0x7F80808080808080; I dont know anything
		 * 00000000
		 * 00000000
		 * 00000000
		 * 00000000
		 * 00000000
		 * 00000000
		 * 00000000
		 */

		// Constants Class

		private const byte INDEX_PAWN_BITBOARD = 0;
		private const byte INDEX_KNIGHT_BITBOARD = 1;
		private const byte INDEX_BISHOP_BITBOARD = 2;
		private const byte INDEX_ROOK_BITBOARD = 3;
		private const byte INDEX_QUEEN_BITBOARD = 4;
		private const byte INDEX_KING_BITBOARD = 5;
		private const byte INDEX_FULL_BITBOARD = 6;
		private const byte INDEX_CASTLE_BITBOARD = 7;
		private const byte INDEX_EN_PASSANT_BITBOARD = 8;

		private const byte BITBOARD_ARRAY_SIZE = 9;

		#region Move Data

		// Reminder why movedata none doesn't exist
		[Obsolete("Please specify piece bitboard index.", true)]	// true causes compiler error
		private const byte MOVEDATA_NONE = 0;

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

		#endregion

		#region Castle Options Square and Data masks

		// Vulnerable Squares for Kingside castleing, includes the King square since cannot castle out of check
		private const ulong CASTLE_SQUAREMASK_VULNERABLE_WHITE = 0b00111110UL;
		private const ulong CASTLE_SQUAREMASK_VULNERABLE_BLACK = 0x3E00000000000000UL; // 0b0000 1110 000...
		private const ulong CASTLE_SQUAREMASK_VULNERABLE_BOTH = 0x3E0000000000003EUL; // 0b0000 1110 000...

		private const ulong CASTLE_SQUAREMASK_VULNERABLE_KINGSIDE_WHITE = 0b00001110UL;
		private const ulong CASTLE_SQUAREMASK_VULNERABLE_QUEENSIDE_WHITE = 0b00111000UL;
		private const ulong CASTLE_SQUAREMASK_VULNERABLE_KINGSIDE_BLACK = 0x0E00000000000000UL; // 0b0000 1110 000...
		private const ulong CASTLE_SQUAREMASK_VULNERABLE_QUEENSIDE_BLACK = 0x3700000000000000UL;    // 0b0011 1000 000...

		// Masks for the King Position later
		private const ulong CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE = 0b00000010UL;
		private const ulong CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE = 0b00100000UL;
		private const ulong CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK = 0x2000000000000000UL;
		private const ulong CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK = 0x0200000000000000UL;

		// Masks for the King Position later
		private const ulong CASTLE_BITMASK_NOT_CASTLE_KINGSIDE_WHITE = ~CASTLE_BITMASK_CASTLE_KINGSIDE_WHITE;
		private const ulong CASTLE_BITMASK_NOT_CASTLE_QUEENSIDE_WHITE = ~CASTLE_BITMASK_CASTLE_QUEENSIDE_WHITE;
		private const ulong CASTLE_BITMASK_NOT_CASTLE_KINGSIDE_BLACK = ~CASTLE_BITMASK_CASTLE_KINGSIDE_BLACK;
		private const ulong CASTLE_BITMASK_NOT_CASTLE_QUEENSIDE_BLACK = ~CASTLE_BITMASK_CASTLE_QUEENSIDE_BLACK;

		// Squares for rook taking
		private const byte CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_WHITE = 0;
		private const byte CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_WHITE = 7;
		private const byte CASTLE_SQUARE_ROOK_PREV_INDEX_KINGSIDE_BLACK = 56;
		private const byte CASTLE_SQUARE_ROOK_PREV_INDEX_QUEENSIDE_BLACK = 63;

		// Squares for king castle legal move
		private const byte CASTLE_TO_SQUARE_KING_INDEX_KINGSIDE_WHITE = 1;
		private const byte CASTLE_TO_SQUARE_KING_INDEX_QUEENSIDE_WHITE = 5;
		private const byte CASTLE_TO_SQUARE_KING_INDEX_KINGSIDE_BLACK = 57;
		private const byte CASTLE_TO_SQUARE_KING_INDEX_QUEENSIDE_BLACK = 61;

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

		#region Algorithm Constants

		private const int ALGORITHM_CONSTANT_KING_CAPTUREVALUE = 999999999;

		#endregion
	}
}
