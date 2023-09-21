using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessV1.Stormcloud.Connect4.Weightfinder.PSO_JSON_WRITER;
using UnitMatch = ChessV1.Stormcloud.Connect4.Unittest.UnitMatch;

namespace ChessV1.Stormcloud.Connect4.Weightfinder
{
	internal class ParticleSwarmOptimization
	{
		#region Resume Progress

		// -1 = latest
		internal static ParticleSwarmOptimization ResumePSO(int particleFolderID, bool useCommasInBoolparse = true)
		{
			string path = $"{DATA_PATH}particle_{particleFolderID}";
			if (!Directory.Exists(path))
				throw new FileNotFoundException($"File path {path} could not be found and loaded.", path);
			
			string last = Directory.GetDirectories(path)
				.Where(x => Path.GetFileName(x).StartsWith("phase_"))
				.OrderByDescending(d => Directory.GetCreationTime(d))
				.FirstOrDefault();

			if (last == null)
				throw new FileNotFoundException($"File path {path} contains no folders from which to load data.", path);
			
			if (!last.Contains("phase_")) throw new FileLoadException($"Last folder {last} is not a valid data folder!", last);

			DateTime startTime = Directory.GetCreationTime(Directory.GetDirectories(path)
				.OrderBy(d => Directory.GetCreationTime(d))
				.FirstOrDefault());

			last += '/';

			Console.WriteLine($"Loaded folder {last}");

			var infoFile = File.ReadAllLines(last + "INFO.strmcld");

			double parseWeight(string w)
			{
				if (!w.Contains("="))
				{
					Console.WriteLine($"Tried to parse: \"{w}\" as weight");
					return 0;
				}
				w = w.Split('=')[1].Trim();
				w = w.Replace(",", "");
				if (useCommasInBoolparse) w = w.Replace(".", ",");
				return double.Parse(w);
			}

			// get info.
			// Initialize
			int particles = 10, iterations = 0, rows = 6, cols = 7, engineDepth = 6;
			double globalScore = 0;
			double[] globalWeights = new double[Particle.Dimensions];
			for (int i = 0; i < infoFile.Length; i++)
			{
				string s = infoFile[i];
				if (s.StartsWith("Particles: ")) { particles = int.Parse(s.Substring(11)); continue; }
				if (s.StartsWith("Iterations: ")) { iterations = int.Parse(s.Substring(12)); continue; }
				if (s.StartsWith("Boardsize: "))
				{
					var size = s.Split(' ');
					cols = size[1][0] - '0';
					rows = size[3][0] - '0';
					continue;
				}
				if (s.StartsWith("Engine Depth: ")) { engineDepth = int.Parse(s.Substring(14)); continue; }
				if (s.StartsWith("Best Global Particle Score Internally: ")) { globalScore = double.Parse(s.Substring(39)); continue; }

				if (s == "{")
				{
					int w1 = i + 3;
					for (int i2 = 0; i2 < Particle.Dimensions; i2++)
					{
						int s_i = i2;
						if (i2 >= 4) s_i++;
						if (i2 >= 6) s_i++;
						globalWeights[i2] = parseWeight(infoFile[s_i + w1]);
					}
					break;
				}
			}

			string folderLast = last.Substring(path.Length);
			int iterationsCurrentPhase = int.Parse(folderLast.Split('_')[3].Split(' ')[0]);
			int phase = folderLast.Split('_')[1][0] - '0';


			var infoParticles = File.ReadAllLines(last + "PARTICLES.strmcld");
			Particle[] Particles = new Particle[particles];

			Particle ParseParticle(int lineID, int id)
			{
				double scoreCurrent, scoreBest;
				double[] weightsCurrent = new double[Particle.Dimensions],
					weightsBest = new double[Particle.Dimensions];

				scoreCurrent = double.Parse(infoParticles[lineID + 1].Split(' ')[3]);
				int i;
				for (i = 0; i < Particle.Dimensions; i++)
				{
					int line = lineID + i + 6;
					if (i >= 4) line++;
					if (i >= 6) line++;
					weightsCurrent[i] = parseWeight(infoParticles[line]);
				}

				scoreBest = double.Parse(infoParticles[lineID + 21].Split(' ')[3]);
				for (i = 0; i < Particle.Dimensions; i++)
				{
					int line = lineID + i + 26;
					if (i >= 4) line++;
					if (i >= 6) line++;
					weightsBest[i] = parseWeight(infoParticles[line]);
				}

				Particle p = new Particle(id)
				{
					BestPosition = weightsBest,
					BestPositionScore = scoreBest,
					CurrentPosition = weightsCurrent,
					CurrentPositionScore = scoreCurrent,
				};
				// Debug: Console.WriteLine($"Created new Particle ID {id} with Best Score {scoreBest} and current {scoreCurrent}");
				// Mutate to set velocity
				p.Iterate(scoreCurrent);
				p.Mutate();
				return p;
			}

			for (int i = 0; i < infoParticles.Length; i++)
			{
				string s = infoParticles[i];
				if(!s.StartsWith("Particle ID: ")) continue;
				int id = int.Parse(s.Split(' ')[2]);
				Particles[id] = ParseParticle(i, id);
			}

			ParticleSwarmOptimization pso = new ParticleSwarmOptimization(
				Particles: Particles, Rows: rows, Columns: cols, EngineDepth: engineDepth,
				Iterations: iterations, IterationsCurPhase: iterationsCurrentPhase, Phase: phase,
				GlobalScore: globalScore, GlobalWeights: globalWeights, FLUSH_FOLDER: path,
				StartTime: startTime);

			return pso;
		}

		#endregion

		//public const double OUT_OF_BOUNDS_PENALTY_SCALAR = 0.9;
		public const double OUT_OF_BOUNDS_BOUNCE_STRENGTH = 0.25; //0.2;
		public const double GAMES_PER_MINUTE = 139.1;// Old Eval, D6, 12Threads, 25 Particles, etc: 62.58;

		// Old values:
		//private const double VALUE_WIN_YELLOW = 2.5, VALUE_DRAW_YELLOW = 0.5, VALUE_LOSS_YELLOW = -3,
		//	VALUE_WIN_RED = 3, VALUE_DRAW_RED = 0.6, VALUE_LOSS_RED = -1.8;
		// More... extreme values:
		private const double VALUE_WIN_YELLOW = 7.5, VALUE_DRAW_YELLOW = -1.4, VALUE_LOSS_YELLOW = -25,
			VALUE_WIN_RED = 6, VALUE_DRAW_RED = -1.6, VALUE_LOSS_RED = -2.8;

		public bool SilentThreads;

		private double MAX_SCORE;

		private int Rows, Columns;
		private int EngineDepth;

		private Particle[] Particles;

		private DateTime start;
		private DateTime phasestart;

		private string FLUSH_FOLDER = "C:/";

		public ParticleSwarmOptimization(int Particles, int Rows, int Columns, int EngineDepth)
		{
			this.Rows = Rows;
			this.Columns = Columns;
			SetMasks();
			UnitMatch.Setup(Rows, Columns, EngineDepth);

			Particle.Init();
			this.Particles = new Particle[Particles];
			int gamesPerIt = Particles * Particles;
			// We want lossy conversion because of that game
			this.MAX_SCORE = gamesPerIt / 2 * VALUE_WIN_YELLOW + gamesPerIt / 2 * VALUE_WIN_RED + (((gamesPerIt & 1) == 1) ? VALUE_WIN_YELLOW : 0);
			for (int i = 0; i < Particles; i++)
			{
				this.Particles[i] = new Particle(i);
			}
			// Iterate
			start = DateTime.Now;
			Console.WriteLine($"Started at {start}");
			Console.WriteLine($"Boardsize: {Columns} x {Rows}");
			Console.WriteLine($"Particles: {Particles}");
			Console.WriteLine($"Games per Iteration: {gamesPerIt}");
			Console.WriteLine($"Games per Minute: {GAMES_PER_MINUTE}");
			Console.WriteLine($"Estimated Iteration Time: {gamesPerIt / GAMES_PER_MINUTE}");
			Console.WriteLine($"Max Iterations: {PhaseConditions.MAX_ITERATIONS_TOTAL}");
			Console.WriteLine($"MAX_SCORE: {MAX_SCORE}");
			Console.WriteLine($"SPEED: {PhaseConditions.SPEED}");

			if (EngineDepth < 6) SilentThreads = true;	// Too many logs

			double gameMins = (PhaseConditions.MAX_ITERATIONS_TOTAL * gamesPerIt) / GAMES_PER_MINUTE;
			if(gameMins <= PhaseConditions.MAX_MINUTES_TOTAL) Console.WriteLine($"ETA: {new TimeSpan((int)(gameMins / 60), (int)(gameMins % 60), 0)} (Maxed out Iterations ({PhaseConditions.MAX_ITERATIONS_TOTAL}))");
			else Console.WriteLine($"ETA: {new TimeSpan(PhaseConditions.MAX_MINUTES_TOTAL / 60, PhaseConditions.MAX_MINUTES_TOTAL % 60, 0)} (Maxed out Time ({PhaseConditions.MAX_MINUTES_TOTAL} mins))");
			
			TotalIterations = 0;
			coreCount = (int)(Environment.ProcessorCount / TOTAL_CORE_DIVIDER);
			Console.WriteLine($"Core count: {coreCount}");

			bool done = false;
			int max;
			for (max = 0; max <= MAX_FOLDERS; max++)
			{
				if (Directory.Exists(DATA_PATH + "particle_" + max)) continue;
				done = true;
				break;
			}

			if (!done)
			{
				Console.Error.WriteLine("Error: Max amount of folders reached, could not flush data.");
				return;
			}

			FLUSH_FOLDER = DATA_PATH + "particle_" + max;

			PhaseExploration();
			DoIteration();
		}

		internal ParticleSwarmOptimization(Particle[] Particles, int Rows, int Columns, int EngineDepth,
			int Iterations, int IterationsCurPhase, int Phase, double GlobalScore, double[] GlobalWeights,
			string FLUSH_FOLDER, DateTime StartTime)
		{
			this.Particles = Particles;
			this.Rows = Rows;
			this.Columns = Columns;
			this.EngineDepth = EngineDepth;
			this.FLUSH_FOLDER = FLUSH_FOLDER;

			SetMasks();
			UnitMatch.Setup(Rows, Columns, EngineDepth);
			Particle.Init();

			Particle.GlobalBestPosition = GlobalWeights;
			Particle.GlobalBestPositionScore = GlobalScore;

			int gamesPerIt = Particles.Length * Particles.Length;
			this.MAX_SCORE = gamesPerIt / 2 * VALUE_WIN_YELLOW + gamesPerIt / 2 * VALUE_WIN_RED + (((gamesPerIt & 1) == 1) ? VALUE_WIN_YELLOW : 0);
			start = StartTime;
			Console.WriteLine($"Started at {StartTime}");
			Console.WriteLine($"Resumed at {DateTime.Now}");
			Console.WriteLine($"Best Score so far: {GlobalScore}");
			Console.WriteLine($"Boardsize: {Columns} x {Rows}");
			Console.WriteLine($"Particles: {Particles.Length}");
			Console.WriteLine($"Games per Iteration: {gamesPerIt}");
			Console.WriteLine($"Games per Minute: {GAMES_PER_MINUTE}");
			Console.WriteLine($"Estimated Iteration Time: {gamesPerIt / GAMES_PER_MINUTE}");
			Console.WriteLine($"Max Iterations: {PhaseConditions.MAX_ITERATIONS_TOTAL}");
			Console.WriteLine($"MAX_SCORE: {MAX_SCORE}");
			Console.WriteLine($"SPEED: {PhaseConditions.SPEED}");

			if (EngineDepth < 6) SilentThreads = true;  // Too many logs

			double gameMins = (PhaseConditions.MAX_ITERATIONS_TOTAL * gamesPerIt) / GAMES_PER_MINUTE;
			if (gameMins <= PhaseConditions.MAX_MINUTES_TOTAL) Console.WriteLine($"ETA: {new TimeSpan((int)(gameMins / 60), (int)(gameMins % 60), 0)} (Maxed out Iterations ({PhaseConditions.MAX_ITERATIONS_TOTAL}))");
			else Console.WriteLine($"ETA: {new TimeSpan(PhaseConditions.MAX_MINUTES_TOTAL / 60, PhaseConditions.MAX_MINUTES_TOTAL % 60, 0)} (Maxed out Time ({PhaseConditions.MAX_MINUTES_TOTAL} mins))");

			coreCount = (int)(Environment.ProcessorCount / TOTAL_CORE_DIVIDER);
			Console.WriteLine($"Core count: {coreCount}");

			switch (Phase)
			{
				case 2: PhaseSocialization(); break;
				case 3: PhaseConvergence(); break;
				default: PhaseExploration(); break;
			}
			this.TotalIterations = Iterations;
			this.PhaseIterations = IterationsCurPhase;

			DoIteration();
		}

		private void DoIteration()
		{
			do
			{
				Iteration().Wait();
				Flush($"/phase_{Phase}_iteration_{PhaseIterations} ({TotalIterations})");
				// Mutate. If the program is over, it has exited by now and won't mutate, just like it shouldn't
				// So, we only mutate after flushing the data and if there is a next iteration.
				foreach (var particle in Particles) particle.Mutate();
			} while (ContinueLoop());
		}

		bool ContinueLoop()
		{
			return true;	// Todo this -> Handled
		}

		private int TotalIterations, PhaseIterations;

		private async Task Iteration()
		{
			TotalIterations++;
			PhaseIterations++;
			Console.WriteLine($"Beginning Iteration {TotalIterations} | Total Time spent: {DateTime.Now - start} | Time spent in Phase: {DateTime.Now - phasestart}");
			await PlayAllMatches();
			for (int i = 0; i < this.Particles.Length; i++)
			{
				if(!Scores.ContainsKey(i)) continue;
				(double, double) totalScore = Scores[i];
				double score = totalScore.Item1 * totalScore.Item2 * 0.02;
				if (!SilentThreads)
					Console.WriteLine($"{i}. >> Particle ID {Particles[i].ID} | totalScore: ({totalScore.Item1} | {totalScore.Item2}) = {score}");
				Particles[i].Iterate(score);
			}
			// Potentially update phase or similar
			if (Phase == 1)
			{
				if (Particle.GlobalBestPositionScore < PhaseConditions.MIN_SCORE_PHASE1)
				{
					if ((DateTime.Now - phasestart).TotalMinutes < PhaseConditions.MAX_MINUTES_PHASE1 * PhaseConditions.MIN_SCORE_ESCAPE_MULTIPLIER &&
					    this.PhaseIterations < PhaseConditions.MAX_ITERATIONS_PHASE1 * PhaseConditions.MIN_SCORE_ESCAPE_MULTIPLIER)
					{
						// Remain in phase a bit longer
						return;
					}
				}
				if ((DateTime.Now - phasestart).TotalMinutes >= PhaseConditions.MAX_MINUTES_PHASE1 ||
				   this.PhaseIterations >= PhaseConditions.MAX_ITERATIONS_PHASE1) PhaseSocialization();
				return;
			}
			if (Phase == 2)
			{
				if (Particle.GlobalBestPositionScore < PhaseConditions.MIN_SCORE_PHASE2)
				{
					if ((DateTime.Now - phasestart).TotalMinutes < PhaseConditions.MAX_MINUTES_PHASE2 * PhaseConditions.MIN_SCORE_ESCAPE_MULTIPLIER &&
					    this.PhaseIterations < PhaseConditions.MAX_ITERATIONS_PHASE2 * PhaseConditions.MIN_SCORE_ESCAPE_MULTIPLIER)
					{
						// Remain in phase a bit longer
						return;
					}
				}
				if ((DateTime.Now - phasestart).TotalMinutes >= PhaseConditions.MAX_MINUTES_PHASE2 ||
				   this.PhaseIterations >= PhaseConditions.MAX_ITERATIONS_PHASE3) PhaseConvergence();
				return;
			}
			if (Phase == 3)
			{
				if (Particle.GlobalBestPositionScore < PhaseConditions.MIN_SCORE_PHASE3)
				{
					if ((DateTime.Now - phasestart).TotalMinutes < PhaseConditions.MAX_MINUTES_PHASE3 * PhaseConditions.MIN_SCORE_ESCAPE_MULTIPLIER &&
						 this.PhaseIterations < PhaseConditions.MAX_ITERATIONS_PHASE3 * PhaseConditions.MIN_SCORE_ESCAPE_MULTIPLIER)
					{
						// Release from the pain
						return;
					}
				}
				if ((DateTime.Now - phasestart).TotalMinutes >= PhaseConditions.MAX_MINUTES_PHASE3 ||
				    this.PhaseIterations >= PhaseConditions.MAX_ITERATIONS_PHASE3)
				{
					Flush();
					PSOExportParser.ConvertToJSONs(FLUSH_FOLDER);
					PSOExportParser.ConvertToJSONSingleFile(FLUSH_FOLDER);
					PSOExportParser.ConvertToJSONSingleFileInvertedOrder(FLUSH_FOLDER);
					Console.WriteLine("Exiting...");
					Environment.Exit(0);
				}
			}
		}

		#region Gameplay

		ConcurrentDictionary<int, (double, double)> Scores = new ConcurrentDictionary<int, (double, double)>();
		ConcurrentDictionary<int, (double, int)> MatchPoints = new ConcurrentDictionary<int, (double, int)>();
		ConcurrentDictionary<int, (int, int, int)> MatchPointStats = new ConcurrentDictionary<int, (int, int, int)>();
		ConcurrentQueue<(int, int)> Matches = new ConcurrentQueue<(int, int)>();

		private void EnqueueMatches()
		{
			Scores.Clear();
			MatchPoints.Clear();
			MatchPointStats.Clear();
			matches = 1;
			helper = 1;
			for (int id1 = 0; id1 < Particles.Length; id1++)
			{
				Matches.Enqueue((id1, id1));
				for (int id2 = 0; id2 < Particles.Length; id2++)
				{
					if(id1 == id2) continue;
					Matches.Enqueue((id1, id2));
				}
			}
		}

		private void Load(Particle particle, ref EngineWeightMap map)
		{
			map.NAME = $"Particle-{particle.ID}";
			map.WEIGHT_SCORE_OWN = particle.Position[0];
			map.WEIGHT_SCORE_OPPONENT = particle.Position[1];
			map.WEIGHT_HAMMINGDISTANCE_OWN = particle.Position[2];
			map.WEIGHT_HAMMINGDISTANCE_OPPONENT = particle.Position[3];

			map.WEIGHT_WALL_DISTANCE = particle.Position[4];
			map.WEIGHT_NEIGHBORS = particle.Position[5];
			map.WEIGHT_NEIGHBOR_FREE = particle.Position[6];
			map.WEIGHT_NEIGHBOR_OWNED = particle.Position[7];
			map.WEIGHT_NEIGHBOR_TAKEN = particle.Position[8];
		}

		// The start and workerThreads logic is abstracted from ChatGPT code
		private List<Task> threadPoolTasks;

		private const double TOTAL_CORE_DIVIDER = 1;  // On my pc with 16 Cores: 1.1 = 14 Cores, 1.15 = 13 Cores, 1.3 = 12 Cores
		private int coreCount;
		public async Task PlayAllMatches()
		{
			// Create a task for each core in the system.
			threadPoolTasks = new List<Task>(coreCount);

			EnqueueMatches();

			for (int i = 0; i < coreCount; i++)
			{
				var task = Task.Run(() => RunMatchHandler());
				threadPoolTasks.Add(task);
			}

			// No need for Run = false since the Tasks only end when Run = false
			await Task.WhenAll(threadPoolTasks);
			Console.WriteLine("All Finished.");
		}

		private int helper;
		private void RunMatchHandler()
		{
			EngineWeightMap Yellow = new EngineWeightMap(), Red = new EngineWeightMap();
			Connect4Engine EngineYellow = new Connect4Engine(Rows, Columns, EngineDepth, MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2, Silenced: true),
				EngineRed = new Connect4Engine(Rows, Columns, EngineDepth, MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2, Silenced: true);
			while (!Matches.IsEmpty)
			{
				// Don't use excessively many WeightMaps but still two per thread. Perhaps globalize these in an array.
				RunNextMatch(ref Yellow, ref Red, ref EngineYellow, ref EngineRed);
			}
			Console.WriteLine($"Ending RunMatchHandler ({helper++}/{coreCount})");
		}

		private int matches;
		private void RunNextMatch(ref EngineWeightMap Yellow, ref EngineWeightMap Red, ref Connect4Engine EngineYellow, ref Connect4Engine EngineRed)
		{
			try
			{
				if (Matches.Count == 0)
				{
					Console.WriteLine(
						"Maximum maps reached, Thread Generation is now off and all Games are being processed. Stopping Thread...");
					return;
				}

				if (!Matches.TryDequeue(out (int, int) next))
				{
					Console.Error.WriteLine($"Couldn't get match {matches}(?), Queue size: {Matches.Count}");
					return; // None gotten
				}

				int thisMatch = matches++;

				Load(Particles[next.Item1], ref Yellow);
				Load(Particles[next.Item2], ref Red);

				EngineYellow.WeightMap = Yellow;
				EngineRed.WeightMap = Red;

				int result = UnitMatch.PlayMatch(EngineYellow, EngineRed);

				// Default is (0, 0), if that changes the add line scoreMap2 = scoreMap2ValueExport ?? (0, 0)
				Scores.TryGetValue(next.Item1, out (double, double) scoreMap1);
				Scores.TryGetValue(next.Item2, out (double, double) scoreMap2);
				MatchPoints.TryGetValue(next.Item1, out (double, int) matchPoints1);
				MatchPoints.TryGetValue(next.Item2, out (double, int) matchPoints2);
				MatchPointStats.TryGetValue(next.Item1, out (int, int, int) stats1);
				MatchPointStats.TryGetValue(next.Item2, out (int, int, int) stats2);

				// Both have played 1 game
				// Add potential value
				scoreMap1.Item2 += VALUE_WIN_YELLOW;
				scoreMap2.Item2 += VALUE_WIN_RED;
				matchPoints1.Item2++;
				matchPoints2.Item2++;

				switch (result)
				{
					case 0:
						// Draw
						matchPoints1.Item1 += 0.5;
						matchPoints2.Item1 += 0.5;
						scoreMap1.Item1 += VALUE_DRAW_YELLOW;
						scoreMap2.Item1 += VALUE_DRAW_RED;
						stats1.Item2++;
						stats2.Item2++;
						break;
					case 1:
						// Yellow Wins
						matchPoints1.Item1++;
						scoreMap1.Item1 += VALUE_WIN_YELLOW;
						scoreMap2.Item1 += VALUE_LOSS_RED;
						stats1.Item1++;
						stats2.Item3++;
						break;
					case 2:
						// Red wins
						matchPoints2.Item1++;
						scoreMap1.Item1 += VALUE_LOSS_YELLOW;
						scoreMap2.Item1 += VALUE_WIN_RED;
						stats1.Item3++;
						stats2.Item1++;
						break;
				}

				// Just add / replace the old values
				Scores.AddOrUpdate(next.Item1, scoreMap1, (key, oldValue) => scoreMap1);
				Scores.AddOrUpdate(next.Item2, scoreMap2, (key, oldValue) => scoreMap2);
				MatchPoints.AddOrUpdate(next.Item1, matchPoints1, (key, oldValue) => matchPoints1);
				MatchPoints.AddOrUpdate(next.Item2, matchPoints2, (key, oldValue) => matchPoints2);
				MatchPointStats.AddOrUpdate(next.Item1, stats1, (key, oldValue) => stats1);
				MatchPointStats.AddOrUpdate(next.Item2, stats2, (key, oldValue) => stats2);
				if(!SilentThreads) Console.WriteLine($"Finished match {thisMatch}");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error:\nSource: {ex.Source}\nMessage:{ex.Message}\nStacktrace: {ex.StackTrace}");
				throw ex;
			}
		}

		#endregion

		#region Flush

		internal const string DATA_PATH = "E:/Coding/C#/Forms/Stormcloud/Connect4Data/Buffer2/";
		const int MAX_FOLDERS = 10000;
		
		internal string Flush(string temp = "")
		{
			Console.WriteLine("Attempting Data Flush");
			bool done = false;

			string path = FLUSH_FOLDER + temp + "/";
			Directory.CreateDirectory(path);

			// Now, gather the data and actually write it.
			// 3 Files corresponding to maps, mapscores and info

			int amount = Particles.Length;
			DateTime end = DateTime.Now;

			try
			{
				StringBuilder FileInfo = new StringBuilder();
				FileInfo.AppendLine("File info:");
				FileInfo.AppendLine($"Boardsize: {Columns} x {Rows}");
				FileInfo.AppendLine($"Particles: {Particles.Length}");
				FileInfo.AppendLine($"Iterations: {TotalIterations}");
				FileInfo.AppendLine($"Games Played Per Iteration: {Particles.Length * Particles.Length}");
				FileInfo.AppendLine($"Games Played: {Particles.Length * Particles.Length * TotalIterations}");
				FileInfo.AppendLine($"Games Played Per Minute: {(Particles.Length * Particles.Length * TotalIterations) / (end - start).TotalMinutes}");
				FileInfo.AppendLine($"MAX_SCORE: {MAX_SCORE}");
				FileInfo.AppendLine($"Cores: {coreCount}");
				FileInfo.AppendLine($"Start Time: {start}");
				FileInfo.AppendLine($"End Time: {end}");
				FileInfo.AppendLine($"Total Time: {end - start}");
				FileInfo.AppendLine($"Boardsize: {UnitMatch.Boardsize}");
				FileInfo.AppendLine($"Engine Depth: {UnitMatch._EngineDepth}");

				FileInfo.AppendLine("\nWin Values:");
				FileInfo.AppendLine($"VALUE_WIN_YELLOW: {VALUE_WIN_YELLOW}");
				FileInfo.AppendLine($"VALUE_DRAW_YELLOW: {VALUE_DRAW_YELLOW}");
				FileInfo.AppendLine($"VALUE_LOSS_YELLOW: {VALUE_LOSS_YELLOW}");
				FileInfo.AppendLine($"VALUE_WIN_RED: {VALUE_WIN_RED}");
				FileInfo.AppendLine($"VALUE_DRAW_RED: {VALUE_DRAW_RED}");
				FileInfo.AppendLine($"VALUE_LOSS_RED: {VALUE_LOSS_RED}");

				FileInfo.AppendLine("\n\nBest Global Particle:");
				FileInfo.AppendLine($"\nBest Global Particle Score Internally: {Particle.GlobalBestPositionScore}");
				FileInfo.AppendLine("{");
				FileInfo.AppendLine("	NAME = \"Particle-GLOBAL-BEST\",");
				FileInfo.AppendLine($"\n	WEIGHT_SCORE_OWN = {Particle.GlobalBestPosition[0].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_SCORE_OPPONENT = {Particle.GlobalBestPosition[1].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OWN = {Particle.GlobalBestPosition[2].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OPPONENT = {Particle.GlobalBestPosition[3].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"\n	WEIGHT_FORK_HAMMINGDISTANCE_S = {Particle.GlobalBestPosition[4].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_FORK_HAMMINGDISTANCE_L = {Particle.GlobalBestPosition[5].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"\n	WEIGHT_WALL_DISTANCE = {Particle.GlobalBestPosition[6].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_NEIGHBORS = {Particle.GlobalBestPosition[7].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_NEIGHBOR_FREE = {Particle.GlobalBestPosition[8].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_NEIGHBOR_OWNED = {Particle.GlobalBestPosition[9].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_NEIGHBOR_TAKEN = {Particle.GlobalBestPosition[10].ToString().Replace(",", ".")}");
				FileInfo.AppendLine("};");

				File.WriteAllText(path + "INFO.strmcld", FileInfo.ToString());

				StringBuilder FileMaps = new StringBuilder();

				FileMaps.AppendLine($"\nParticles: {amount}");
				foreach (var particle in Particles)
				{
					FileMaps.AppendLine($"\n\nParticle ID: {particle.ID}");
					FileMaps.AppendLine($"Current Position Score: {particle.CurrentPositionScore}");
					FileMaps.AppendLine("Current Position Values:");
					FileMaps.AppendLine("{");
					FileMaps.AppendLine($"	NAME = \"Particle-{particle.ID} - {particle.CurrentPositionScore}\",");
					FileMaps.AppendLine($"\n	WEIGHT_SCORE_OWN = {particle.CurrentPosition[0].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_SCORE_OPPONENT = {particle.CurrentPosition[1].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OWN = {particle.CurrentPosition[2].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OPPONENT = {particle.CurrentPosition[3].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"\n	WEIGHT_FORK_HAMMINGDISTANCE_S = {particle.CurrentPosition[4].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_FORK_HAMMINGDISTANCE_L = {particle.CurrentPosition[5].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"\n	WEIGHT_WALL_DISTANCE = {particle.CurrentPosition[6].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBORS = {particle.CurrentPosition[7].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_FREE = {particle.CurrentPosition[8].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_OWNED = {particle.CurrentPosition[9].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_TAKEN = {particle.CurrentPosition[10].ToString().Replace(",", ".")}");
					FileMaps.AppendLine("};");
					FileMaps.AppendLine($"\nBest Position Score: {particle.BestPositionScore}");
					FileMaps.AppendLine("Best Position Values:");
					FileMaps.AppendLine("{");
					FileMaps.AppendLine($"	NAME = \"Particle-{particle.ID} - {particle.BestPositionScore}\",");
					FileMaps.AppendLine($"\n	WEIGHT_SCORE_OWN = {particle.BestPosition[0].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_SCORE_OPPONENT = {particle.BestPosition[1].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OWN = {particle.BestPosition[2].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OPPONENT = {particle.BestPosition[3].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"\n	WEIGHT_FORK_HAMMINGDISTANCE_S = {particle.BestPosition[4].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_FORK_HAMMINGDISTANCE_L = {particle.BestPosition[5].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"\n	WEIGHT_WALL_DISTANCE = {particle.BestPosition[6].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBORS = {particle.BestPosition[7].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_FREE = {particle.BestPosition[8].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_OWNED = {particle.BestPosition[9].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_TAKEN = {particle.BestPosition[10].ToString().Replace(",", ".")}");
					FileMaps.AppendLine("};");
				}

				File.WriteAllText(path + "PARTICLES.strmcld", FileMaps.ToString());

				StringBuilder FileScores_Current = new StringBuilder();
				int i = 1;
				foreach (var particle in Particles.OrderByDescending(x => x.CurrentPositionScore))
				{
					FileScores_Current.AppendLine($"\nParticle {i++} of {amount}");
					FileScores_Current.AppendLine($" >> Particle ID: {particle.ID}");
					FileScores_Current.AppendLine($" >> Current Score: {particle.CurrentPositionScore}   ({particle.BestPositionScore} Best)");
					MatchPoints.TryGetValue(particle.ID, out (double, int) matchPoints);
					MatchPointStats.TryGetValue(particle.ID, out (int, int, int) stats);
					FileScores_Current.AppendLine($" >> Game Score: {matchPoints.Item1}/{matchPoints.Item2}");
					FileScores_Current.AppendLine($" >> Current Stats: Wins: {stats.Item1}  |  Draws: {stats.Item2}  |  Losses: {stats.Item3}");
				}
				File.WriteAllText(path + "PARTICLES_CURRENT_SCORES.strmcld", FileScores_Current.ToString());


				StringBuilder FileScores_Best = new StringBuilder();
				i = 1;
				foreach (var particle in Particles.OrderByDescending(x => x.BestPositionScore))
				{
					FileScores_Best.AppendLine($"\nParticle {i++} of {amount}");
					FileScores_Best.AppendLine($" >> Particle ID: {particle.ID}");
					FileScores_Best.AppendLine($" >> Best Score: {particle.BestPositionScore}   ({particle.CurrentPositionScore} Current)");
					MatchPoints.TryGetValue(particle.ID, out (double, int) matchPoints);
					MatchPointStats.TryGetValue(particle.ID, out (int, int, int) stats);
					FileScores_Best.AppendLine($" >> Game Score: {matchPoints.Item1}/{matchPoints.Item2}");
					FileScores_Best.AppendLine($" >> Current Stats: Wins: {stats.Item1}  |  Draws: {stats.Item2}  |  Losses: {stats.Item3}");
				}
				File.WriteAllText(path + "PARTICLES_BEST_SCORES.strmcld", FileScores_Best.ToString());

				Console.WriteLine($"Data flushed to: {path}");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error:\nSource: {ex.Source}\nMessage:{ex.Message}\nStacktrace: {ex.StackTrace}");
			}

			return path;
		}

		#endregion

		#region MASKS

		private const short MASK_ROW = 0x000F; // 1111 | just shift around as needed
		private long MASK_COL;
		private long MASK_DIAG1;
		private long MASK_DIAG2;

		// Masks need to be set because anything transcending own rows need to know how wide a row is
		void SetMasks()
		{
			int cols = Columns; // Leave space for the 1 in the column
			// Basically: value is 1, shift so much across << that new line, then add 1 again, shift until 4
			MASK_COL = (((((1 << cols) | 1) << cols) | 1) << cols) | 1;

			// Now do the same for +/- 1 each time for the diagnonal
			cols--; // Shift 1 less for the diagonal going upwards L-R. Only thing is this will need leading 0s, but it's got that. Just don't forget
			// We skipped 3 0s that are quite essential here (I think), so let's add them to the back
			MASK_DIAG1 = ((((((1 << cols) | 1) << cols) | 1) << cols) | 1) << 3;

			cols += 2;
			// Now to +1, since Columns is +1, we can just use that instead
			MASK_DIAG2 = (((((1 << cols) | 1) << cols) | 1) << cols) | 1;
		}

		#endregion

		/*	My Gameplan:
		 *
		 * Stage 1: Early Exploration

			High cognitive coefficient: Encourages particles to trust their own paths, promoting a wide search across the search space.
			Low-Moderate social coefficient: Ensures some communication between particles, but doesn't let global or local best dominate the search direction too early.
			Moderate-Slightly High Inertia: Gives particles some momentum to move around and not get stuck.
		
		 * Stage 2: Socialization

			Moderate cognitive coefficient: Still values individual exploration, but not as dominantly as the early phase.
			Moderate-Slightly higher social coefficient: Promotes the idea that now that there's a good understanding of the landscape, particles can learn more from each other.
			Moderate Inertia: Keeps a balance in the particles' movements.
			
		 * Stage 3: Convergence

			Low cognitive coefficient: Less emphasis on individual discoveries, as by now the algorithm assumes particles have explored significant portions of the search space.
			High social Coefficient: Maximizes learning from the global (or local) best positions.
			Low Inertia: Reduces the overall movement, allowing particles to settle into potential optima.
		 *
		 *
		 * Boundaries:
		 * Inertia: [0.4, 0.9]
		 * Cognitive Coefficient PhiP (Phi Personal): [1, 3]
		 * Social Coefficient PhiG (Phi Global): [1, 3]
		 */

		private byte Phase;

		void PhaseExploration()
		{
			PhaseIterations = 0;
			phasestart = DateTime.Now;
			Console.WriteLine($"Switching to Phase 1: Exploration | Best Current Score: {Particle.GlobalBestPositionScore}");
			Particle.CognitiveCoefficient = 2.77;	// [1, 3]
			Particle.SocialCoefficient = 1.25;		// [1, 3]
			Particle.Inertia = 0.82;        // [0.4, 0.9]
			Phase = 1;
		}

		void PhaseSocialization()
		{
			PhaseIterations = 0;
			phasestart = DateTime.Now;
			Console.WriteLine($"Switching to Phase 2: Socialization | Best Current Score: {Particle.GlobalBestPositionScore}");
			Particle.CognitiveCoefficient = 1.95;   // [1, 3]
			Particle.SocialCoefficient = 2.07;      // [1, 3]
			Particle.Inertia = 0.64;        // [0.4, 0.9]
			Phase = 2;
		}

		void PhaseConvergence()
		{
			PhaseIterations = 0;
			phasestart = DateTime.Now;
			Console.WriteLine($"Switching to Phase 3: Convergence | Best Current Score: {Particle.GlobalBestPositionScore}");
			Particle.CognitiveCoefficient = 1.15;   // [1, 3]
			Particle.SocialCoefficient = 2.73;      // [1, 3]
			Particle.Inertia = 0.41;        // [0.4, 0.9]
			Phase = 3;
		}
	}
}
