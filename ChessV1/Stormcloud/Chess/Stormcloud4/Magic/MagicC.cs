using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	internal static class MagicC
	{
		// The C code but translated to C#, currently rook only

		// This conforms to how Stormcloud4 is mapping the bits to the squares
		
		// Mask for blocker constellations, so without the last rank
		internal static ulong rmask(int sq)
		{
			ulong result = 0UL;
			int rk = sq / 8, fl = sq % 8, r, f;
			for (r = rk + 1; r <= 6; r++) result |= (1UL << (fl + r * 8));
			for (r = rk - 1; r >= 1; r--) result |= (1UL << (fl + r * 8));
			for (f = fl + 1; f <= 6; f++) result |= (1UL << (f + rk * 8));
			for (f = fl - 1; f >= 1; f--) result |= (1UL << (f + rk * 8));
			return result;
		}

		// 8 byte per long, 12 = 4096 entries, 11 = 2048 and 10 = 1024
		// (6*6)*1024 + 4*6*2048 + 4*4096 = 102400 entries for the entire table
		// 102400 * 8 byte / entry = 819,200 byte = 800kb (/1024) raw data
		// 12 bytes header + 4 bytes padding to make a multiple of 8 bytes per square (per single lookup table)
		// -> 16 bytes * 64 squares = 1024
		// -> 819,200 + 1024 bytes = 820,224 bytes => 801kb for all Rook lookup tables
		internal static readonly byte[] RBits = {
			12, 11, 11, 11, 11, 11, 11, 12,
			11, 10, 10, 10, 10, 10, 10, 11,
			11, 10, 10, 10, 10, 10, 10, 11,
			11, 10, 10, 10, 10, 10, 10, 11,
			11, 10, 10, 10, 10, 10, 10, 11,
			11, 10, 10, 10, 10, 10, 10, 11,
			11, 10, 10, 10, 10, 10, 10, 11,
			12, 11, 11, 11, 11, 11, 11, 12
		};

		// Find a magic number for a given square, number of bits, and piece (rook/bishop).
		public static (ulong, int) find_magicR(int square, int maxSize = 1024)
        {
	        ulong[] blockers = new ulong[4096], attacks = new ulong[4096], used = new ulong[4096];
	        ulong mask, magic;
	        int i, hash, k, n, shiftBits = RBits[square];
	        bool fail;

	        mask = rmask(square);
	        n = count_1s(mask);

	        for (i = 0; i < (1 << n); i++)
	        {
				//printf(
				//    $"Same: {Convert.ToString((long)index_to_uint64(i, n, mask), 2) == Convert.ToString((long)my_index_to_uint64(i, n, mask), 2)}  |  index to uint64: {Convert.ToString((long)index_to_uint64(i, n, mask), 2)}, " +
				//    $"my version: {Convert.ToString((long) my_index_to_uint64(i, n, mask), 2)}");
				if (rookAttacks(square, blockers[i]) != my_rookAttacks(square, blockers[i]))
				printf(
				    $"Same: {Convert.ToString((long)rookAttacks(square, blockers[i]), 2) == Convert.ToString((long)my_rookAttacks(square, blockers[i]), 2)}  |  index to uint64: {Convert.ToString((long)rookAttacks(square, blockers[i]), 2)}, " +
				    $"my version: {Convert.ToString((long) my_rookAttacks(square, blockers[i]), 2)}");
				// Set blockers
				blockers[i] = my_index_to_uint64(i, n, mask);
		        attacks[i] = rookAttacks(square, blockers[i]);	// Todo replace these methods with mine, then at some point it should take minutes for 1, then we know which one is wrong. Otherwise, its test logic
	        }
	        for (k = 0; k < 100000000; k++)
	        {
		        magic = random_uint64_fewbits();
		        if (count_1s((mask * magic) & 0xFF00000000000000UL) < 6) continue;	// Too few
		        for (i = 0; i < 4096; i++) used[i] = ulong.MaxValue; // Reset used, originally 0L but no moves would be possible, right?

		        for (i = 0, fail = false; !fail && i < (1 << n); i++)
		        {
			        hash = transform(blockers[i], magic, shiftBits);
			        if (used[hash] == ulong.MaxValue) used[hash] = attacks[i];
			        else if (used[hash] != attacks[i]) fail = true;
		        }

		        if (!fail)
		        {
			        int size = 0;
					for(int i2 = 0; i2 < 4096; i2++) if (used[i2] != ulong.MaxValue) size++;
					if(size > maxSize) continue;	// Don't return if it's too large
			        return (magic, size);
		        }
	        }
	        printf("***Failed***\n");
	        return (0UL, -1);
        }

		// A pre-computed table to determine which bit is set.
		static readonly int[] BitTable = {
			63, 30, 3, 32, 25, 41, 22, 33, 15, 50, 42, 13, 11, 53, 19, 34, 61, 29, 2,
			51, 21, 43, 45, 10, 18, 47, 1, 54, 9, 57, 0, 35, 62, 31, 40, 4, 49, 5, 52,
			26, 60, 6, 23, 44, 46, 27, 56, 16, 7, 39, 48, 24, 59, 14, 12, 55, 38, 28,
			58, 20, 37, 17, 36, 8
		};

		// Pop the least significant set bit and return its index.
		static unsafe int pop_1st_bit(ulong* bb)
		{
			ulong b = *bb ^ (*bb - 1);
			uint fold = (uint)((b & 0xffffffff) ^ (b >> 32));
			*bb &= (*bb - 1);
			return BitTable[(fold * 0x783a9b23) >> 26];
		}

		// Convert an index to a 64-bit integer based on the bits and mask.
		static unsafe ulong index_to_uint64(int index, int bits, ulong m)
		{
			int i, j;
			ulong result = 0UL;
			for (i = 0; i < bits; i++)
			{
				j = pop_1st_bit(&m);
				if ((index & (1 << i)) != 0) result |= (1UL << j);
			}
			return result;
		}

		// Generate a random 64-bit integer with fewer set bits.
		static ulong random_uint64_fewbits()
		{
			return random_uint64() & random_uint64() & random_uint64();
		}

		// Count the number of set bits (1's) in a 64-bit integer.
		static int count_1s(ulong b)
		{
			int r;
			for (r = 0; b != 0; r++, b &= b - 1) ;
			return r;
		}

		private static readonly Random random = new ();

		static ulong random_uint64()
		{
			ulong u1, u2, u3, u4;
			u1 = (ulong)(random.NextInt64()) & 0xFFFF; // Take the lower 16 bits.
			u2 = (ulong)(random.NextInt64()) & 0xFFFF;
			u3 = (ulong)(random.NextInt64()) & 0xFFFF;
			u4 = (ulong)(random.NextInt64()) & 0xFFFF;
			return u1 | (u2 << 16) | (u3 << 32) | (u4 << 48);
		}


		// Generate rook attacks for a given square and block configuration.
		internal static ulong rookAttacks(byte rank, byte file, ulong blockerBoard) => rookAttacks(rank * 8 + file, blockerBoard);
		internal static ulong rookAttacks(int sq, ulong block)
		{
			ulong result = 0UL;
			int rk = sq / 8, fl = sq % 8, r, f;
			for (r = rk + 1; r <= 7; r++)
			{
				result |= (1UL << (fl + r * 8));
				if ((block & (1UL << (fl + r * 8))) != 0) break;
			}
			for(r = rk-1; r >= 0; r--) {
				result |= (1UL << (fl + r*8));
				if((block & (1UL << (fl + r*8))) != 0) break;
			}
			for(f = fl+1; f <= 7; f++) {
				result |= (1UL << (f + rk*8));
				if((block & (1UL << (f + rk * 8))) != 0) break;
			}
			for (f = fl - 1; f >= 0; f--)
			{
				result |= (1UL << (f + rk * 8));
				if ((block & (1UL << (f + rk * 8))) != 0) break;
			}
			return result;
		}

		// Transform the board configuration with a magic number.
		static int transform(ulong b, ulong magic, int bits)
		{
			return (int)((b * magic) >> (64 - bits));
		}

		static void printf(string s) => MagicNumberFinder.Log(s);

		#region My Methods (Debugging)

		// result is the same as index_to_uint64
		static ulong my_index_to_uint64(int index, int bits, ulong mask)
		{
			// If bits == 5 => 1 << 5 = 100000 -1 => 011111 = 5 1s
			// ulong max = (1UL << bits) - 1;
			return MagicNumberFinder.ExpandFromUShort((ushort)index, mask);
		}

		// Not the best way but I must test my previous implementations
		static ulong my_rookAttacks(int square, ulong blockerConstellation)
		{
			byte rank = (byte) (square & 0b111);	// % 8
			byte file = (byte) (square >> 3);		// / 8

			ulong LegalMovesComplete = (0x0101010101010101UL << file) | (0b11111111UL << (square & ~0b111));
			// make self square gone
			LegalMovesComplete &= ~(1UL << square);

			// File
			ulong fileAnalysis = blockerConstellation >> file; // For analysis, bring the relevant column to the right

			// Down
			for (byte offset = 1; rank + offset < 7; ++offset)
			{
				// Get if blocker or not
				// => Move: fileAnalysis >> (rank+offset * 8)
				if (((fileAnalysis >> ((rank + offset) << 3)) & 1) == 0) continue;  // No blocker
																					// Blocker found. Now, beyond this
				byte _file = (byte)(rank + offset + 1);
				for (; _file < 8; _file++)
				{
					byte reverseIndex = (byte)((_file << 3) + file);    // _file * 8 + file
					LegalMovesComplete &= ~(1UL << reverseIndex);
				}
			}

			// Up
			for (byte offset = 1; rank - offset > 0; ++offset)
			{
				// Get if blocker or not
				if (((fileAnalysis >> ((rank - offset) << 3)) & 1) == 0) continue;  // No blocker
																					// Blocker found. Now, beyond this
				sbyte _file = (sbyte)(rank - offset - 1);       // -1 because next file
				for (; _file > -1; _file--)
				{
					byte reverseIndex = (byte)((_file << 3) + file);    // _file * 8 + file
					LegalMovesComplete &= ~(1UL << reverseIndex);
				}
			}


			ulong rankAnalysis = blockerConstellation >> (rank << 3); // For analysis, bring the relevant row to the bottom

			// Right
			for (byte offset = 1; file + offset < 7; ++offset)
			{
				// Get if blocker or not
				// => Move: fileAnalysis >> (file + offset)
				if (((rankAnalysis >> file + offset) & 1) == 0) continue;   // No blocker
																			// Blocker found. Now, beyond this
				byte _rank = (byte)(file + offset + 1);
				for (; _rank < 8; _rank++)
				{
					byte reverseIndex = (byte)((rank << 3) + _rank);   // file * 8 + _rank
					LegalMovesComplete &= ~(1UL << reverseIndex);
				}
			}

			// Left
			for (byte offset = 1; file - offset > -1; ++offset)
			{
				// Get if blocker or not
				// => Move: fileAnalysis >> (file + offset)
				if (((rankAnalysis >> file - offset) & 1) == 0) continue;   // No blocker
																			// Blocker found. Now, beyond this
				sbyte _rank = (sbyte)(file - offset - 1);
				for (; _rank > -1; _rank--)
				{
					byte reverseIndex = (byte)((rank << 3) + _rank);   // file * 8 + _rank
					LegalMovesComplete &= ~(1UL << reverseIndex);
				}
			}

			return LegalMovesComplete;
		}

		#endregion
	}
}
