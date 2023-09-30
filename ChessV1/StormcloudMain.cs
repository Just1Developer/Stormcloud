using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessV1.Stormcloud.Chess.Stormcloud4;

namespace ChessV1
{
	internal class StormcloudMain
	{
		public static int Main()
		{
			DateTime before = DateTime.Now;
			MoveGen.PreGenerateAllPossibleMoves();
			printf($"All moves pre-generated in {(DateTime.Now - before).TotalMilliseconds} ms");
			return 0;
		}

		static void printf(object s)
		{
			if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s.ToString());
			else Console.WriteLine(s.ToString());
		}
	}
}
