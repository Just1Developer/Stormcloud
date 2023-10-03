using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	// No longer To-Do Incorporate Castle and En Passant (both included now I think)
	partial class Stormcloud4
	{
		public static byte GenerateAllMoves(ulong[] myBitboards, ulong[] opponentBitboards, Span<ushort> moves, bool isWhite)
			=> GenerateAllMoves(myBitboards, opponentBitboards, myBitboards[INDEX_FULL_BITBOARD] | opponentBitboards[INDEX_FULL_BITBOARD], moves, isWhite);
		public static unsafe byte GenerateAllMoves(ulong[] myBitboards, ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, bool isWhite)
		{
			ulong allMyPieces = myBitboards[INDEX_FULL_BITBOARD];
			ulong myBitboardInverted = ~allMyPieces;
			byte move = 0;
			// Get all moves in order, order is based on piece amount and how long they probably live:
			// Pawn, Rook, Bishop, Knight, King, Queen
			while (allMyPieces != 0)
			{
				ulong moveBitboard = allMyPieces & (ulong)-(long)allMyPieces;
				byte square = (byte) System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

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
					MoveGen_PackMovesFromBitboard(RookMoves, moves, square, &move, INDEX_ROOK_BITBOARD);
					continue;
				}

				if (((myBitboards[INDEX_BISHOP_BITBOARD] >> square) & 1) == 1)
				{
					ulong BishopMoves = MoveGen.GetBishopMoves(square, myBitboardInverted, CompleteGamestate);
					MoveGen_PackMovesFromBitboard(BishopMoves, moves, square, &move, INDEX_BISHOP_BITBOARD);
					continue;
				}

				if (((myBitboards[INDEX_KNIGHT_BITBOARD] >> square) & 1) == 1)
				{
					ulong KnightMoves = MoveGen.GetKnightMoves(square, myBitboardInverted);
					MoveGen_PackMovesFromBitboard(KnightMoves, moves, square, &move, INDEX_KING_BITBOARD);
					continue;
				}

				if (((myBitboards[INDEX_QUEEN_BITBOARD] >> square) & 1) == 1)
				{
					ulong QueenMoves = MoveGen.GetQueenMoves(square, myBitboardInverted, CompleteGamestate);
					MoveGen_PackMovesFromBitboard(QueenMoves, moves, square, &move, INDEX_QUEEN_BITBOARD);
					continue;
				}

				if (((myBitboards[INDEX_KING_BITBOARD] >> square) & 1) == 1)
				{
					PackKingMovesAndCastleOptions(myBitboards, opponentBitboards, CompleteGamestate, myBitboardInverted,
						moves, square, &move, isWhite);
				}

				allMyPieces ^= moveBitboard;
			}

			return move;
		}

		// Also perhaps disallow moves where the King walks into a check.
		// We could also calculate pins for pinned pieces, but that is too much computation for here.
		// But on the other hand, we get attacked squares anyway, and instead of just ruling out castleing, why not rule out kingmoves too.
		// => Just also NAND it with the King move bitboard
		static unsafe void PackKingMovesAndCastleOptions(ulong[] myBitboards, ulong[] opponentBitboards, ulong combinedBitboard, ulong myBitboardInverted, Span<ushort> moves, byte fromSquare, byte* move, bool isWhite)
		{
			// Generate All Attacks of Opponent
			ulong opponentAttacks = GetAllNonPawnAttackBitboard(opponentBitboards, combinedBitboard);
			if(isWhite) ApplyAllPawnAttackBitboardWhite(opponentBitboards[INDEX_PAWN_BITBOARD], &opponentAttacks);
			else ApplyAllPawnAttackBitboardBlack(opponentBitboards[INDEX_PAWN_BITBOARD], &opponentAttacks);

			ulong KingMoves = MoveGen.GetKingMoves(fromSquare, myBitboardInverted, myBitboards[INDEX_CASTLE_BITBOARD]);
			MoveGen_PackMovesFromBitboard(KingMoves & ~opponentAttacks, moves, fromSquare, move, INDEX_KING_BITBOARD);

			// All moves that would be castle castle moves (allowed)
			// Lets temp them out
			ulong castleOptions = myBitboards[INDEX_CASTLE_BITBOARD];

			if (isWhite)
			{
				ulong shortCastle = CASTLE_SQUAREMASK_VULNERABLE_KINGSIDE_WHITE & castleOptions;
				if ((shortCastle & opponentAttacks) == 0 && shortCastle != 0)
					moves[(*move)++] = Pack(fromSquare, CASTLE_TO_SQUARE_KING_INDEX_KINGSIDE_WHITE, MOVEDATA_CASTLE_SHORT);
				ulong longCastle = CASTLE_SQUAREMASK_VULNERABLE_QUEENSIDE_WHITE & castleOptions;
				if ((shortCastle & opponentAttacks) == 0 && longCastle != 0)
					moves[(*move)++] = Pack(fromSquare, CASTLE_TO_SQUARE_KING_INDEX_QUEENSIDE_WHITE, MOVEDATA_CASTLE_LONG);
			}
			else
			{
				ulong shortCastle = CASTLE_SQUAREMASK_VULNERABLE_KINGSIDE_BLACK & castleOptions;
				if ((shortCastle & opponentAttacks) == 0 && shortCastle != 0)
					moves[(*move)++] = Pack(fromSquare, CASTLE_TO_SQUARE_KING_INDEX_KINGSIDE_BLACK, MOVEDATA_CASTLE_SHORT);
				ulong longCastle = CASTLE_SQUAREMASK_VULNERABLE_QUEENSIDE_BLACK & castleOptions;
				if ((shortCastle & opponentAttacks) == 0 && longCastle != 0)
					moves[(*move)++] = Pack(fromSquare, CASTLE_TO_SQUARE_KING_INDEX_QUEENSIDE_BLACK, MOVEDATA_CASTLE_LONG);
			}
		}

		static ulong GetAllNonPawnAttackBitboard(ulong[] Bitboards, ulong completeGamestateBlockers)
		{
			ulong possibleMoves = 0;

			// We cant really outsource this since the part from the MoveGen class is always different.

			// Sliding pieces first, Bishop:
			var AllPiecesBitboard = Bitboards[INDEX_BISHOP_BITBOARD];
			while (AllPiecesBitboard != 0)
			{
				ulong pieceOnlyBitboard = AllPiecesBitboard & (ulong)-(long)AllPiecesBitboard;
				int square = System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

				// Partial from MoveGen class:
				ulong blockers = MoveGen.BishopBlockerBitboard(square, completeGamestateBlockers);
				int hashBishop = MoveGen.TranslateBishop(square, blockers);

				possibleMoves |= MoveGen.BishopMoves[square][hashBishop];

				AllPiecesBitboard ^= pieceOnlyBitboard; // Clear the least significant bit set.
			}

			// Rooks:
			AllPiecesBitboard = Bitboards[INDEX_ROOK_BITBOARD];
			while (AllPiecesBitboard != 0)
			{
				ulong pieceOnlyBitboard = AllPiecesBitboard & (ulong)-(long)AllPiecesBitboard;
				int square = System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

				// Partial from MoveGen class:
				ulong blockers = MoveGen.RookBlockerBitboard(square, completeGamestateBlockers);
				int hashBishop = MoveGen.TranslateRook(square, blockers);

				possibleMoves |= MoveGen.RookMoves[square][hashBishop];

				AllPiecesBitboard ^= pieceOnlyBitboard; // Clear the least significant bit set.
			}

			// Queen(s):
			AllPiecesBitboard = Bitboards[INDEX_QUEEN_BITBOARD];
			while (AllPiecesBitboard != 0)
			{
				ulong pieceOnlyBitboard = AllPiecesBitboard & (ulong)-(long)AllPiecesBitboard;
				int square = System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

				// Partial from MoveGen class:
				// Bishop:
				ulong blockers = MoveGen.BishopBlockerBitboard(square, completeGamestateBlockers);
				int hashBishop = MoveGen.TranslateBishop(square, blockers);
				possibleMoves |= MoveGen.BishopMoves[square][hashBishop];
				// Rook:
				blockers = MoveGen.RookBlockerBitboard(square, completeGamestateBlockers);
				hashBishop = MoveGen.TranslateRook(square, blockers);
				possibleMoves |= MoveGen.RookMoves[square][hashBishop];

				AllPiecesBitboard ^= pieceOnlyBitboard; // Clear the least significant bit set.
			}

			// Knights:
			AllPiecesBitboard = Bitboards[INDEX_KNIGHT_BITBOARD];
			while (AllPiecesBitboard != 0)
			{
				ulong pieceOnlyBitboard = AllPiecesBitboard & (ulong)-(long)AllPiecesBitboard;
				int square = System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

				// Partial from MoveGen class:
				possibleMoves |= MoveGen.KnightMoves[square];

				AllPiecesBitboard ^= pieceOnlyBitboard; // Clear the least significant bit set.
			}

			// King:
			AllPiecesBitboard = Bitboards[INDEX_KING_BITBOARD];
			// Only 1 king, 100%
			{
				ulong pieceOnlyBitboard = AllPiecesBitboard & (ulong)-(long)AllPiecesBitboard;
				int square = System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

				// Partial from MoveGen class:
				possibleMoves |= MoveGen.KingMoves[square];
			}

			return possibleMoves;
		}

		static unsafe void ApplyAllPawnAttackBitboardWhite(ulong pawnBitboard, ulong* moveBitboard)
		{
			*moveBitboard |= (pawnBitboard & Bitboard_NotAFile) << 9;	// West
			*moveBitboard |= (pawnBitboard & Bitboard_NotHFile) << 7;	// East
		}

		static unsafe void ApplyAllPawnAttackBitboardBlack(ulong pawnBitboard, ulong* moveBitboard)
		{
			*moveBitboard |= (pawnBitboard & Bitboard_NotAFile) >> 7;	// West
			*moveBitboard |= (pawnBitboard & Bitboard_NotHFile) >> 9;	// East
		}

		static unsafe void MoveGen_PackMovesFromBitboard(ulong LegalMoveBitboard, Span<ushort> moves, byte fromSquare, byte* move, byte MOVEDATA)
		{
			// Trust inversion has happened when move generation
			// From GPT-4:
			while (LegalMoveBitboard != 0)
			{
				ulong toMove = LegalMoveBitboard & (ulong)-(long)LegalMoveBitboard;
				int toSquare = System.Numerics.BitOperations.TrailingZeroCount(toMove);

				moves[(*move)++] = Pack(fromSquare, toSquare, MOVEDATA);

				LegalMoveBitboard ^= toMove; // Clear the least significant bit set.
			}
		}

		static unsafe void GenerateAllPawnMovesWhite(ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, byte square, byte* move)
		{
			byte squareShift3 = (byte)(square >> 3);
			if (((CompleteGamestate >> (square + 8)) & 1) == 0)
			{
				if (squareShift3 != 6)
				{
					moves[(*move)++] = Pack(square, square + 8, INDEX_PAWN_BITBOARD);
					if (squareShift3 == 1 && ((CompleteGamestate >> (square + 16)) & 1) == 0)
						moves[(*move)++] = Pack(square, square + 16, MOVEDATA_PAWN_JUMPSTART);
				}
				else
				{
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_Q);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_R);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_B);
					moves[(*move)++] = Pack(square, square + 8, MOVEDATA_PROMOTION_N);
				}
			}

			// West Attack, if (NOT A-File AND opponent has piece on there)
			if ((square & 0b111) != 7 && ((opponentBitboards[INDEX_FULL_BITBOARD] >> (square + 9)) & 1) == 1
			                          && ((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square + 9)) & 1) == 1)
			{
				if (squareShift3 != 6) moves[(*move)++] = Pack(square, square + 9, INDEX_PAWN_BITBOARD);
				else
				{
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_Q);
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_R);
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_B);
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_N);
				}
			}

			// East Attack, if (NOT H-File AND opponent has piece on there)
			if ((square & 0b111) != 0 && ((opponentBitboards[INDEX_FULL_BITBOARD] >> (square + 7)) & 1) == 1
			                          && ((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square + 7)) & 1) == 1)
			{
				if (squareShift3 != 6) moves[(*move)++] = Pack(square, square + 7, INDEX_PAWN_BITBOARD);
				else
				{
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_PROMOTION_Q);
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_PROMOTION_R);
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_PROMOTION_B);
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_PROMOTION_N);
				}
			}
		}

		static unsafe void GenerateAllPawnMovesBlack(ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, byte square, byte* move)
		{
			byte squareShift3 = (byte)(square >> 3);
			if (((CompleteGamestate >> (square - 8)) & 1) == 0)
			{
				if (squareShift3 != 1)
				{
					moves[(*move)++] = Pack(square, square - 8, INDEX_PAWN_BITBOARD);
					if (squareShift3 == 6 && ((CompleteGamestate >> (square - 16)) & 1) == 0)
						moves[(*move)++] = Pack(square, square - 16, MOVEDATA_PAWN_JUMPSTART);
				}
				else
				{
					moves[(*move)++] = Pack(square, square - 8, MOVEDATA_PROMOTION_Q);
					moves[(*move)++] = Pack(square, square - 8, MOVEDATA_PROMOTION_R);
					moves[(*move)++] = Pack(square, square - 8, MOVEDATA_PROMOTION_B);
					moves[(*move)++] = Pack(square, square - 8, MOVEDATA_PROMOTION_N);
				}
			}

			// West Attack, if (NOT A-File AND opponent has piece on there)
			if ((square & 0b111) != 7 && ((opponentBitboards[INDEX_FULL_BITBOARD] >> (square - 7)) & 1) == 1
			                          && ((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square - 7)) & 1) == 1)
			{
				if (squareShift3 != 1) moves[(*move)++] = Pack(square, square - 7, INDEX_PAWN_BITBOARD);
				else
				{
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_PROMOTION_Q);
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_PROMOTION_R);
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_PROMOTION_B);
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_PROMOTION_N);
				}
			}

			// East Attack, if (NOT H-File AND opponent has piece on there)
			if ((square & 0b111) != 0 && ((opponentBitboards[INDEX_FULL_BITBOARD] >> (square - 9)) & 1) == 1
			                          && ((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square - 9)) & 1) == 1)
			{
				if (squareShift3 != 1) moves[(*move)++] = Pack(square, square - 9, INDEX_PAWN_BITBOARD);
				else
				{
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_PROMOTION_Q);
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_PROMOTION_R);
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_PROMOTION_B);
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_PROMOTION_N);
				}
			}
		}
	}
}
