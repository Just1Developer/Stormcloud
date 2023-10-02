using System;
namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	public class PawnMoves
    {
        // Calculation methods at runtime

        // White

        const ulong Bitboard_NotAFile = 0x7F7F7F7F7F7F7F7F;
        const ulong Bitboard_NotHFile = 0xFEFEFEFEFEFEFEFE;

        public static ulong PawnAttacksWhite(ulong pawnBitboard, ulong opponentBitboardComplete)
        {
            ulong moves = 0;
            moves |= (pawnBitboard & Bitboard_NotAFile) << 9;
            moves |= (pawnBitboard & Bitboard_NotHFile) << 7;
            moves &= opponentBitboardComplete;
            return moves;
        }

        public static ulong PawnForwardsWhite(ulong pawnBitboard, ulong myBitboardComplete, ulong opponentBitboardComplete)
        //    => PawnForwards(pawnBitboard, myBitboardComplete | opponentBitboardComplete);
        //public static ulong PawnForwards(ulong pawnBitboard, )
        {
            return (pawnBitboard << 8) & ~(myBitboardComplete | opponentBitboardComplete);
        }

        public static ulong AllPawnMovesWhite(ulong pawnBitboard, ulong myBitboardComplete, ulong opponentBitboardComplete)
        {
            ulong moves = PawnForwardsWhite(pawnBitboard, myBitboardComplete, opponentBitboardComplete);
            moves |= PawnAttacksWhite(pawnBitboard, opponentBitboardComplete);
            return moves;
        }

        // Black

        public static ulong PawnAttacksBlack(ulong pawnBitboard, ulong opponentBitboardComplete)
        {
            ulong moves = 0;
            moves |= (pawnBitboard & Bitboard_NotAFile) >> 7;
            moves |= (pawnBitboard & Bitboard_NotHFile) >> 9;
            moves &= opponentBitboardComplete;
            return moves;
        }

        public static ulong PawnForwardsBlack(ulong pawnBitboard, ulong myBitboardComplete, ulong opponentBitboardComplete)
        //    => PawnForwards(pawnBitboard, myBitboardComplete | opponentBitboardComplete);
        //public static ulong PawnForwards(ulong pawnBitboard, )
        {
            return (pawnBitboard >> 8) & ~(myBitboardComplete | opponentBitboardComplete);
        }

        public static ulong AllPawnMovesBlack(ulong pawnBitboard, ulong myBitboardComplete, ulong opponentBitboardComplete)
        {
            ulong moves = PawnForwardsBlack(pawnBitboard, myBitboardComplete, opponentBitboardComplete);
            moves |= PawnAttacksBlack(pawnBitboard, opponentBitboardComplete);
            return moves;
        }

        public static ulong AllPawnMoves(ulong pawnBitboard, ulong myBitboardComplete, ulong opponentBitboardComplete, bool isWhite)
            => isWhite ? AllPawnMovesWhite(pawnBitboard, myBitboardComplete, opponentBitboardComplete) :
                AllPawnMovesBlack(pawnBitboard, myBitboardComplete, opponentBitboardComplete);
    }
}

