using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	// Todo Incorporate Castle and En Passant
	partial class Stormcloud4
	{
		public static void GenerateAllMoves(ulong[] myBitboards, ulong[] opponentBitboards, Span<ushort> moves, bool isWhite)
			=> GenerateAllMoves(myBitboards, opponentBitboards, myBitboards[INDEX_FULL_BITBOARD] | opponentBitboards[INDEX_FULL_BITBOARD], moves, isWhite);
		public static unsafe void GenerateAllMoves(ulong[] myBitboards, ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, bool isWhite)
		{
			ulong myBitboardInverted = ~myBitboards[INDEX_FULL_BITBOARD];
			int move = 0;
			// Get all moves in order, order is based on piece amount and how long they probably live:
			// Pawn, Rook, Bishop, Knight, King, Queen
			for (byte square = 0; square < 64; square++)
			{
				if (((myBitboards[INDEX_PAWN_BITBOARD] >> square) & 1) == 1)
				{
					// Perhaps re-do this but since for complete pawn bitboard we have no way of knowing the origin for a given pawn attack,
					// would not be that usable / more efficient
					if (isWhite)
						GenerateAllPawnMovesWhite(opponentBitboards, CompleteGamestate, moves, square, &move);
					else
						GenerateAllPawnMovesBlack(opponentBitboards, CompleteGamestate, moves, square, &move);
					continue;
				}

				if (((myBitboards[INDEX_ROOK_BITBOARD] >> square) & 1) == 1)
				{
					ulong RookMoves = MoveGen.GetRookMoves(square, myBitboardInverted, CompleteGamestate);
					MoveGen_PackMovesFromBitboard(RookMoves, moves, square, &move);
					continue;
				}

				if (((myBitboards[INDEX_BISHOP_BITBOARD] >> square) & 1) == 1)
				{
					ulong BishopMoves = MoveGen.GetBishopMoves(square, myBitboardInverted, CompleteGamestate);
					MoveGen_PackMovesFromBitboard(BishopMoves, moves, square, &move);
					continue;
				}

				if (((myBitboards[INDEX_KNIGHT_BITBOARD] >> square) & 1) == 1)
				{
					ulong KnightMoves = MoveGen.GetKnightMoves(square, myBitboardInverted);
					MoveGen_PackMovesFromBitboard(KnightMoves, moves, square, &move);
					continue;
				}

				if (((myBitboards[INDEX_QUEEN_BITBOARD] >> square) & 1) == 1)
				{
					ulong QueenMoves = MoveGen.GetQueenMoves(square, myBitboardInverted, CompleteGamestate);
					MoveGen_PackMovesFromBitboard(QueenMoves, moves, square, &move);
					continue;
				}

				if (((myBitboards[INDEX_KING_BITBOARD] >> square) & 1) == 1)
				{
					ulong KingMoves = MoveGen.GetKingMoves(square, myBitboardInverted, myBitboards[INDEX_CASTLE_BITBOARD]);
					MoveGen_PackMovesFromBitboard(KingMoves, moves, square, &move);
				}
			}
		}

		static unsafe void MoveGen_PackMovesFromBitboard(ulong LegalMoveBitboard, Span<ushort> moves, byte square, int* move)
		{
			// Trust inversion has happened when move generation
			// From GPT-4:
			while (LegalMoveBitboard != 0)
			{
				ulong toMove = LegalMoveBitboard & (ulong)-(long)LegalMoveBitboard;
				int toSquare = System.Numerics.BitOperations.TrailingZeroCount(toMove);

				moves[(*move)++] = Pack(square, toSquare, MOVEDATA_NONE);

				LegalMoveBitboard ^= toMove; // Clear the least significant bit set.
			}
		}

		static unsafe void GenerateAllPawnMovesWhite(ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, byte square, int* move)
		{

			if (((CompleteGamestate >> (square + 8)) & 1) == 0)
			{
				if (square >> 3 != 6) moves[(*move)++] = Pack(square, square + 8, MOVEDATA_NONE);
				else
				{
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_QUEEN);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_ROOK);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_BISHOP);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_KNIGHT);
				}
			}

			// West Attack, if (NOT A-File AND opponent has piece on there)
			if ((square & 0b111) != 7 && ((opponentBitboards[INDEX_FULL_BITBOARD] >> (square + 9)) & 1) == 1
			                          && ((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square + 9)) & 1) == 1)
			{
				if (square >> 3 != 6) moves[(*move)++] = Pack(square, square + 9, MOVEDATA_NONE);
				else
				{
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_QUEEN);
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_ROOK);
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_BISHOP);
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_KNIGHT);
				}
			}

			// East Attack, if (NOT H-File AND opponent has piece on there)
			if ((square & 0b111) != 0 && ((opponentBitboards[INDEX_FULL_BITBOARD] >> (square + 7)) & 1) == 1
			                          && ((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square + 7)) & 1) == 1)
			{
				if (square >> 3 != 6) moves[(*move)++] = Pack(square, square + 7, MOVEDATA_NONE);
				else
				{
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_PROMOTION_QUEEN);
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_PROMOTION_ROOK);
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_PROMOTION_BISHOP);
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_PROMOTION_KNIGHT);
				}
			}
		}

		static unsafe void GenerateAllPawnMovesBlack(ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, byte square, int* move)
		{

			if (((CompleteGamestate >> (square + 8)) & 1) == 0)
			{
				if (square >> 3 != 6) moves[(*move)++] = Pack(square, square + 8, MOVEDATA_NONE);
				else
				{
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_QUEEN);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_ROOK);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_BISHOP);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_KNIGHT);
				}
			}

			// West Attack, if (NOT A-File AND opponent has piece on there)
			if ((square & 0b111) != 7 && ((opponentBitboards[INDEX_FULL_BITBOARD] >> (square - 7)) & 1) == 1
			                          && ((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square - 7)) & 1) == 1)
			{
				if (square >> 3 != 6) moves[(*move)++] = Pack(square, square - 7, MOVEDATA_NONE);
				else
				{
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_PROMOTION_QUEEN);
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_PROMOTION_ROOK);
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_PROMOTION_BISHOP);
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_PROMOTION_KNIGHT);
				}
			}

			// East Attack, if (NOT H-File AND opponent has piece on there)
			if ((square & 0b111) != 0 && ((opponentBitboards[INDEX_FULL_BITBOARD] >> (square - 9)) & 1) == 1
			                          && ((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square - 9)) & 1) == 1)
			{
				if (square >> 3 != 6) moves[(*move)++] = Pack(square, square - 9, MOVEDATA_NONE);
				else
				{
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_PROMOTION_QUEEN);
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_PROMOTION_ROOK);
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_PROMOTION_BISHOP);
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_PROMOTION_KNIGHT);
				}
			}
		}
	}
}
