using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormcloud
{
	internal class Stormcloud4
	{
		// Idea for Stormcloud4: Position is 64-byte array. Each piece has 3 bits identifying the piece, 1 bit for color, and other bits for stuff like isPinned or maybe 2 bits for preferred third of the board
		
		/* Taken from Stormcloud3 13.07.2023 21:13
		/// <summary>
		/// Returns Tuple<Score, EvalResult, AllActuallyLegalMoves, (CastleOptionsActual)>
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="IsWhitesTurn"></param>
		/// <param name="CastleOptions"></param>
		/// <returns></returns>
		Tuple<double, byte, List<short>> AdvancedPositionEvaluation(byte[] Position, bool IsWhitesTurn, byte CastleOptions, List<short> AllLegalNextMoves)
		{
			double score = 0.0;
			byte result = IsWhitesTurn ? EvaluationResultBlackTurn : EvaluationResultWhiteTurn;     // Default Value

			double materialAdvantage = IsWhitesTurn ? MaterialEvaluation(Position) : -MaterialEvaluation(Position);

			score += materialAdvantage;
			score += (0.2 * (AllLegalNextMoves.Count - 10));    // less than 10 moves is negative, more than 10 is positive. Mobile positions are preferred

			// ...

			// Todo calculate result

			bool GameOver = (result & EvalResultGameOverMask) == EvalResultGameOverMask;    // 0110 or 1001 is for turns, so we need to actually check for 1100
			bool Draw = GameOver && ((result & EvalResultDrawMask) == EvaluationResultDraw);
			if (Draw) result = EvaluationResultDraw;
			else if (GameOver) result = (result & EvalResultWhiteMask) != 0 ? EvaluationResultWhiteWon : EvaluationResultBlackWon;

			return new Tuple<double, byte, List<short>>(score, result, AllLegalNextMoves);
		}
		*/

	}
}
