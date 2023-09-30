using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		 * 00000000		Rook a1 mask: 01111111 10000000 1000... => 0x7F80808080808080; I dont know anything
		 * 00000000
		 * 00000000
		 * 00000000
		 * 00000000
		 * 00000000
		 * 00000000
		 * 00000000
		 */

		/*
		private ulong BoardstateWhitePawns;
		private ulong BoardstateWhiteKnights;
		private ulong BoardstateWhiteBishops;
		private ulong BoardstateWhiteRooks;
		private ulong BoardstateWhiteQueens;
		private ulong BoardstateWhiteKing;

		private ulong BoardstateBlackPawns;
		private ulong BoardstateBlackKnights;
		private ulong BoardstateBlackBishops;
		private ulong BoardstateBlackRooks;
		private ulong BoardstateBlackQueens;
		private ulong BoardstateBlackKing;

		private ulong BoardstateEnPassant;

		private ulong BoardstateWhiteAllCastles;
		private ulong BoardstateBlackAllCastles;
		*/

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

		private const byte BITBOARD_ARRAY_SIZE = 9;

		#region Move Data

		private const byte MOVEDATA_NONE = 0;

		private const byte MOVEDATA_SHORTCASTLE_WHITE = 1;
		private const byte MOVEDATA_SHORTCASTLE_BLACK = 2;
		private const byte MOVEDATA_LONGCASTLE_WHITE = 3;
		private const byte MOVEDATA_LONGCASTLE_BLACK = 4;

		private const byte MOVEDATA_PROMOTION_KNIGHT = 5;
		private const byte MOVEDATA_PROMOTION_BISHOP = 6;
		private const byte MOVEDATA_PROMOTION_ROOK = 7;
		private const byte MOVEDATA_PROMOTION_QUEEN = 8;

		private const byte MOVEDATA_PAWN_JUMPSTART_WHITE = 9;	// Add index << 8 or >> 8 to en passant board
		private const byte MOVEDATA_PAWN_JUMPSTART_BLACK = 10;

		#endregion

		#endregion

		internal double Failsoft_AlphaBeta(double alpha, double beta, ulong[] myBitboards, ulong[] opponentBitboards,
			int depthRemaining, bool isWhite, bool isRoot = false)
		{
			if (depthRemaining == 0) return Evaluate(myBitboards, opponentBitboards, isWhite);

			double HighestScore = double.NegativeInfinity;

			// Somehow we need to save from and to, so we need to save
			Span<ushort> moves = stackalloc ushort[218];	// Does this make sense? This is 218 * 8 bytes + overhead for each node (But when one finishes the memory is released, so there is always just 1 path, meaning at most 218*8*depth, with probably >= 1MB stack size available

			// Fill up moves
			moves = MoveGen.GenerateAllMoves(myBitboards, opponentBitboards, moves);

			foreach (ushort move in moves)
			{

			}

			return HighestScore;
		}

		// Pack and Unpack
		static ushort Pack(byte FromSquare, int ToSquare, byte data)
		{
			return (ushort) ((FromSquare << 12) | (ToSquare << 4) | (data & 0xF));
		}
		static ulong Unpack(ushort packedMove)
		{
			return (1UL << GetMoveSquareFrom(packedMove)) | (1UL << GetMoveSquareTo(packedMove));
		}

		static byte GetMoveSquareFrom(ushort move) => (byte) (move >> 12);
		static byte GetMoveSquareTo(ushort move) => (byte) ((move >> 4) & 0x3F);
		static byte GetMoveData(ushort move) => (byte)(move & 0xF);

	}
}
