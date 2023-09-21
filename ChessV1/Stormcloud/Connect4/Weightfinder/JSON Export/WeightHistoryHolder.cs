using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Connect4.Weightfinder.JSON_Export
{
	internal class WeightHistoryHolder
	{
		/*
		 * Array of double[]
		 * each double[] has size of particle amount, the size of array of double[] is the amount of iterations
		 * /
		private double[][] WeightValues;

		internal WeightHistoryHolder(int Particles, int Iterations)
		{
			WeightValues = new double[Iterations][];
			for (int i = 0; i < Iterations; i++)
				WeightValues[i] = new double[Particles];
		}//*/

		// Alternative
		private List<double[]> WeightValues;

		internal WeightHistoryHolder()
		{
			WeightValues = new List<double[]>();
		}

		public void Add(double[] WeightValuesOfAllParticles)
		{
			WeightValues.Add(WeightValuesOfAllParticles);
		}

		public void AddBetter(double[] WeightValuesOfAllParticles)
		{
			double[] better = new double[WeightValuesOfAllParticles.Length];

			if (WeightValues.Count == 0)
			{
				WeightValues.Add(WeightValuesOfAllParticles);
				return;
			}

			double[] last = WeightValues[WeightValues.Count - 1];
			for (int i = 0; i < better.Length; i++)
			{
				better[i] = Math.Max(WeightValuesOfAllParticles[i], last[i]);
			}

			WeightValues.Add(better);
		}

		public override string ToString() => ToString(true);
		public string ToString(bool comma)
		{
			if (WeightValues.Count == 0) return "{},\n";
			StringBuilder[] builders = new StringBuilder[WeightValues[0].Length];
			for(int i = 0; i < builders.Length; i++)
				builders[i] = new StringBuilder();

			StringBuilder complete = new StringBuilder();
			complete.Append("	{\n");
			
			AppendAll("		particle");
			for (int i = 0; i < builders.Length; i++) builders[i].Append(i);
			AppendAll(": [");

			// Add weight values
			int index = 0;
			foreach (var weightIteration in WeightValues)
			{
				index++;
				if (weightIteration.Length != builders.Length) throw new InvalidDataException($"Data of Iteration {index} could not be 1:1 mapped to Stringbuilders");
				for (int i = 0; i < builders.Length; i++)
				{
					builders[i].Append(weightIteration[i].ToString().Replace(",", "."));
					if (index < WeightValues.Count) builders[i].Append(", ");
				}
			}

			AppendAll("],\n");

			foreach (var builder in builders) complete.Append(builder);
			if(comma) complete.Append("	},\n");
			else complete.Append("	}\n");
			return complete.ToString();

			void AppendAll(string text)
			{
				foreach (var builder in builders) builder.Append(text);
			}
		}
	}
}
