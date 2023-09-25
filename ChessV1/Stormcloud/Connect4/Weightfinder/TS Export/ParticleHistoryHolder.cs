using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChessV1.Stormcloud.Connect4.Weightfinder.PSO_TS_WRITER
{
	internal class ParticleHistoryHolder
	{
		private readonly int ParticleID;
		internal List<ParticleIteration> Iterations { get; }

		public ParticleHistoryHolder(int ParticleID)
		{
			this.ParticleID = ParticleID;
			Iterations = new List<ParticleIteration>();
		}

		public void Add(ParticleIteration iteration)
		{
			Iterations.Add(iteration);
		}

		public void ExportJSON(string folder)
		{
			string file = folder + $"/particle-{ParticleID}.json";

			StringBuilder ScoreBuilder = new StringBuilder();
			StringBuilder ScoreBuilderBestSoFar = new StringBuilder();
			StringBuilder[] WeightBuilders = new StringBuilder[22];

			for (int i = 0; i < WeightBuilders.Length; i++)
			{
				WeightBuilders[i] = new StringBuilder();
			}

			void AppendAll(string s)
			{
				foreach (var WeightBuilder in WeightBuilders)
					WeightBuilder.Append(s);
				ScoreBuilder.Append(s);
				ScoreBuilderBestSoFar.Append(s);
			}

			StringBuilder pre = new StringBuilder();
			StringBuilder post = new StringBuilder();
			pre.Append($"{{\n	\"particle\": {{\n		\"id\": {ParticleID}\n");
			AppendAll("		\"");
			for (int i = 0; i < 11; i++)
				WeightBuilders[i].Append($"CurrentWeight_{i+1}");
			for (int i = 11; i < 22; i++)
				WeightBuilders[i].Append($"BestWeight_{i-10}");
			ScoreBuilder.Append("scores");
			ScoreBuilderBestSoFar.Append("scoresBestSoFar");
			AppendAll("\": [");

			// Add data:
			int it = 0;
			foreach (var iteration in Iterations)
			{
				it++;
				for (int i = 0; i < 11; i++)
				{
					WeightBuilders[i].Append($"{iteration.WeightsCurrent[i]}".Replace(",", "."));
					if(it < Iterations.Count) WeightBuilders[i].Append(", ");
					else WeightBuilders[i].Append(" ");
				}
				for (int i = 11; i < 22; i++)
				{
					WeightBuilders[i].Append($"{iteration.WeightsBestSoFar[i-11]}".Replace(",", "."));
					if(it < Iterations.Count) WeightBuilders[i].Append(", ");
					else WeightBuilders[i].Append(" ");
				}

				ScoreBuilder.Append($"{iteration.ScoreCurrent}".Replace(",", "."));
				if (it < Iterations.Count) ScoreBuilder.Append(", ");
				else ScoreBuilder.Append(" ");

				ScoreBuilderBestSoFar.Append($"{iteration.ScoreBestSoFar}".Replace(",", "."));
				if (it < Iterations.Count) ScoreBuilderBestSoFar.Append(", ");
				else ScoreBuilderBestSoFar.Append(" ");
			}

			AppendAll("],\n");

			post.Append("	},\n");
			post.Append("}");

			StreamWriter writer = new StreamWriter(file);
			writer.Write(pre);
			writer.Write(ScoreBuilder);
			for (int i = 0; i < 11; i++)
			{
				writer.Write(WeightBuilders[i]);
			}
			writer.Write(ScoreBuilderBestSoFar);
			for (int i = 11; i < 22; i++)
			{
				writer.Write(WeightBuilders[i]);
			}
			writer.Write(post);
			writer.Flush();
			writer.Close();

			Console.WriteLine($"Done: {file}");
		}

		public override string ToString()
		{
			StringBuilder ScoreBuilder = new StringBuilder();
			StringBuilder ScoreBuilderBestSoFar = new StringBuilder();
			StringBuilder[] WeightBuilders = new StringBuilder[22];

			for (int i = 0; i < WeightBuilders.Length; i++)
			{
				WeightBuilders[i] = new StringBuilder();
			}

			void AppendAll(string s)
			{
				foreach (var WeightBuilder in WeightBuilders)
					WeightBuilder.Append(s);
				ScoreBuilder.Append(s);
				ScoreBuilderBestSoFar.Append(s);
			}

			StringBuilder pre = new StringBuilder();
			StringBuilder post = new StringBuilder();
			pre.Append($" {{\n		\"id\": {ParticleID},\n");
			AppendAll("		\"");
			for (int i = 0; i < 11; i++)
				WeightBuilders[i].Append($"CurrentWeight_{i + 1}");
			for (int i = 11; i < 22; i++)
				WeightBuilders[i].Append($"BestWeight_{i - 10}");
			ScoreBuilder.Append("scores");
			ScoreBuilderBestSoFar.Append("scoresBestSoFar");
			AppendAll("\": [");

			// Add data:
			int it = 0;
			foreach (var iteration in Iterations)
			{
				it++;
				for (int i = 0; i < 11; i++)
				{
					WeightBuilders[i].Append($"{iteration.WeightsCurrent[i]}".Replace(",", "."));
					if (it < Iterations.Count) WeightBuilders[i].Append(", ");
					else WeightBuilders[i].Append(" ");
				}
				for (int i = 11; i < 22; i++)
				{
					WeightBuilders[i].Append($"{iteration.WeightsBestSoFar[i - 11]}".Replace(",", "."));
					if (it < Iterations.Count) WeightBuilders[i].Append(", ");
					else WeightBuilders[i].Append(" ");
				}

				ScoreBuilder.Append($"{iteration.ScoreCurrent}".Replace(",", "."));
				if (it < Iterations.Count) ScoreBuilder.Append(", ");
				else ScoreBuilder.Append(" ");

				ScoreBuilderBestSoFar.Append($"{iteration.ScoreBestSoFar}".Replace(",", "."));
				if (it < Iterations.Count) ScoreBuilderBestSoFar.Append(", ");
				else ScoreBuilderBestSoFar.Append(" ");
			}

			AppendAll("],\n");

			post.Append("	},\n");

			StringBuilder file = new StringBuilder();
			file.Append(pre);
			file.Append(ScoreBuilder);
			for (int i = 0; i < 11; i++)
			{
				file.Append(WeightBuilders[i]);
			}
			file.Append(ScoreBuilderBestSoFar);
			for (int i = 11; i < 22; i++)
			{
				file.Append(WeightBuilders[i]);
			}
			file.Append(post);

			return file.ToString();
		}
	}
}
