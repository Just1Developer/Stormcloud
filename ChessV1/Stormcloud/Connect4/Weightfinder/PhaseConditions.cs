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
		internal const double MIN_SCORE_ESCAPE_MULTIPLIER = 2;

		internal const int MAX_ITERATIONS_PHASE1 = (int) (35 / SPEED);
		internal const int MAX_MINUTES_PHASE1 = (int) (900 / SPEED);
		internal const int MIN_SCORE_PHASE1 = (int)(420 / SPEED);

		internal const int MAX_ITERATIONS_PHASE2 = (int) (40 / SPEED);
		internal const int MAX_MINUTES_PHASE2 = (int) (1200 / SPEED); // Maximum time spent in Phase 2
		internal const int MIN_SCORE_PHASE2 = (int) (460 / SPEED);

		internal const int MAX_ITERATIONS_PHASE3 = (int) (20 / SPEED);
		internal const int MAX_MINUTES_PHASE3 = (int) (1500 / SPEED);    // Maximum time spent in Phase 3
		internal const int MIN_SCORE_PHASE3 = (int)(480 / SPEED);

		internal const int MAX_MINUTES_TOTAL = MAX_MINUTES_PHASE1 + MAX_MINUTES_PHASE2 + MAX_MINUTES_PHASE3;
		internal const int MAX_ITERATIONS_TOTAL = MAX_ITERATIONS_PHASE1 + MAX_ITERATIONS_PHASE2 + MAX_ITERATIONS_PHASE3;
	}
}
