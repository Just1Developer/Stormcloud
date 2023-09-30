using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	internal partial class MagicNumberFinder
	{

		/*
		 * Files are from right to left, so shifting is made easier
		 * Ranks are from bottom to top, so shifting is made easier
		 * -> Getting Rank 0 is 0b11111111 >> (0*8), so the bottom most rank
		 * -> Getting Rank 7 is 0b11111111 >> (7*8), so the bottom most rank
		 *
		 */


		#region Rook MagicNumberFinder


		#region Magic Finder


		#region New Magic Finder with sq 0 = h1

		static byte[] RBits = {
				12, 11, 11, 11, 11, 11, 11, 12,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				12, 11, 11, 11, 11, 11, 11, 12
			};


		// Test if the magic number is valid.
		static int TestMagicNumber2(ulong magic, int square, HashSet<ulong> allBlockers)
		{
			byte shiftBits = (byte)(64 - RBits[square]);
			ulong[] used = new ulong[4096];
			for (int i = 0; i < 4096; i++) used[i] = ulong.MaxValue;
			int size = 0;

			foreach (ulong blockerPos in allBlockers)
			{
				int hash = (int)((blockerPos * magic) >> shiftBits);
				ulong legalMoves = MagicC.rookAttacks(square, blockerPos);

				if (used[hash] == ulong.MaxValue) { used[hash] = legalMoves; size++; }
				else if (used[hash] != legalMoves) return -1;

				// Old:
				//if (!hashs.ContainsKey(hash)) hashs.Add(hash, legalMoves);
				//else if (hashs[hash] != legalMoves) return -1;
			}
			if (size <= 8)
			{
				int i, n;
				Log("--------------");
				for (i = 0, n = 0; i < 4096; i++)
				{
					if (used[i] == ulong.MaxValue) continue;
					n++;
					Log($"{n}. used[{i}] == {Convert.ToString((long)used[i], 2)}");
				}
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
			ulong mask = (RookBBSpecial_GetBitboardFileOnly0to7(square % 8) | RookBBSpecial_GetBitboardRankOnly0to7(square / 8)) & ~(1UL << square);
			var AllBlockers = GetAllBlockerPositionsFromSquareNew(mask);
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
				int maxSize = RBits[i] == 12 ? 4096 : RBits[i] == 11 ? 2048 : 1024;
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

		internal static HashSet<ulong> GetAllBlockerPositionsFromSquareNew(ulong blockerMask)
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

		#endregion

		// Since we want to calculate the index for the lookup table, the last square of each file/rank is redundant, since we'd include that anyway (squares are calculated as if every square was empty/had an opponent on it.
		// Thus, we can lower the search space
		static ulong RookBBSpecial_GetBitboardRankOnly0to7(int rank)
		{
			return RookBBSpecial_LastRank << (rank * 8);
		}
		static ulong RookBBSpecial_GetBitboardFileOnly0to7(int file)
		{
			return RookBBSpecial_LastFile << file;
		}

		#endregion



		#region Numberfinder Operation

		// Stores the index that results from the magic number and the Cutoffs in each direction. This Checks for overlay.

		private static Random random = new ();

		// Generates a random 64-bit number with a specific number of bits set. By GPT-4.
		static ulong GenerateRandomNumber(int bitsSet)
		{
			ulong result = 0;
			HashSet<int> usedBits = new HashSet<int>();

			for (int i = 0; i < bitsSet; ++i)
			{
				int bitPos;
				do
				{
					bitPos = random.Next(64);
				} while (usedBits.Contains(bitPos));

				usedBits.Add(bitPos);
				result |= (1UL << bitPos);
			}

			return result;
		}

		static ulong GenerateRandomNumber2()
		{
			ulong u1, u2, u3, u4;
			u1 = (ulong)(random.NextInt64()) & 0xFFFF; // Take the lower 16 bits.
			u2 = (ulong)(random.NextInt64()) & 0xFFFF;
			u3 = (ulong)(random.NextInt64()) & 0xFFFF;
			u4 = (ulong)(random.NextInt64()) & 0xFFFF;
			return u1 | (u2 << 16) | (u3 << 32) | (u4 << 48);
		}

		// Test if the magic number is valid.
		static int TestMagicNumber(ulong magic, byte shiftBits, byte rank, byte file, HashSet<ulong> allBlockerPositions)
		{
			shiftBits = (byte) (64 - shiftBits);
			// For each blocker configuration, compute the hash using the magic number.
			// Check for collisions.
			// If there are collisions, return false.
			// Otherwise, return true.
			//Dictionary<ushort, (byte, byte, byte, byte)> CutoffIndecesOld = new();
			Dictionary<ushort, ulong> CutoffIndeces = new();

			//Log("Starting new test.");

			foreach (ulong blockerPos in allBlockerPositions)
			{
				ushort hash = (ushort) ((blockerPos * magic) >> shiftBits);
				//var cutoffs = GetCutoffInDirectionsLegalMovesPos(blockerPos, rank, file);
				var cutoffs = MagicC.rookAttacks(rank, file, blockerPos);
				if (!CutoffIndeces.ContainsKey(hash))
				{
					CutoffIndeces.Add(hash, cutoffs);
					continue;
				}
				if(CutoffIndeces[hash] == cutoffs) continue;
				//Log($"Collision! hash: {hash} | Blocker Constellation: {Convert.ToString((long)blockerPos, 2)}  |  Content: {Convert.ToString((long) CutoffIndeces[hash], 2)}  |  New Content: {Convert.ToString((long) cutoffs, 2)}");
				return -1;	// Not the same -> Overlap / Hash collision
			}

			//Log("Done.");

			// This is a placeholder, fill with the proper logic.
			return CutoffIndeces.Count;
		}

		#region Start Test Methods & Log

		
		static void StartTesting(byte rank, byte file)
		{
			Log($"[{String_file(file)}{String_rank(rank)}] Beginning Search for rank {rank} and file {file}");
			var allBlockerPositions = GetAllBlockerPositionsFromSquare(rank, file);
			int bestSize = short.MaxValue;
			ulong runs = 0;
			int field = (file << 3) + rank;
			HighlightBitboard |= 1UL << field;
			Runs.TryAdd(field, 0);
			while (searching)
			{
				//runs++;
				runs += 4;
				if (runs % 100000 == 0)
				{
					Runs[field] = (runs / 100000) / 10.0;
					Repaint();
					// Silent! Log($"[{String_file(file)}{String_rank(rank)}] Reached {runs / 1000000} million runs");
				}
				byte movBits = 9;	// 1 less than blocker bits required, which is 10
				//ulong magicCandidate = (ulong) random.NextInt64(long.MaxValue);


				ulong magicCandidate = GenerateRandomNumber(movBits);  // for example, 12 bits set.
				//ulong magicCandidate = GenerateRandomNumber2();  // for example, 12 bits set.



				int result = TestMagicNumber(magicCandidate, movBits, rank, file, allBlockerPositions);
				if (result > -1)
				{
					if (result > bestSize) continue;
					Console.WriteLine($"[{String_file(file)}{String_rank(rank)}] [Shift by {movBits}] Found a better valid magic number: {magicCandidate}. Dictionary Size: {result}");
					bestSize = result;
				}

				magicCandidate = GenerateRandomNumber(movBits + 1);  // for example, 12 bits set.
				result = TestMagicNumber(magicCandidate, (byte) (movBits + 1), rank, file, allBlockerPositions);
				if (result > -1)
				{
					if (result > bestSize) continue;
					Console.WriteLine($"[{String_file(file)}{String_rank(rank)}] [Shift by {movBits + 1}] Found a better valid magic number: {magicCandidate}. Dictionary Size: {result}");
					bestSize = result;
				}

				magicCandidate = GenerateRandomNumber(movBits + 2);  // for example, 12 bits set.
				result = TestMagicNumber(magicCandidate, (byte)(movBits + 2), rank, file, allBlockerPositions);
				if (result > -1)
				{
					if (result > bestSize) continue;
					Console.WriteLine($"[{String_file(file)}{String_rank(rank)}] [Shift by {movBits + 2}] Found a better valid magic number: {magicCandidate}. Dictionary Size: {result}");
					bestSize = result;
				}

				magicCandidate = GenerateRandomNumber(movBits + 3);  // for example, 12 bits set.
				result = TestMagicNumber(magicCandidate, (byte)(movBits + 3), rank, file, allBlockerPositions);
				if (result > -1)
				{
					if (result > bestSize) continue;
					Console.WriteLine($"[{String_file(file)}{String_rank(rank)}] [Shift by {movBits + 3}] Found a better valid magic number: {magicCandidate}. Dictionary Size: {result}");
					bestSize = result;
				}
				
				
				
				
				/*int result2 = TestMagicNumber(magicCandidate, (byte) (movBits - 2), rank, file, allBlockerPositions);
				if (result2 > -1)
				{
					if (result2 > bestSize) continue;
					Console.WriteLine($"[{String_file(file)}{String_rank(rank)}] [Shift by {movBits - 2}] Found a better valid magic number: {magicCandidate}. Dictionary Size: {result2}");
					bestSize = result2;
				}//*/
			}
		}

		static void StartTestingBetter(byte rank, byte file)
		{

			const ulong importantBitsMaxPossible = 0xFF00000000000000UL;	// Mask for frontal 8 bits
			const int minimumImportantBits1s = 6;	// How many of those bits must be 1 for a potential magic number
			byte[] RBits = {
				12, 11, 11, 11, 11, 11, 11, 12,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				12, 11, 11, 11, 11, 11, 11, 12
			};


			Log($"[{String_file(file)}{String_rank(rank)}] Beginning Search for rank {rank} and file {file}");
			var completeMask = GetBitboardFileOnly0to7(file) ^ GetBitboardRankOnly0to7(rank);
			var allBlockerPositions = GetAllBlockerPositionsFromSquare(rank, file);
			int bestSize = short.MaxValue;
			ulong runs = 0;
			int field = (file << 3) + rank;
			HighlightBitboard |= 1UL << field;
			Runs.TryAdd(field, 0);
			while (searching)
			{
				byte movBits = RBits[field];
				ulong magicCandidate = GenerateRandomNumber(movBits);  // for example, 12 bits set.

				if (CompressBitboard(importantBitsMaxPossible, (completeMask * magicCandidate) & importantBitsMaxPossible) < minimumImportantBits1s) continue;	// Invalid magic candidate

				runs++;
				if (runs % 100000 == 0)
				{
					Runs[field] = (runs / 100000) / 10.0;
					Repaint();
					// Silent! Log($"[{String_file(file)}{String_rank(rank)}] Reached {runs / 1000000} million runs");
				}



				int result = TestMagicNumber(magicCandidate, movBits, rank, file, allBlockerPositions);
				if (result > -1)
				{
					if (result > bestSize) continue;
					Console.WriteLine($"[{String_file(file)}{String_rank(rank)}] [Shift by {movBits}] Found a better valid magic number: {magicCandidate}. Dictionary Size: {result}");
					bestSize = result;
				}
			}
		}


		public static void StartTestingSpecific()
		{
			Log("Testing all 64 magics.");

			byte[] RBits = {
				12, 11, 11, 11, 11, 11, 11, 12,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				11, 10, 10, 10, 10, 10, 10, 11,
				12, 11, 11, 11, 11, 11, 11, 12
			};

			// Test all generated magics on all squares

			for (byte rank = 0; rank < 8; rank++)
			{
				for (byte file = 0; file < 8; file++)
				{
					var allBlockerPositions = GetAllBlockerPositionsFromSquare(rank, file);
					int bestSize = -1;
					int field = (file << 3) + rank;
					HighlightBitboard |= 1UL << field;

					ulong bestmagic = 0L;
					int bestIndex = -1;
					for (int i = 0; i < 64; i++)
					{
						ulong magic = 0;//Magics.rook_magics_1[i];
						var result = TestMagicNumber(magic, RBits[field], rank, file, allBlockerPositions);
						if (result == -1) continue;
						if (result < bestSize || bestSize == -1)
						{
							bestSize = result;
							bestmagic = magic;
							bestIndex = i;
						}
					}

					Log($"[Field {String_file(file)}{String_rank(rank)} | {field}] Best Size: {bestSize}, Best Magic [{bestIndex}]: {bestmagic}  |  {Convert.ToString((long) bestmagic, 2)}");

				}
			}

			Log("Done.");
			Environment.Exit(0);
		}

		public static async Task StartTesting()
		{

			/*
			ulong[] magics = new ulong[64];
			int[] sizes = new int[64];
			for (int i = 0; i < 64; i++)
			{
				int maxSize = MagicC.RBits[i] == 12 ? 4096 : MagicC.RBits[i] == 11 ? 2048 : 1024;
				var result = MagicC.find_magicR(i, 4096);
				ulong magic = result.Item1;
				magics[i] = magic;
				sizes[i] = result.Item2;
				Log($"Square: {i}  |  Size: {result.Item2}  |  Magic Found: {magic}UL  |  0b{Convert.ToString((long) magic, 2)}UL  |  0x{Convert.ToString((long) magic, 16)}UL");
			}

			Log("internal static readonly ulong[] rook_magics_X = {");
			for (int i = 0; i < 64; i++)
			{
				ulong magic = magics[i];
				Log($"0x{Convert.ToString((long)magic, 16)}UL,		// size: {sizes[i]}");
			}
			Log("};");

			Environment.Exit(0);

			//*/

			//StartTestingSpecific();
			searching = true;
			Log("Hello world!");

			//*
			HashSet<Task> taskPool = new HashSet<Task>();

			int count = 0;
			// Search the first quarter first (i and i2 < 4
			for (byte i = 1; i < 5; i++)
			{
				for (byte i2 = 1; i2 < 5; i2++)
				{
					count++;
					// Capture variable values because of race conditions
					byte capturedI = i;
					byte capturedI2 = i2;
					Task.Run(() => StartTesting(capturedI, capturedI2));
				}
			}

			Thread.Sleep(2000);	// Wait for all tasks to start

			await Task.WhenAll(taskPool);
			//*/

			//searching = false;

			StartTestingBetter(selectedY, selectedX);

		}

		public static void Log(string s)
		{
			Console.WriteLine(s);
			if(System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s);
		}


		#endregion

		#endregion

		internal static HashSet<ulong> GetAllBlockerPositionsFromSquare(byte rank, byte file)
		{
			//byte s = (byte) (0b111111 - (rank << 3 + file));	// reverse index, 63 - index, index = rank*8 + file
			ulong blockerMask = RookBBSpecial_GetBitboardRankOnly0to7(rank) ^ RookBBSpecial_GetBitboardFileOnly0to7(file);	// XOR to eliminate the rooks current square (overlap)
			// This has 10-12 active bytes, determined by how much overlap there is.
			HashSet<ulong> blockerPositions = new HashSet<ulong>();

			ushort maxCount = CompressBitboard(ulong.MaxValue, blockerMask);
			for (ushort i = 0; i <= maxCount; ++i)
			{
				ulong blockerPosition = ExpandFromUShort(i, blockerMask);
				blockerPositions.Add(blockerPosition);
			}
			return blockerPositions;
		}

		// Bugfree verified
		internal static ulong GetCutoffInDirectionsLegalMovesPos(ulong board, byte rank, byte file)
		{
			ulong LegalMoves = GetBitboardFileOnly0to7(file) ^ GetBitboardRankOnly0to7(rank);
			
			// File
			ulong fileAnalysis = board >> file; // For analysis, bring the relevant column to the right

			// Down
			for (byte offset = 1; rank + offset < 7; ++offset)
			{
				// Get if blocker or not
				// => Move: fileAnalysis >> (rank+offset * 8)
				if(((fileAnalysis >> ((rank + offset) << 3)) & 1) == 0) continue;	// No blocker
				// Blocker found. Now, beyond this
				byte _file = (byte) (rank + offset + 1);
				for (; _file < 8; _file++)
				{
					byte reverseIndex = (byte) ((_file << 3) + file);	// _file * 8 + file
					LegalMoves &= ~(1UL << reverseIndex);
				}
			}

			// Up
			for (byte offset = 1; rank - offset > 0; ++offset)
			{
				// Get if blocker or not
				if(((fileAnalysis >> ((rank - offset) << 3)) & 1) == 0) continue;	// No blocker
				// Blocker found. Now, beyond this
				sbyte _file = (sbyte) (rank - offset - 1);		// -1 because next file
				for (; _file > -1; _file--)
				{
					byte reverseIndex = (byte) ((_file << 3) + file);	// _file * 8 + file
					LegalMoves &= ~(1UL << reverseIndex);
				}
			}

			
			ulong rankAnalysis = board >> (rank << 3); // For analysis, bring the relevant row to the bottom
			
			// Right
			for (byte offset = 1; file + offset < 7; ++offset)
			{
				// Get if blocker or not
				// => Move: fileAnalysis >> (file + offset)
				if (((rankAnalysis >> file + offset) & 1) == 0) continue;	// No blocker
				// Blocker found. Now, beyond this
				byte _rank = (byte) (file + offset + 1);
				for (; _rank < 8; _rank++)
				{
					byte reverseIndex = (byte) ((rank << 3) + _rank);   // file * 8 + _rank
					LegalMoves &= ~(1UL << reverseIndex);
				}
			}

			// Left
			for (byte offset = 1; file - offset > -1; ++offset)
			{
				// Get if blocker or not
				// => Move: fileAnalysis >> (file + offset)
				if (((rankAnalysis >> file - offset) & 1) == 0) continue;	// No blocker
				// Blocker found. Now, beyond this
				sbyte _rank = (sbyte) (file - offset - 1);
				for (; _rank > -1; _rank--)
				{
					byte reverseIndex = (byte) ((rank << 3) + _rank);   // file * 8 + _rank
					LegalMoves &= ~(1UL << reverseIndex);
				}
			}

			return LegalMoves;
		}

		/// <summary>
		/// returns (up, down, left, right)
		/// </summary>
		/// <param name="board"></param>
		/// <param name="rank"></param>
		/// <param name="file"></param>
		/// <returns></returns>
		static (byte, byte, byte, byte) GetCutoffInDirections(ulong board, byte rank, byte file)
		{
			ulong fileAnalysis = board >> file;	// For analysis, bring the relevant column to the right
			// Calculate up
			(byte, byte, byte, byte) cutoffs = (0, 0, 0, 0);
			byte offset = 1;
			while (rank + offset < 8)	// use rank variable since that defines the cross section with the file, which is what I need for file analysis
			{
				if ((fileAnalysis >> ((rank + offset) << 3) & 1) == 1 || rank + offset == 7) { cutoffs.Item1 = offset; break; }
				offset++;
			}

			offset = 1;
			while (rank - offset > -1)   // use rank variable since that defines the cross section with the file, which is what I need for file analysis
			{
				if ((fileAnalysis >> ((rank - offset) << 3) & 1) == 1 || rank - offset == 0) { cutoffs.Item2 = offset; break; }
				offset++;
			}

			// Rank
			ulong rankAnalysis = board >> (rank << 3); // For analysis, bring the relevant row to the bottom
			offset = 1;
			while (file + offset < 8)   // use rank variable since that defines the cross section with the file, which is what I need for file analysis
			{
				if ((rankAnalysis >> (file + offset) & 1) == 1 || file + offset == 7) { cutoffs.Item3 = offset; break; }
				offset++;
			}

			offset = 1;
			while (file - offset > -1)   // use rank variable since that defines the cross section with the file, which is what I need for file analysis
			{
				if ((rankAnalysis >> (file - offset) & 1) == 1 || file - offset == 0) { cutoffs.Item4 = offset; break; }
				offset++;
			}

			return cutoffs;
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
		internal static ushort CompressBitboard(ulong bitboard, ulong mask)
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


/*
 
C Output:

const uint64 RMagic[64] = {
  0x2080020500400f0ULL,
  0x28444000400010ULL,
  0x20000a1004100014ULL,
  0x20010c090202006ULL,
  0x8408008200810004ULL,
  0x1746000808002ULL,
  0x2200098000808201ULL,
  0x12c0002080200041ULL,
  0x104000208e480804ULL,
  0x8084014008281008ULL,
  0x4200810910500410ULL,
  0x100014481c20400cULL,
  0x4014a4040020808ULL,
  0x401002001010a4ULL,
  0x202000500010001ULL,
  0x8112808005810081ULL,
  0x40902108802020ULL,
  0x42002101008101ULL,
  0x459442200810c202ULL,
  0x81001103309808ULL,
  0x8110000080102ULL,
  0x8812806008080404ULL,
  0x104020000800101ULL,
  0x40a1048000028201ULL,
  0x4100ba0000004081ULL,
  0x44803a4003400109ULL,
  0xa010a00000030443ULL,
  0x91021a000100409ULL,
  0x4201e8040880a012ULL,
  0x22a000440201802ULL,
  0x30890a72000204ULL,
  0x10411402a0c482ULL,
  0x40004841102088ULL,
  0x40230000100040ULL,
  0x40100010000a0488ULL,
  0x1410100200050844ULL,
  0x100090808508411ULL,
  0x1410040024001142ULL,
  0x8840018001214002ULL,
  0x410201000098001ULL,
  0x8400802120088848ULL,
  0x2060080000021004ULL,
  0x82101002000d0022ULL,
  0x1001101001008241ULL,
  0x9040411808040102ULL,
  0x600800480009042ULL,
  0x1a020000040205ULL,
  0x4200404040505199ULL,
  0x2020081040080080ULL,
  0x40a3002000544108ULL,
  0x4501100800148402ULL,
  0x81440280100224ULL,
  0x88008000000804ULL,
  0x8084060000002812ULL,
  0x1840201000108312ULL,
  0x5080202000000141ULL,
  0x1042a180880281ULL,
  0x900802900c01040ULL,
  0x8205104104120ULL,
  0x9004220000440aULL,
  0x8029510200708ULL,
  0x8008440100404241ULL,
  0x2420001111000bdULL,
  0x4000882304000041ULL,
};

const uint64 BMagic[64] = {
  0x100420000431024ULL,
  0x280800101073404ULL,
  0x42000a00840802ULL,
  0xca800c0410c2ULL,
  0x81004290941c20ULL,
  0x400200450020250ULL,
  0x444a019204022084ULL,
  0x88610802202109aULL,
  0x11210a0800086008ULL,
  0x400a08c08802801ULL,
  0x1301a0500111c808ULL,
  0x1280100480180404ULL,
  0x720009020028445ULL,
  0x91880a9000010a01ULL,
  0x31200940150802b2ULL,
  0x5119080c20000602ULL,
  0x242400a002448023ULL,
  0x4819006001200008ULL,
  0x222c10400020090ULL,
  0x302008420409004ULL,
  0x504200070009045ULL,
  0x210071240c02046ULL,
  0x1182219000022611ULL,
  0x400c50000005801ULL,
  0x4004010000113100ULL,
  0x2008121604819400ULL,
  0xc4a4010000290101ULL,
  0x404a000888004802ULL,
  0x8820c004105010ULL,
  0x28280100908300ULL,
  0x4c013189c0320a80ULL,
  0x42008080042080ULL,
  0x90803000c080840ULL,
  0x2180001028220ULL,
  0x1084002a040036ULL,
  0x212009200401ULL,
  0x128110040c84a84ULL,
  0x81488020022802ULL,
  0x8c0014100181ULL,
  0x2222013020082ULL,
  0xa00100002382c03ULL,
  0x1000280001005c02ULL,
  0x84801010000114cULL,
  0x480410048000084ULL,
  0x21204420080020aULL,
  0x2020010000424a10ULL,
  0x240041021d500141ULL,
  0x420844000280214ULL,
  0x29084a280042108ULL,
  0x84102a8080a20a49ULL,
  0x104204908010212ULL,
  0x40a20280081860c1ULL,
  0x3044000200121004ULL,
  0x1001008807081122ULL,
  0x50066c000210811ULL,
  0xe3001240f8a106ULL,
  0x940c0204030020d4ULL,
  0x619204000210826aULL,
  0x2010438002b00a2ULL,
  0x884042004005802ULL,
  0xa90240000006404ULL,
  0x500d082244010008ULL,
  0x28190d00040014e0ULL,
  0x825201600c082444ULL,
};
 
 */