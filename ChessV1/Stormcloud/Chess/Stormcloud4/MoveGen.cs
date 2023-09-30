using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
    internal class MoveGen : PawnMoves
    {

        private static readonly ulong[] RookFullBlockerMasks = new ulong[64];
        private static readonly ulong[] BishopFullBlockerMasks = new ulong[64];

        private static readonly ulong[][] RookMoves = new ulong[64][];
        private static readonly ulong[][] BishopMoves = new ulong[64][];
        private static readonly ulong[] KingMoves = new ulong[64];
        private static readonly ulong[] KnightMoves = new ulong[64];

        #region Move Gen

        // Moving Pieces

        // Todo myCastleOptionsBitboard
        public static ulong GetKingMoves(int square, ulong myBitboardInverted, ulong myCastleOptionsBitboard)
            => KingMoves[square] & myBitboardInverted;

        public static ulong GetKnightMoves(int square, ulong myBitboardInverted)
            => KnightMoves[square] & myBitboardInverted;

        // Sliding Pieces

        public static ulong GetQueenMoves(ulong myBitboard, ulong opponentBitboard, int square)
            => GetQueenMoves(square, ~myBitboard, myBitboard | opponentBitboard);
        public static ulong GetRookMoves(ulong myBitboard, ulong opponentBitboard, int square)
            => GetRookMoves(square, ~myBitboard, myBitboard | opponentBitboard);
        public static ulong GetBishopMoves(ulong myBitboard, ulong opponentBitboard, int square)
            => GetBishopMoves(square, ~myBitboard, myBitboard | opponentBitboard);

        public static ulong GetQueenMoves(int square, ulong myBitboardInverted, ulong combinedBitboard)
        {
            ulong blockersRook = RookBlockerBitboard(square, combinedBitboard);
            ulong blockersBishop = BishopBlockerBitboard(square, combinedBitboard);
            int hashRook = TranslateRook(square, blockersRook);
            int hashBishop = TranslateBishop(square, blockersBishop);

            ulong moves = RookMoves[square][hashRook] | BishopMoves[square][hashBishop];
            moves &= myBitboardInverted;
            return moves;
        }

        public static ulong GetRookMoves(int square, ulong myBitboardInverted, ulong combinedBitboard)
        {
            ulong blockers = RookBlockerBitboard(square, combinedBitboard);
            int hashRook = TranslateRook(square, blockers);

            ulong moves = RookMoves[square][hashRook] & myBitboardInverted;
            return moves;
        }

        public static ulong GetBishopMoves(int square, ulong myBitboardInverted, ulong combinedBitboard)
        {
            ulong blockers = BishopBlockerBitboard(square, combinedBitboard);
            int hashBishop = TranslateBishop(square, blockers);

            ulong moves = BishopMoves[square][hashBishop] & myBitboardInverted;
            return moves;
        }

        private const ulong MASK_FILE_BLOCKER = 0x0001010101010100UL;
        private const ulong MASK_RANK_BLOCKER = 0b011111110UL;

        static ulong BlockerBitboard_Rook_Old(int square, ulong combinedBitboard)
        {
            byte file = (byte)(square & 0b000111), rank = (byte)(square & 0b111000);
            ulong mask = (MASK_FILE_BLOCKER << file) | (MASK_RANK_BLOCKER << rank);
            ulong board = combinedBitboard & mask;
            board &= ~(1UL << square);
            return board;
        }

        static ulong RookBlockerBitboard(int square, ulong combinedBitboard)
        {
	        return combinedBitboard & RookFullBlockerMasks[square];
        }

        static ulong BishopBlockerBitboard(int square, ulong combinedBitboard)
        {
	        return combinedBitboard & BishopFullBlockerMasks[square];
        }

        static int TranslateRook(int square, ulong BlockerBitboard)
        {
            return (int) (BlockerBitboard * RookMagics[square]) >> (64-RBits[square]);
        }

        static int TranslateBishop(int square, ulong BlockerBitboard)
        {
            return (int) (BlockerBitboard * RookMagics[square]) >> (64-RBits[square]);
        }

        #endregion

        #region Pre Generate All Possible Moves (except pawns)

        public static void PreGenerateAllPossibleMoves()
        {
            // Normal Pieces, Pawn moves are generated at runtime
            PreGenerateAllLegalKingMoves();
            PreGenerateAllLegalKnightMoves();

            // Sliding Pieces
            PreGenerateAllLegalBishopMoves();
            PreGenerateAllLegalRookMoves();
        }

        private static void PreGenerateAllLegalKingMoves()
        {
            static ulong KingLegalMoves(int square)
            {
                ulong moves = 0;
                //square = 63 - square;	// since layout matters, flip
                int rank = square / 8, file = square % 8;
                if (rank > 0)
                {
                    moves |= 1UL << (square - 8);
                    if (file > 0) moves |= 1UL << (square - 9);
                    if (file < 7) moves |= 1UL << (square - 7);
                }
                if (rank < 7)
                {
                    moves |= 1UL << (square - 9);
                    if (file > 0) moves |= 1UL << (square + 7);
                    if (file < 7) moves |= 1UL << (square + 9);
                }
                if (file > 0) moves |= 1UL << (square - 1);
                if (file < 7) moves |= 1UL << (square + 1);
                return moves;
            }

            for (int sq = 0; sq < 64; sq++)
                KingMoves[sq] = KingLegalMoves(sq);

        }

        private static void PreGenerateAllLegalKnightMoves()
        {
            static ulong KnightLegalMoves(int square)
            {
                ulong moves = 0;
                //square = 63 - square;	// since layout matters, flip
                int rank = square / 8, file = square % 8;
                if (rank != 0)
                {
                    if (file > 1) moves |= 1UL << (square - 10);
                    if (file < 6) moves |= 1UL << (square - 6);
                    if (rank > 1)
                    {
                        if (file != 0) moves |= 1UL << (square - 17);
                        if (file != 7) moves |= 1UL << (square - 15);
                    }
                }
                if (rank != 7)
                {
                    if (file > 1) moves |= 1UL << (square + 6);
                    if (file < 6) moves |= 1UL << (square + 10);
                    if (rank < 6)
                    {
                        if (file != 0) moves |= 1UL << (square + 15);
                        if (file != 7) moves |= 1UL << (square + 17);
                    }
                }
                return moves;
            }

            for (int sq = 0; sq < 64; sq++)
	          KnightMoves[sq] = KnightLegalMoves(sq);
        }

        private static void PreGenerateAllLegalRookMoves()
        {
            void PreGenerateRookMoves(int square)
            {
                ulong magic = RookMagics[square];
                ulong[] moves = new ulong[(int)Math.Pow(2, RBits[square])];
                ulong mask = (MASK_FILE_BLOCKER << (square & 0b111000) | MASK_RANK_BLOCKER << (square & 0b000111)) & ~(1UL << square);
                RookFullBlockerMasks[square] = mask;
                var allBlockers = GetAllBlockerPositions(mask);

                foreach (var blockerPos in allBlockers)
                {
	                int hash = (int) ((blockerPos * magic) >> (64-RBits[square]));
	                moves[hash] = RookAttacks(square, blockerPos);
                }

                RookMoves[square] = moves;
            }

            Parallel.For(0, 64, (square) =>
            {
                PreGenerateRookMoves(square);
            });
        }

        private static void PreGenerateAllLegalBishopMoves()
        {
            void PreGenerateBishopMoves(int square)
            {
                ulong magic = BishopMagics[square];
                ulong[] moves = new ulong[(int)Math.Pow(2, BBits[square])];
                ulong mask = BishopMask(square);
                BishopFullBlockerMasks[square] = mask;
                var allBlockers = GetAllBlockerPositions(mask);

                foreach (var blockerPos in allBlockers)
                {
	                int hash = (int) ((blockerPos * magic) >> (64-BBits[square]));
	                moves[hash] = BishopAttacks(square, blockerPos);
                }

                BishopMoves[square] = moves;
            }

            Parallel.For(0, 64, (square) =>
            {
	            PreGenerateBishopMoves(square);
            });
        }

        #region Sliding Piece Helper

        // Blocker Masks
        internal static HashSet<ulong> GetAllBlockerPositions(ulong blockerMask)
        {
	        //byte s = (byte) (0b111111 - (rank << 3 + file));	// reverse index, 63 - index, index = rank*8 + file
	        // This has 10-12 active bytes, determined by how much overlap there is.
	        HashSet<ulong> blockerPositions = new HashSet<ulong>();

	        int maxCount = count_1s(blockerMask); //CompressBitboard(ulong.MaxValue, blockerMask);
	        //Log("Mask: " + Convert.ToString((long)blockerMask, 2));
	        for (ushort i = 0; i <= (1 << maxCount); ++i)
	        {
		        ulong blockerPosition = ExpandFromUShort(i, blockerMask);
		        //Log($"blockerPos {i}: " + Convert.ToString((long)blockerPosition, 2));
		        blockerPositions.Add(blockerPosition);
	        }
	        return blockerPositions;
        }

        internal static ulong ExpandFromUShort(ushort small, ulong mask)
        {
	        ulong result = 0;

	        while (mask != 0)
	        {
		        ulong lsb = mask & (~mask + 1); // Extract least significant bit of mask, ulong fix for mask & -mask
		        if ((small & 1) != 0)  // Check if the least significant bit of small is set
		        {
			        result |= lsb;  // Set the corresponding bit in result
		        }
		        mask ^= lsb;  // Remove the least significant bit from mask
		        small >>= 1;  // Move to the next bit in small
	        }

	        return result;
        }

        #endregion

        #region Magic Utils


        #region Rooks Mask & Move Data

        // Number of relevant bits for bishops on each square.
        protected static byte[] RBits = {
            12, 11, 11, 11, 11, 11, 11, 12,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            12, 11, 11, 11, 11, 11, 11, 12
        };

        protected static ulong RookMask(int sq)
        {
            ulong result = 0;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1; r <= 6; r++) result |= (1UL << (fl + r * 8));
            for (r = rk - 1; r >= 1; r--) result |= (1UL << (fl + r * 8));
            for (f = fl + 1; f <= 6; f++) result |= (1UL << (f + rk * 8));
            for (f = fl - 1; f >= 1; f--) result |= (1UL << (f + rk * 8));
            return result;
        }

        protected static ulong RookAttacks(int sq, ulong block)
        {
            ulong result = 0UL;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1; r <= 7; r++)
            {
                result |= (1UL << (fl + r * 8));
                if ((block & (1UL << (fl + r * 8))) != 0) break;
            }
            for (r = rk - 1; r >= 0; r--)
            {
                result |= (1UL << (fl + r * 8));
                if ((block & (1UL << (fl + r * 8))) != 0) break;
            }
            for (f = fl + 1; f <= 7; f++)
            {
                result |= (1UL << (f + rk * 8));
                if ((block & (1UL << (f + rk * 8))) != 0) break;
            }
            for (f = fl - 1; f >= 0; f--)
            {
                result |= (1UL << (f + rk * 8));
                if ((block & (1UL << (f + rk * 8))) != 0) break;
            }
            return result;
        }

        #endregion

        #region Bishop Mask & Move Data

        // Number of relevant bits for bishops on each square.
        protected static byte[] BBits = {
            6, 5, 5, 5, 5, 5, 5, 6,
            5, 5, 5, 5, 5, 5, 5, 5,
            5, 5, 7, 7, 7, 7, 5, 5,
            5, 5, 7, 9, 9, 7, 5, 5,
            5, 5, 7, 9, 9, 7, 5, 5,
            5, 5, 7, 7, 7, 7, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5,
            6, 5, 5, 5, 5, 5, 5, 6
        };

        protected static ulong BishopMask(int sq)
        {
            ulong result = 0;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1, f = fl + 1; r <= 6 && f <= 6; r++, f++) result |= (1UL << (f + r * 8));
            for (r = rk + 1, f = fl - 1; r <= 6 && f >= 1; r++, f--) result |= (1UL << (f + r * 8));
            for (r = rk - 1, f = fl + 1; r >= 1 && f <= 6; r--, f++) result |= (1UL << (f + r * 8));
            for (r = rk - 1, f = fl - 1; r >= 1 && f >= 1; r--, f--) result |= (1UL << (f + r * 8));
            return result;
        }

        protected static ulong BishopAttacks(int sq, ulong block)
        {
            ulong result = 0UL;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1, f = fl + 1; r <= 7 && f <= 7; r++, f++)
            {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            for (r = rk + 1, f = fl - 1; r <= 7 && f >= 0; r++, f--)
            {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            for (r = rk - 1, f = fl + 1; r >= 0 && f <= 7; r--, f++)
            {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            for (r = rk - 1, f = fl - 1; r >= 0 && f >= 0; r--, f--)
            {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            return result;
        }

        #endregion

        #region Mapping Utils

        // A pre-computed table to determine which bit is set.
        protected static readonly int[] BitTable = {
            63, 30, 3, 32, 25, 41, 22, 33, 15, 50, 42, 13, 11, 53, 19, 34, 61, 29, 2,
            51, 21, 43, 45, 10, 18, 47, 1, 54, 9, 57, 0, 35, 62, 31, 40, 4, 49, 5, 52,
            26, 60, 6, 23, 44, 46, 27, 56, 16, 7, 39, 48, 24, 59, 14, 12, 55, 38, 28,
            58, 20, 37, 17, 36, 8
        };

        // Pop the least significant set bit and return its index.
        protected static unsafe int pop_1st_bit(ulong* bb)
        {
            ulong b = *bb ^ (*bb - 1);
            uint fold = (uint)((b & 0xffffffff) ^ (b >> 32));
            *bb &= (*bb - 1);
            return BitTable[(fold * 0x783a9b23) >> 26];
        }

        protected static int count_1s(ulong b)
        {
            int r;
            for (r = 0; b != 0; r++, b &= b - 1) ;
            return r;
        }

        #endregion


        #endregion

        #endregion

        private static readonly ulong[] RookMagics = {
            0x8000c004201382UL,		// size: 4096
            0x440200210044000UL,		// size: 2048
            0x2080100080082000UL,		// size: 2048
            0x20004400a001020UL,		// size: 2048
            0x4600020020041089UL,		// size: 2048
            0x1a00420001100814UL,		// size: 2048
            0x200380120842200UL,		// size: 2048
            0x4100008021000042UL,		// size: 4096
            0x64800086a14008UL,		// size: 2048
            0x404000201000UL,		// size: 1024
            0x2080100020008aUL,		// size: 1024
            0x3041800800801002UL,		// size: 1024
            0x41800401800801UL,		// size: 1024
            0x20808002008400UL,		// size: 1024
            0x4000421020850UL,		// size: 1024
            0x600010a008044UL,		// size: 2048
            0x80010020408104UL,		// size: 2048
            0x110084000482000UL,		// size: 1024
            0x2010002000280400UL,		// size: 1024
            0x20120042000820UL,		// size: 1024
            0x4921010004100800UL,		// size: 1024
            0x2008004008002UL,		// size: 1024
            0x600240042411850UL,		// size: 1024
            0x1020009118244UL,		// size: 2048
            0x11205020023c080UL,		// size: 2048
            0x500240022000UL,		// size: 1024
            0x10008180112000UL,		// size: 1024
            0x402200900100101UL,		// size: 1024
            0x810110100040800UL,		// size: 1024
            0x14008080040200UL,		// size: 1024
            0x1880020400282110UL,		// size: 1024
            0x108200204104UL,		// size: 2048
            0x20e0204000800080UL,		// size: 2048
            0x40002000808040UL,		// size: 1024
            0x402004082001024UL,		// size: 1024
            0x140100080800800UL,		// size: 1024
            0x210800800800401UL,		// size: 1024
            0x2801401800200UL,		// size: 1024
            0x8a1100804000201UL,		// size: 1024
            0x15001088200004cUL,		// size: 2048
            0x852258340028003UL,		// size: 2048
            0x101008200220044UL,		// size: 1024
            0x18c8402200820010UL,		// size: 1024
            0x1020100101000aUL,		// size: 1024
            0x40801010010UL,		// size: 1024
            0x10600a811220004UL,		// size: 1024
            0x20010208040010UL,		// size: 1024
            0x400100a044060005UL,		// size: 2048
            0x8508008210100UL,		// size: 2048
            0xa004080210200UL,		// size: 1024
            0x888200049001100UL,		// size: 1024
            0x201000220b0100UL,		// size: 1024
            0x28010009041100UL,		// size: 1024
            0x800400020080UL,		// size: 1024
            0x800c20178100c00UL,		// size: 1024
            0x2005000080420100UL,		// size: 2048
            0x6002008048201106UL,		// size: 4096
            0x10400080110021UL,		// size: 2048
            0x2030402005001009UL,		// size: 2048
            0x1080200810000501UL,		// size: 2048
            0xc1001002080005UL,		// size: 2048
            0x200a000130880422UL,		// size: 2048
            0x1000100108022084UL,		// size: 2048
            0x6298002040840d02UL,		// size: 4096
        };

        private static readonly ulong[] BishopMagics = {
            0x4048100414404200UL,           // size: 64
            0x5110112124208010UL,           // size: 32
            0xb1411202042108UL,             // size: 32
            0x4424414180080000UL,           // size: 32
            0x1494230601400001UL,           // size: 32
            0x2c0900420408000UL,            // size: 32
            0x7180980110102001UL,           // size: 32
            0x81d10c01044008UL,             // size: 64
            0x60144c410240101UL,            // size: 32
            0x4c800801c1040100UL,           // size: 32
            0x40410408e204182UL,            // size: 32
            0x24080865000001UL,             // size: 32
            0x2420210100840UL,              // size: 32
            0x8020130890200UL,              // size: 32
            0x11080104a220UL,               // size: 32
            0x10108208010480UL,             // size: 32
            0x108421002100420UL,            // size: 32
            0x21a4010244042400UL,           // size: 32
            0x920811004868008UL,            // size: 128
            0x4000241020200UL,              // size: 128
            0x42a820400a06000UL,            // size: 128
            0x100040020110212aUL,           // size: 128
            0x750804402119010UL,            // size: 32
            0x5120280413000UL,              // size: 32
            0x4002091311901000UL,           // size: 32
            0x4002a00408010410UL,           // size: 32
            0x2002280004080024UL,           // size: 128
            0x4040808208020082UL,           // size: 512
            0x10840200802004UL,             // size: 512
            0x410122001040100UL,            // size: 128
            0x2049084148802UL,              // size: 32
            0x211010800208800UL,            // size: 32
            0x20a848040040041aUL,           // size: 32
            0x42020200103008UL,             // size: 32
            0x3044a29010080020UL,           // size: 128
            0x204a004040140102UL,           // size: 512
            0x10008200202200UL,             // size: 512
            0x4210208080380a00UL,           // size: 128
            0x1440808404043cUL,             // size: 32
            0x802420200284642UL,            // size: 32
            0x88e2012402004UL,              // size: 32
            0x81009010440480UL,             // size: 32
            0x40a0082041010UL,              // size: 128
            0x12109c4200801805UL,           // size: 128
            0x2000080104020040UL,           // size: 128
            0x4008092008100UL,              // size: 128
            0x209411c404095100UL,           // size: 32
            0x404108082011900UL,            // size: 32
            0x84008804120201UL,             // size: 32
            0x200410848c088UL,              // size: 32
            0x890104404040080UL,            // size: 32
            0x112100104880080UL,            // size: 32
            0x2081042020040UL,              // size: 32
            0x1100290010002UL,              // size: 32
            0x20041010c10081UL,             // size: 32
            0x82044c102002100UL,            // size: 32
            0x160a010048020802UL,           // size: 64
            0xc08084a08140210UL,            // size: 32
            0x4004100200840408UL,           // size: 32
            0x4000831010460800UL,           // size: 32
            0x480400004050404UL,            // size: 32
            0x9002004504084UL,              // size: 32
            0x8411014210c0410UL,            // size: 32
            0x1064042408020014UL,           // size: 64
        };
    }
}



/*
 *
 * For printing out in a file:
			builderHex.AppendLine("\t\tinternal static ulong[][] AllLegalRookMovesHex = {");
            builderDec.AppendLine("\t\tinternal static ulong[][] AllLegalRookMovesDec = {");
            for (int square = 0; square < 64; square++)
            {
	            builderHex.AppendLine($"\t\t\t// square: {square+1}");
	            builderDec.AppendLine($"\t\t\t// square: {square+1}");
	            builderHex.AppendLine($"\t\t\tnew ulong[] {{\t\t");
	            builderDec.AppendLine($"\t\t\tnew ulong[] {{\t\t");
                PreGenerateRookMoves(square);
                builderHex.AppendLine("\t\t\t},");
                builderDec.AppendLine("\t\t\t},");
            }
            builderHex.AppendLine("\t\t};");
            builderDec.AppendLine("\t\t};");

            Log("namespace ChessV1.Stormcloud.Chess.Stormcloud4\n{\n\tinternal class RookMoves\n\t{");
            Log(builderHex.ToString());
            Log(builderDec.ToString());
            Log("\t}\n}");


			Inside the rook move generation after moves[] is generated:


                for (int i = 0; i < moves.Length; i++)
                {
                    //Log($"\t\t\t\t0x{Convert.ToString((long) moves[i], 16)}UL,");
                    builderHex.AppendLine($"\t\t\t\t0x{Convert.ToString((long) moves[i], 16)}UL,");
                    builderDec.AppendLine($"\t\t\t\t{moves[i]}UL,");
                }
 *
 */