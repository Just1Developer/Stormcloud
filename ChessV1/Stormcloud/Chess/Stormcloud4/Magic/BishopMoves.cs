using System;
using System.Collections.Generic;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	public class BishopMoves
    {

        static Random random = new ();

        #region Magic Finder

        // Test if the magic number is valid.
        static int TestMagicNumber2(ulong magic, int square, HashSet<ulong> allBlockers)
        {
            byte shiftBits = (byte)(64 - BBits[square]);
            ulong[] used = new ulong[512];  // 2^9 (9 being max bits)
            for (int i = 0; i < 512; i++) used[i] = ulong.MaxValue;
            int size = 0;

            foreach (ulong blockerPos in allBlockers)
            {
                int hash = (int)((blockerPos * magic) >> shiftBits);
                ulong legalMoves = BishopAttacks(square, blockerPos);

                if (used[hash] == ulong.MaxValue) { used[hash] = legalMoves; size++; }
                else if (used[hash] != legalMoves) return -1;
            }
            return size;
        }

        static int count_1s(ulong number)
        {
            int count = 0;
            for (int i = 0; i < 64; i++) if (((number >> i) & 1) == 1) count++;
            return count;
        }

        const int maxSecondsTime = 5;

        static (ulong, int) FindMagicCmethods(int square, int maxSize)
        {
            string str_square = String_square(square);
            int size = int.MaxValue;
            ulong bestMagic = 0;
            ulong mask = BishopMask(square);
            var AllBlockers = GetAllBlockerPositionsFromSquare(mask);
            Log($"Starting, square: {square} | {str_square}, maxSize: {maxSize}, Blockers: {AllBlockers.Count}");
            Log("mask: " + Convert.ToString((long)mask, 2));
            Log("square: " + Convert.ToString(1L << square, 2));
            DateTime start = DateTime.Now;
            while (size == int.MaxValue && true)
            {
                if ((DateTime.Now - start).TotalSeconds > maxSecondsTime)
                {
                    Log($"[{str_square}] Time limit exceeded ({maxSecondsTime}s)");
                    if (size == int.MaxValue) return (0, 0);
                    break;
                }

                // generate new magic number candidate
                ulong magic = (ulong)(random.NextInt64() & random.NextInt64() & random.NextInt64());
                // Check if there are at least 6 1s in the highest 8 bits
                if (count_1s((mask * magic) & 0xFF00000000000000) < 6) continue;    // root out unpromising candidates

                int magicSize = TestMagicNumber2(magic, square, AllBlockers);
                if (magicSize == -1) continue;
                if (magicSize > maxSize) continue;
                if (magicSize >= size) continue;
                bestMagic = magic;
                size = magicSize;
                Log($"[{str_square}] Found new magic number: size: {size}, magic: {magic}, binary: {Convert.ToString((long)magic, 2)}");
            }
            return (bestMagic, size);
        }


        public static void FindMagicsAllSquares()
        {
            ulong[] magics = new ulong[64];
            int[] sizes = new int[64];
            for (int i = 0; i < 64; i++)
            {
                int maxSize = (int) Math.Pow(2, BBits[i]);
                var result = FindMagicCmethods(i, maxSize);
                ulong magic = result.Item1;
                magics[i] = magic;
                sizes[i] = result.Item2;
                Log($"Square: {i}  |  Size: {result.Item2}  |  Magic Found: {magic}UL  |  0b{Convert.ToString((long)magic, 2)}UL  |  0x{Convert.ToString((long)magic, 16)}UL");
            }

            Log("internal static readonly ulong[] rook_magics_X = {");
            for (int i = 0; i < 64; i++)
            {
                ulong magic = magics[i];
                Log($"0x{Convert.ToString((long)magic, 16)}UL,		// size: {sizes[i]}");
            }
            Log("};");

            Environment.Exit(0);

        }

        static string String_square(int square) => $"{(char)('h' - (square % 8))}{(char)('8' - (square / 8))}";

        internal static HashSet<ulong> GetAllBlockerPositionsFromSquare(ulong blockerMask)
        {
            HashSet<ulong> blockerPositions = new HashSet<ulong>();

            int maxCount = count_1s(blockerMask);
            for (ushort i = 0; i <= (1 << maxCount); ++i)
            {
                ulong blockerPosition = MagicNumberFinder.ExpandFromUShort(i, blockerMask);
                blockerPositions.Add(blockerPosition);
            }
            return blockerPositions;
        }

        #endregion

        #region Bishop Mask & Move Data

        // Number of relevant bits for bishops on each square.
        static byte[] BBits = {
          6, 5, 5, 5, 5, 5, 5, 6,
          5, 5, 5, 5, 5, 5, 5, 5,
          5, 5, 7, 7, 7, 7, 5, 5,
          5, 5, 7, 9, 9, 7, 5, 5,
          5, 5, 7, 9, 9, 7, 5, 5,
          5, 5, 7, 7, 7, 7, 5, 5,
          5, 5, 5, 5, 5, 5, 5, 5,
          6, 5, 5, 5, 5, 5, 5, 6
        };

        static ulong BishopMask(int sq)
        {
            ulong result = 0;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1, f = fl + 1; r <= 6 && f <= 6; r++, f++) result |= (1UL << (f + r * 8));
            for (r = rk + 1, f = fl - 1; r <= 6 && f >= 1; r++, f--) result |= (1UL << (f + r * 8));
            for (r = rk - 1, f = fl + 1; r >= 1 && f <= 6; r--, f++) result |= (1UL << (f + r * 8));
            for (r = rk - 1, f = fl - 1; r >= 1 && f >= 1; r--, f--) result |= (1UL << (f + r * 8));
            return result;
        }

       static ulong BishopAttacks(int sq, ulong block)
        {
            ulong result = 0UL;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1, f = fl + 1; r <= 7 && f <= 7; r++, f++)
            {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            for(r = rk+1, f = fl-1; r <= 7 && f >= 0; r++, f--) {
                result |= (1UL << (f + r*8));
                if ((block & (1UL << (f + r* 8))) != 0) break;
            }
            for(r = rk-1, f = fl+1; r >= 0 && f <= 7; r--, f++) {
                result |= (1UL << (f + r*8));
                if ((block & (1UL << (f + r* 8))) != 0) break;
            }
            for (r = rk - 1, f = fl - 1; r >= 0 && f >= 0; r--, f--)
            {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            return result;
        }

        #endregion

        static void Log(string s)
        {
            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s);
            Console.WriteLine(s);
        }
	}
}

