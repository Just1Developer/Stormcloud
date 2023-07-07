using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessV1.Stormcloud
{
	/// <summary>
	/// Engine Calculation using the TQA Approach from Chessboard2.cs and Alpha-Beta Pruning
	/// </summary>
	internal partial class Stormcloud3	// Evaluation
	{
		#region Evaluation Weights



		#endregion

		#region Debug_DeleteMe_Unsafe

		public Stormcloud3()
		{
			byte[] position = {
				0xCA, 0xBD, 0xEB, 0xAC,
				0x99, 0x99, 0x99, 0x99,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x11, 0x11, 0x11, 0x11,
				0x42, 0x35, 0x63, 0x24
			};
			double Test_Eval = PositionEvaluation(position, new PositionData() { PositionKey = "", Turn = EvaluationResultWhiteTurn }).Score;
			Console.WriteLine("C >> Test Eval mat: " + Test_Eval);
			System.Diagnostics.Debug.WriteLine("D >> Test Eval mat: " + Test_Eval);
		}

		#endregion

		struct EvaluationResult
		{
			public double Score;
			public byte Result;
			public EvaluationResult(double score, byte evalResult)
			{
				this.Score = score;
				this.Result = evalResult;
			}
		}

		EvaluationResult PositionEvaluation(SearchNode Node) => PositionEvaluation(Node.Position, Node.PositionData);
		EvaluationResult PositionEvaluation(byte[] Position, PositionData PositionData)
		{
			double score = 0;

			byte result = (byte)~PositionData.Turn;	// Invert previous turn byte as default result

			bool GameOver = (result & EvalResultGameOverMask) == EvalResultGameOverMask;	// 0110 or 1001 is for turns, so we need to actually check for 1100
			bool Draw = GameOver && ((result & EvalResultDrawMask) == EvaluationResultDraw);
			if (Draw) result = EvaluationResultDraw;
			else if(GameOver) result = (result & EvalResultWhiteMask) != 0 ? EvaluationResultWhiteWon : EvaluationResultBlackWon;

			// ...

			double materialAdvantage = (PositionData.Turn & 0x0F) != 0 ? MaterialEvaluation(Position) : -MaterialEvaluation(Position);

			score += materialAdvantage;

			return new EvaluationResult(score, result);
		}

		/**
		 * Evaluation Results work like this:
		 * First and second bit 1: Game Over
		 *   - Third and fourth bit 0 -> Game Over by draw
		 *   - Third and fourth bit 1 -> Game Over by win
		 * Else:
		 * First and Second bits are 0
		 * Last 4 bits 1: Whites Turn (Also for Game over -> White wins)
		 * Last 4 bits 0: Blacks Turn (Also for Game over -> Black wins)
		 */

		const byte EvalResultGameOverMask = 0xC0; /// 11000000 => 1100 => 12 => C
		const byte EvalResultDrawMask = 0xF0; /// 11110000, but result needs to be 1100 in the first 4 bits. Its just that all 4 bits matter here
		const byte EvalResultWhiteMask = 0x0F; /// 00001111 => 1111 => 15 => F

		public const byte EvaluationResultWhiteTurn = 0x8F;    /// 10011111	(bits 3 and 4 don't matter here, this is just for inverse)
		public const byte EvaluationResultBlackTurn = 0x40;    /// 01101111	(bits 3 and 4 don't matter here, this is just for inverse)
		const byte EvaluationResultWhiteWon = 0xFF;		/// 11111111
		const byte EvaluationResultBlackWon = 0xF0;		/// 11110000
		const byte EvaluationResultDraw = EvalResultGameOverMask;	/// 11000000, but result  Maybe add types of draw later, but it doesnt really matter

		const byte firstHalfMask = 0xF0;	/// First meaning the first 4 bits from the left
		const byte secondHalfMask = 0x0F;

		// OG: private
		public static double MaterialEvaluation(byte[] Position)
		{
			double score = 0.0;
			foreach (byte doublePiece in Position)
			{
				// byte piece1 = (byte)(doublePiece & firstHalfMask);
				// byte piece2 = (byte) (doublePiece & secondHalfMask);

				if (IsWhitePieceFirstHalf(doublePiece)) score += BytePieceValue((byte) ((doublePiece & firstHalfMask) >> 4));    // Shift by 4 to shift bits to second half
				else score -= BytePieceValue((byte) ((doublePiece & firstHalfMask) >> 4));

				if (IsWhitePieceSecondHalf(doublePiece)) score += BytePieceValue((byte) (doublePiece & secondHalfMask));
				else score -= BytePieceValue((byte) (doublePiece & secondHalfMask));
			}
			return score;
		}
	}

	partial class Stormcloud3	// Search Algorithm
	{
		private ConcurrentQueue<SearchNode> SearchNodes = new ConcurrentQueue<SearchNode>();	// We're using a queue so that we can use just one, right?
		private ConcurrentDictionary<byte[], double> Hard_InitialMoveScores = new ConcurrentDictionary<byte[], double>();	// Finished Depth
		private ConcurrentDictionary<byte[], double> Temp_InitialMoveScores = new ConcurrentDictionary<byte[], double>();   // Calculating (Live)

		private ConcurrentDictionary<string, double> PositionDataCacheDirectEvaluation = new ConcurrentDictionary<string, double>();

		/*
		private ConcurrentQueue<SearchNode> NodeQueueOne = new ConcurrentQueue<SearchNode>();
		private ConcurrentQueue<SearchNode> NodeQueueTwo = new ConcurrentQueue<SearchNode>();
		private byte CurrentQueue = 0;
		private SearchNode GetNode()
		{
			SearchNode Node = null;
			if(CurrentQueue == 0 || NodeQueueTwo.Count == 0) NodeQueueOne.TryDequeue(out Node);
			else if(CurrentQueue == 1 || NodeQueueOne.Count == 0) NodeQueueTwo.TryDequeue(out Node);
			return Node;
		}
		*/

		private byte[] StartPosition;
		private Turn StartTurnColor;

		public Stormcloud3(byte[] Position, Turn CurrentTurnColor)
		{
			this.StartPosition = Position;
			this.StartTurnColor = CurrentTurnColor;
		}

		// ToDo Actually process position keys and values and stuff

		private int TargetDepth, CurrentDepth;

		private void StartProcessingMultiThread()
		{

		}

		private void StartProcessingNodesSingleThread()
		{
			while(SearchNodes.Count > 0 && CurrentDepth <= TargetDepth)
			{
				ProcessNextNode();
			}
		}

		private void ProcessNextNode()
		{
			SearchNode node;
			SearchNodes.TryDequeue(out node);

			var AllNextOpponentMovesAndPositions = GetAllLegalMoveAndResultingPositionPairs(node.Position);
			var OpponentMoveScores = new Dictionary<byte[], double>();//new List<KeyValuePair<byte[], double>>();
			var OpponentMoveFollowUps = new Dictionary<byte[], List<byte[]>>();

			// Evaluate all Opponent moves:

			foreach (var move in AllNextOpponentMovesAndPositions)
			{
				double score = 0;
				var moves = new List<byte[]>();
				foreach (var pos in GetAllLegalMoveAndResultingPositionPairs(move.Result))
				{
					score += PositionEvaluation(pos.Result, new PositionData()).Score;
					moves.Add(pos.Move);
				}
				OpponentMoveScores.Add(move.Move, score);
				OpponentMoveFollowUps.Add(move.Move, moves);
			}


			/** Top 3 Moves:
			List<Move> topMoves = OpponentMoveScores
				.OrderByDescending(pair => pair.Value)
				.Take(3)
				.Select(pair => pair.Key)
				.ToList();*/
			// No order by descending because O(n) is better than O(n²)

			byte[] bestMove = { 0, 0 };
			double maxScore = double.NegativeInfinity;

			foreach (var pair in OpponentMoveScores)
			{
				if (pair.Value > maxScore)
				{
					maxScore = pair.Value;
					bestMove = pair.Key;
				}
			}

			// Move with highest score is at index 0
			SearchNode OpponentNode = node.Result(bestMove);   // Perhaps use an implementation where the already saved new position is used.

			// Now get and enqueue all new stuff
			foreach (var moves in OpponentMoveFollowUps[bestMove])
			{
				SearchNode node2 = OpponentNode.Result(moves);
				// ToDo Cache + Eval + PositionData object rework
				if(!PositionDataCacheDirectEvaluation.ContainsKey(node2.PositionData.PositionKey))
					SearchNodes.Enqueue(node2);
			}
		}

		/// <summary>
		/// A list of all legal moves based on the provided position,<br/>
		/// paired with their respective resulting position. <br/> <br/>
		/// First is the move: [0] = From | [1] = To. <br/>
		/// Indexes are 64-based. <br/>
		/// Second is the new position: [0-31]: double-piece byte.
		/// </summary>
		/// <param name="Position"> The Position, a size 32 byte array. </param>
		/// <returns> List of all legal moves paired with their resulting position. </returns>
		private List<MoveResultingPositionPair> GetAllLegalMoveAndResultingPositionPairs(byte[] Position)
		{
			var movePairs = new List<MoveResultingPositionPair>();
			foreach (byte[] move in GetAllLegalMoves(Position))
			{
				movePairs.Add(new MoveResultingPositionPair(move, ResultingPosition(Position, move)));
			}
			return movePairs;
		}

		/// <summary>
		/// A list of all legal moves based on the provided position. <br/>
		/// Each Move: [0] = From | [1] = To. <br/>
		/// Indexes are 64-based.
		/// </summary>
		/// <param name="Position"> The Position, a size 32 byte array. </param>
		/// <returns> List of all legal moves. </returns>
		private List<byte[]> GetAllLegalMoves(byte[] Position)				// Todo perhaps discard of this? Or move part of the other function in here
		{
			var moves = new List<byte[]>();

			return moves;
		}
	}

	struct MoveResultingPositionPair
	{
		public byte[] Move; // key
		public byte[] Result; // value

		public MoveResultingPositionPair(byte[] move, byte[] result)
		{
			Move = move;
			Result = result;
		}
	}

	class SearchNode
	{
		internal byte[] Position;    // See position binary data docs
		// Position data
		byte castle;
		internal PositionData PositionData;

		public bool WhiteCastleKingside() => (castle & (1 << 0)) != 0; // Check if the 1st bit is set
		public bool WhiteCastleQueenside() => (castle & (1 << 1)) != 0; // Check if the 2nd bit is set
		public bool BlackCastleKingside() => (castle & (1 << 2)) != 0; // Check if the 3rd bit is set
		public bool BlackCastleQueenside() => (castle & (1 << 3)) != 0; // Check if the 4th bit is set

		// Set castle
		public void SetWhiteCastleKingside(bool canCastle) => castle = (byte)(canCastle ? (castle | (1 << 0)) : (castle & ~(1 << 0)));
		public void SetWhiteCastleQueenside(bool canCastle) => castle = (byte)(canCastle ? (castle | (1 << 1)) : (castle & ~(1 << 1)));
		public void SetBlackCastleKingside(bool canCastle) => castle = (byte)(canCastle ? (castle | (1 << 2)) : (castle & ~(1 << 2)));
		public void SetBlackCastleQueenside(bool canCastle) => castle = (byte)(canCastle ? (castle | (1 << 3)) : (castle & ~(1 << 3)));

		public SearchNode(byte[] Position)
			: this(Position,
				  new PositionData(Stormcloud3.EvaluationResultWhiteTurn, Stormcloud3.GeneratePositionKey(Position))
				{
					// ToDo Auto-Generate Position Data
				})
		{ }
		public SearchNode(byte[] Position, PositionData PositionData)
		{
			if (Position == null) this.Position = new byte[32];
			else this.Position = Position;
			this.PositionData = PositionData;
		}
		public SearchNode(byte[] Position, byte Turn, string PositionKey)
		{
			if (Position == null) this.Position = new byte[32];
			else this.Position = Position;
			this.PositionData = new PositionData(Turn, PositionKey);
		}

		public SearchNode Result(byte[] move)
		{
			byte[] newPosition = Stormcloud3.ResultingPosition(Position, move);
			PositionData newData = PositionData.Next(newPosition);
			return new SearchNode(newPosition, newData);
		}
	}
	
	internal struct PositionData
	{
		public byte Turn;
		public string PositionKey;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Turn"></param>
		public PositionData(byte Turn, string PositionKey)
		{
			this.Turn = Turn;
			this.PositionKey = PositionKey;
		}

		public PositionData Next(byte[] newPosition) => Next(Stormcloud3.GeneratePositionKey(newPosition));
		public PositionData Next(string newPositionKey)
		{
			PositionData data = new PositionData((byte) ~this.Turn, newPositionKey);
			return data;
		}
	}

	partial class Stormcloud3	// Piece Values and Legal moves
	{


		#region Legal Moves

		public static byte[] GetLegalMovesPiece(byte[] position, byte pieceLocationIndex)
		{
			byte piece = position[pieceLocationIndex >> 1];
			bool isPieceWhite = false;
			if ((pieceLocationIndex & 1) == 1)
			{
				isPieceWhite = (piece & 0x08) == 0;	// 4th bit is 0
				piece &= 0x07; // Uneven index => 2nd half
				piece = (byte) (piece << 4);	// Move to first half
			}
			else
			{
				isPieceWhite = (piece & 0x80) == 0; // 4th bit is 0
				piece &= 0x70; // Even index => 1st half
			}

			// Todo look for checks
			if (piece == 0x10) return GetLegalMovesPawn(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x20) return GetLegalMovesKnight(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x30) return GetLegalMovesBishop(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x40) return GetLegalMovesRook(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x50) return GetLegalMovesQueen(position, pieceLocationIndex, isPieceWhite);
			if (piece == 0x60) return GetLegalMovesKing(position, pieceLocationIndex, isPieceWhite);

			return new byte[0];
		}


		public static byte[] GetLegalMovesPawn(byte[] position, byte pawnLocationIndex, bool isPieceWhite, byte promotionPiece = 0x90)	// 0x09 = black pawn (1001)
		{
			List<byte> legalMoves = new List<byte>();
			// This is not here.
			if (isPieceWhite) promotionPiece &= 0x70;	// 0111 mask

			

			return legalMoves.ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="knightLocationIndex">Location Index in 64-format.</param>
		/// <returns></returns>
		public static byte[] GetLegalMovesKnight(byte[] position, byte knightLocationIndex, bool isPieceWhite)
		{
			byte[] possibleMoves = KnightPossibleMoves[knightLocationIndex];
			//																							mask is important so we only judge the color of the pony now
			// What? ->						div index by 2		get last bit to see if first half or 2nd	bit == 1 => uneven index => second half, else first half						=> this works because the other half is definetly 0000 bcs mask
			//																																	then, when that masked with 10001000 (first and 2nd half black) yields 0, that means the horse is white.
			// bool IsHorseWhite = (position[knightLocationIndex >> 1] & ((knightLocationIndex & 0x01) == 0x01 ? secondHalfMask : firstHalfMask) & 0x88) == 0;

			//bool IsKnightWhite = IsWhitePiece((byte) (position[knightLocationIndex >> 1] & ((knightLocationIndex & 1) == 1 ? secondHalfMask : firstHalfMask)));
			
			//bool IsKnightWhite = IsWhitePiece(position[knightLocationIndex >> 1], (knightLocationIndex & 1) == 1);

			// This seems buggy
			// Todo

			List<byte> legalMoves = new List<byte>();
			foreach (byte possibleMove in possibleMoves)
			{
				// Check if piece is of own color
				//if((knightLocationIndex & 0x01) == 0x01 ? IsPieceWhite2ndHalf(position[move >> 1]) : IsPieceWhite1stHalf(position[move >> 1]) != IsKnightWhite)

				bool isSecondHalf = (possibleMove & 1) == 1;
				// Check if the target field is empty
				if (IsFieldEmpty(position[knightLocationIndex >> 1], isSecondHalf))
				{
					legalMoves.Add(possibleMove);
				}
				// If not, check if the colors are different. This might not be the prettiest, but its the fastest
				else if (IsWhitePiece(position[possibleMove >> 1], isSecondHalf) != isPieceWhite)
				{
					legalMoves.Add(possibleMove);
				}
			}
			return legalMoves.ToArray();
		}

		public static byte[] GetLegalMovesBishop(byte[] position, byte pawnLocationIndex, bool isPieceWhite)
		{
			List<byte> legalMoves = new List<byte>();
			return legalMoves.ToArray();
		}

		public static byte[] GetLegalMovesRook(byte[] position, byte pawnLocationIndex, bool isPieceWhite)
		{
			List<byte> legalMoves = new List<byte>();
			return legalMoves.ToArray();
		}

		public static byte[] GetLegalMovesQueen(byte[] position, byte pawnLocationIndex, bool isPieceWhite)
		{
			List<byte> legalMoves = new List<byte>();
			return legalMoves.ToArray();
		}

		public static byte[] GetLegalMovesKing(byte[] position, byte pawnLocationIndex, bool isPieceWhite)
		{
			List<byte> legalMoves = new List<byte>();
			return legalMoves.ToArray();
		}

		// Check if the first bit of each half is 0 (indicates white piece)
		static bool IsWhitePiece(byte piece) => (piece & 0x88) == 0 && (piece & 0xFF) != 0x00 /* not empty */;
		static bool IsWhitePiece(byte piece, bool isSecondHalf) => isSecondHalf ? IsWhitePieceFirstHalf(piece) : IsWhitePieceSecondHalf(piece);
		static bool IsWhitePieceFirstHalf(byte piece) => (piece & 0x80) == 0 && (piece & 0xF0) != 0;
		static bool IsWhitePieceSecondHalf(byte piece) => (piece & 0x08) == 0 && (piece & 0x0F) != 0;
		static bool IsFieldEmpty(byte piece, bool isSecondHalf) => isSecondHalf ? IsFieldEmptyFirstHalf(piece) : IsFieldEmptySecondHalf(piece);
		static bool IsFieldEmptyFirstHalf(byte piece) => (piece & 0xF0) == 0;
		static bool IsFieldEmptySecondHalf(byte piece) => (piece & 0x0F) == 0;


		#endregion

		
		internal static string GeneratePositionKey(byte[] position)
		{
			if (position == null) return "null";
			StringBuilder key = new StringBuilder(position.Length * 2);	// String += is inefficient bcs immutable strings
			foreach (byte b in position) key.Append(b.ToString("X2"));	// Since every square is accounted for and every piece has its own Hex character (4 bits), this should provide a unique key in the most effiecient way possible
			return key.ToString();
		}


		// Layout for adding pawn moves (pseudo code):
		//	if (inFront is Clear)
		//	{
		//		addMove (Walk_Forward);
		//		if (rank == 1 (0-based) AND inFront (2) is Clear)
		//		{
		//			addMove (Walk_Forward_Two);
		//		}
		//	}

		/// <summary>
		/// piece in Hex, first byte
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="move"></param>
		/// <param name="differentPiece0xX0"></param>
		/// <returns></returns>
		public static byte[] ResultingPosition(byte[] Position, byte[] move, byte piece = 0x00)
		{
			// Input Validation not needed, as this is an internal process

			byte fromIndex = (byte) (move[0] >> 1); // equivalent to move[0] / 2
			byte toIndex = (byte) (move[1] >> 1);   // equivalent to move[1] / 2

			//bool isFromSecondHalf = (move[0] & 1) == 1; // equivalent to move[0] % 2 == 1
			//bool isToSecondHalf = (move[1] & 1) == 1;   // equivalent to move[1] % 2 == 1

			byte fromByte = Position[fromIndex];
			byte toByte = Position[toIndex];

			if ((move[0] & 1) == 1)     // equivalent to move[0] % 2 == 1 => uneven number, second half is piece, erase piece, so erase 2nd half
			{
				if(piece == 0x00) piece = (byte)(fromByte & secondHalfMask);
				else piece |= (byte) ((fromByte & 0x08) << 4);	// add color bit of fromByte for piece color (2nd half since there is the piece)
				fromByte &= firstHalfMask; // Keep only first 4 bits
			}
			else
			{
				if (piece == 0x00) piece = (byte)((fromByte & firstHalfMask) << 4);
				else piece |= (byte) (fromByte & 0x80); // add color bit of fromByte for piece color (1st half since there is the piece)		1000 mask to get color bit and then do an or to maybe apply it
				fromByte &= secondHalfMask; // Keep only second 4 bits => erase first 4 bits
											//fromByte >>= bitsPerHalf; // Shift right by 4 bits to keep only first 4 bits			=> this is still from inverted version
			}

			// piece is xxxx0000

			if ((move[1] & 1) == 1)     // equivalent to move[1] % 2 == 1
			{
				toByte &= firstHalfMask; // Keep only first 4 bits
				toByte += piece;
			}
			else
			{
				toByte &= secondHalfMask; // Keep only second 4 bits => erase first 4 bits
										  //toByte >>= bitsPerHalf; // Shift right by 4 bits to keep only first 4 bits
				toByte += (byte) (piece >> 4);
			}

			byte[] newPosition = (byte[])Position.Clone();
			newPosition[fromIndex] = fromByte;
			newPosition[toIndex] = toByte;

			// remove previous en passants

			// If first bit of piece is 1, its black. So, we need to check the white rank (3), since their en passant "expires" now.
			byte mask1, mask2, start;
			if ((piece & 0x80) == 0x80)	// If piece is 1xxx, meaning black
			{
				// Set values for white rank:
				mask1 = 0x70;
				mask2 = 0x07;
				// 3rd rank is en passant black, which is 1111 or 0x0F
				start = 20;
			}
			else
			{
				// Set values for black rank:
				mask1 = 0xF0;
				mask2 = 0x0F;
				// 3rd rank is en passant black, which is 1111 or 0x0F
				start = 8;
			}
			
			for (byte i = start; i < start+4; ++i)   // Indexes 8 - 11 and 20 - 23 (3rd rank and 3rd to last rank)
			{
				byte _piece = newPosition[i];
				if ((_piece & mask1) == mask1) { newPosition[i] &= secondHalfMask; break; }	// If first half is en passant, erase the first half
				if ((_piece & mask2) == mask2) { newPosition[i] &= firstHalfMask; break; } // If second half is en passant, erase the second half
			}

			// Add en passant
			if((piece & 0x70) == 1)	// Pawn
			{
				// Its always +/- 16
				// Check if they are the same without the 16er-bit => 11101111 => 0xEF
				if((move[0] & 0xEF) == (move[1] & 0xEF) && move[0] != move[1])
				// Check if they are the same without the 16er-bit => 11110111 => 0xF7
				//if((fromByte & 0xF7) == (toByte & 0xF7) && move[0] != move[1])
				{
					// moved by 16 (2 ranks) and is a pawn => Insert en passant
					byte enPassantIndex = (byte) ((fromIndex + toIndex) >> 1);	// Average of the indexes
					
					// Mask is what we're going to keep, so if its the second half (uneven index) we want the first half masked
					if((move[0] & 1) == 1)
					{
						// Clear Index not necessary as its guaranteed empty
						// add en passant to 2nd part of the byte and add the coloring bit if piece is also that color
						newPosition[enPassantIndex] += (byte) (0x07 + ((piece >> 4) & 0x08));
					}
					else
					{
						newPosition[enPassantIndex] += (byte)(0x70 + (piece & 0x80));
					}
				}
			}

			return newPosition;
		}



		private static double BytePieceValue(byte piece2ndHalf)
			=> BytePieceValues[piece2ndHalf & 0x07];	// Only last 3 bits => Mask 00000111 => 7
		private static double[] BytePieceValues =
		{
		// Value, Representation	| Hex  | 2nd Bit Half | Hex Value | Hex Value (Black)
			0,	// Empty			| 0x00 | 000		  | 0		  | (8)
			1,	// Pawn				| 0x01 | 001		  | 1		  | 9
			3,	// Knight			| 0x02 | 010		  | 2		  | A
			3,	// Bishop			| 0x03 | 011		  | 3		  | B
			5,	// Rook				| 0x04 | 100		  | 4		  | C
			9,	// Queen			| 0x05 | 101		  | 5		  | D
			999,// King				| 0x06 | 110		  | 6		  | E
			1,	// En Passant		| 0x07 | 111		  | 7		  | F
		};


		/// <summary>
		/// KnightPossibleMoves[KnightPositionByte as index] returns AllMoveDestinationsAsBytes[] <br/>
		/// Move Destinations are a 
		/// </summary>
		public static byte[][] KnightPossibleMoves =
		{
			// To override half a byte:
			// byte originalValue = ...; // some original value
			// byte newValueForHigher4Bits = ...; // This should have lower 4 bits as 0.
			// result = (byte)((originalValue & 0x0F) | newValueForHigher4Bits) or (byte)((originalValue & 0xF0) | newValueForLower4Bits)

			// A pinned knight has no moves, an unpinned knight will always have these moves (except if there is an own piece standing there)

			//{ (byte)((0xFF & 0x0F) | newValueForHigher4Bits) },
			// From Field 0: 10, 17
			new byte[2] { 10, 17 },
			// From Field 1: 11, 16, 18
			new byte[3] { 11, 16, 18 },
			// From Field 2: 8, 12, 17, 19
			new byte[4] { 8, 12, 17, 19 },
			// From Field 3: 9, 13, 18, 20
			new byte[4] { 9, 13, 18, 20 },
			// From Field 4: 10, 14, 19, 21
			new byte[4] { 10, 14, 19, 21 },
			// From Field 5: 11, 15, 20, 22
			new byte[4] { 11, 15, 20, 22 },
			// From Field 6: 12, 21, 23
			new byte[3] { 12, 21, 23 },
			// From Field 7: 13, 22
			new byte[2] { 13, 22 },
			// From Field 8: 2, 18, 25
			new byte[3] { 2, 18, 25 },
			// From Field 9: 3, 19, 24, 26
			new byte[4] { 3, 19, 24, 26 },
			// From Field 10: 0, 4, 16, 20, 25, 27
			new byte[6] { 0, 4, 16, 20, 25, 27 },
			// From Field 11: 1, 5, 17, 21, 26, 28
			new byte[6] { 1, 5, 17, 21, 26, 28 },
			// From Field 12: 2, 6, 18, 22, 27, 29
			new byte[6] { 2, 6, 18, 22, 27, 29 },
			// From Field 13: 3, 7, 19, 23, 28, 30
			new byte[6] { 3, 7, 19, 23, 28, 30 },
			// From Field 14: 4, 20, 29, 31
			new byte[4] { 4, 20, 29, 31 },
			// From Field 15: 5, 21, 30
			new byte[3] { 5, 21, 30 },
			// From Field 16: 1, 10, 26, 33
			new byte[4] { 1, 10, 26, 33 },
			// From Field 17: 0, 2, 11, 27, 32, 34
			new byte[6] { 0, 2, 11, 27, 32, 34 },
			// From Field 18: 1, 3, 8, 12, 24, 28, 33, 35
			new byte[8] { 1, 3, 8, 12, 24, 28, 33, 35 },
			// From Field 19: 2, 4, 9, 13, 25, 29, 34, 36
			new byte[8] { 2, 4, 9, 13, 25, 29, 34, 36 },
			// From Field 20: 3, 5, 10, 14, 26, 30, 35, 37
			new byte[8] { 3, 5, 10, 14, 26, 30, 35, 37 },
			// From Field 21: 4, 6, 11, 15, 27, 31, 36, 38
			new byte[8] { 4, 6, 11, 15, 27, 31, 36, 38 },
			// From Field 22: 5, 7, 12, 28, 37, 39
			new byte[6] { 5, 7, 12, 28, 37, 39 },
			// From Field 23: 6, 13, 29, 37
			new byte[4] { 6, 13, 29, 37 },
			// From Field 24: 9, 18, 34, 41
			new byte[4] { 9, 18, 34, 41 },
			// From Field 25: 8, 10, 19, 35, 40, 42
			new byte[6] { 8, 10, 19, 35, 40, 42 },
			// From Field 26: 9, 11, 16, 20, 32, 36, 41, 43
			new byte[8] { 9, 11, 16, 20, 32, 36, 41, 43 },
			// From Field 27: 10, 12, 17, 21, 33, 37, 42, 44
			new byte[8] { 10, 12, 17, 21, 33, 37, 42, 44 },
			// From Field 28: 11, 13, 18, 22, 34, 38, 43, 45
			new byte[8] { 11, 13, 18, 22, 34, 38, 43, 45 },
			// From Field 29: 12, 14, 19, 23, 35, 39, 44, 46
			new byte[8] { 12, 14, 19, 23, 35, 39, 44, 46 },
			// From Field 30: 13, 15, 20, 36, 45, 47
			new byte[6] { 13, 15, 20, 36, 45, 47 },
			// From Field 31: 14, 21, 37, 46
			new byte[4] { 14, 21, 37, 46 },
			// From Field 32: 17, 26, 42, 49
			new byte[4] { 17, 26, 42, 49 },
			// From Field 33: 16, 18, 27, 43, 48, 50
			new byte[6] { 16, 18, 27, 43, 48, 50 },
			// From Field 34: 17, 19, 24, 28, 40, 44, 49, 51
			new byte[8] { 17, 19, 24, 28, 40, 44, 49, 51 },
			// From Field 35: 18, 20, 25, 29, 41, 45, 50, 52
			new byte[8] { 18, 20, 25, 29, 41, 45, 50, 52 },
			// From Field 36: 19, 21, 26, 30, 42, 46, 51, 53
			new byte[8] { 19, 21, 26, 30, 42, 46, 51, 53 },
			// From Field 37: 20, 22, 27, 31, 43, 47, 52, 54
			new byte[8] { 20, 22, 27, 31, 43, 47, 52, 54 },
			// From Field 38: 21, 23, 28, 44, 53, 55
			new byte[6] { 21, 23, 28, 44, 53, 55 },
			// From Field 39: 22, 29, 45, 54
			new byte[4] { 22, 29, 45, 54 },
			// From Field 40: 25, 34, 50, 57
			new byte[4] { 25, 34, 50, 57 },
			// From Field 41: 24, 26, 35, 51, 56, 58
			new byte[6] { 24, 26, 35, 51, 56, 58 },
			// From Field 42: 25, 27, 32, 36, 48, 52, 57, 59
			new byte[8] { 25, 27, 32, 36, 48, 52, 57, 59 },
			// From Field 43: 26, 28, 33, 37, 49, 53, 58, 60
			new byte[8] { 26, 28, 33, 37, 49, 53, 58, 60 },
			// From Field 44: 27, 29, 34, 38, 50, 54, 59, 61
			new byte[8] { 27, 29, 34, 38, 50, 54, 59, 61 },
			// From Field 45: 28, 30, 35, 39, 51, 55, 60, 62
			new byte[8] { 28, 30, 35, 39, 51, 55, 60, 62 },
			// From Field 46: 29, 31, 36, 52, 61, 63
			new byte[6] { 29, 31, 36, 52, 61, 63 },
			// From Field 47: 30, 37, 53, 62
			new byte[4] { 30, 37, 53, 62 },
			// From Field 48: 33, 42, 58
			new byte[3] { 33, 42, 58 },
			// From Field 49: 32, 34, 43, 59
			new byte[4] { 32, 34, 43, 59 },
			// From Field 40: 33, 35, 40, 44, 56, 60
			new byte[6] { 33, 35, 40, 44, 56, 60 },
			// From Field 51: 34, 36, 41, 45, 57, 61
			new byte[6] { 34, 36, 41, 45, 57, 61 },
			// From Field 52: 35, 37, 42, 46, 58, 62
			new byte[6] { 35, 37, 42, 46, 58, 62 },
			// From Field 53: 36, 38, 43, 47, 59, 63
			new byte[6] { 36, 38, 43, 47, 59, 63 },
			// From Field 54: 37, 39, 44, 60
			new byte[4] { 37, 39, 44, 60 },
			// From Field 55: 38, 45, 61
			new byte[3] { 38, 45, 61 },
			// From Field 56: 41, 50
			new byte[2] { 41, 50 },
			// From Field 57: 40, 42, 51
			new byte[3] { 40, 42, 51 },
			// From Field 58: 41, 43, 48, 52
			new byte[4] { 41, 43, 48, 52 },
			// From Field 59: 42, 44, 49, 53
			new byte[4] { 42, 44, 49, 53 },
			// From Field 60: 43, 45, 50, 54
			new byte[4] { 43, 45, 50, 54 },
			// From Field 61: 44, 46, 51, 55
			new byte[4] { 44, 46, 51, 55 },
			// From Field 62: 45, 47, 52
			new byte[3] { 45, 47, 52 },
			// From Field 63: 46, 53
			new byte[2] { 46, 53 }
		};
	}
}

/*
 * Old code:
 * 		private void ProcessNextNode()
		{
			SearchNode node;
			SearchNodes.TryDequeue(out node);

			var AllNextOpponentMovesAndPositions = GetAllLegalMoveAndResultingPositionPairs(node.Position);
			var OpponentMoveScores = new List<KeyValuePair<byte[], double>>();

			foreach (var movePair in AllNextOpponentMovesAndPositions)
			{
				double score = 0;
				foreach (var pos in GetAllLegalMoveAndResultingPositionPairs(movePair.Value))
				{
					score += PositionEvaluation(pos.Value, new PositionData());
				}
				OpponentMoveScores.Add(new KeyValuePair<byte[], double>(movePair.Key, score));
			}
			OpponentMoveScores.Sort((x, y) => y.Value.CompareTo(x.Value));
			// Move with highest score is at index 0
			SearchNode OpponentNode = node.Result(OpponentMoveScores[0].Key);   // Perhaps use an implementation where the already saved new position is used.

			// Now get and enqueue all new stuff
			var AllNextOwnMovesAndPositions = GetAllLegalMoveAndResultingPositionPairs(OpponentNode.Position);
			foreach (var pair in AllNextOwnMovesAndPositions)
			{
				SearchNode node2 = OpponentNode.Result(pair.Key);
				// ToDo Cache + Eval + PositionData object rework
				if(PositionDataCache.ContainsKey(node2.PositionData.PositionKey)) SearchNodes.Enqueue(node2);
			}
		}

		private List<KeyValuePair<byte[], byte[]>> GetAllLegalMoveAndResultingPositionPairs(byte[] Position)
		{
			var pos = new List<KeyValuePair<byte[], byte[]>>();
			return pos;
		}


My own, outperfomed by GPT-4:
		public SearchNode Result(byte[] Move)
		{
			// [0] = From, [1] = To
			// Position: 32 double-piece array
			byte[] index = { (byte)(Move[0] / 2), (byte) (Move[1] / 2) };
			bool[] secHalf = { Move[0] % 2 == 1, Move[1] % 2 == 1 };
			byte fromByte = Position[index[0]];
			byte toByte = Position[index[1]];
			if(secHalf[0])
			{
				// get and preserve only second 4 bits => % 16
				fromByte = (byte) (fromByte % 16);
			}
			else
			{
				// get and preserve only first 4 bits => - (%16)
				fromByte -= (byte) (fromByte % 16);
			}
			if (secHalf[1])
			{
				// get and preserve only second 4 bits => % 16
				toByte = (byte)(fromByte % 16);
			}
			else
			{
				// get and preserve only first 4 bits => - (%16)
				toByte -= (byte)(fromByte % 16);
			}
			byte[] pos = new byte[32];
			Array.Copy(Position, pos, 32);
			pos[index[0]] = fromByte;
			pos[index[1]] = toByte;
			return new SearchNode(pos);
		}



		public static Dictionary<byte, double> BytePieceValues2 = new Dictionary<byte, double>()
			{
				{ 0x00, 1 },	// White Pawn
				{ 0x01, 1 },	// White Knight
				{ 0x02, 1 },	// White Bishop
				{ 0x03, 1 },	// White Rook
				{ 0x04, 1 },	// White Queen
				{ 0x05, 1 },	// White King
				{ 0x06, 1 },	// White En Passant Pawn

				{ 0x08, 1 },	// Black Pawn
				{ 0x09, 1 },	// Black Knight
				{ 0x0A, 1 },	// Black Bishop
				{ 0x0B, 1 },	// Black Rook
				{ 0x0C, 1 },	// Black Queen
				{ 0x0D, 1 },	// Black King
				{ 0x0E, 1 },	// Black En Passant Pawn
			};

		Full Value Array:
		private static double BytePieceValue(byte piece2ndHalf) => BytePieceValues[piece2ndHalf & 0x07];	// Only last 3 bits => Mask 00000111 => 7
		private static double[] BytePieceValues =
		{
		// Value, Representation	| Hex  | 2nd Bit Half
			0,	// Empty			| 0x00 | 0000
			1,	// White Pawn		| 0x01 | 0001
			3,	// White Knight		| 0x02 | 0010
			3,	// White Bishop		| 0x03 | 0011
			5,	// White Rook		| 0x04 | 0100
			9,	// White Queen		| 0x05 | 0101
			double.PositiveInfinity,	// White King		| 0x06 | 0110
			1,	// White En Passant	| 0x07 | 0111

			0,	// Index-Filler		| 0x08 | 1000
				
			1,	// Black Pawn		| 0x09 | 1001
			3,	// Black Knight		| 0x0A | 1010
			3,	// Black Bishop		| 0x0B | 1011
			5,	// Black Rook		| 0x0C | 1100
			9,	// Black Queen		| 0x0D | 1101
			double.PositiveInfinity,	// Black King		| 0x0E | 1110
			1,	// Black En Passant	| 0x0F | 1111
		};



	internal struct PositionDataOld
	{
		List<Move2> AllLegalMoves;
		double PositionKeyEval;
		public string PositionKey;
	}

	internal struct PositionDataOlder
	{
		Turn TurnColor;
		int NodeDepth;
		double PositionKeyEval;
		string PositionKey;
	}











Old Classes:

	struct Move
	{
		internal byte From;
		internal byte To;

		public Move(byte From, byte To)
		{
			this.From = From;
			this.To = To;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			Move other = (Move)obj;
			return From == other.From && To == other.To;
		}

		public override int GetHashCode()
		{
			return From.GetHashCode() * 17 + To.GetHashCode();
		}

		// Overload == and != operators
		public static bool operator ==(Move c1, Move c2)
		{
			if (ReferenceEquals(c1, null))
			{
				return ReferenceEquals(c2, null);
			}

			return c1.Equals(c2);
		}

		public static bool operator !=(Move c1, Move c2)
		{
			return !(c1 == c2);
		}

		public override string ToString()
		{
			return $"[From: {From}, To: {To}]";	// Todo print
		}
	}
	
	// Or Define Move As KeyValuePair<Coordinate, Coordinate>
	struct Move2
	{
		Coordinate From;
		Coordinate To;

		public Move2(Coordinate From, Coordinate To)
		{
			this.From = From;
			this.To = To;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			Move2 other = (Move2)obj;
			return From == other.From && To == other.To;
		}

		public override int GetHashCode()
		{
			return From.GetHashCode() * 17 + To.GetHashCode();
		}

		// Overload == and != operators
		public static bool operator ==(Move2 c1, Move2 c2)
		{
			if (ReferenceEquals(c1, null))
			{
				return ReferenceEquals(c2, null);
			}

			return c1.Equals(c2);
		}

		public static bool operator !=(Move2 c1, Move2 c2)
		{
			return !(c1 == c2);
		}

		public override string ToString()
		{
			return $"[From: {From}, To: {To}]";
		}
	}

	struct Coordinate
	{
		public static Coordinate NullCoord = new Coordinate(-1, -1);
		public int Row { get; set; }
		public int Col { get; set; }

		public Coordinate(int row, int col)
		{
			Row = row;
			Col = col;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			Coordinate other = (Coordinate)obj;
			return Row == other.Row && Col == other.Col;
		}

		public override int GetHashCode()
		{
			return Row * 17 + Col;
		}

		// Overload == and != operators
		public static bool operator ==(Coordinate c1, Coordinate c2)
		{
			if (ReferenceEquals(c1, null))
			{
				return ReferenceEquals(c2, null);
			}

			return c1.Equals(c2);
		}

		public static bool operator !=(Coordinate c1, Coordinate c2)
		{
			return !(c1 == c2);
		}

		public override string ToString()
		{
			return $"({Row}, {Col})";
		}
	}

	enum PieceType
	{
		WhiteKing = 9999, WhiteQueen = 9, WhiteRook = 5, WhiteBishop = 3, WhiteKnight = 4, WhitePawn = 1, WhiteEnpassant = 2,
		BlackKing = 9991, BlackQueen = 19, BlackRook = 15, BlackBishop = 13, BlackKnight = 14, BlackPawn = 11, BlackEnpassant = 12
	}


		// not my best work honestly, but it'll do for now
		public static int GetPieceValue(PieceType Type)
		{
			if ((int)Type > 9000) return (int) PieceType.WhiteKing;
			int type = (int) Type;
			if (type >= 10) type -= 10;
			if (type == 4) type = 3;
			if (type == 2) type = 1;
			return type;
		}

*/