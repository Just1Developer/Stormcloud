using System;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	public class KnightMoves
	{
		// Todo change layout to map 0 = h1, 63 = a8, 7 = a1

		// Square 0 = h1, 1 << square
		static ulong KnightLegalMoves(int square)
		{
			ulong moves = 0;
			//square = 63 - square;	// since layout matters, flip
			int rank = square / 8, file = square % 8;
			if(rank != 0)
            {
                if (file > 1) moves |= 1UL << (square - 10);
                if (file < 6) moves |= 1UL << (square - 6);
                if (rank > 1)
                {
                    if (file != 0) moves |= 1UL << (square - 17);
                    if (file != 7) moves |= 1UL << (square - 15);
                }
            }
            if (rank != 7)
            {
                if (file > 1) moves |= 1UL << (square + 6);
                if (file < 6) moves |= 1UL << (square + 10);
                if (rank < 6)
                {
                    if (file != 0) moves |= 1UL << (square + 15);
                    if (file != 7) moves |= 1UL << (square + 17);
                }
            }
            return moves;
        }

        public static void GenerateAllKnightMoves()
        {
            ulong[] moves = new ulong[64];
            Log("\t\tinternal static readonly ulong[] KnightMoves = {");
            for(int sq = 0; sq < 64; sq++)
            {
                moves[sq] = KnightLegalMoves(sq);
                //Log($"[{sq}] Knight is here: [{Convert.ToString(1L << sq, 2)}] | Generated Knight Legal moves: {Convert.ToString((long)moves[sq], 2)}");
                Log($"\t\t\t0x{Convert.ToString((long) moves[sq], 16)}, // square: {sq}");
            }
            Log("\t\t};");
        }

        static void Log(string s)
        {
            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debug.WriteLine(s);
            else Console.WriteLine(s);
        }
	}
}

