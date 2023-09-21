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
	internal class UnittestComputerSpecific
	{

		#region Data Flush

		private const string DATA_PATH = "E:/Coding/C#/Forms/Stormcloud/Connect4Data/Buffer/";
		const int MAX_FOLDERS = 10000;

		// Should be >= 1, divides # of cores by this to determine how many threads are created, minimum (1) creates as many threads as there are Cores.
		private double TOTAL_CORE_DIVIDER = 1;

		internal string Flush(string temp = "")
		{
			Console.WriteLine("Attempting Data Flush");
			bool done = false;
			int max;
			for (max = 0; max <= MAX_FOLDERS; max++)
			{
				if (Directory.Exists(DATA_PATH + "custom_" + max + temp)) continue;
				done = true;
				break;
			}

			if (!done)
			{
				Console.Error.WriteLine("Error: Max amount of folders reached, could not flush data.");
				return "-";
			}

			string path = DATA_PATH + "custom_" + max + temp + "/";
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

				File.WriteAllText(path + "WEIGHTMAPS.strmcld", FileMaps.ToString());

				StringBuilder FileScores = new StringBuilder();

				foreach (var mapID in Scores.OrderByDescending(x => x.Value.Item1 / (x.Value.Item2 == 0 ? 1 : x.Value.Item2)).Select(x => x.Key))
				{
					FileScores.AppendLine($"\nMap {i++} of {amount}");
					FileScores.AppendLine($" >> Map ID: {mapID}");
					(double, int) score = Scores[mapID];
					Stats.TryGetValue(mapID, out (int, int, int, int, int, int) stats);
					FileScores.AppendLine($" >> Score: {score.Item1} / {score.Item2}");
					FileScores.AppendLine($" >> Wins with Yellow: {stats.Item1}  |  Draws with Yellow: {stats.Item2}  |  Losses with Yellow: {stats.Item3}");
					FileScores.AppendLine($" >> Wins with Red: {stats.Item4}  |  Draws with Red: {stats.Item5}  |  Losses with Red: {stats.Item6}");
				}

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
		/// <summary>
		/// Order: (Wins, Draws, Losses)
		/// </summary>
		ConcurrentDictionary<int, (int, int, int, int, int, int)> Stats = new ConcurrentDictionary<int, (int, int, int, int, int, int)> ();

		public bool AllowGenerateNew { private set; get; }
		public bool Run { private set; get; }

		// The start and workerThreads logic is abstracted from ChatGPT code
		private List<Task> threadPoolTasks;

		private int coreCount;
		public async Task StartAsync(ICollection<EngineWeightMap> maps, EngineWeightMap Template = null)
		{
			if (Run) return;
			Run = true;
			AllowGenerateNew = true;

			// Set Template
			if (Template == null) TemplateWeightMap = EngineWeightMap.HighestEloEngineBoard;
			else TemplateWeightMap = Template;

			GenerateAllMatches(maps);

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

		private int matches = 1;

		private async Task RunNextMatch()
		{
			if (UpcomingMatches.Count == 0)
			{
				Console.WriteLine("Maximum maps reached, Thread Generation is now off and all Games are being processed. Stopping Thread...");
				Run = false;
				return;
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
			Scores.TryGetValue(next.Item1, out (double, int) scoreMap1);
			Scores.TryGetValue(next.Item2, out (double, int) scoreMap2);


			Stats.TryGetValue(next.Item1, out (int, int, int, int, int, int) stats1);	// will be 0,0,0 if does not contain
			Stats.TryGetValue(next.Item2, out (int, int, int, int, int, int) stats2);

			// Both have played 1 game
			scoreMap1.Item2++;
			scoreMap2.Item2++;

			switch (result)
			{
				case 0:
					// Draw
					scoreMap1.Item1 += 0.5;
					scoreMap2.Item1 += 0.5;
					stats1.Item2++;
					stats2.Item5++;
					break;
				case 1:
					// Yellow Wins
					scoreMap1.Item1++;
					stats1.Item1++;
					stats2.Item6++;
					break;
				case 2:
					// Red wins
					scoreMap2.Item1++;
					stats1.Item3++;
					stats2.Item4++;
					break;
			}

			Scores.AddOrUpdate(next.Item1, scoreMap1, (key, oldValue) => scoreMap1);
			Scores.AddOrUpdate(next.Item2, scoreMap2, (key, oldValue) => scoreMap2);
			Stats.AddOrUpdate(next.Item1, stats1, (key, oldValue) => stats1);
			Stats.AddOrUpdate(next.Item2, stats1, (key, oldValue) => stats2);
			Console.WriteLine($"Finished match {thisMatch}");
		}

		EngineWeightMap TemplateWeightMap;

		private void GenerateAllMatches(ICollection<EngineWeightMap> weightmaps)
		{
			foreach (var weightmap in weightmaps)
			{
				int id = EngineWeightMaps.Count;
				EngineWeightMaps.TryAdd(id, weightmap);
				UpcomingMatches.Enqueue((id, id));
				for (int i = 0; i < id; i++)
				{
					UpcomingMatches.Enqueue((id, i));
					UpcomingMatches.Enqueue((i, id));
				}
			}
		}
	}
}