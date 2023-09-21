using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Connect4.Weightfinder
{
	internal class Particle
	{
		public static (double, double)[] SearchSpace = new (double, double)[Dimensions];
		internal const int Dimensions = 11;
		private static Random rng = new Random();

		public static double Inertia, CognitiveCoefficient, SocialCoefficient;
		internal static double[] GlobalBestPosition = new double[Dimensions];
		internal static double GlobalBestPositionScore;

		public static void Init()
		{
			// Define Searchspace
			SearchSpace[0] = (-1.2, 2.8);          // WEIGHT_SCORE_OWN
			SearchSpace[1] = (-1.2, 2.8);          // WEIGHT_SCORE_OPPONENT
			SearchSpace[2] = (-1.2, 2.8);          // WEIGHT_HAMMINGDISTANCE_OWN
			SearchSpace[3] = (-2.2, 2.8);          // WEIGHT_HAMMINGDISTANCE_OPPONENT
			SearchSpace[4] = (-1.2, 2.8);          // WEIGHT_FORK_HAMMINGDISTANCE_S
			SearchSpace[5] = (-1.2, 2.7);          // WEIGHT_FORK_HAMMINGDISTANCE_L
			SearchSpace[6] = (-1.8, 3.2);          // WEIGHT_WALL_DISTANCE
			SearchSpace[7] = (-2.0, 3.1);          // WEIGHT_NEIGHBORS
			SearchSpace[8] = (-2.3, 3.7);          // WEIGHT_NEIGHBOR_FREE
			SearchSpace[9] = (-1.9, 3.3);          // WEIGHT_NEIGHBOR_OWNED
			SearchSpace[10] = (-2.2, 2.3);          // WEIGHT_NEIGHBOR_TAKEN
		}

		public double[] Position
		{
			get => CurrentPosition;
		}

		// General Docs: https://en.wikipedia.org/wiki/Particle_swarm_optimization

		internal double[] CurrentPosition = new double[Dimensions],
			BestPosition = new double[Dimensions],
			Velocity = new double[Dimensions];

		internal double CurrentPositionScore, BestPositionScore;

		public readonly int ID;

		public Particle(int ID)
		{
			this.ID = ID;
			for (int dim = 0; dim < Dimensions; dim++)
			{
				CurrentPosition[dim] = rng.NextDouble() *
					(SearchSpace[dim].Item2 - SearchSpace[dim].Item1) + SearchSpace[dim].Item1;
			}

			BestPosition = CurrentPosition;
		}

		// There is a problem with continuity here regarding when the current particle is being evaluated

		internal void Mutate()
		{
			double[] VelocityNew = new double[Dimensions];
			for (int dim = 0; dim < Dimensions; dim++)
			{
				VelocityNew[dim] = Inertia * Velocity[dim] +
				                   CognitiveCoefficient * rng.NextDouble() * (BestPosition[dim] - CurrentPosition[dim]) +
				                   SocialCoefficient * rng.NextDouble() * (GlobalBestPosition[dim] - BestPosition[dim]);
				CurrentPosition[dim] += VelocityNew[dim];
				// Check if out of bounds
				if(CurrentPosition[dim] > SearchSpace[dim].Item2) CurrentPosition[dim] -= Math.Abs(SearchSpace[dim].Item2 - CurrentPosition[dim]) * ParticleSwarmOptimization.OUT_OF_BOUNDS_BOUNCE_STRENGTH;
				else if(CurrentPosition[dim] < SearchSpace[dim].Item1) CurrentPosition[dim] += Math.Abs(SearchSpace[dim].Item1 - CurrentPosition[dim]) * ParticleSwarmOptimization.OUT_OF_BOUNDS_BOUNCE_STRENGTH;
			}
			Velocity = VelocityNew;
		}

		internal void Iterate(double currentPositionEvaluation)
		{
			CurrentPositionScore = currentPositionEvaluation;
			// Update best
			if (CurrentPositionScore > BestPositionScore)
			{
				BestPosition = CurrentPosition;
				BestPositionScore = CurrentPositionScore;
			}
			if (CurrentPositionScore > GlobalBestPositionScore)
			{
				GlobalBestPosition = CurrentPosition;
				GlobalBestPositionScore = CurrentPositionScore;
			}
		}
	}
}
