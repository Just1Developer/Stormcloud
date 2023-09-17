using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Connect4
{
	internal class EngineWeightMap
	{
		public string NAME;

		public double WEIGHT_SCORE_OWN;                    // yes: 1.2
		public double WEIGHT_SCORE_OPPONENT;              // yes: 1.03
		public double WEIGHT_HAMMINGDISTANCE_OWN;          // yes: 0.9
		public double WEIGHT_HAMMINGDISTANCE_OPPONENT;    // yes: 1.05

		public double WEIGHT_WALL_DISTANCE;
		public double WEIGHT_NEIGHBORS;   // Overall neighbor weight
		public double WEIGHT_NEIGHBOR_FREE;    // Available
		public double WEIGHT_NEIGHBOR_OWNED;   // Owned by me
		public double WEIGHT_NEIGHBOR_TAKEN;  // Taken by opponent

		public EngineWeightMap() { }

		public EngineWeightMap(
			string NAME,
			double WEIGHT_SCORE_OWN,
			double WEIGHT_SCORE_OPPONENT,
			double WEIGHT_HAMMINGDISTANCE_OWN,
			double WEIGHT_HAMMINGDISTANCE_OPPONENT,
			double WEIGHT_WALL_DISTANCE,
			double WEIGHT_NEIGHBORS,
			double WEIGHT_NEIGHBOR_FREE,
			double WEIGHT_NEIGHBOR_OWNED,
			double WEIGHT_NEIGHBOR_TAKEN
			)
		{
			this.NAME = NAME;

			this.WEIGHT_SCORE_OWN = WEIGHT_SCORE_OWN;
			this.WEIGHT_SCORE_OPPONENT = WEIGHT_SCORE_OPPONENT;
			this.WEIGHT_HAMMINGDISTANCE_OWN = WEIGHT_HAMMINGDISTANCE_OWN;
			this.WEIGHT_HAMMINGDISTANCE_OPPONENT = WEIGHT_HAMMINGDISTANCE_OPPONENT;

			this.WEIGHT_WALL_DISTANCE = WEIGHT_WALL_DISTANCE;
			this.WEIGHT_NEIGHBORS = WEIGHT_NEIGHBORS;
			this.WEIGHT_NEIGHBOR_FREE = WEIGHT_NEIGHBOR_FREE;
			this.WEIGHT_NEIGHBOR_OWNED = WEIGHT_NEIGHBOR_OWNED;
			this.WEIGHT_NEIGHBOR_TAKEN = WEIGHT_NEIGHBOR_TAKEN;
		}

		public static EngineWeightMap HighestEloEngineBoard
		{
			get => ParticleBest_Run1;	// Might change here based on more Weightmaps, but this always returns the best known engine map.
		}

		public static EngineWeightMap DefaultPreset1 = new EngineWeightMap()
		{
			NAME = "DefaultPreset1",

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
			NAME = "DefaultPreset_Old",

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
			NAME = "DefaultPreset2",

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

		public static EngineWeightMap DefaultPreset3 = new EngineWeightMap()
		{
			NAME = "DefaultPreset3",

			WEIGHT_SCORE_OWN = 1.2,                    // yes: 1.2
			WEIGHT_SCORE_OPPONENT = 1.13,              // yes: 1.03
			WEIGHT_HAMMINGDISTANCE_OWN = 0.95,          // yes: 0.9
			WEIGHT_HAMMINGDISTANCE_OPPONENT = 1.30,    // yes: 1.05

			WEIGHT_WALL_DISTANCE = 0.32,
			WEIGHT_NEIGHBORS = 0.57,   // Overall neighbor weight
			WEIGHT_NEIGHBOR_FREE = 0.7,    // Available
			WEIGHT_NEIGHBOR_OWNED = 1.2,   // Owned by me
			WEIGHT_NEIGHBOR_TAKEN = -0.9,  // Taken by opponent
		};

		public static EngineWeightMap DefaultPresetBalanced = new EngineWeightMap()
		{
			NAME = "DefaultPresetBalanced",

			WEIGHT_SCORE_OWN = 1.15,                    // yes: 1.2
			WEIGHT_SCORE_OPPONENT = 1.1,              // yes: 1.03
			WEIGHT_HAMMINGDISTANCE_OWN = 0.93,          // yes: 0.9
			WEIGHT_HAMMINGDISTANCE_OPPONENT = 0.98,    // yes: 1.05

			WEIGHT_WALL_DISTANCE = 0.41,
			WEIGHT_NEIGHBORS = 0.91,   // Overall neighbor weight
			WEIGHT_NEIGHBOR_FREE = 0.5,    // Available
			WEIGHT_NEIGHBOR_OWNED = 1.1,   // Owned by me
			WEIGHT_NEIGHBOR_TAKEN = -0.95,  // Taken by opponent
		};

		public static EngineWeightMap PresetComp1 = new EngineWeightMap()
		{
			NAME = "PresetComp1",

			WEIGHT_SCORE_OWN = 0.11,
			WEIGHT_SCORE_OPPONENT = 0.11,
			WEIGHT_HAMMINGDISTANCE_OWN = 0.9,
			WEIGHT_HAMMINGDISTANCE_OPPONENT = 1.35,

			WEIGHT_WALL_DISTANCE = 0,
			WEIGHT_NEIGHBORS = 0,
			WEIGHT_NEIGHBOR_FREE = 1,
			WEIGHT_NEIGHBOR_OWNED = 1.1,
			WEIGHT_NEIGHBOR_TAKEN = -0.9,
		};

		public static EngineWeightMap ParticleBest_Run1 = new EngineWeightMap()
		{
			NAME = "Particle-GLOBAL-BEST",

			WEIGHT_SCORE_OWN = 2.1975336947996,
			WEIGHT_SCORE_OPPONENT = 1.65849962183485,
			WEIGHT_HAMMINGDISTANCE_OWN = 1.19310165249792,
			WEIGHT_HAMMINGDISTANCE_OPPONENT = -1.14269030238719,

			WEIGHT_WALL_DISTANCE = 0.14681299066677,
			WEIGHT_NEIGHBORS = 0.499797714695559,
			WEIGHT_NEIGHBOR_FREE = 0.297057464037173,
			WEIGHT_NEIGHBOR_OWNED = 2.14931392398733,
			WEIGHT_NEIGHBOR_TAKEN = -1.70059287079633
		};
	}
}
