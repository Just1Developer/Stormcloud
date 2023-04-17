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
			var TestBoardPosition = new Dictionary<int[], PieceType>();

			TestBoardPosition.Add(new int[2] { 1, 3 }, PieceType.rook);
			TestBoardPosition.Add(new int[2] { 2, 2 }, PieceType.king);
			TestBoardPosition.Add(new int[2] { 2, 6 }, PieceType.ROOK);
			TestBoardPosition.Add(new int[2] { 3, 4 }, PieceType.bishop);
			TestBoardPosition.Add(new int[2] { 3, 6 }, PieceType.PAWN);
			TestBoardPosition.Add(new int[2] { 3, 7 }, PieceType.PAWN);
			TestBoardPosition.Add(new int[2] { 4, 0 }, PieceType.rook);
			TestBoardPosition.Add(new int[2] { 4, 4 }, PieceType.pawn);
			TestBoardPosition.Add(new int[2] { 4, 5 }, PieceType.PAWN);
			TestBoardPosition.Add(new int[2] { 4, 7 }, PieceType.pawn);
			TestBoardPosition.Add(new int[2] { 5, 1 }, PieceType.PAWN);
			TestBoardPosition.Add(new int[2] { 5, 2 }, PieceType.pawn);
			TestBoardPosition.Add(new int[2] { 5, 3 }, PieceType.PAWN);
			TestBoardPosition.Add(new int[2] { 6, 5 }, PieceType.KING);
			TestBoardPosition.Add(new int[2] { 7, 4 }, PieceType.ROOK);

			int[] i = new int[2] { 4, 9 }; TestBoardPosition.Add(i, PieceType.ROOK); bool c2 = TestBoardPosition.ContainsKey(i); bool c3 = TestBoardPosition.ContainsKey(new int[2] { 4, 9 });
			// c2 is true, c3 is false, GPT-4 is right; ContainsKey compares pointers not value

			MoveHistory moveHistory = new MoveHistory(TestBoardPosition);
			moveHistory.BlackCastleOptions = CastleOptions.None;
			moveHistory.WhiteCastleOptions = CastleOptions.None;

			Chessboard2.Log("Starting Calculation...");
			Calculation calc = new Calculation(moveHistory, 10, Turn.White);
		}
	}
}
