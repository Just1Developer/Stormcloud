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
		BoardPosition Position { get; set; }
		PieceType PieceType { get; }
		Turn PieceColor { get; set; }

		void Move(BoardPosition delta);
	}

	internal class Pawn : Piece
	{
		public BoardPosition Position { get; set; }
		public PieceType PieceType { get; set; }
		public Turn PieceColor { get; set; }

		internal Pawn(BoardPosition InitialPosition)
		{
			Position = InitialPosition;
		}

		public void Move(BoardPosition delta)
		{

		}
	}

	public class Pawn2
	{
		internal Chessboard2 Chessboard;
		internal BoardPosition Position;
		bool moved = false;

		internal Pawn2(Chessboard2 Chessboard, BoardPosition Position)
		{
			this.Chessboard = Chessboard;
		}

		internal List<BoardPosition> LegalMoves()
		{
			List<BoardPosition> m = new List<BoardPosition>();
			
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
