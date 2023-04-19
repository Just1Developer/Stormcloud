using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1
{
	internal class UnitTest
	{
		public UnitTest()
		{
			var TestBoardPosition = new Dictionary<Coordinate, PieceType>();

			TestBoardPosition.Add(new Coordinate(1, 3), PieceType.rook);
			TestBoardPosition.Add(new Coordinate(1, 6), PieceType.ROOK);
			TestBoardPosition.Add(new Coordinate(2, 2), PieceType.king);
			TestBoardPosition.Add(new Coordinate(3, 4), PieceType.bishop);
			TestBoardPosition.Add(new Coordinate(3, 6), PieceType.PAWN);
			TestBoardPosition.Add(new Coordinate(3, 7), PieceType.PAWN);
			TestBoardPosition.Add(new Coordinate(4, 0), PieceType.rook);
			TestBoardPosition.Add(new Coordinate(5, 4), PieceType.pawn);
			TestBoardPosition.Add(new Coordinate(4, 5), PieceType.PAWN);
			TestBoardPosition.Add(new Coordinate(4, 7), PieceType.pawn);
			TestBoardPosition.Add(new Coordinate(5, 1), PieceType.PAWN);
			TestBoardPosition.Add(new Coordinate(5, 2), PieceType.pawn);
			TestBoardPosition.Add(new Coordinate(5, 3), PieceType.PAWN);
			TestBoardPosition.Add(new Coordinate(6, 5), PieceType.KING);
			TestBoardPosition.Add(new Coordinate(7, 4), PieceType.ROOK);

			Coordinate i = new Coordinate(4, 9); TestBoardPosition.Add(i, PieceType.ROOK); bool c2 = TestBoardPosition.ContainsKey(i); bool c3 = TestBoardPosition.ContainsKey(new Coordinate(4, 9));
			// c2 is true, c3 is false, GPT-4 is right; ContainsKey compares pointers not value

			MoveHistory moveHistory = new MoveHistory(TestBoardPosition);
			moveHistory.BlackCastleOptions = CastleOptions.None;
			moveHistory.WhiteCastleOptions = CastleOptions.None;

			Chessboard2.Log("Starting Calculation...");
			Calculation calc = new Calculation(moveHistory, 50, Turn.White);    // Something is wrong, Depth 50: 145ms, Kf1 (not even in the top 3 according to stockfish)
			Chessboard2.Log($"InCheck: {calc.IsCheck}, IsCheckmate: {calc.IsCheckmate}, IsStalemate: {calc.IsStalemate}");
		}
	}
}
