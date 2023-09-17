using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud.Connect4.Weightfinder
{
	internal static class PhaseConditions
	{
		internal const double SPEED = 1;

		internal const int MAX_ITERATIONS_PHASE1 = (int) (100 / SPEED);
		internal const int MAX_MINUTES_PHASE1 = (int) (90 / SPEED);

		internal const int MAX_ITERATIONS_PHASE2 = (int) (150 / SPEED);
		internal const int MAX_MINUTES_PHASE2 = (int) (120 / SPEED); // Maximum time spent in Phase 2

		internal const int MAX_ITERATIONS_PHASE3 = (int) (150 / SPEED);
		internal const int MAX_MINUTES_PHASE3 = (int) (150 / SPEED);    // Maximum time spent in Phase 3

		internal const int MAX_MINUTES_TOTAL = MAX_MINUTES_PHASE1 + MAX_MINUTES_PHASE2 + MAX_MINUTES_PHASE3;
		internal const int MAX_ITERATIONS_TOTAL = MAX_ITERATIONS_PHASE1 + MAX_ITERATIONS_PHASE2 + MAX_ITERATIONS_PHASE3;
	}
}
