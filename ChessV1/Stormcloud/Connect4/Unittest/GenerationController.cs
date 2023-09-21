using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Connect4.Unittest
{
	internal class GenerationController
	{

		private static DateTime start = DateTime.Now, end = DateTime.Now;

		public static DateTime End
		{
			get => end;
			set
			{
				end = value;
				Console.WriteLine($"Finished at {end}. Total time: {end - start}");
			}
		}

		public static DateTime StartTime
		{
			get => start;
			set => start = value;
		}

		public static TimeSpan Time
		{
			get => end - start;
		}

		/*
		 * Commands:
		 *   stop / exit: Exists the program immediately after finishing up current games.
		 *   finish: Plays all games currently enqueued, then flushes and stops
		 *   flush: Flushes current data.
		 */

		private UnittestComputer current;
		private UnittestComputerSpecific currentSpecific;

		private bool run = true;

		public void Start(int Rows, int Columns, int EngineDepth, int MAX_GENERATIONS = -1, double ESCAPE_VALUE = 0)
		{
			start = DateTime.Now;
			Console.WriteLine($"Started at {start}");
			current = new UnittestComputer(MAX_GENERATIONS, ESCAPE_VALUE);
			// Handle Console Calls in Main Thread for pause / flush on next
			Console.WriteLine($"Max Generations are {MAX_GENERATIONS}, so {(MAX_GENERATIONS > 0 ? (MAX_GENERATIONS * MAX_GENERATIONS) + "" : "Infinite")} matches will be played if left unattended");
			Console.WriteLine($"Escape Value is {ESCAPE_VALUE}");
			Console.WriteLine("Setting up Matchmaking");
			UnitMatch.Setup(Rows, Columns, EngineDepth);
			Console.WriteLine("Starting processing...");
			Task.Run(() => current.StartAsync());
			recursionConsole();
		}

		void performCommand(string command)
		{
			switch (command)
			{
				case "flush":
					Console.WriteLine("Flushing data...");
					end = DateTime.Now;
					string path = current.Flush();
					Console.WriteLine($"Flushed data to {path}");
					break;
				case "finish":
					Console.WriteLine("Letting queue empty and finishing up...");
					current.EmptyQueueThenStop();
					break;
			}
		}

		void recursionConsole()
		{
			while (run)
			{
				string command = Console.ReadLine();
				if(command == null) continue;
				command = command.ToLower().Trim();
				if (command == "exit" || command == "stop") break;
				performCommand(command);
			}

			Console.WriteLine("Stopping...");
			current.Stop();
		}

		public void StartSpecific(ICollection<EngineWeightMap> weightmaps, int Rows, int Columns, int EngineDepth)
		{
			start = DateTime.Now;
			Console.WriteLine($"Started at {start}");
			currentSpecific = new UnittestComputerSpecific();
			// Handle Console Calls in Main Thread for pause / flush on next
			Console.WriteLine($"Generations are {weightmaps.Count}, resulting in {Math.Pow(weightmaps.Count, 2)} games.");
			Console.WriteLine("Setting up Matchmaking");
			UnitMatch.Setup(Rows, Columns, EngineDepth);
			Console.WriteLine("Starting processing...");
			Task.Run(() => currentSpecific.StartAsync(weightmaps));
			recursionConsole();
		}

		public void StartSpecificPreset(int EngineDepth = 6)
		{
			ICollection<EngineWeightMap> weightMaps = new HashSet<EngineWeightMap>();
			weightMaps.Add(new EngineWeightMap()
			{
				NAME = "MapID-1",
				WEIGHT_SCORE_OWN = 0.11,
				WEIGHT_SCORE_OPPONENT = 0.11,
				WEIGHT_HAMMINGDISTANCE_OWN = 0.9,
				WEIGHT_HAMMINGDISTANCE_OPPONENT = 1.35,

				WEIGHT_FORK_HAMMINGDISTANCE_S = 1,
				WEIGHT_FORK_HAMMINGDISTANCE_L = 1,

				WEIGHT_WALL_DISTANCE = 0,
				WEIGHT_NEIGHBORS = 0,
				WEIGHT_NEIGHBOR_FREE = 1,
				WEIGHT_NEIGHBOR_OWNED = 1.1,
				WEIGHT_NEIGHBOR_TAKEN = -0.9
			});
			weightMaps.Add(EngineWeightMap.DefaultPreset_Old);
			weightMaps.Add(EngineWeightMap.DefaultPreset1);
			weightMaps.Add(EngineWeightMap.DefaultPreset2);
			weightMaps.Add(EngineWeightMap.DefaultPreset3);
			weightMaps.Add(EngineWeightMap.DefaultPresetBalanced);
			weightMaps.Add(EngineWeightMap.ParticleBest_Run1);
			weightMaps.Add(EngineWeightMap.Particle_NewEngine_Best_Run3_D2);
			weightMaps.Add(EngineWeightMap.Particle_NewEngine_Best_Run4_TempIT28_465);
			weightMaps.Add(EngineWeightMap.Particle_NewEngine_Best_Run4_TempIT34_481);
			StartSpecific(weightMaps, Rows: 6, Columns: 7, EngineDepth: EngineDepth);
		}
	}
}
