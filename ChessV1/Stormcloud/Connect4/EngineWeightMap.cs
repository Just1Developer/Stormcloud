using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Connect4
{
	internal class EngineWeightMap
	{
		public double WEIGHT_SCORE_OWN;                    // yes: 1.2
		public double WEIGHT_SCORE_OPPONENT;              // yes: 1.03
		public double WEIGHT_HAMMINGDISTANCE_OWN;          // yes: 0.9
		public double WEIGHT_HAMMINGDISTANCE_OPPONENT;    // yes: 1.05

		public double WEIGHT_WALL_DISTANCE;
		public double WEIGHT_NEIGHBORS;   // Overall neighbor weight
		public double WEIGHT_NEIGHBOR_FREE;    // Available
		public double WEIGHT_NEIGHBOR_OWNED;   // Owned by me
		public double WEIGHT_NEIGHBOR_TAKEN;  // Taken by opponent

		public static EngineWeightMap HighestEloEngineBoard
		{
			get => DefaultPreset1;	// Might change here based on more Weightmaps, but this always returns the best known engine map.
		}

		public static EngineWeightMap DefaultPreset1 = new EngineWeightMap()
		{
			WEIGHT_SCORE_OWN = 1.2,                    // yes: 1.2
			WEIGHT_SCORE_OPPONENT = 1.03,              // yes: 1.03
			WEIGHT_HAMMINGDISTANCE_OWN = 0.9,          // yes: 0.9
			WEIGHT_HAMMINGDISTANCE_OPPONENT = 1.35,    // yes: 1.05

			WEIGHT_WALL_DISTANCE = 0.32,
			WEIGHT_NEIGHBORS = 0.67,   // Overall neighbor weight
			WEIGHT_NEIGHBOR_FREE = 1.0,    // Available
			WEIGHT_NEIGHBOR_OWNED = 1.1,   // Owned by me
			WEIGHT_NEIGHBOR_TAKEN = -0.9,  // Taken by opponent
		};

		public static EngineWeightMap DefaultPreset_Old = new EngineWeightMap()
		{
			WEIGHT_SCORE_OWN = 1.1,                    // yes: 1.2
			WEIGHT_SCORE_OPPONENT = 1.03,              // yes: 1.03
			WEIGHT_HAMMINGDISTANCE_OWN = 0.9,          // yes: 0.9
			WEIGHT_HAMMINGDISTANCE_OPPONENT = 1.05,    // yes: 1.05

			WEIGHT_WALL_DISTANCE = 0.27,
			WEIGHT_NEIGHBORS = 0.67,   // Overall neighbor weight
			WEIGHT_NEIGHBOR_FREE = 1.0,    // Available
			WEIGHT_NEIGHBOR_OWNED = 1.1,   // Owned by me
			WEIGHT_NEIGHBOR_TAKEN = -0.9,  // Taken by opponent
		};

		public static EngineWeightMap DefaultPreset2 = new EngineWeightMap()
		{
			WEIGHT_SCORE_OWN = 1.6,                    // yes: 1.2
			WEIGHT_SCORE_OPPONENT = 1.53,              // yes: 1.03
			WEIGHT_HAMMINGDISTANCE_OWN = 0.95,          // yes: 0.9
			WEIGHT_HAMMINGDISTANCE_OPPONENT = 1.10,    // yes: 1.05

			WEIGHT_WALL_DISTANCE = 0.27,
			WEIGHT_NEIGHBORS = 0.67,   // Overall neighbor weight
			WEIGHT_NEIGHBOR_FREE = 1.0,    // Available
			WEIGHT_NEIGHBOR_OWNED = 1.1,   // Owned by me
			WEIGHT_NEIGHBOR_TAKEN = -0.9,  // Taken by opponent
		};
	}
}
