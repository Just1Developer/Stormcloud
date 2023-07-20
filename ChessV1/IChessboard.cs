using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessUI
{
	internal interface IChessboard
	{
		// Variable Implementations
		bool LegalMovesEnabled { get; set; }
		bool ScanForChecks { get; set; }
		bool AllowSelfTakes { get; set; }
		bool EnableFlipBoard { get; set; }
		ChessMode ChessMode { get; set; }
		int DisplaySize { get; set; }
		
		// Method Implementations
		void Reset();
		bool UndoLastMove();
		bool Focus();
	}
}
