using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ChessV1.Stormcloud.Connect4.Weightfinder.PSO_TS_WRITER
{
	internal class TS_PSOExportParser
	{
		internal static (ParticleHistoryHolder[], double[], double) LoadHistory(string ParticlePath, int ParticleDimensions)
		{
			string[] foldersByDate =
				Directory.GetDirectories(ParticlePath)
					.Where(x => Path.GetFileName(x).StartsWith("phase_"))
					.OrderBy(x => Directory.GetCreationTime(x))
					.ToArray();

			if (foldersByDate.Length == 0) return (Array.Empty<ParticleHistoryHolder>(), Array.Empty<double>(), 0);

			string last = foldersByDate[foldersByDate.Length - 1];

			// Extract particle data
			string[] infoFile = ReadInfoFile(last);
			int particles = 10;
			double globalScore = 0;
			double[] globalWeights = new double[ParticleDimensions];
			for (int i = 0; i < infoFile.Length; i++)
			{
				string s = infoFile[i];
				if (s.StartsWith("Particles: ")) { particles = int.Parse(s.Substring(11)); continue; }
				if (s.StartsWith("Best Global Particle Score Internally: ")) { globalScore = double.Parse(s.Substring(39)); continue; }

				if (s == "{")
				{
					int w1 = i + 3;
					for (int i2 = 0; i2 < ParticleDimensions; i2++)
					{
						int s_i = i2;
						if (i2 >= 4) s_i++;
						if (i2 >= 6) s_i++;
						globalWeights[i2] = ParseWeight(infoFile[s_i + w1]);
					}
					break;
				}
			}

			ParticleHistoryHolder[] history = new ParticleHistoryHolder[particles];
			for (int i = 0; i < particles; i++)
			{
				history[i] = new ParticleHistoryHolder(i);
			}

			// Go through every folder, extract all the data from all particles, and then save it
			foreach (string folder in foldersByDate)
			{
				var particlesFile = ReadParticlesFile(folder);
				for (int i = 0; i < particlesFile.Length; i++)
				{
					string s = particlesFile[i];
					if (!s.StartsWith("Particle ID: ")) continue;
					int id = int.Parse(s.Split(' ')[2]);

					// Do this because some older files have incorrect exporting for this value, so we need to set it manually
					double prevBest = history[id].Iterations.Count == 0 ? double.NegativeInfinity : history[id].Iterations.LastOrDefault().ScoreBestSoFar;

					ParticleIteration iteration =
						new ParticleIteration(ParseParticle(Dimensions: ParticleDimensions, particlesFile, i, prevBest));

					history[id].Add(iteration);
				}
			}

			return (history, globalWeights, globalScore);
		}

		internal static (WeightHistoryHolder[], double[], double) LoadHistoryInvertedOrder(string ParticlePath, int ParticleDimensions)
		{
			string[] foldersByDate =
				Directory.GetDirectories(ParticlePath)
					.Where(x => Path.GetFileName(x).StartsWith("phase_"))
					.OrderBy(x => Directory.GetCreationTime(x))
					.ToArray();
			Console.WriteLine("path: " + ParticlePath);

			if (foldersByDate.Length == 0) return (Array.Empty<WeightHistoryHolder>(), Array.Empty<double>(), 0);

			string last = foldersByDate[foldersByDate.Length - 1];

			// Extract particle data
			string[] infoFile = ReadInfoFile(last);
			int particles = -1;
			double globalScore = 0;
			double[] globalWeights = new double[ParticleDimensions];
			for (int i = 0; i < infoFile.Length; i++)
			{
				string s = infoFile[i];
				if (s.StartsWith("Particles: ")) { particles = int.Parse(s.Substring(11)); continue; }
				if (s.StartsWith("Best Global Particle Score Internally: ")) { globalScore = double.Parse(s.Substring(39)); continue; }

				if (s == "{")
				{
					int w1 = i + 3;
					for (int i2 = 0; i2 < ParticleDimensions; i2++)
					{
						int s_i = i2;
						if (i2 >= 4) s_i++;
						if (i2 >= 6) s_i++;
						globalWeights[i2] = ParseWeight(infoFile[s_i + w1]);
					}
					break;
				}
			}




			WeightHistoryHolder[] history = new WeightHistoryHolder[2 * (ParticleDimensions + 1)]; // Weights + 1 for Scores, 2 times (current and best)
			for (int i = 0; i < history.Length; i++)
			{
				history[i] = new WeightHistoryHolder();
			}

			// Go through every folder, extract all the data from all particles, and then save it
			foreach (string folder in foldersByDate)
			{
				var particlesFile = ReadParticlesFile(folder);

				// Now, we read the file and add all weights
				// Weights: index (0: scoreCurrent, 1-11: weightsCurrent, 12: scoreBest, 13-24: weightsBest
				double[][] Weights = new double[history.Length][];
				for (int i = 0; i < Weights.Length; i++)
				{
					// new double which can fit all values from a particle for a single weight for a single iteration
					Weights[i] = new double[particles];
				}

				for (int lineID = 0; lineID < particlesFile.Length; lineID++)
				{
					string s = particlesFile[lineID];
					if (!s.StartsWith("Particle ID: ")) continue;
					int particleID = int.Parse(s.Split(' ')[2]);

					// ScoreCurrent
					Weights[0][particleID] = double.Parse(particlesFile[lineID + 1].Split(' ')[3]);
					int weightID;
					for (weightID = 0; weightID < ParticleDimensions; weightID++)
					{
						int line = lineID + weightID + 6;
						if (weightID >= 4) line++;
						if (weightID >= 6) line++;
						Weights[weightID + 1][particleID] = ParseWeight(particlesFile[line]);
					}

					// ScoreBest
					Weights[12][particleID] = double.Parse(particlesFile[lineID + 21].Split(' ')[3]);
					for (weightID = 0; weightID < ParticleDimensions; weightID++)
					{
						int line = lineID + weightID + 26;
						if (weightID >= 4) line++;
						if (weightID >= 6) line++;
						// + 13 = +1 (prev) + 11 (prev weights) + 1 (bestScore)
						Weights[weightID + 13][particleID] = ParseWeight(particlesFile[line]);
					}
				}

				// now we have all relevant data from the file in Weights[weightID][particleID]
				for (int i = 0; i < 12; i++)
				{
					history[i].Add(Weights[i]);
				}
				for (int i = 12; i < 24; i++)
				{
					history[i].AddBetter(Weights[i]);
				}
			}

			return (history, globalWeights, globalScore);
		}


		internal static void ConvertToTSSingleFileInvertedOrder(int ParticleRunID, int ParticleDimensions = 11)
			=> ConvertToTSSingleFileInvertedOrder($"{ParticleSwarmOptimization.DATA_PATH}particle_{ParticleRunID}", ParticleDimensions);
		internal static void ConvertToTSSingleFileInvertedOrder(string ParticlePath, int ParticleDimensions = 11)
		{
			var historyItem = LoadHistoryInvertedOrder(ParticlePath, ParticleDimensions);

			var history = historyItem.Item1;
			var globalWeights = historyItem.Item2;
			var globalScore = historyItem.Item3;

			// All data read, I think.
			// Now, export it

			string newFolder = ParticlePath.Replace("Buffer2", "Buffer5");
			if (!Directory.Exists(newFolder)) Directory.CreateDirectory(newFolder);

			// Write Current Scores
			string file = newFolder + "/current-scores.ts";
			StreamWriter write = new StreamWriter(file);
			write.Write("export const currentScores = [\n");
				write.Write(history[0].ToString(false));
			write.Write("]");
			write.Flush();
			write.Close();

			// Write Current Weights
			file = newFolder + "/current-weights.ts";
			write = new StreamWriter(file);
			write.Write("export const currentWeights = [\n");
			for (int i = 1; i < 12; i++)
			{
				write.Write(history[i].ToString(i < 11));
			}
			write.Write("]");
			write.Flush();
			write.Close();

			// Write Best Scores
			file = newFolder + "/best-scores.ts";
			write = new StreamWriter(file);
			write.Write("export const bestScores = [\n");
				write.Write(history[12].ToString(false));
			write.Write("]");
			write.Flush();
			write.Close();

			// Write Best Weights
			file = newFolder + "/best-weights.ts";
			write = new StreamWriter(file);
			write.Write("export const bestWeights = [\n");
			for (int i = 13; i < 24; i++)
			{
				write.Write(history[i].ToString(i < 23));
			}
			write.Write("]");
			write.Flush();
			write.Close();

			ExportTSGlobalBest(newFolder, globalWeights, globalScore);
		}


		#region Helpers

		static string[] ReadInfoFile(string folder) => ReadFile(folder, "INFO");
		static string[] ReadParticlesFile(string folder) => ReadFile(folder, "PARTICLES");

		static string[] ReadFile(string folder, string filename)
		{
			if(!File.Exists($"{folder}/{filename}.strmcld")) return Array.Empty<string>();
			return File.ReadAllLines($"{folder}/{filename}.strmcld");
		}

		static double ParseWeight(string w)
		{
			if (!w.Contains("="))
			{
				Console.WriteLine($"Tried to parse: \"{w}\" as weight");
				return 0;
			}
			w = w.Split('=')[1].Trim();
			w = w.Replace(",", "");
			w = w.Replace(".", ",");		// use commas in double parse
			return double.Parse(w);
		}

		static (double[], double[], double, double) ParseParticle(int Dimensions, string[] file, int lineID, double previousBest)
		{
			double scoreCurrent, scoreBest;
			double[] weightsCurrent = new double[Dimensions],
				weightsBest = new double[Dimensions];

			scoreCurrent = double.Parse(file[lineID + 1].Split(' ')[3]);
			int i;
			for (i = 0; i < Dimensions; i++)
			{
				int line = lineID + i + 6;
				if (i >= 4) line++;
				if (i >= 6) line++;
				weightsCurrent[i] = ParseWeight(file[line]);
			}

			scoreBest = double.Parse(file[lineID + 21].Split(' ')[3]);
			for (i = 0; i < Dimensions; i++)
			{
				int line = lineID + i + 26;
				if (i >= 4) line++;
				if (i >= 6) line++;
				weightsBest[i] = ParseWeight(file[line]);
			}

			return (weightsCurrent, weightsBest, scoreCurrent, Math.Max(scoreBest, previousBest));
		}

		#endregion

		static void ExportTSGlobalBest(string folder, double[] GlobalWeights, double GlobalScore)
		{
			string file = folder + $"/global-best.ts";
			StreamWriter writer = new StreamWriter(file);

			writer.Write("export const globalBest = {\n");
			writer.Write($"	score: {GlobalScore}".Replace(",", "."));
			writer.Write(",\n");
			writer.Write("	weights: [ ");
			foreach (double weight in GlobalWeights) writer.Write($"{weight.ToString().Replace(",", ".")}, ");
			writer.Write("	],\n");
			writer.Write("};");

			writer.Flush();
			writer.Close();
		}
	}
}
