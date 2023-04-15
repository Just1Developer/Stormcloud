using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1
{
	internal class Catfish
	{

		public static int LookForChecks(Chessboard Chessboard, Turn Color)
		{
			if (!Chessboard.ScanForChecks) return -4;
			if (Color != Turn.White && Color != Turn.Black) return -1;

			int KingSquare = -3;
			// First, Get the king square
			foreach (int field in Chessboard.Pieces.Keys)
			{
				if (Chessboard.GetPieceColor(field) != Color) continue;
				if (Chessboard.IsTypeOf(field, PieceType.KING)) { KingSquare = field; break; }
			}
			if (KingSquare == -3) return -2;

			foreach (int field in Chessboard.Pieces.Keys)
			{
				// Check Legal moves of opponents
				if (Chessboard.GetPieceColor(field) == Color) continue;
				if (Chessboard.IsTypeOf(field, PieceType.QUEEN))
					if (GetLegalMoves(field, Chessboard).Contains(KingSquare))
						return KingSquare;
			}

			return -3;
		}

		#region Legal Moves

		#region Add Legal Moves

		// Adds the move under a given a condition but only when it's in bounds; Covers Self-Taking
		private static List<int> AddLegalMove(List<int> CurrentLegalMoves, int field, Func<int, bool> Condition, Chessboard Chessboard)
		{
			if (Condition(field) && field >= 0 && field < 64 && !(Chessboard.IsOwnPiece(field) && !Chessboard.AllowSelfTakes)) CurrentLegalMoves.Add(field);
			return CurrentLegalMoves;
		}
		private static List<int> AddLegalMove(List<int> CurrentLegalMoves, int field, Chessboard Chessboard)
		{
			if (field >= 0 && field < 64 && !(Chessboard.IsOwnPiece(field) && !Chessboard.AllowSelfTakes)) CurrentLegalMoves.Add(field);
			return CurrentLegalMoves;
		}
		private static List<int> AddLegalMove(List<int> CurrentLegalMoves, BoardLocation BoardPosition, BoardLocation DeltaBoardPosition, Chessboard Chessboard)
		{
			BoardPosition.Add(DeltaBoardPosition);
			if (!BoardPosition.Illegal && !(Chessboard.IsOwnPiece(BoardPosition.Value) && !Chessboard.AllowSelfTakes)) CurrentLegalMoves.Add(BoardPosition.Value);
			return CurrentLegalMoves;
		}

		private static List<int> AddLegalMovesInDirection(List<int> Moves, int currentField, int delta, Chessboard Chessboard)
			=> AddLegalMovesInDirection(Moves, currentField, new BoardLocation(delta), Chessboard);
		private static List<int> AddLegalMovesInDirection(List<int> Moves, int currentField, BoardLocation deltaPos, Chessboard Chessboard)
		{
			BoardLocation currentPosition = new BoardLocation(currentField);

			while (currentField > 0 && currentField < 64)
			{
				// First next field
				currentPosition.Add(deltaPos);
				// Check if it's legal, if not cancel the loop
				if (Chessboard.IsOwnPiece(currentPosition.Value) || currentPosition.Illegal) break;
				// Now add the move
				Moves = AddLegalMove(Moves, currentPosition.Value, move => !Chessboard.IsOwnPiece(move), Chessboard);
				// If there is an opponent piece on there, cancel now
				if (Chessboard.IsOpponentPiece(currentPosition.Value)) break;
			}
			return Moves;
		}

		#endregion

		// Looks confusing, just check GetLegalMovesNormal in Chessboard.cs
		private static List<int> GetLegalMoves(int field, Chessboard Chessboard)
		{
			Dictionary<int, PieceType> Pieces = Chessboard.Pieces;
			List<int> Moves = new List<int>();
			if (!Pieces.ContainsKey(field)) return Moves;

			PieceType Piece = Pieces[field];
			string piecetype = Piece.ToString().ToLower();

			bool invert = !Chessboard.EnableFlipBoard && Chessboard.Turn == Turn.Black;
			int Up = invert ? 8 : -8;
			int Down = invert ? -8 : 8;
			//int UpLeft = invert ? 9 : -9;
			BoardLocation UpLeft = invert ? new BoardLocation(1, 1) : new BoardLocation(-1, -1);
			//int UpRight = invert ? 7 : -7;
			BoardLocation UpRight = invert ? new BoardLocation(1, -1) : new BoardLocation(-1, 1);
			//int DownLeft = invert ? -7 : 7;
			BoardLocation DownLeft = invert ? new BoardLocation(-1, 1) : new BoardLocation(1, -1);
			//int DownRight = invert ? -9 : 9;
			BoardLocation DownRight = invert ? new BoardLocation(-1, -1) : new BoardLocation(1, 1);
			int Left = invert ? 1 : -1;
			int Right = invert ? -1 : 1;

			if (piecetype == "pawn")  // TODO pawns can queen
			{       // TODO en passant
				Moves = AddLegalMove(Moves, field + Up, move => Chessboard.GetPieceType(move) == PieceType.Empty, Chessboard);
				Moves = AddLegalMove(Moves, field + UpLeft.Value, move => Chessboard.IsOpponentPiece(move) || (Chessboard.Turn == Turn.White ? Chessboard.EnPassantBlack : Chessboard.EnPassantWhite).Contains(move), Chessboard);
				Moves = AddLegalMove(Moves, field + UpRight.Value, move => Chessboard.IsOpponentPiece(move) || (Chessboard.Turn == Turn.White ? Chessboard.EnPassantBlack : Chessboard.EnPassantWhite).Contains(move), Chessboard);

				Moves = AddLegalMove(Moves, field + Up + Up, move => { return Chessboard.GetPieceType(move + Down) == PieceType.Empty && Chessboard.GetPieceType(move) == PieceType.Empty && /*Pawn not moved*/((!Chessboard.EnableFlipBoard && Chessboard.Turn == Turn.Black && field / 8 == 1) || field / 8 == 6); }, Chessboard);
			}
			else if (piecetype == "king")   // Todo get if king is in check, castle
			{
				Func<int, bool> Condition = move => { return (Chessboard.IsOpponentPiece(move) && Chessboard.ChessMode != ChessMode.Atomic) || Chessboard.GetPieceType(move) == PieceType.Empty; };

				Moves = AddLegalMove(Moves, field + UpLeft.Value, Condition, Chessboard);
				Moves = AddLegalMove(Moves, field + Up, Condition, Chessboard);
				Moves = AddLegalMove(Moves, field + UpRight.Value, Condition, Chessboard);
				Moves = AddLegalMove(Moves, field + Left, Condition, Chessboard);
				Moves = AddLegalMove(Moves, field + Right, Condition, Chessboard);
				Moves = AddLegalMove(Moves, field + DownLeft.Value, Condition, Chessboard);
				Moves = AddLegalMove(Moves, field + Down, Condition, Chessboard);
				Moves = AddLegalMove(Moves, field + DownRight.Value, Condition, Chessboard);

				int CastleShort = Chessboard.Turn == Turn.White ? Right : Left;
				if ((Chessboard.CastleAvailability[Chessboard.Turn] == CastleOptions.Short || Chessboard.CastleAvailability[Chessboard.Turn] == CastleOptions.Both) &&
					Chessboard.GetPieceType(field + CastleShort) == PieceType.Empty && Chessboard.GetPieceType(field + CastleShort * 2) == PieceType.Empty) Moves = AddLegalMove(Moves, field + CastleShort * 2, Chessboard);
				// CastleLong = -CastleShort
				if ((Chessboard.CastleAvailability[Chessboard.Turn] == CastleOptions.Long || Chessboard.CastleAvailability[Chessboard.Turn] == CastleOptions.Both) &&
					Chessboard.GetPieceType(field - CastleShort) == PieceType.Empty && Chessboard.GetPieceType(field - CastleShort * 2) == PieceType.Empty && Chessboard.GetPieceType(field - CastleShort * 3) == PieceType.Empty)
					Moves = AddLegalMove(Moves, field - CastleShort * 2, Chessboard);
			}
			else if (piecetype == "rook")
			{
				Moves = AddLegalMovesInDirection(Moves, field, Up, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, Down, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, Left, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, Right, Chessboard);
			}
			else if (piecetype == "bishop")
			{
				Moves = AddLegalMovesInDirection(Moves, field, UpLeft, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, UpRight, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, DownLeft, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, DownRight, Chessboard);
			}
			else if (piecetype == "queen")
			{
				Moves = AddLegalMovesInDirection(Moves, field, Up, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, Down, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, Left, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, Right, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, UpLeft, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, UpRight, Chessboard);    // Upright = Special
				Moves = AddLegalMovesInDirection(Moves, field, DownLeft, Chessboard);
				Moves = AddLegalMovesInDirection(Moves, field, DownRight, Chessboard);
			}
			else if (piecetype == "knight")
			{
				BoardLocation current = new BoardLocation(field);
				Moves = AddLegalMove(Moves, current, new BoardLocation(-2, 1), Chessboard);
				Moves = AddLegalMove(Moves, current, new BoardLocation(-2, -1), Chessboard);
				Moves = AddLegalMove(Moves, current, new BoardLocation(2, 1), Chessboard);
				Moves = AddLegalMove(Moves, current, new BoardLocation(2, -1), Chessboard);
				Moves = AddLegalMove(Moves, current, new BoardLocation(1, 2), Chessboard);
				Moves = AddLegalMove(Moves, current, new BoardLocation(1, -2), Chessboard);
				Moves = AddLegalMove(Moves, current, new BoardLocation(-1, 2), Chessboard);
				Moves = AddLegalMove(Moves, current, new BoardLocation(-1, -2), Chessboard);
			}

			// TODO Legal Moves
			return Moves;
		}

		#endregion
	}
}
