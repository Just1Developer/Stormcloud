using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChessV1.Stormcloud.TicTacToe;
using ChessV1.Stormcloud.Connect4;

namespace ChessUI
{
	internal static class Program
	{
		/// <summary>
		/// Der Haupteinstiegspunkt für die Anwendung.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			//new Stormcloud.Stormcloud3();
			//new Stormcloud.Stormcloud3(false);
			//new Stormcloud.Stormcloud3(4);

			//For Chess: Application.Run(new Form1());
			//Application.Run(new TicTacToeUI());
			Application.Run(new Connect4UI(6, 7));
		}
	}
}
