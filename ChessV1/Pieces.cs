using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1
{
	internal abstract class Pieces
	{
		public static void Init()
		{

		}
	}

	internal interface Piece
	{
		BoardLocation Position { get; set; }
		PieceType PieceType { get; }
		Turn PieceColor { get; set; }
		int PieceValue { get; }

		void Move(BoardLocation delta);
	}

	internal class Pawn : Piece
	{
		public BoardLocation Position { get; set; }
		public PieceType PieceType { get; set; }
		public Turn PieceColor { get; set; }
		public int PieceValue { get => 1; }

		internal Pawn(BoardLocation InitialPosition)
		{
			Position = InitialPosition;
		}

		public void Move(BoardLocation delta)
		{

		}
	}

	public class Pawn2
	{
		internal Chessboard2 Chessboard;
		internal BoardLocation Position;
		bool moved = false;

		internal Pawn2(Chessboard2 Chessboard, BoardLocation Position)
		{
			this.Chessboard = Chessboard;
		}

		internal List<BoardLocation> LegalMoves()
		{
			List<BoardLocation> m = new List<BoardLocation>();
			
			return m;
		}

		internal void Move()
		{

		}
	}

	enum PieceType
	{
		KING = 10, QUEEN = 9, BISHOP = 3, ROOK = 5, KNIGHT = 2 /* treat as 3 */, PAWN = 1, Empty = 0,
		king = 20, queen = 19, bishop = 13, rook = 15, knight = 12, pawn = 11   // Black pieces, use values from white
	}

	// TODO Split Turn in Turn and Gamestate
}
