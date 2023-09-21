namespace ChessV1.Stormcloud.Connect4.Weightfinder.PSO_JSON_WRITER
{
	internal struct ParticleIteration
	{
		public readonly double[] WeightsCurrent, WeightsBestSoFar;
		public readonly double ScoreCurrent, ScoreBestSoFar;

		public ParticleIteration(double[] WeightsCurrent, double[] weightsBestSoFar, double ScoreCurrent, double ScoreBestSoFar)
		{
			this.WeightsCurrent = WeightsCurrent;
			this.WeightsBestSoFar = weightsBestSoFar;
			this.ScoreCurrent = ScoreCurrent;
			this.ScoreBestSoFar = ScoreBestSoFar;
		}

		public ParticleIteration((double[], double[], double, double) data)
		{
			this.WeightsCurrent = data.Item1;
			this.WeightsBestSoFar = data.Item2;
			this.ScoreCurrent = data.Item3;
			this.ScoreBestSoFar = data.Item4;
		}

	}
}
