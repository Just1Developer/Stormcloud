using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChessUI;
using ChessV1.Stormcloud.Chess.Stormcloud4;

namespace ChessV1
{
	internal class StormcloudMain
	{
		[STAThread]
		public static int Main()
		{

			DateTime before = DateTime.Now;
			MoveGen.PreGenerateAllPossibleMoves();
			printf($"All moves pre-generated in {(DateTime.Now - before).TotalMilliseconds} ms");

			ApplicationConfiguration.Initialize();
			Application.Run(new Form1());

			//*
			printf("Starting test");
			Stormcloud4 engine = new Stormcloud4();
			engine.Test_1(20);
			//*/
			return 0;
		}

		static void printf(object s)
		{
			if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s.ToString());
			else Console.WriteLine(s.ToString());
		}
	}
}
