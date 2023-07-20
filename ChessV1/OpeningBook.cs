using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormcloud
{
	// using c as KeyValuePair<ushort, string>>;
	internal static class OpeningBook
	{
		private static Dictionary<string, List<KeyValuePair<ushort, string>>> FollowUpsMoveSequenceWhite;
		private static Dictionary<string, List<KeyValuePair<ushort, string>>> FollowUpsMoveSequenceBlack;

		private static Dictionary<string, List<ushort>> PositionKeyFollowUpsWhite;
		private static Dictionary<string, List<ushort>> PositionKeyFollowUpsBlack;		// No need for storing follow up keys, because the position array needs to be updated anyways

		public static void Init()
		{
			FollowUpsMoveSequenceWhite = new Dictionary<string, List<KeyValuePair<ushort, string>>>();
			FollowUpsMoveSequenceBlack = new Dictionary<string, List<KeyValuePair<ushort, string>>>();
			PositionKeyFollowUpsWhite = new Dictionary<string, List<ushort>>();
			PositionKeyFollowUpsBlack = new Dictionary<string, List<ushort>>();

			// 1-3 Follow-ups for *every* opening move

			// a3

			var MoveList = new List<KeyValuePair<ushort, string>>();
			MoveList.Add(new KeyValuePair<ushort, string>(0x31C0, "e5"));     // e5: 12 -> 28 | 001100 011100 0000 | 0011 0001 1100 0000 | 0x31C0		|	e4e5

			FollowUpsMoveSequenceBlack.Add("a3", MoveList);

			// a4

			MoveList.Clear();
			MoveList.Add(new KeyValuePair<ushort, string>(0x31C0, "e5"));     // e5: 12 -> 28 | 001100 011100 0000 | 0011 0001 1100 0000 | 0x31C0		|	e4e5

			FollowUpsMoveSequenceBlack.Add("a4", MoveList);
			
			// b3

			MoveList.Clear();
			MoveList.Add(new KeyValuePair<ushort, string>(0x31C0, "e5"));     // e5: 12 -> 28 | 001100 011100 0000 | 0011 0001 1100 0000 | 0x31C0		|	e4e5

			FollowUpsMoveSequenceBlack.Add("a4", MoveList);
			
			// b4

			MoveList.Clear();
			MoveList.Add(new KeyValuePair<ushort, string>(0x31C0, "e5"));     // e5: 12 -> 28 | 001100 011100 0000 | 0011 0001 1100 0000 | 0x31C0		|	e4e5

			FollowUpsMoveSequenceBlack.Add("a4", MoveList);

			// ...

			// e4

			MoveList.Clear();
			MoveList.Add(new KeyValuePair<ushort, string>(0x31C0, "e5"));     // e5: 12 -> 28 | 001100 011100 0000 | 0011 0001 1100 0000 | 0x31C0		|	e4e5
			MoveList.Add(new KeyValuePair<ushort, string>(0x3140, "e6"));     // e6: 12 -> 20 | 001100 010100 0000 | 0011 0001 0100 0000 | 0x3140		|	French Defense		|		Refuted at gm/high engine lvl, maybe remove later
			MoveList.Add(new KeyValuePair<ushort, string>(0x29A0, "c5"));     // c5: 10 -> 26 | 001010 011010 0000 | 0010 1001 1010 0000 | 0x29A0		|	Sicilian Defense
			MoveList.Add(new KeyValuePair<ushort, string>(0x2920, "c6"));     // c6: 10 -> 18 | 001010 010010 0000 | 0010 1001 0010 0000 | 0x2920		|	Caro-Kann Defense

			FollowUpsMoveSequenceBlack.Add("e4", MoveList);

			// f3

			MoveList.Clear();
			MoveList.Add(new KeyValuePair<ushort, string>(0x31C0, "e5"));     // e5: 12 -> 28 | 001100 011100 0000 | 0011 0001 1100 0000 | 0x31C0		|	e4e5
			FollowUpsMoveSequenceBlack.Add("e4e5d4", MoveList);

			// ...
		}

		/// <summary>
		/// Edits the move sequence and returns the follow up move. If the move is 0, the Engine needs to calculate.
		/// </summary>
		/// <returns></returns>
		public static ushort Next(ref string sequence, bool isTurnColorWhite, bool bestMoveOnly = false)
		{
			if (isTurnColorWhite)
			{
				if (!FollowUpsMoveSequenceWhite.ContainsKey(sequence)) return 0;
				var l = FollowUpsMoveSequenceWhite[sequence];
				var res = l[new Random().Next(l.Count)];
				sequence += res.Value;
				return res.Key;
			}
			if (!FollowUpsMoveSequenceBlack.ContainsKey(sequence)) return 0;
			var l2 = FollowUpsMoveSequenceBlack[sequence];
			var res2 = l2[new Random().Next(l2.Count)];
			sequence += res2.Value;
			return res2.Key;
		}

		/// <summary>
		/// Returns the follow up move by position key. If the move is 0, the Engine needs to calculate.
		/// </summary>
		/// <returns></returns>
		public static ushort Next(string positionKey, bool isTurnColorWhite, bool bestMoveOnly = false)
		{
			if(isTurnColorWhite)
			{
				if (!PositionKeyFollowUpsWhite.ContainsKey(positionKey)) return 0;
				List<ushort> l = PositionKeyFollowUpsWhite[positionKey];
				return l[new Random().Next(l.Count)];
			}
			if (!PositionKeyFollowUpsBlack.ContainsKey(positionKey)) return 0;
			List<ushort> l2 = PositionKeyFollowUpsBlack[positionKey];
			return l2[new Random().Next(l2.Count)];
		}
	}
}
