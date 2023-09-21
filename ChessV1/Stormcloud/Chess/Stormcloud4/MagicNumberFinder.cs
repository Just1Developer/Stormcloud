using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	internal class MagicNumberFinder
	{
		#region Rook MagicNumberFinder

		#region Numberfinder Operation

		// Stores the index that results from the magic number and the Cutoffs in each direction. This Checks for overlay.
		private Dictionary<ushort, (byte, byte, byte, byte)> CutoffIndeces =
			new Dictionary<ushort, (byte, byte, byte, byte)>();

		#endregion

		ulong RookPossibleMoves(byte rank, byte file, ulong Bitboard = ulong.MaxValue)
		{
			return (GetBitboardRankOnly0to7(Bitboard, rank) & GetBitboardFileOnly0to7(Bitboard, file)) ^ (1UL << (rank * 8 + file));
		}

		private ushort MagicNumber = 0;

		ushort RookPossibleMoves(ulong bitboard, byte rank, byte file)
		{
			ulong mask = (RookBBSpecial_GetBitboardRankOnly0to7(rank) & RookBBSpecial_GetBitboardFileOnly0to7(file)) & ~(1UL << (rank * 8 + file));
			ushort index_unmodified = CompressBitboard(bitboard, mask);
			
			ushort newIndex = (ushort) (index_unmodified * MagicNumber);
			
			
			// Temp
			return 0;
		}

		private const ulong RookBBSpecial_LastFile = 0x0001010101010100;
		private const ulong RookBBSpecial_LastRank = 0b01111110;

		// Since we want to calculate the index for the lookup table, the last square of each file/rank is redundant, since we'd include that anyway (squares are calculated as if every square was empty/had an opponent on it.
		// Thus, we can lower the search space
		static ulong RookBBSpecial_GetBitboardRankOnly0to7(byte rank)
		{
			return RookBBSpecial_LastRank << (rank * 8);
		}
		static ulong RookBBSpecial_GetBitboardFileOnly0to7(byte file)
		{
			return RookBBSpecial_LastFile << file;
		}

		#endregion

		/*
		 * By GPT-4:
		 * This code takes advantage of the fact that (x & -x) gives the lowest set bit in x.
		 * It processes the mask bit by bit, extracting the corresponding bits from the original
		 * bitboard, and then building the final compressed value.
		 */
		static ushort CompressBitboard(ulong bitboard, ulong mask)
		{
			ushort result = 0;
			int pos = 0;

			while (mask != 0)
			{
				ulong bit = mask & (ulong)(-(long)mask);

				if ((bitboard & bit) != 0)
				{
					result |= (ushort)(1 << pos);
				}
				pos++;

				mask ^= bit;
			}

			return result;
		}

		static ulong GetBitboardRankOnly0to7(ulong bitboard, byte rank)
		{
			return bitboard & ((ulong) 0xFF << (rank * 8));
		}
		static ulong GetBitboardRankOnly0to7(byte rank)
		{
			return (ulong) 0xFF << (rank * 8);
		}

		/// <summary>
		/// 00000001 <br/>
		/// 00000001 <br/>
		/// 00000001 <br/>
		/// 00000001 <br/>
		/// 00000001 <br/>
		/// 00000001 <br/>
		/// 00000001 <br/>
		/// 00000001 <br/>
		/// </summary>
		private const ulong LastFile = 0x0101010101010101;

		static ulong GetBitboardFileOnly0to7(ulong bitboard, byte file)
		{
			return bitboard & (LastFile << file);
		}
		static ulong GetBitboardFileOnly0to7(byte file)
		{
			return LastFile << file;
		}
	}
}
