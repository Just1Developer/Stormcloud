using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Connect4.Unittest
{
	internal class UnittestComputer
	{

		#region Data Flush

		private const string DATA_PATH = "E:/Coding/C#/Forms/Stormcloud/Connect4Data/Buffer/";
		const int MAX_FOLDERS = 10000;
		private int MAX_GENERATIONS;
		private const int START_MAPS = 3;
		private const double VALUE_OFFSET = 0.45;

		// Should be >= 1, divides # of cores by this to determine how many threads are created, minimum (1) creates as many threads as there are Cores.
		private double TOTAL_CORE_DIVIDER = 1.3;

		/// <summary>
		/// ESCAPE_VALUE: The Threshold the last value of the Map Value Array has to reach to stop NewGen. -1 = infinite.
		/// </summary>
		private double ESCAPE_VALUE;

		internal string Flush()
		{
			Console.WriteLine("Attempting Data Flush");
			bool done = false;
			int max;
			for (max = 0; max <= MAX_FOLDERS; max++)
			{
				if (Directory.Exists(DATA_PATH + max)) continue;
				done = true;
				break;
			}

			if (!done)
			{
				Console.Error.WriteLine("Error: Max amount of folders reached, could not flush data.");
				return "-";
			}

			string path = DATA_PATH + max + "/";
			Directory.CreateDirectory(path);

			// Now, gather the data and actually write it.
			// 3 Files corresponding to maps, mapscores and info

			int i = 1, amount = Scores.Count;

			try
			{
				StringBuilder FileInfo = new StringBuilder();
				FileInfo.AppendLine("File info:");
				FileInfo.AppendLine($"WeightMaps run (total): {amount}");
				FileInfo.AppendLine($"Games Played: {matches-1}");
				FileInfo.AppendLine($"Maximum Games per Player: {amount * 2 - 1}");
				FileInfo.AppendLine($"  -> Explanation {amount} maps, so each map has {amount-1} opponents. Each opponent 2 times, 1 per color, thus {(amount - 1) * 2} games, and then 1 game against itself.");
				FileInfo.AppendLine($"Start Time: {GenerationController.StartTime}");
				FileInfo.AppendLine($"End Time: {GenerationController.End}");
				FileInfo.AppendLine($"Total Time: {GenerationController.Time}");
				FileInfo.AppendLine($"Boardsize: {UnitMatch.Boardsize}");
				FileInfo.AppendLine($"Engine Depth: {UnitMatch._EngineDepth}");
				FileInfo.AppendLine("Statistics:");
				FileInfo.AppendLine("  >> Todo");

				File.Create(path + "INFO.strmcld").Close();
				File.WriteAllText(path + "INFO.strmcld", FileInfo.ToString());

				StringBuilder FileMaps = new StringBuilder();

				FileMaps.AppendLine($"\nMaps run: {amount}");
				FileMaps.AppendLine($"Map Template: {TemplateWeightMap.NAME}");
				foreach (var map in EngineWeightMaps)
				{
					FileMaps.AppendLine($"\n\nMap ID: {map.Key}");
					FileMaps.AppendLine("Map Values:");
					FileMaps.AppendLine("{");
					FileMaps.AppendLine($"	NAME = \"{map.Value.NAME}\",");
					FileMaps.AppendLine($"\n	WEIGHT_SCORE_OWN = {map.Value.WEIGHT_SCORE_OWN.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_SCORE_OPPONENT = {map.Value.WEIGHT_SCORE_OPPONENT.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OWN = {map.Value.WEIGHT_HAMMINGDISTANCE_OWN.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OPPONENT = {map.Value.WEIGHT_HAMMINGDISTANCE_OPPONENT.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"\n	WEIGHT_FORK_HAMMINGDISTANCE_S = {map.Value.WEIGHT_FORK_HAMMINGDISTANCE_S.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_FORK_HAMMINGDISTANCE_L = {map.Value.WEIGHT_FORK_HAMMINGDISTANCE_L.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"\n	WEIGHT_WALL_DISTANCE = {map.Value.WEIGHT_WALL_DISTANCE.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBORS = {map.Value.WEIGHT_NEIGHBORS.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_FREE = {map.Value.WEIGHT_NEIGHBOR_FREE.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_OWNED = {map.Value.WEIGHT_NEIGHBOR_OWNED.ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_TAKEN = {map.Value.WEIGHT_NEIGHBOR_TAKEN.ToString().Replace(",", ".")},");
					FileMaps.AppendLine("};");
				}

				File.Create(path + "WEIGHTMAPS.strmcld").Close();
				File.WriteAllText(path + "WEIGHTMAPS.strmcld", FileMaps.ToString());

				StringBuilder FileScores = new StringBuilder();

				foreach (var mapID in Scores.OrderByDescending(x => x.Value.Item1 / (x.Value.Item2 == 0 ? 1 : x.Value.Item2)).Select(x => x.Key))
				{
					FileScores.AppendLine($"\nMap {i++} of {amount}");
					FileScores.AppendLine($" >> Map ID: {mapID}");
					(double, int) score = Scores[mapID];
					FileScores.AppendLine($" >> Score: {score.Item1} / {score.Item2}");
				}

				//File.Create(path + "WEIGHTMAP_SCORES.strmcld");
				File.WriteAllText(path + "WEIGHTMAP_SCORES.strmcld", FileScores.ToString());

				Console.WriteLine($"Data flushed to: {path}");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error:\nSource: {ex.Source}\nMessage:{ex.Message}\nStacktrace: {ex.StackTrace}");
			}

			return path;
		}

		#endregion

		/// <summary>
		/// Enqueue matches.
		/// </summary>
		ConcurrentQueue<(int, int)> UpcomingMatches = new ConcurrentQueue<(int, int)> ();

		// Is there a way to calculate the weightmaps instead? Would it be better?
		/// <summary>
		/// Stores the weightmaps along with their ID.
		/// </summary>
		ConcurrentDictionary<int, EngineWeightMap> EngineWeightMaps = new ConcurrentDictionary<int, EngineWeightMap>();

		/// <summary>
		/// Stores the id of the WeightMaps along with their current score and their maximum score. <br/>
		/// Max score is always an int, since there are no half games, but Draws give half a point, so the actual score type needs to account for that.
		/// </summary>
		ConcurrentDictionary<int, (double, int)> Scores = new ConcurrentDictionary<int, (double, int)> ();

		public bool AllowGenerateNew { private set; get; }
		public bool Run { private set; get; }

		/*
		 * Mission:
		 * 1. Create a lot of WeightMaps
		 * 2. 16 Threads, pit them against each other, record collective scores
		 * 3. Variate only a few parameters
		 * 4. Resign at mate in depth-2, because both engines *should* see it

		// Perhaps multiple generations to just stay close to the best of the last version?
		// So previously it was +/- 0.1, so now everything slides in the bounds of bestOfPrevGen +/- 3*0.1 with maybe 0.05, and so on
		 */

		public UnittestComputer(int MAX_GENERATIONS = -1, double ESCAPE_VALUE = 0)
		{
			this.MAX_GENERATIONS = MAX_GENERATIONS;
			this.ESCAPE_VALUE = ESCAPE_VALUE;
			this.creationParamters = new[] { VALUE_OFFSET, VALUE_OFFSET, VALUE_OFFSET, VALUE_OFFSET };
		}

		// The start and workerThreads logic is abstracted from ChatGPT code
		private List<Task> threadPoolTasks;

		private int coreCount;
		public async Task StartAsync(EngineWeightMap Template = null)
		{
			if (Run) return;
			Run = true;
			AllowGenerateNew = true;
			creationParamters = new[] { VALUE_OFFSET, VALUE_OFFSET, VALUE_OFFSET, VALUE_OFFSET };

			// Set Template
			if (Template == null) TemplateWeightMap = EngineWeightMap.HighestEloEngineBoard;
			else TemplateWeightMap = Template;

			for (int i = 0; i < START_MAPS; i++)
			{
				try
				{
					await NewMatches();
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"Error:\nSource: {ex.Source}\nMessage:{ex.Message}\nStacktrace: {ex.StackTrace}");
				}

				Console.WriteLine($"Added new Weightmap. Queue size: {UpcomingMatches.Count}");
			}

			// Create a task for each core in the system.
			coreCount = (int) (Environment.ProcessorCount / TOTAL_CORE_DIVIDER);         // Run on Half Cores
			Console.WriteLine($"Running on {coreCount} cores");
			threadPoolTasks = new List<Task>(coreCount);

			for (int i = 0; i < coreCount; i++)
			{
				var task = Task.Run(() => RunMatchHandler());
				threadPoolTasks.Add(task);
			}

			// No need for Run = false since the Tasks only end when Run = false
			await Task.WhenAll(threadPoolTasks);
			Console.WriteLine("All Finished.");
			Stop();
		}

		private int helper = 1;
		private void RunMatchHandler()
		{
			while (Run)
			{
				RunNextMatch().Wait();  // Wait synchronously for the RunNextMatch to complete
				//Console.WriteLine($"Completed Game. | Run: {Run} | Count: {UpcomingMatches.Count} | IsEmpty: {UpcomingMatches.IsEmpty} | AllowNewGen: {AllowGenerateNew} | break: {UpcomingMatches.IsEmpty && !AllowGenerateNew}");
				if (UpcomingMatches.IsEmpty && !AllowGenerateNew) break;    // No matches available and no new are coming
			}
			Console.WriteLine($"Ending RunMatchHandler ({helper++}/{coreCount})");
		}

		public void Stop()
		{
			Run = false;
			// Wait for all tasks to complete. 
			Task.WaitAll(threadPoolTasks.ToArray());
			GenerationController.End = DateTime.Now;
			Flush();
			Environment.Exit(0);
		}
		public void EmptyQueueThenStop()
		{
			AllowGenerateNew = false;
			// Wait for all tasks to complete. 
			Task.WaitAll(threadPoolTasks.ToArray());
			Run = false;
			GenerationController.End = DateTime.Now;
			Flush();
			Environment.Exit(0);
		}

		private int matches = 1;

		private async Task RunNextMatch()
		{
			while(generatingNew) Thread.Sleep(100);	// Wait for generation of new matches
			if (UpcomingMatches.Count == 0)
			{
				if (!AllowGenerateNew)
				{
					Console.WriteLine("Maximum maps reached, Thread Generation is now off and all Games are being processed. Stopping Thread...");
					return;
				}
				await NewMatches();
				Console.WriteLine($"Loaded new Matches, Queue size: {UpcomingMatches.Count} | Run: {Run} | AllowGenerateNew: {AllowGenerateNew}");
				if (!Run) return;
			}
			if (!UpcomingMatches.TryDequeue(out (int, int) next))
			{
				Console.Error.WriteLine($"Couldn't get match {matches}(?), Queue size: {UpcomingMatches.Count}");
				return;  // None gotten
			}

			int thisMatch = matches++;

			/* So either some threads still fail / clog up sometimes or I solved the issue somewhere
			if (thisMatch == 4)
			{
				Console.WriteLine($"This is match 4, it will just return. Match: ID {next.Item1} vs {next.Item2}. Apparently this matchup fucks up the engine somewhere, idk");
				return;
			}//*/

			//Console.WriteLine($"Running match {thisMatch} | Left in queue: {UpcomingMatches.Count}");
			EngineWeightMap WeightMapYellow = EngineWeightMaps[next.Item1];
			EngineWeightMap WeightMapRed = EngineWeightMaps[next.Item2];

			int result = UnitMatch.PlayMatch(WeightMapYellow, WeightMapRed);
			(double, int) scoreMap1 = Scores.ContainsKey(next.Item1) ? Scores[next.Item1] : (0, 0);
			(double, int) scoreMap2 = Scores.ContainsKey(next.Item2) ? Scores[next.Item2] : (0, 0);

			// Both have played 1 game
			scoreMap1.Item2++;
			scoreMap2.Item2++;

			switch (result)
			{
				case 0:
					// Draw
					scoreMap1.Item1 += 0.5;
					scoreMap2.Item1 += 0.5;
					break;
				case 1:
					// Yellow Wins
					scoreMap1.Item1++;
					break;
				case 2:
					// Red wins
					scoreMap2.Item1++;
					break;
			}

			Scores[next.Item1] = scoreMap1;
			Scores[next.Item2] = scoreMap2;
			Console.WriteLine($"Finished match {thisMatch}");
		}

		private const double parameterDeltaDefault = 0.11;
		private double[] creationParamters;
		private double[] creationParamterDeltas = { parameterDeltaDefault, parameterDeltaDefault, parameterDeltaDefault, parameterDeltaDefault };
		//private int lastChanged = -1;
		private int changingIndex = 0;

		//int allChanges;


		EngineWeightMap TemplateWeightMap;
		private bool generatingNew;

		private bool UpdateCreationParametersAndIndex()
		{
			if (changingIndex == -1 || creationParamters.Length <= changingIndex)
			{
				changingIndex = 0;
				return false;
			}

			if (ESCAPE_VALUE > 0 && creationParamters[changingIndex] + creationParamterDeltas[changingIndex] > ESCAPE_VALUE)
			{
				changingIndex++;
			}

			if (changingIndex == creationParamters.Length)
			{
				AllowGenerateNew = false;
				return true;
			}

			creationParamters[changingIndex] += creationParamterDeltas[changingIndex];
			return false;
		}

		/// <summary>
		/// Creates new WeightMaps and enqueues new matches
		/// </summary>
		/// <returns></returns>
		private async Task NewMatches()
		{
			if(!Run) return;
			if (!AllowGenerateNew)
			{
				// The Queue is Empty (method called) + not allowing new, so lets stop running
				Run = false;
				return;
			}
			if(generatingNew) return;

			if (MAX_GENERATIONS > 0 && EngineWeightMaps.Count >= MAX_GENERATIONS)
			{
				AllowGenerateNew = false;
				return;
			}

			Console.WriteLine("Generating new WeightMap and Matches");

			generatingNew = true;

			int id = EngineWeightMaps.Count;

			if (UpdateCreationParametersAndIndex()) return;

			// Escape Value: When the last value of creationParameterDeltas exceeds the escape value
			//Old: if (ESCAPE_VALUE > 0 && VALUE_OFFSET + creationParamterDeltas[0] * (run + (threshold == (creationParamterDeltas.Length-1) ? 1 : 0)) > ESCAPE_VALUE)
			//if (ESCAPE_VALUE > 0 && VALUE_OFFSET + creationParamterDeltas[0] * (run + 1) > ESCAPE_VALUE)
			/*
			if (ESCAPE_VALUE > 0 && VALUE_OFFSET + creationParamters[...] > ESCAPE_VALUE)
			{
				AllowGenerateNew = false;
				return;
			}
			*/

			EngineWeightMap next = new EngineWeightMap()
			{
				WEIGHT_SCORE_OWN = creationParamters[0],
				WEIGHT_SCORE_OPPONENT = creationParamters[1],
				WEIGHT_HAMMINGDISTANCE_OWN = TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OWN,
				WEIGHT_HAMMINGDISTANCE_OPPONENT = TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OPPONENT,

				WEIGHT_WALL_DISTANCE = creationParamters[2],
				WEIGHT_NEIGHBORS = creationParamters[3],
				WEIGHT_NEIGHBOR_FREE = TemplateWeightMap.WEIGHT_NEIGHBOR_FREE,
				WEIGHT_NEIGHBOR_OWNED = TemplateWeightMap.WEIGHT_NEIGHBOR_OWNED,
				WEIGHT_NEIGHBOR_TAKEN = TemplateWeightMap.WEIGHT_NEIGHBOR_TAKEN,
			};

			//Console.WriteLine($"Generated new Map with id {id}. EngineWeightMaps Size before adding: {EngineWeightMaps.Count}");
			EngineWeightMaps.TryAdd(id, next);
			//Console.WriteLine($"Id >> {id}. EngineWeightMaps Size after adding: {EngineWeightMaps.Count}. Queue Size before: {UpcomingMatches.Count}");
			// Enqueue against itself
			UpcomingMatches.Enqueue((id, id));
			// Enqueue against others, both colors
			for (int i = 0; i < id; i++)
			{
				UpcomingMatches.Enqueue((i, id));
				UpcomingMatches.Enqueue((id, i));
			}
			//Console.WriteLine($"Id >> {id}. Queue Size after: {UpcomingMatches.Count}");
			generatingNew = false;
		}
	}
}


// newMatches()
// To calculate without counting, we'd need last change to go out to infinity and then do lastChanged % size and / size to get respective values.
/* Theoretic Code Doodle for immediate calculation using only the ID
given: int allChanges;

int run = allChanges / creationParamterDeltas.Length;
// Threshold of which variable changed 1 more.
int threshold = allChanges % creationParamterDeltas.Length;
EngineWeightMap next = new EngineWeightMap()
{
	WEIGHT_SCORE_OWN = creationParamterDeltas[0] * (run + 1),			// 0
	WEIGHT_SCORE_OPPONENT = creationParamters[1] * (run + (threshold >= 1 ? 1 : 0)),		// 1
	WEIGHT_HAMMINGDISTANCE_OWN = TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OWN,
	WEIGHT_HAMMINGDISTANCE_OPPONENT = TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OPPONENT,

	WEIGHT_WALL_DISTANCE = creationParamters[2] * (run + (threshold >= 2 ? 1 : 0)),		// 2
	WEIGHT_NEIGHBORS = creationParamters[3] * (run + (threshold == 3 ? 1 : 0)),			// 3
	WEIGHT_NEIGHBOR_FREE = TemplateWeightMap.WEIGHT_NEIGHBOR_FREE,
	WEIGHT_NEIGHBOR_OWNED = TemplateWeightMap.WEIGHT_NEIGHBOR_OWNED,
	WEIGHT_NEIGHBOR_TAKEN = TemplateWeightMap.WEIGHT_NEIGHBOR_TAKEN,
};
allChanges++;
 //*/



/*
 *
 *			EngineWeightMap next = new EngineWeightMap(
			{
				NAME = $"Temp-{id}",

				WEIGHT_SCORE_OWN = creationParamterDeltas[0] * (run + 1),           // 0
				WEIGHT_SCORE_OPPONENT = creationParamters[1] * (run + (threshold >= 1 ? 1 : 0)),        // 1
				WEIGHT_HAMMINGDISTANCE_OWN = TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OWN,
				WEIGHT_HAMMINGDISTANCE_OPPONENT = TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OPPONENT,

				WEIGHT_WALL_DISTANCE = creationParamters[2] * (run + (threshold >= 2 ? 1 : 0)),     // 2
				WEIGHT_NEIGHBORS = creationParamters[3] * (run + (threshold == 3 ? 1 : 0)),         // 3
				WEIGHT_NEIGHBOR_FREE = TemplateWeightMap.WEIGHT_NEIGHBOR_FREE,
				WEIGHT_NEIGHBOR_OWNED = TemplateWeightMap.WEIGHT_NEIGHBOR_OWNED,
				WEIGHT_NEIGHBOR_TAKEN = TemplateWeightMap.WEIGHT_NEIGHBOR_TAKEN,
			};
 *
 */



//lastChanged++;
//if (lastChanged == creationParamters.Length) lastChanged = 0;
//creationParamters[lastChanged] += creationParamterDeltas[lastChanged];

/* Old, lastChanged-based stuff
EngineWeightMap next = new EngineWeightMap()
{
	WEIGHT_SCORE_OWN = creationParamters[0],
	WEIGHT_SCORE_OPPONENT = creationParamters[1],
	WEIGHT_HAMMINGDISTANCE_OWN = TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OWN,
	WEIGHT_HAMMINGDISTANCE_OPPONENT = TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OPPONENT,

	WEIGHT_WALL_DISTANCE = creationParamters[2],
	WEIGHT_NEIGHBORS = creationParamters[3],
	WEIGHT_NEIGHBOR_FREE = TemplateWeightMap.WEIGHT_NEIGHBOR_FREE,
	WEIGHT_NEIGHBOR_OWNED = TemplateWeightMap.WEIGHT_NEIGHBOR_OWNED,
	WEIGHT_NEIGHBOR_TAKEN = TemplateWeightMap.WEIGHT_NEIGHBOR_TAKEN,
};
*/




/*
			//int run = allChanges / creationParamterDeltas.Length;
			// Threshold of which variable changed 1 more.
			//int threshold = allChanges % creationParamterDeltas.Length;

EngineWeightMap next = new EngineWeightMap(
	NAME: $"Temp-{id}",

	WEIGHT_SCORE_OWN: VALUE_OFFSET + creationParamterDeltas[0] * (run + 1), // 0
	WEIGHT_SCORE_OPPONENT: VALUE_OFFSET + creationParamterDeltas[1] * (run + (threshold >= 1 ? 1 : 0)), // 1
	WEIGHT_HAMMINGDISTANCE_OWN: TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OWN,
	WEIGHT_HAMMINGDISTANCE_OPPONENT: TemplateWeightMap.WEIGHT_HAMMINGDISTANCE_OPPONENT,

	WEIGHT_WALL_DISTANCE: VALUE_OFFSET + creationParamterDeltas[2] * (run + (threshold >= 2 ? 1 : 0)), // 2
	WEIGHT_NEIGHBORS: VALUE_OFFSET + creationParamterDeltas[3] * (run + (threshold == 3 ? 1 : 0)), // 3
	WEIGHT_NEIGHBOR_FREE: TemplateWeightMap.WEIGHT_NEIGHBOR_FREE,
	WEIGHT_NEIGHBOR_OWNED: TemplateWeightMap.WEIGHT_NEIGHBOR_OWNED,
	WEIGHT_NEIGHBOR_TAKEN: TemplateWeightMap.WEIGHT_NEIGHBOR_TAKEN
);

allChanges++;
//*/


/*
 * Old:
 *
 * 
					FileMaps.AppendLine($"\n\nMap ID: {map.Key}");
					FileMaps.AppendLine("Map Values:");
					FileMaps.AppendLine($"  >> NAME = {map.Value.NAME}");
					FileMaps.AppendLine($"\n  >> WEIGHT_SCORE_OWN = {map.Value.WEIGHT_SCORE_OWN}");
					FileMaps.AppendLine($"  >> WEIGHT_SCORE_OPPONENT = {map.Value.WEIGHT_SCORE_OPPONENT}");
					FileMaps.AppendLine($"  >> WEIGHT_HAMMINGDISTANCE_OWN = {map.Value.WEIGHT_HAMMINGDISTANCE_OWN}");
					FileMaps.AppendLine($"  >> WEIGHT_HAMMINGDISTANCE_OPPONENT = {map.Value.WEIGHT_HAMMINGDISTANCE_OPPONENT}");
					FileMaps.AppendLine($"\n  >> WEIGHT_WALL_DISTANCE = {map.Value.WEIGHT_WALL_DISTANCE}");
					FileMaps.AppendLine($"  >> WEIGHT_NEIGHBORS = {map.Value.WEIGHT_NEIGHBORS}");
					FileMaps.AppendLine($"  >> WEIGHT_NEIGHBOR_FREE = {map.Value.WEIGHT_NEIGHBOR_FREE}");
					FileMaps.AppendLine($"  >> WEIGHT_NEIGHBOR_OWNED = {map.Value.WEIGHT_NEIGHBOR_OWNED}");
					FileMaps.AppendLine($"  >> WEIGHT_NEIGHBOR_TAKEN = {map.Value.WEIGHT_NEIGHBOR_TAKEN}");
 *
 *
 */