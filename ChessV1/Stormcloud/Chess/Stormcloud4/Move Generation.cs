using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	// No longer To-Do Incorporate Castle and En Passant (both included now I think)
	// => Engine (Move Gen) crashes when no King is on the board	
	//    -> Added line if (*CurrentPieces == 0) return; to start of King Movegen for normal and capture only
	partial class Stormcloud4
	{

		#region Default Move Generation

		public static byte GenerateAllMoves(ulong[] myBitboards, ulong[] opponentBitboards, Span<ushort> moves, bool isWhite)
			=> GenerateAllMoves(myBitboards, opponentBitboards, myBitboards[INDEX_FULL_BITBOARD] | opponentBitboards[INDEX_FULL_BITBOARD], moves, isWhite);
		public static unsafe byte GenerateAllMoves(ulong[] myBitboards, ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, bool isWhite)
		{
			ulong myBitboardInverted = ~myBitboards[INDEX_FULL_BITBOARD];
			byte move = 0;
			
			// differentiating order in endgame

			// Very simple right now: Neither side has a queen and total pieces on the board (og: 20) are < 20 (or constant, 20 is first constant value, might change)
			bool IsEndgame = IsGamestateEndgame(myBitboards, opponentBitboards, CompleteGamestate);


			ulong CurrentPieces;
			if (IsEndgame)
			{
				PackQueenMoves(&CurrentPieces, myBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackRookMoves(&CurrentPieces, myBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackBishopMoves(&CurrentPieces, myBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackKnightMoves(&CurrentPieces, myBitboards, myBitboardInverted, moves, &move);
				PackPawnMoves(&CurrentPieces, myBitboards, opponentBitboards, CompleteGamestate, moves, &move, isWhite);
				PackKingMoves(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate,
					moves, &move, isWhite);
			}
			else
			{
				PackKnightMoves(&CurrentPieces, myBitboards, myBitboardInverted, moves, &move);
				PackPawnMoves(&CurrentPieces, myBitboards, opponentBitboards, CompleteGamestate, moves, &move, isWhite);
				PackBishopMoves(&CurrentPieces, myBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackQueenMoves(&CurrentPieces, myBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackRookMoves(&CurrentPieces, myBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackKingMoves(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate,
					moves, &move, isWhite);
			}

			return move;
		}

		#endregion

		#region Capture Only Move Generation

		// This region servers for capture only move generations
		public static byte GenerateAllMoves_CapturesOnly(ulong[] myBitboards, ulong[] opponentBitboards, Span<ushort> moves, bool isWhite)
	=> GenerateAllMoves_CapturesOnly(myBitboards, opponentBitboards, myBitboards[INDEX_FULL_BITBOARD] | opponentBitboards[INDEX_FULL_BITBOARD], moves, isWhite);
		public static unsafe byte GenerateAllMoves_CapturesOnly(ulong[] myBitboards, ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, bool isWhite)
		{
			ulong myBitboardInverted = ~myBitboards[INDEX_FULL_BITBOARD];
			byte move = 0;

			// differentiating order in endgame

			// Very simple right now: Neither side has a queen and total pieces on the board (og: 20) are < 20 (or constant, 20 is first constant value, might change)
			bool IsEndgame = IsGamestateEndgame(myBitboards, opponentBitboards, CompleteGamestate);


			ulong CurrentPieces;
			if (IsEndgame)
			{
				PackQueenMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackRookMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackBishopMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackKnightMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, moves, &move);
				PackPawnMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, moves, &move, isWhite);
				PackKingMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate,
					moves, &move, isWhite);
			}
			else
			{
				PackPawnMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, moves, &move, isWhite);
				PackKnightMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, moves, &move);
				PackBishopMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackQueenMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackRookMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate, moves, &move);
				PackKingMoves_CapturesOnly(&CurrentPieces, myBitboards, opponentBitboards, myBitboardInverted, CompleteGamestate,
					moves, &move, isWhite);
			}

			return move;
		}


		#endregion

		#region Move Packing

		#region Layer 1 | All Moves

		static unsafe void PackQueenMoves(ulong* CurrentPieces, ulong[] myBitboards, ulong myBitboardInverted, ulong CompleteGamestate, Span<ushort> moves, byte* move)
		{
			*CurrentPieces = myBitboards[INDEX_QUEEN_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				ulong QueenMoves = MoveGen.GetQueenMoves(fromSquare, myBitboardInverted, CompleteGamestate);
				MoveGen_PackMovesFromBitboard(QueenMoves, moves, fromSquare, move, INDEX_QUEEN_BITBOARD);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackRookMoves(ulong* CurrentPieces, ulong[] myBitboards, ulong myBitboardInverted, ulong CompleteGamestate, Span<ushort> moves, byte* move)
		{
			*CurrentPieces = myBitboards[INDEX_ROOK_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				ulong RookMoves = MoveGen.GetRookMoves(fromSquare, myBitboardInverted, CompleteGamestate);
				MoveGen_PackMovesFromBitboard(RookMoves, moves, fromSquare, move, INDEX_ROOK_BITBOARD);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackBishopMoves(ulong* CurrentPieces, ulong[] myBitboards, ulong myBitboardInverted, ulong CompleteGamestate, Span<ushort> moves, byte* move)
		{
			*CurrentPieces = myBitboards[INDEX_BISHOP_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				ulong BishopMoves = MoveGen.GetBishopMoves(fromSquare, myBitboardInverted, CompleteGamestate);
				MoveGen_PackMovesFromBitboard(BishopMoves, moves, fromSquare, move, INDEX_BISHOP_BITBOARD);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackKnightMoves(ulong* CurrentPieces, ulong[] myBitboards, ulong myBitboardInverted, Span<ushort> moves, byte* move)
		{
			*CurrentPieces = myBitboards[INDEX_KNIGHT_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				ulong KnightMoves = MoveGen.GetKnightMoves(fromSquare, myBitboardInverted);
				MoveGen_PackMovesFromBitboard(KnightMoves, moves, fromSquare, move, INDEX_KNIGHT_BITBOARD);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackPawnMoves(ulong* CurrentPieces, ulong[] myBitboards, ulong[] opponentBitboards, ulong CompleteGamestate, Span<ushort> moves, byte* move, bool isWhite)
		{
			*CurrentPieces = myBitboards[INDEX_PAWN_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				if (isWhite)
					GenerateAllPawnMovesWhite(opponentBitboards, CompleteGamestate, moves, fromSquare, move);
				else
					GenerateAllPawnMovesBlack(opponentBitboards, CompleteGamestate, moves, fromSquare, move);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackKingMoves(ulong* CurrentPieces, ulong[] myBitboards, ulong[] opponentBitboards, ulong myBitboardInverted, ulong CompleteGamestate, Span<ushort> moves, byte* move, bool isWhite)
		{
			*CurrentPieces = myBitboards[INDEX_KING_BITBOARD];
			if ((*CurrentPieces) == 0) return;
			*CurrentPieces = myBitboards[INDEX_KING_BITBOARD];
			ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
			byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

			PackKingMovesAndCastleOptions(myBitboards, opponentBitboards, CompleteGamestate, myBitboardInverted,
				moves, fromSquare, move, isWhite);
		}

		#endregion

		#region Layer 2 | All Moves

		// Also perhaps disallow moves where the King walks into a check.
		// We could also calculate pins for pinned pieces, but that is too much computation for here.
		// But on the other hand, we get attacked squares anyway, and instead of just ruling out castleing, why not rule out kingmoves too.
		// => Just also NAND it with the King move bitboard
		static unsafe void PackKingMovesAndCastleOptions(ulong[] myBitboards, ulong[] opponentBitboards, ulong combinedBitboard,
			ulong myBitboardInverted, Span<ushort> moves, byte fromSquare, byte* move, bool isWhite)
		{
			// Generate All Attacks of Opponent
			ulong opponentAttacks = GetAllNonPawnAttackBitboard(opponentBitboards, combinedBitboard);
			if(isWhite) ApplyAllPawnAttackBitboardBlack(opponentBitboards[INDEX_PAWN_BITBOARD], &opponentAttacks);
			else ApplyAllPawnAttackBitboardWhite(opponentBitboards[INDEX_PAWN_BITBOARD], &opponentAttacks);

			ulong KingMoves = MoveGen.GetKingMoves(fromSquare, myBitboardInverted);
			MoveGen_PackMovesFromBitboard(KingMoves & ~opponentAttacks, moves, fromSquare, move, INDEX_KING_BITBOARD);

			// All moves that would be castle castle moves (allowed)
			// Lets temp them out
			ulong castleOptions = myBitboards[INDEX_CASTLE_BITBOARD];

			if (isWhite)
			{
				ulong shortCastle = CASTLE_SQUAREMASK_VULNERABLE_KINGSIDE_WHITE & castleOptions & CASTLE_SQUARES_MUST_BE_FREE_KINGSIDE_WHITE & myBitboardInverted;
				if (shortCastle != 0) if ((shortCastle & opponentAttacks) == 0)
					moves[(*move)++] = Pack(fromSquare, CASTLE_TO_SQUARE_KING_INDEX_KINGSIDE_WHITE, MOVEDATA_CASTLE_SHORT);
				ulong longCastle = CASTLE_SQUAREMASK_VULNERABLE_QUEENSIDE_WHITE & castleOptions & CASTLE_SQUARES_MUST_BE_FREE_QUEENSIDE_WHITE & myBitboardInverted;
				if (longCastle != 0) if ((longCastle & opponentAttacks) == 0)
					moves[(*move)++] = Pack(fromSquare, CASTLE_TO_SQUARE_KING_INDEX_QUEENSIDE_WHITE, MOVEDATA_CASTLE_LONG);
			}
			else
			{
				ulong shortCastle = CASTLE_SQUAREMASK_VULNERABLE_KINGSIDE_BLACK & castleOptions & CASTLE_SQUARES_MUST_BE_FREE_KINGSIDE_BLACK & myBitboardInverted;
				if(shortCastle != 0) if ((shortCastle & opponentAttacks) == 0)
					moves[(*move)++] = Pack(fromSquare, CASTLE_TO_SQUARE_KING_INDEX_KINGSIDE_BLACK, MOVEDATA_CASTLE_SHORT);
				ulong longCastle = CASTLE_SQUAREMASK_VULNERABLE_QUEENSIDE_BLACK & castleOptions & CASTLE_SQUARES_MUST_BE_FREE_QUEENSIDE_BLACK & myBitboardInverted;
				if(longCastle != 0) if ((longCastle & opponentAttacks) == 0)
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
				byte square = (byte) System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

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
				byte square = (byte) System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

				// Partial from MoveGen class:
				ulong blockers = MoveGen.RookBlockerBitboard(square, completeGamestateBlockers);
				int hashRook = MoveGen.TranslateRook(square, blockers);

				possibleMoves |= MoveGen.RookMoves[square][hashRook];

				AllPiecesBitboard ^= pieceOnlyBitboard; // Clear the least significant bit set.
			}

			// Queen(s):
			AllPiecesBitboard = Bitboards[INDEX_QUEEN_BITBOARD];
			while (AllPiecesBitboard != 0)
			{
				ulong pieceOnlyBitboard = AllPiecesBitboard & (ulong)-(long)AllPiecesBitboard;
				byte square = (byte) System.Numerics.BitOperations.TrailingZeroCount(pieceOnlyBitboard);

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

		static unsafe void MoveGen_PackMovesFromBitboard(ulong LegalMoveBitboard, Span<ushort> moves, byte fromSquare, byte* move, byte movedata)
		{
			// Trust inversion has happened when move generation
			// From GPT-4:
			while (LegalMoveBitboard != 0)
			{
				ulong toMove = LegalMoveBitboard & (ulong)-(long)LegalMoveBitboard;
				int toSquare = System.Numerics.BitOperations.TrailingZeroCount(toMove);

				moves[(*move)++] = Pack(fromSquare, toSquare, movedata);

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

			GenerateAllPawnMovesWhite_CapturesOnly(opponentBitboards, moves, square, move);
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

			GenerateAllPawnMovesBlack_CapturesOnly(opponentBitboards, moves, square, move);
		}

		#endregion


		#region Layer 1 | Captures Only

		static unsafe void PackQueenMoves_CapturesOnly(ulong* CurrentPieces, ulong[] myBitboards, ulong[] opponentBitboards, ulong myBitboardInverted, ulong CompleteGamestate, Span<ushort> moves, byte* move)
		{
			*CurrentPieces = myBitboards[INDEX_QUEEN_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				ulong QueenMoves = MoveGen.GetQueenMoves(fromSquare, myBitboardInverted, CompleteGamestate);
				MoveGen_PackMovesFromBitboard(QueenMoves & opponentBitboards[INDEX_FULL_BITBOARD], moves, fromSquare, move, INDEX_QUEEN_BITBOARD);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackRookMoves_CapturesOnly(ulong* CurrentPieces, ulong[] myBitboards, ulong[] opponentBitboards, ulong myBitboardInverted, ulong CompleteGamestate, Span<ushort> moves, byte* move)
		{
			*CurrentPieces = myBitboards[INDEX_ROOK_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				ulong RookMoves = MoveGen.GetRookMoves(fromSquare, myBitboardInverted, CompleteGamestate);
				MoveGen_PackMovesFromBitboard(RookMoves & opponentBitboards[INDEX_FULL_BITBOARD], moves, fromSquare, move, INDEX_ROOK_BITBOARD);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackBishopMoves_CapturesOnly(ulong* CurrentPieces, ulong[] myBitboards, ulong[] opponentBitboards, ulong myBitboardInverted, ulong CompleteGamestate, Span<ushort> moves, byte* move)
		{
			*CurrentPieces = myBitboards[INDEX_BISHOP_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				ulong BishopMoves = MoveGen.GetBishopMoves(fromSquare, myBitboardInverted, CompleteGamestate);
				MoveGen_PackMovesFromBitboard(BishopMoves & opponentBitboards[INDEX_FULL_BITBOARD], moves, fromSquare, move, INDEX_BISHOP_BITBOARD);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackKnightMoves_CapturesOnly(ulong* CurrentPieces, ulong[] myBitboards, ulong[] opponentBitboards, ulong myBitboardInverted, Span<ushort> moves, byte* move)
		{
			*CurrentPieces = myBitboards[INDEX_KNIGHT_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				ulong KnightMoves = MoveGen.GetKnightMoves(fromSquare, myBitboardInverted);
				MoveGen_PackMovesFromBitboard(KnightMoves & opponentBitboards[INDEX_FULL_BITBOARD], moves, fromSquare, move, INDEX_KING_BITBOARD);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackPawnMoves_CapturesOnly(ulong* CurrentPieces, ulong[] myBitboards, ulong[] opponentBitboards, Span<ushort> moves, byte* move, bool isWhite)
		{
			*CurrentPieces = myBitboards[INDEX_PAWN_BITBOARD];
			while (*CurrentPieces != 0)
			{
				ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
				byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

				if (isWhite)
					GenerateAllPawnMovesWhite_CapturesOnly(opponentBitboards, moves, fromSquare, move);
				else
					GenerateAllPawnMovesBlack_CapturesOnly(opponentBitboards, moves, fromSquare, move);

				*CurrentPieces ^= moveBitboard;
			}
		}

		static unsafe void PackKingMoves_CapturesOnly(ulong* CurrentPieces, ulong[] myBitboards, ulong[] opponentBitboards, ulong myBitboardInverted, ulong CompleteGamestate, Span<ushort> moves, byte* move, bool isWhite)
		{
			if (*CurrentPieces == 0) return;
			*CurrentPieces = myBitboards[INDEX_KING_BITBOARD];
			ulong moveBitboard = *CurrentPieces & (ulong)-(long)*CurrentPieces;
			byte fromSquare = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveBitboard);

			// Generate All Attacks of Opponent
			ulong opponentAttacks = GetAllNonPawnAttackBitboard(opponentBitboards, CompleteGamestate);
			if (isWhite) ApplyAllPawnAttackBitboardBlack(opponentBitboards[INDEX_PAWN_BITBOARD], &opponentAttacks);	// Opposite color because we are calculating opponent's moves
			else ApplyAllPawnAttackBitboardWhite(opponentBitboards[INDEX_PAWN_BITBOARD], &opponentAttacks);

			ulong KingMoves = MoveGen.GetKingMoves(fromSquare, myBitboardInverted);
			MoveGen_PackMovesFromBitboard(KingMoves & ~opponentAttacks & opponentBitboards[INDEX_FULL_BITBOARD], moves, fromSquare, move, INDEX_KING_BITBOARD);

			// Castle will never be a capture, so we can leave it out
		}


		#endregion


		#region Layer 2 | Captures Only

		static unsafe void GenerateAllPawnMovesWhite_CapturesOnly(ulong[] opponentBitboards, Span<ushort> moves, byte square, byte* move)
		{
			byte squareShift3 = (byte)(square >> 3);

			// West Attack, if (NOT A-File AND opponent has piece on there)
			if ((square & 0b111) != 7)
			{
				if (((opponentBitboards[INDEX_FULL_BITBOARD] >> (square + 9)) & 1) == 1)
				{
					if (squareShift3 != 6) moves[(*move)++] = Pack(square, square + 9, INDEX_PAWN_BITBOARD);
					else
					{
						moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_Q);	// Is first move here so Auto-Queen, but remember to put something in the UI choose
						moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_R);
						moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_B);
						moves[(*move)++] = Pack(square, square + 9, MOVEDATA_PROMOTION_N);
					}
				}
				else if (((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square + 9)) & 1) == 1)
				{
					// En passant is never a promotion
					moves[(*move)++] = Pack(square, square + 9, MOVEDATA_EN_PASSANT_CAPTURE);
				}
			}

			// East Attack, if (NOT H-File AND opponent has piece on there)
			if ((square & 0b111) != 0)
			{
				if (((opponentBitboards[INDEX_FULL_BITBOARD] >> (square + 7)) & 1) == 1)
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
				else if (((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square + 7)) & 1) == 1)
				{
					// En passant is never a promotion
					moves[(*move)++] = Pack(square, square + 7, MOVEDATA_EN_PASSANT_CAPTURE);
				}
			}
		}

		static unsafe void GenerateAllPawnMovesBlack_CapturesOnly(ulong[] opponentBitboards, Span<ushort> moves, byte square, byte* move)
		{
			byte squareShift3 = (byte)(square >> 3);
			// West Attack, if (NOT A-File AND opponent has piece on there)
			if ((square & 0b111) != 7)
			{
				if (((opponentBitboards[INDEX_FULL_BITBOARD] >> (square - 7)) & 1) == 1)
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
				else if (((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square - 7)) & 1) == 1)
				{
					// En passant is never a promotion
					moves[(*move)++] = Pack(square, square - 7, MOVEDATA_EN_PASSANT_CAPTURE);
				}
			}

			// East Attack, if (NOT H-File AND opponent has piece on there)
			if ((square & 0b111) != 0)
			{
				if (((opponentBitboards[INDEX_FULL_BITBOARD] >> (square - 9)) & 1) == 1)
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
				else if (((opponentBitboards[INDEX_EN_PASSANT_BITBOARD] >> (square - 9)) & 1) == 1)
				{
					// En passant is never a promotion
					moves[(*move)++] = Pack(square, square - 9, MOVEDATA_EN_PASSANT_CAPTURE);
				}
			}
		}

		#endregion


		#endregion

		#region Helpers

		// Todo this is a little bottleneck
		// After tests, this is slightly faster than the while-xor variant
		static byte Count_1s(ulong b)
		{
			byte r;
			for (r = 0; b != 0; r++, b &= b - 1) ;
			return r;
		}

		static bool IsGamestateEndgame(ulong[] myBitboards, ulong[] opponentBitboards, ulong CompleteGamestate)
			=> false; //(myBitboards[INDEX_QUEEN_BITBOARD] & opponentBitboards[INDEX_QUEEN_BITBOARD]) == 0 && Count_1s(CompleteGamestate) < MIN_TOTAL_PIECES_ON_BOARD_FOR_MIDDLEGAME;


		#endregion
	}
}