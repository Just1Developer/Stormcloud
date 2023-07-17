using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessV1
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
			new Stormcloud.Stormcloud3(false);
			//new Stormcloud.Stormcloud3(5);
			//Application.Run(new Form1());
		}
	}
}
