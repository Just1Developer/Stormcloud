using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	partial class Stormcloud4 // Evaluation
	{
		// East / West attack squares for pawns by shifting entire pawn matrix
		const ulong Bitboard_NotAFile = 0x7F7F7F7F7F7F7F7F;
		const ulong Bitboard_NotHFile = 0xFEFEFEFEFEFEFEFE;

		private double Evaluate(ulong[] myBitboards, ulong[] opponentBitboards, bool isWhite) =>
			Evaluate(myBitboards, opponentBitboards, myBitboards[INDEX_FULL_BITBOARD] | opponentBitboards[INDEX_FULL_BITBOARD], isWhite);
		private double Evaluate(ulong[] myBitboards, ulong[] opponentBitboards, ulong combinedBoardstate, bool isWhite)
		{
			bool IsEndgame = IsGamestateEndgame(myBitboards, opponentBitboards, combinedBoardstate);

			double score = 0;

			double materialAdvantage = EvaluateMaterial(myBitboards, IsEndgame) - EvaluateMaterial(opponentBitboards, IsEndgame);
			score += materialAdvantage * WEIGHT_EVAL_MATERIAL;

			return score;
		}

		private double EvaluateMaterial(ulong[] Bitboards, bool isEndgame)
		{
			//return 0;
			double score = 0;

			if (isEndgame)
			{
				score += Count_1s(Bitboards[INDEX_PAWN_BITBOARD]) * PIECE_VALUE_PAWN_MIDDLEGAME;
				score += Count_1s(Bitboards[INDEX_KNIGHT_BITBOARD]) * PIECE_VALUE_KNIGHT_MIDDLEGAME;
				score += Count_1s(Bitboards[INDEX_BISHOP_BITBOARD]) * PIECE_VALUE_BISHOP_MIDDLEGAME;
				score += Count_1s(Bitboards[INDEX_ROOK_BITBOARD]) * PIECE_VALUE_ROOK_MIDDLEGAME;
				score += Count_1s(Bitboards[INDEX_QUEEN_BITBOARD]) * PIECE_VALUE_QUEEN_MIDDLEGAME;
			}
			else
			{
				score += Count_1s(Bitboards[INDEX_PAWN_BITBOARD]) * PIECE_VALUE_PAWN_ENDGAME;
				score += Count_1s(Bitboards[INDEX_KNIGHT_BITBOARD]) * PIECE_VALUE_KNIGHT_ENDGAME;
				score += Count_1s(Bitboards[INDEX_BISHOP_BITBOARD]) * PIECE_VALUE_BISHOP_ENDGAME;
				score += Count_1s(Bitboards[INDEX_ROOK_BITBOARD]) * PIECE_VALUE_ROOK_ENDGAME;
				score += Count_1s(Bitboards[INDEX_QUEEN_BITBOARD]) * PIECE_VALUE_QUEEN_ENDGAME;
			}

			return score;
		}

		private static bool NotEnoughCheckmatingMaterial(ulong[] Bitboards, ulong[] otherBitboards)
		{
			return false;
			//return !EnoughCheckmatingMaterial(Bitboards) && !EnoughCheckmatingMaterial(otherBitboards);
		}
		private static bool EnoughCheckmatingMaterial(ulong[] Bitboards)
		{
			// Minimum:
			// 1 Queen, 1 Rook, 2 Bishops (opposite color) (through promotion it's possible to get 2 bishops of the same color), 1 Bishop + 1 Knight, 3 Knights (2 knights can only force stalemate), 1 Pawn
			if (Bitboards[INDEX_QUEEN_BITBOARD] != 0) return true;
			if (Bitboards[INDEX_ROOK_BITBOARD] != 0) return true;
			if (Bitboards[INDEX_PAWN_BITBOARD] != 0) return true;
			byte knights = Count_1s(Bitboards[INDEX_KNIGHT_BITBOARD]);
			if (knights > 2) return true;
			byte bishops = Count_1s(Bitboards[INDEX_BISHOP_BITBOARD]);
			if(knights != 0 && bishops != 0) return true;
			if (bishops > 1)
			{
				// Check if at least 1 of each color
				bool dark = false, light = false;
				ulong Position = Bitboards[INDEX_BISHOP_BITBOARD];
				while (Position != 0)
				{
					ulong moveBitboard = Position & (ulong)-(long)Position;
					byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

					byte fileColorIndex = (byte) (fromSquare & 1);
					byte rankColorIndex = (byte) ((fromSquare >> 3) & 1);

					// FromSquare: File=0 + Rank=0 => Light Square
					// FromSquare: File=0 + Rank=1 => Dark Square
					// FromSquare: File=1 + Rank=0 => Dark Square
					// FromSquare: File=1 + Rank=1 => Light Square

					if ((fileColorIndex ^ rankColorIndex) == 0) light = true;
					else dark = true;

					Position ^= moveBitboard;
				}
				return dark && light;
			}
			return false;
		}

		// Todo Use moveGeneration's methods to create the complete attack bitboard of a given bitboard array

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
