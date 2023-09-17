using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitMatch = ChessV1.Stormcloud.Connect4.Unittest.UnitMatch;

namespace ChessV1.Stormcloud.Connect4.Weightfinder
{
	internal class ParticleSwarmOptimization
	{
		//public const double OUT_OF_BOUNDS_PENALTY_SCALAR = 0.9;
		public const double OUT_OF_BOUNDS_BOUNCE_STRENGTH = 0.15; //0.2;
		public const double GAMES_PER_MINUTE = 62.58;

		private int Rows, Columns;
		private int EngineDepth;

		private List<Particle> Particles;

		private DateTime start;
		private DateTime phasestart;

		public ParticleSwarmOptimization(int Particles, int Rows, int Columns, int EngineDepth)
		{
			this.Rows = Rows;
			this.Columns = Columns;
			SetMasks();
			UnitMatch.Setup(Rows, Columns, EngineDepth);

			Particle.Init();
			this.Particles = new List<Particle>();
			for (int i = 0; i < Particles; i++)
			{
				this.Particles.Add(new Particle(i));
			}
			// Iterate
			start = DateTime.Now;
			Console.WriteLine($"Started at {start}");
			Console.WriteLine($"Boardsize: {Columns} x {Rows}");
			Console.WriteLine($"Particles: {Particles}");
			Console.WriteLine($"Games per Iteration: {Particles * Particles}");
			Console.WriteLine($"Games per Minute: {GAMES_PER_MINUTE}");
			Console.WriteLine($"Estimated Iteration Time: {Particles * Particles / GAMES_PER_MINUTE}");
			Console.WriteLine($"Max Iterations: {PhaseConditions.MAX_ITERATIONS_TOTAL}");
			Console.WriteLine($"SPEED: {PhaseConditions.SPEED}");

			double gameMins = (PhaseConditions.MAX_ITERATIONS_TOTAL * Particles * Particles) / GAMES_PER_MINUTE;
			if(gameMins <= PhaseConditions.MAX_MINUTES_TOTAL) Console.WriteLine($"ETA: {new TimeSpan((int)(gameMins / 60), (int)(gameMins % 60), 0)} (Maxed out Iterations ({PhaseConditions.MAX_ITERATIONS_TOTAL}))");
			else Console.WriteLine($"ETA: {new TimeSpan(PhaseConditions.MAX_MINUTES_TOTAL / 60, PhaseConditions.MAX_MINUTES_TOTAL % 60, 0)} (Maxed out Time ({PhaseConditions.MAX_MINUTES_TOTAL} mins))");
			
			TotalIterations = 0;
			coreCount = (int)(Environment.ProcessorCount / TOTAL_CORE_DIVIDER);
			Console.WriteLine($"Core count: {coreCount}");
			PhaseExploration();
			do
			{
				Iteration().Wait();
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
			for (int i = 0; i < this.Particles.Count; i++)
			{
				if(!Scores.ContainsKey(i)) continue;
				(double, int) totalScore = Scores[i];
				double score = totalScore.Item1 / totalScore.Item2 + totalScore.Item2 * 0.3;
				Particles[i].Iterate(score);
			}
			// Potentially update phase or similar
			if (Phase == 1)
			{
				if((DateTime.Now - phasestart).TotalMinutes >= PhaseConditions.MAX_MINUTES_PHASE1 ||
				   this.PhaseIterations >= PhaseConditions.MAX_ITERATIONS_PHASE1) PhaseSocialization();
				return;
			}
			if (Phase == 2)
			{
				if((DateTime.Now - phasestart).TotalMinutes >= PhaseConditions.MAX_MINUTES_PHASE2 ||
				   this.PhaseIterations >= PhaseConditions.MAX_ITERATIONS_PHASE3) PhaseConvergence();
				return;
			}
			if (Phase == 3)
			{
				if ((DateTime.Now - phasestart).TotalMinutes >= PhaseConditions.MAX_MINUTES_PHASE3 ||
				    this.PhaseIterations >= PhaseConditions.MAX_ITERATIONS_PHASE3)
				{
					Flush();
					Console.WriteLine("Exiting...");
					Environment.Exit(0);
				}
			}
		}

		#region Gameplay

		ConcurrentDictionary<int, (double, int)> Scores = new ConcurrentDictionary<int, (double, int)>();
		ConcurrentQueue<(int, int)> Matches = new ConcurrentQueue<(int, int)>();

		private void EnqueueMatches()
		{
			Scores.Clear();
			matches = 1;
			helper = 1;
			for (int id1 = 0; id1 < Particles.Count; id1++)
			{
				Matches.Enqueue((id1, id1));
				for (int id2 = 0; id2 < Particles.Count; id2++)
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

		private const double TOTAL_CORE_DIVIDER = 1.15;
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
				Scores.TryGetValue(next.Item1, out (double, int) scoreMap1);
				Scores.TryGetValue(next.Item2, out (double, int) scoreMap2);

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

				// Just add / replace the old values
				Scores.AddOrUpdate(next.Item1, scoreMap1, (key, oldValue) => scoreMap1);
				Scores.AddOrUpdate(next.Item2, scoreMap2, (key, oldValue) => scoreMap2);
				Console.WriteLine($"Finished match {thisMatch}");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error:\nSource: {ex.Source}\nMessage:{ex.Message}\nStacktrace: {ex.StackTrace}");
				throw ex;
			}
		}

		#endregion

		#region Flush

		private const string DATA_PATH = "E:/Coding/C#/Forms/Stormcloud/Connect4Data/Buffer2/";
		const int MAX_FOLDERS = 10000;
		
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

			string path = DATA_PATH + "particle_" + max + "/";
			Directory.CreateDirectory(path);

			// Now, gather the data and actually write it.
			// 3 Files corresponding to maps, mapscores and info

			int i = 1, amount = Particles.Count;
			DateTime end = DateTime.Now;

			try
			{
				StringBuilder FileInfo = new StringBuilder();
				FileInfo.AppendLine("File info:");
				FileInfo.AppendLine($"Boardsize: {Columns} x {Rows}");
				FileInfo.AppendLine($"Particles: {Particles.Count}");
				FileInfo.AppendLine($"Iterations: {TotalIterations}");
				FileInfo.AppendLine($"Games Played Per Iteration: {Particles.Count * Particles.Count}");
				FileInfo.AppendLine($"Games Played: {Particles.Count * Particles.Count * TotalIterations}");
				FileInfo.AppendLine($"Games Played Per Minute: {(Particles.Count * Particles.Count * TotalIterations) / (end - start).TotalMinutes}");
				FileInfo.AppendLine($"Cores: {coreCount}");
				FileInfo.AppendLine($"Start Time: {start}");
				FileInfo.AppendLine($"End Time: {end}");
				FileInfo.AppendLine($"Total Time: {end - start}");
				FileInfo.AppendLine($"Boardsize: {UnitMatch.Boardsize}");
				FileInfo.AppendLine($"Engine Depth: {UnitMatch._EngineDepth}");

				FileInfo.AppendLine("\n\nBest Global Particle:");
				FileInfo.AppendLine($"\nBest Global Particle Score: {Particle.GlobalBestPositionScore}");
				FileInfo.AppendLine("{");
				FileInfo.AppendLine("	NAME = \"Particle-GLOBAL-BEST\",");
				FileInfo.AppendLine($"\n	WEIGHT_SCORE_OWN = {Particle.GlobalBestPosition[0].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_SCORE_OPPONENT = {Particle.GlobalBestPosition[1].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OWN = {Particle.GlobalBestPosition[2].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OPPONENT = {Particle.GlobalBestPosition[3].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"\n	WEIGHT_WALL_DISTANCE = {Particle.GlobalBestPosition[4].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_NEIGHBORS = {Particle.GlobalBestPosition[5].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_NEIGHBOR_FREE = {Particle.GlobalBestPosition[6].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_NEIGHBOR_OWNED = {Particle.GlobalBestPosition[7].ToString().Replace(",", ".")},");
				FileInfo.AppendLine($"	WEIGHT_NEIGHBOR_TAKEN = {Particle.GlobalBestPosition[8].ToString().Replace(",", ".")}");
				FileInfo.AppendLine("};");

				File.WriteAllText(path + "INFO.strmcld", FileInfo.ToString());

				StringBuilder FileMaps = new StringBuilder();

				FileMaps.AppendLine($"\nMaps run: {amount}");
				foreach (var particle in Particles)
				{
					FileMaps.AppendLine($"\n\nParticle ID: {particle.ID}");
					FileMaps.AppendLine($"Current Position Score: {particle.CurrentPositionScore}");
					FileMaps.AppendLine("Current Position Values:");
					FileMaps.AppendLine("{");
					FileMaps.AppendLine($"	NAME = \"Particle-{particle.ID}\",");
					FileMaps.AppendLine($"\n	WEIGHT_SCORE_OWN = {particle.CurrentPosition[0].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_SCORE_OPPONENT = {particle.CurrentPosition[1].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OWN = {particle.CurrentPosition[2].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OPPONENT = {particle.CurrentPosition[3].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"\n	WEIGHT_WALL_DISTANCE = {particle.CurrentPosition[4].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBORS = {particle.CurrentPosition[5].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_FREE = {particle.CurrentPosition[6].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_OWNED = {particle.CurrentPosition[7].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_TAKEN = {particle.CurrentPosition[8].ToString().Replace(",", ".")}");
					FileMaps.AppendLine("};");
					FileMaps.AppendLine($"\nBest Position Score: {particle.CurrentPositionScore}");
					FileMaps.AppendLine("Best Position Values:");
					FileMaps.AppendLine("{");
					FileMaps.AppendLine($"	NAME = \"Particle-{particle.ID}\",");
					FileMaps.AppendLine($"\n	WEIGHT_SCORE_OWN = {particle.BestPosition[0].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_SCORE_OPPONENT = {particle.BestPosition[1].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OWN = {particle.BestPosition[2].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_HAMMINGDISTANCE_OPPONENT = {particle.BestPosition[3].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"\n	WEIGHT_WALL_DISTANCE = {particle.BestPosition[4].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBORS = {particle.BestPosition[5].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_FREE = {particle.BestPosition[6].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_OWNED = {particle.BestPosition[7].ToString().Replace(",", ".")},");
					FileMaps.AppendLine($"	WEIGHT_NEIGHBOR_TAKEN = {particle.BestPosition[8].ToString().Replace(",", ".")}");
					FileMaps.AppendLine("};");
				}

				File.WriteAllText(path + "PARTICLES.strmcld", FileMaps.ToString());

				StringBuilder FileScores_Current = new StringBuilder();
				i = 1;
				foreach (var particle in Particles.OrderByDescending(x => x.CurrentPositionScore))
				{
					FileScores_Current.AppendLine($"\nParticle {i++} of {amount}");
					FileScores_Current.AppendLine($" >> Particle ID: {particle.ID}");
					FileScores_Current.AppendLine($" >> Current Score: {particle.CurrentPositionScore}   ({particle.BestPositionScore} Best)");
				}
				File.WriteAllText(path + "PARTICLES_CURRENT_SCORES.strmcld", FileScores_Current.ToString());


				StringBuilder FileScores_Best = new StringBuilder();
				i = 1;
				foreach (var particle in Particles.OrderByDescending(x => x.BestPositionScore))
				{
					FileScores_Best.AppendLine($"\nParticle {i++} of {amount}");
					FileScores_Best.AppendLine($" >> Particle ID: {particle.ID}");
					FileScores_Best.AppendLine($" >> Best Score: {particle.BestPositionScore}   ({particle.CurrentPositionScore} Current)");
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
			Console.WriteLine("Switching to Phase 1: Exploration");
			Particle.CognitiveCoefficient = 2.77;	// [1, 3]
			Particle.SocialCoefficient = 1.25;		// [1, 3]
			Particle.Inertia = 0.82;        // [0.4, 0.9]
			Phase = 1;
		}

		void PhaseSocialization()
		{
			PhaseIterations = 0;
			phasestart = DateTime.Now;
			Console.WriteLine("Switching to Phase 2: Socialization");
			Particle.CognitiveCoefficient = 1.95;   // [1, 3]
			Particle.SocialCoefficient = 2.07;      // [1, 3]
			Particle.Inertia = 0.64;        // [0.4, 0.9]
			Phase = 2;
		}

		void PhaseConvergence()
		{
			PhaseIterations = 0;
			phasestart = DateTime.Now;
			Console.WriteLine("Switching to Phase 3: Convergence");
			Particle.CognitiveCoefficient = 1.15;   // [1, 3]
			Particle.SocialCoefficient = 2.73;      // [1, 3]
			Particle.Inertia = 0.41;        // [0.4, 0.9]
			Phase = 3;
		}
	}
}
