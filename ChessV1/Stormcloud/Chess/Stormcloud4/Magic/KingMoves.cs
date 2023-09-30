using System;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	public class KingMoves
	{
		// Square 0 = h1, 1 << square
		static ulong KingLegalMoves(int square)
		{
			ulong moves = 0;
			//square = 63 - square;	// since layout matters, flip
			int rank = square / 8, file = square % 8;
			if(rank > 0)
            {
                moves |= 1UL << (square - 8);
                if (file > 0) moves |= 1UL << (square - 9);
                if (file < 7) moves |= 1UL << (square - 7);
            }
            if (rank < 7)
            {
                moves |= 1UL << (square - 9);
                if (file > 0) moves |= 1UL << (square + 7);
                if (file < 7) moves |= 1UL << (square + 9);
            }
            if (file > 0) moves |= 1UL << (square - 1);
            if (file < 7) moves |= 1UL << (square + 1);
            return moves;
        }

        public static void GenerateAllKingMoves()
        {
            ulong[] moves = new ulong[64];
            Log("\t\tinternal static readonly ulong[] KingMoves = {");
            for(int sq = 0; sq < 64; sq++)
            {
                moves[sq] = KingLegalMoves(sq);
                //Log($"[{sq}] King is here: [{Convert.ToString(1L << sq, 2)}] | Generated King Legal moves: {Convert.ToString((long)moves[sq], 2)}");
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

