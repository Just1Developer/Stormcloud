using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	partial class Stormcloud4 // Evaluation
	{
		// East / West attack squares for pawns by shifting entire pawn matrix
		const ulong Bitboard_NotAFile = 0x7F7F7F7F7F7F7F7F;
		const ulong Bitboard_NotHFile = 0xFEFEFEFEFEFEFEFE;

		private double Evaluate(ulong[] myBitboards, ulong[] opponentBitboards, bool isWhite)
		{
			return 0;
		}

		#region Pawn Attacks

		public static ulong PawnAttacksWhiteWest(ulong pawnBitboard, ulong opponentBitboardComplete)
		{
			ulong moves = 0;
			moves |= (pawnBitboard & Bitboard_NotAFile) << 9;
			moves &= opponentBitboardComplete;
			return moves;
		}

		public static ulong PawnAttacksWhiteEast(ulong pawnBitboard, ulong opponentBitboardComplete)
		{
			ulong moves = 0;
			moves |= (pawnBitboard & Bitboard_NotHFile) << 7;
			moves &= opponentBitboardComplete;
			return moves;
		}

		public static ulong PawnAttacksBlackEast(ulong pawnBitboard, ulong opponentBitboardComplete)
		{
			ulong moves = 0;
			moves |= (pawnBitboard & Bitboard_NotAFile) >> 9;
			moves &= opponentBitboardComplete;
			return moves;
		}

		public static ulong PawnAttacksBlackWest(ulong pawnBitboard, ulong opponentBitboardComplete)
		{
			ulong moves = 0;
			moves |= (pawnBitboard & Bitboard_NotHFile) >> 7;
			moves &= opponentBitboardComplete;
			return moves;
		}

		#endregion

	}
}
