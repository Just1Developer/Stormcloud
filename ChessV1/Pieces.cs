using System.Collections.Generic;
using System.Drawing;

namespace ChessUI
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
		Image PieceImage { get; }

		void Move(BoardLocation delta);
	}

	internal class Pawn : Piece
	{
		public BoardLocation Position { get; set; }
		public PieceType PieceType { get => PieceColor == Turn.White ? PieceType.PAWN : PieceType.pawn; }
		public Turn PieceColor { get; set; } = Turn.White;
		public int PieceValue { get => 1; }
		public Image PieceImage { get => Chessboard.PieceImages.ContainsKey(PieceType) ? Chessboard.PieceImages[PieceType] : null; }

		/*
		public List<int[,]> AllPossibleMovesOld { get; } =
                new List<int[,]>
                {
                    new int[,] { { -1 }, { -1 } },
                    new int[,] { { -1 }, { 0 } },
                    new int[,] { { -1 }, { 1 } },
                    new int[,] { { -2 }, { 0 } },
                };

		public List<int[,]> AllPossibleMovesOldInverted { get; private set; }
		*/


		public List<BoardLocation> AllPossibleMoves { get; } =
				new List<BoardLocation>
				{

				};
		public List<BoardLocation> AllPossibleMovesInverted { get; private set; }


		internal Pawn(BoardLocation InitialPosition)
		{
			Position = InitialPosition;

			// Set Inverted Moves
			AllPossibleMovesInverted = new List<BoardLocation>();
			foreach (BoardLocation old in AllPossibleMoves)
				AllPossibleMovesInverted.Add(old.Mirror());
		}

		public void Move(BoardLocation delta)
		{

		}
	}

	internal class Bishop : Piece
	{
		public BoardLocation Position { get; set; }
		public PieceType PieceType { get => PieceColor == Turn.White ? PieceType.PAWN : PieceType.pawn; }
		public Turn PieceColor { get; set; } = Turn.White;
		public int PieceValue { get => 1; }
		public Image PieceImage { get => Chessboard.PieceImages.ContainsKey(PieceType) ? Chessboard.PieceImages[PieceType] : null; }

		/*
		public List<int[,]> AllPossibleMovesOld { get; } =
                new List<int[,]>
                {
                    new int[,] { { -1 }, { -1 } },
                    new int[,] { { -1 }, { 0 } },
                    new int[,] { { -1 }, { 1 } },
                    new int[,] { { -2 }, { 0 } },
                };

		public List<int[,]> AllPossibleMovesOldInverted { get; private set; }
		*/


		public List<BoardLocation> AllPossibleMoves { get; } =
				new List<BoardLocation>
				{

				};
		public List<BoardLocation> AllPossibleMovesInverted { get; private set; }


		internal Bishop(BoardLocation InitialPosition)
		{
			Position = InitialPosition;

			// Set Inverted Moves
			AllPossibleMovesInverted = new List<BoardLocation>();
			foreach (BoardLocation old in AllPossibleMoves)
				AllPossibleMovesInverted.Add(old.Mirror());
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

	// TODO Split Turn in Turn and Gamestate

	public interface Test1
	{

	}

	public struct Test2 : Test1
	{

	}
}
