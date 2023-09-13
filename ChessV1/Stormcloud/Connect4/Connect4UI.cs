using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChessV1.Stormcloud.Connect4
{
	public partial class Connect4UI : Form
	{
		private long BoardstateYellow, BoardstateRed;	// Use last 9 bits only

		private Turn _Turn = Turn.Yellow;
		private Label l_message;

		private bool playComputer = false;
		private bool computerStarts = false;
		private bool computerSolo = false;

		private long WinnerMatrix;

		private Connect4Engine Engine;
		private const int ENGINE_DEPTH = 8;// apparently some maxDepth is actually needed -1; "Strong" would be 10-11, fast is 7 (no)

		private readonly int BoardWidth, BoardHeight;

		private long Boardstate
		{
			get => (long) (BoardstateRed | BoardstateYellow);
		}

		public readonly int Rows, Columns, BOARD_MAX;

		public Connect4UI(int Rows, int Columns)
		{
			InitializeComponent();
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.MouseUp += (s, e) => ClickMouse(e.Location);
			this.AllowTransparency = true;
			this.MouseMove += (s, e) => MouseMoved(e.Location);

			// So currently there is no solution for a board that doesn't fit into a single 64-bit integer
			if (Rows * Columns > 64) { Console.Error.WriteLine("Error: Field larger than 64 fields!"); throw new Exception(); }


			// Set up the game visuals & dimensions
			this.Rows = Rows;
			this.Columns = Columns;
			this.BOARD_MAX = Rows * Columns - 1;	// Because we count 0 too, we need to reduce the entire thing by 1 to account for 0
			this.BoardWidth = (1 + Columns) * Spacing + Columns * CircleSize;
			this.BoardHeight = (1 + Rows) * Spacing + Rows * CircleSize;
			this.Size = new System.Drawing.Size(2 * padding + BoardWidth + 5 * CircleSize, 2 * padding + BoardHeight);

			SetMasks();
			Engine = new Connect4Engine(Rows, Columns, ENGINE_DEPTH, MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2);


			l_message = new Label()
			{
				Font = new Font(FontFamily.GenericSansSerif, 65f),
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = Color.Coral,
				Visible = false,
				BackColor = Color.FromArgb(100, 200, 200, 200),
			};
			l_message.Bounds = new Rectangle(0, 2 * padding + CircleSize, Width, 2*CircleSize);
			l_message.MouseUp += (s, e) => restart();   // Guaranteed visible -> Game is over -> restart
			Controls.Add(l_message);

			Button b2 = new Button()
			{
				Bounds = new Rectangle(padding + BoardWidth + 2 * Spacing,
					padding + 2 * CircleSize + 4 * Spacing, 120, 80),
				Text = "Play Next Move",
				FlatStyle = FlatStyle.Flat,
				Font = new Font(FontFamily.GenericSansSerif, 15f)
			};
			b2.Click += (s, e) => { PlayNextMoveComputer(); };
			Controls.Add(b2);

			Button b = new Button()
			{
				Bounds = new Rectangle(padding + BoardWidth + 2 * Spacing,
					b2.Location.Y + b2.Height + Spacing, 120, 80),
				Text = "Computer Solo",
				FlatStyle = FlatStyle.Flat,
				Font = new Font(FontFamily.GenericSansSerif, 15f)
			};
			b.Click += (s, e) => { computerSolo = true; foreach (Control c in Controls) { if (c.GetType().IsSubclassOf(typeof(ButtonBase))) c.Enabled = false; } PlayNextMoveComputer(); };
			Controls.Add(b);

			CheckBox cb = new CheckBox()
			{
				Location = new Point(padding + BoardWidth + 2 * Spacing,
					padding + CircleSize + 3 * Spacing),
				Text = "Computer Play",
				AutoSize = true,
				FlatStyle = FlatStyle.Flat,
				Font = new Font(FontFamily.GenericSansSerif, 15f)
			};
			cb.CheckedChanged += (s, e) =>
			{
				playComputer = cb.Checked;
				if (!playComputer) return;
				if (_Turn == Turn.Yellow) computerStarts = true;
				else computerStarts = false;
				PlayNextMoveComputer();
			};
			cb.Checked = playComputer;
			Controls.Add(cb);

			this.Load += (s, e) =>
			{
				if (playComputer && computerStarts)
				{
					System.Threading.Thread.Sleep(1000);
					PlayNextMoveComputer();
				}
			};
		}

		void restart()
		{
			_Turn = Turn.Yellow;
			BoardstateYellow = 0;
			BoardstateRed = 0;
			l_message.Text = "";
			l_message.Visible = false;
			computerSolo = false;
			WinnerMatrix = 0L;
			move = 1;
			foreach (Control c in Controls) { if (c.GetType().IsSubclassOf(typeof(ButtonBase))) c.Enabled = true; }
			Refresh();
		}

		private void ClickMouse(Point Location)
		{
			if (computerSolo) return;
			if (playComputer)
			{
				if (computerStarts && _Turn == Turn.Yellow)
					return;
				if (!computerStarts && _Turn == Turn.Red)
					return;
			}

			if (l_message.Text != "")
			{
				// Game is over, so restart:
				restart();
				return;
			}

			if (Location.Y < Connect4UI.padding + Spacing || Location.Y > Connect4UI.padding + BoardHeight - Spacing) return;

			// Get Field
			int pair = CircleSize + Spacing;
			int padding = Connect4UI.padding + Spacing;
			int x = Location.X - padding;
			if (x < 0) return;
			if (x % pair > CircleSize) return;	// On the divider part
			int col = Columns - 1 - x / pair;

			Console.WriteLine($"Clicked on col {col}");

			// Check what row has the slot there, so start from the bottom and check where the first is
			// Delta is always the amount of columns

			int row = -1, reverseIndex = -1;
			long s = Boardstate;
			for (int i = 0; i < Rows; i++)
			{
				// Slot
				reverseIndex = i * Columns + col;    // Index is where it is, reverse it how much shift I need to bring it to the back
				if (((s >> reverseIndex) & 1) == 1) continue;
				row = i;
				break;
			}

			if (row == -1) return;

			// There is a free field at [row,col]

			play(reverseIndex);
		}

		int highlightedCol = -1;
		int HighlightedCol { get => highlightedCol; set { if (value == highlightedCol) return; highlightedCol = value; Refresh(); } }

		public void MouseMoved(Point newLocation)
		{
			if (newLocation.Y < Connect4UI.padding + Spacing || newLocation.Y > Connect4UI.padding + BoardHeight - Spacing) { HighlightedCol = -1;  return; }

			// Get Field
			int pair = CircleSize + Spacing;
			int padding = Connect4UI.padding + Spacing;
			int x = newLocation.X - padding;
			if (x < 0) { HighlightedCol = -1; return; }
			if (x % pair > CircleSize) { HighlightedCol = -1; return; }  // On the circle part

			HighlightedCol = x / pair;
		}

		// If the time for the move calculated is less than that, it will start a new calculation at higher depth.
		// Careful, as once the new calc starts, it cannot be stopped, which means...
		private const int maxMSEngine = 2000;
		// perhaps add a deadline into the engine and it just refuses method calls and immediately returns upon call and score setting inside the loop (skips bestmove)
		// For this we would need a temp bestmove and a sign if it was cancelled, though.

		void PlayNextMoveComputer()
		{
			if(Engine.IsInUse)
			{
				Console.Error.WriteLine("Error: Engine Object is in use right now, please wait until calculations are complete or create another instance.");
				return;
			}
			Move bestMove = _Turn == Turn.Yellow ? Engine.BestMove(BoardstateYellow, BoardstateRed, maxMS: maxMSEngine)
				: Engine.BestMove(BoardstateRed, BoardstateYellow, maxMS: maxMSEngine);
			
			Console.WriteLine($"   {move}. [{_Turn}] Best Column: {bestMove.Column}, Row {bestMove.Row}, Eval: {bestMove.EvaluationResult} ({bestMove.Eval}) | Time: {bestMove.TimeSecs}s | Binary Move: {Convert.ToString(bestMove.BinaryMove, 2)}-");
			play(bestMove.BinaryMove);
		}

		public static int move = 1;

		void play(int reverseIndex) => play(1L << reverseIndex);
		void play(long ORmove)
		{
			if (_Turn == Turn.Yellow)
			{
				BoardstateYellow |= ORmove;
				_Turn = Turn.Red;
			}
			else
			{
				BoardstateRed |= ORmove;
				_Turn = Turn.Yellow;
			}

			int result = PlayerWon();
			switch (result)
			{
				case -1: break;
				case 0: l_message.Text = "Draw!"; l_message.Visible = true; break;
				case 1: l_message.Text = "Player 1 wins!"; l_message.Visible = true; break;
				case 2: l_message.Text = "Player 2 wins!"; l_message.Visible = true; break;
			}

			Refresh();
			if (result >= 0) return;

			if (computerSolo) PlayNextMoveComputer();
			else if (playComputer)
				if(_Turn == Turn.Yellow && computerStarts || _Turn == Turn.Red && !computerStarts)
					PlayNextMoveComputer();
		}

		#region Render UI

		const int CircleSize = 70;
		const int Spacing = 40;
		const int padding = 60;

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.FillRectangle(Brushes.Blue, padding, padding, BoardWidth, BoardHeight);

			Brush ColorOf(int row, int col)
			{
				// CurrentRow * ColumnAmount (RowWidth) + CurrentColumn
				int i = BOARD_MAX - (row * Columns + col); // (reverse for backwards shift)
				if (((BoardstateYellow >> i) & 1) == 1) return Brushes.Yellow;
				if (((BoardstateRed >> i) & 1) == 1) return Brushes.Red;
				if (col == HighlightedCol) return Brushes.Wheat;
				return Brushes.Beige;
			}

			// Draw the Circles
			for (int row = 0; row < Rows; row++)
			{
				for (int col = 0; col < Columns; col++)
				{
					g.FillEllipse(ColorOf(row, col), padding + (1+col) * Spacing + col * CircleSize, padding + (1 + row) * Spacing + row * CircleSize, CircleSize, CircleSize);
					int i = BOARD_MAX - (row * Columns + col);
					if(((WinnerMatrix >> i) & 1) == 1) g.DrawEllipse(new Pen(Brushes.Coral, 10f), padding + (1 + col) * Spacing + col * CircleSize, padding + (1 + row) * Spacing + row * CircleSize, CircleSize, CircleSize);
				}
			}

			base.OnPaint(e);
		}

		#endregion

		#region Sophisticated Win Detection

		int PlayerWon() => Connect4Engine.PlayerWon2(BoardstateYellow, BoardstateRed, Rows, Columns, MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2, ref WinnerMatrix);

		private const short MASK_ROW = 0x000F; // 1111 | just shift around as needed
		private static long MASK_COL;
		private static long MASK_DIAG1;
		private static long MASK_DIAG2;

		// Masks need to be set because anything transcending own rows need to know how wide a row is
		void SetMasks()
		{
			int cols = Columns;	// Leave space for the 1 in the column
			// Basically: value is 1, shift so much across << that new line, then add 1 again, shift until 4
			MASK_COL = (((((1 << cols) | 1) << cols) | 1) << cols) | 1;

			// Now do the same for +/- 1 each time for the diagnonal
			cols--; // Shift 1 less for the diagonal going upwards L-R. Only thing is this will need leading 0s, but it's got that. Just don't forget
			// We skipped 3 0s that are quite essential here (I think), so let's add them to the back
			MASK_DIAG1 = ((((((1 << cols) | 1) << cols) | 1) << cols) | 1) << 3;

			cols += 2;
			// Now to +1, since Columns is +1, we can just use that instead
			MASK_DIAG2 = (((((1 << cols) | 1) << cols) | 1) << cols) | 1;


			MASKS = new[] { MASK_ROW, MASK_COL, MASK_DIAG1, MASK_DIAG2 };
		}

		private long[] MASKS;
		#endregion

		enum Turn
		{
			Yellow, Red
		}
	}
}