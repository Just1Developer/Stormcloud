using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChessV1.Stormcloud.TicTacToe;
using ChessV1.Stormcloud.Connect4;
using ChessV1.Stormcloud.Connect4.Unittest;
using ChessV1.Stormcloud.Connect4.Weightfinder;
using ChessV1.Stormcloud.Connect4.Weightfinder.PSO_JSON_WRITER;

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

			// Todo We still need ALL combinations, not just variance by 1 delta, since that will take too long, make a UI / Background Task to save / pickup where it left off so it can run without data loss in between shutdowns

			//new GenerationController().Start(Columns: 7, Rows: 6, EngineDepth: 6, MAX_GENERATIONS: 4, ESCAPE_VALUE: 0.7);
			//new GenerationController().StartSpecificPreset(EngineDepth: 6);

			// New:
			//new ParticleSwarmOptimization(Particles: 30, Rows: 6, Columns: 7, EngineDepth: 6);

			// Resume previous:
			//ParticleSwarmOptimization.ResumePSO(11);

			// Export to JSON (happens automatically when a run finishes now):
			//PSOExportParser.ConvertToJSONSingleFile(11);
			//PSOExportParser.ConvertToJSONSingleFileInvertedOrder(11);

			Application.Run(new Connect4UI(6, 7));
		}
	}
}
