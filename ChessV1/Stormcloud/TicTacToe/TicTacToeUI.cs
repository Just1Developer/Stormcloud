using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessV1.Stormcloud.TicTacToe
{
	public partial class TicTacToeUI : Form
	{
		short BoardstateBlack, BoardstateWhite;	// Use last 9 bits only

		private Turn _Turn = Turn.X;
		private Label l_message;

		private bool playComputer = false;
		private bool computerStarts = false;
		private bool computerSolo = false;

		private short Boardstate
		{
			get => (short) (BoardstateBlack | BoardstateWhite);
		}

		public TicTacToeUI()
		{
			InitializeComponent();
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.Size = new System.Drawing.Size(850, 700);
			this.MouseUp += (s, e) => ClickMouse(e.Location);
			this.AllowTransparency = true;
			l_message = new Label()
			{
				Font = new Font(FontFamily.GenericSansSerif, 65f),
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = Color.Coral,
				Visible = false,
				BackColor = Color.FromArgb(100, 200, 200, 200),
			};
			l_message.Bounds = new Rectangle(0, 2 * padding + FieldSize, Width, FieldSize);
			l_message.MouseUp += (s, e) => restart();	// Guaranteed visible -> Game is over -> restart
			Controls.Add(l_message);

			Button b = new Button()
			{
				Bounds = new Rectangle(padding + 3 * (FieldSize + Spacing) + Spacing,
					padding + 2 * FieldSize + 4 * Spacing, 120, 80),
				Text = "Computer Solo",
				FlatStyle = FlatStyle.Flat,
				Font = new Font(FontFamily.GenericSansSerif, 15f)
			};
			b.Click += (s, e) => { computerSolo = true; foreach (Control c in Controls) { if(c.GetType().IsSubclassOf(typeof(ButtonBase))) c.Enabled = false; } playNextMoveComputer(); };
			Controls.Add(b);

			Button b2 = new Button()
			{
				Bounds = new Rectangle(padding + 3 * (FieldSize + Spacing) + Spacing,
					3 * padding + FieldSize + 2 * Spacing, 120, 80),
				Text = "Play Next Move",
				FlatStyle = FlatStyle.Flat,
				Font = new Font(FontFamily.GenericSansSerif, 15f)
			};
			b2.Click += (s, e) => { playNextMoveComputer(false); };
			Controls.Add(b2);

			CheckBox cb = new CheckBox()
			{
				Location = new Point(padding + 3 * (FieldSize + Spacing) + Spacing,
					padding + FieldSize + 3 * Spacing),
				Text = "Computer Play",
				AutoSize = true,
				FlatStyle = FlatStyle.Flat,
				Font = new Font(FontFamily.GenericSansSerif, 15f)
			};
			cb.CheckedChanged += (s, e) =>
			{
				playComputer = cb.Checked;
				if (!playComputer) return;
				if (_Turn == Turn.X) computerStarts = true;
				else computerStarts = false;
				playNextMoveComputer();
			};
			cb.Checked = playComputer;
			Controls.Add(cb);

			this.Load += (s, e) =>
			{
				if (playComputer && computerStarts)
				{
					System.Threading.Thread.Sleep(1000);
					playNextMoveComputer();
				}
			};
		}

		void restart()
		{
			_Turn = Turn.X;
			BoardstateWhite = 0;
			BoardstateBlack = 0;
			l_message.Text = "";
			l_message.Visible = false;
			computerSolo = false;
			foreach (Control c in Controls) { if (c.GetType().IsSubclassOf(typeof(ButtonBase))) c.Enabled = true; }
			Refresh();
		}

		private void ClickMouse(Point Location)
		{
			if (computerSolo) return;
			if (playComputer)
			{
				if (computerStarts && _Turn == Turn.X)
					return;
				if (!computerStarts && _Turn == Turn.O)
					return;
			}

			if (l_message.Text != "")
			{
				// Game is over, so restart:
				restart();
				return;
			}

			// Get Field
			int row = -1, col = -1;
			if (Location.X > padding && Location.X < padding + FieldSize) row = 0;
			else if (Location.X > padding + FieldSize + Spacing && Location.X < padding + Spacing + 2 * FieldSize) row = 1;
			else if (Location.X > padding + 2 * (FieldSize + Spacing) && Location.X < padding + 2 * Spacing + 3 * FieldSize) row = 2;

			if (Location.Y > padding && Location.Y < padding + FieldSize) col = 0;
			else if (Location.Y > padding + FieldSize + Spacing && Location.Y < padding + Spacing + 2 * FieldSize) col = 1;
			else if (Location.Y > padding + 2 * (FieldSize + Spacing) && Location.Y < padding + 2 * Spacing + 3 * FieldSize) col = 2;

			int Field = col * 3 + row;
			if (Field < 0) return;
			if (valueOf(Field) != "") return;

			play(Field);
		}

		void playNextMoveComputer(bool delay = true)
		{
			if(delay) System.Threading.Thread.Sleep((int) (new Random().NextDouble() * 300 + 650));
			play(_Turn == Turn.X ?
				new TicTacToeEngine(BoardstateWhite, BoardstateBlack).BestMove :
				new TicTacToeEngine(BoardstateBlack, BoardstateWhite).BestMove
				);

		}

		void play(int field)
		{
			switch (field)
			{
				case 0: select1(); break;
				case 1: select2(); break;
				case 2: select3(); break;
				case 3: select4(); break;
				case 4: select5(); break;
				case 5: select6(); break;
				case 6: select7(); break;
				case 7: select8(); break;
				case 8: select9(); break;
			}

			_Turn = _Turn == Turn.X ? Turn.O : Turn.X;

			int result = playerWon();
			switch (result)
			{
				case -1: break;
				case 0: l_message.Text = "Draw!"; l_message.Visible = true; break;
				case 1: l_message.Text = "Player 1 wins!"; l_message.Visible = true; break;
				case 2: l_message.Text = "Player 2 wins!"; l_message.Visible = true; break;
			}

			Refresh();
			if (result >= 0) return;

			if (computerSolo) playNextMoveComputer();
			else if (playComputer)
				if(_Turn == Turn.X && computerStarts || _Turn == Turn.O && !computerStarts)
					playNextMoveComputer();
		}

		#region Render UI

		const int FieldSize = 180;
		const int Spacing = 20;
		const int padding = 50;

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.FillRectangle(Brushes.Black, FieldSize + padding, padding, Spacing, FieldSize * 3 + Spacing * 2);
			g.FillRectangle(Brushes.Black, 2*FieldSize + padding + Spacing, padding, Spacing, FieldSize * 3 + Spacing * 2);

			g.FillRectangle(Brushes.Black, padding, FieldSize + padding, FieldSize * 3 + Spacing * 2, Spacing);
			g.FillRectangle(Brushes.Black, padding, 2 * FieldSize + padding + Spacing, FieldSize * 3 + Spacing * 2, Spacing);

			// Now Draw the Letters

			// First Line
			g.DrawString(valueOf1(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding, padding);
			g.DrawString(valueOf2(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding + Spacing + FieldSize, padding);
			g.DrawString(valueOf3(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding + 2*(Spacing + FieldSize), padding);

			// Second Line
			g.DrawString(valueOf4(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding, padding + Spacing + FieldSize);
			g.DrawString(valueOf5(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding + Spacing + FieldSize, padding + Spacing + FieldSize);
			g.DrawString(valueOf6(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding + 2*(Spacing + FieldSize), padding + Spacing + FieldSize);

			// Third Line
			g.DrawString(valueOf7(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding, padding + 2 * (Spacing + FieldSize));
			g.DrawString(valueOf8(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding + Spacing + FieldSize, padding + 2 * (Spacing + FieldSize));
			g.DrawString(valueOf9(), new Font(Font.FontFamily, 130f), Brushes.Purple, padding + 2*(Spacing + FieldSize), padding + 2 * (Spacing + FieldSize));

			//g.DrawString(message, new Font(Font.FontFamily, 15f), Brushes.Teal, padding, padding + Spacing + FieldSize);

			base.OnPaint(e);
		}

		// There are better ways of storing and getting the Boardstate here, but this is easy

		// Not the best way but eh

		string valueOf(int field)
		{
			switch (field)
			{
				case 0: return valueOf1();
				case 1: return valueOf2();
				case 2: return valueOf3();
				case 3: return valueOf4();
				case 4: return valueOf5();
				case 5: return valueOf6();
				case 6: return valueOf7();
				case 7: return valueOf8();
				case 8: return valueOf9();
			}

			return "";
		}
		string valueOf1() => (BoardstateBlack & 0x0001) != 0 ? "O" : (BoardstateWhite & 0x0001) != 0 ? "X" : "";
		string valueOf2() => (BoardstateBlack & 0x0002) != 0 ? "O" : (BoardstateWhite & 0x0002) != 0 ? "X" : "";
		string valueOf3() => (BoardstateBlack & 0x0004) != 0 ? "O" : (BoardstateWhite & 0x0004) != 0 ? "X" : "";
		string valueOf4() => (BoardstateBlack & 0x0008) != 0 ? "O" : (BoardstateWhite & 0x0008) != 0 ? "X" : "";
		string valueOf5() => (BoardstateBlack & 0x0010) != 0 ? "O" : (BoardstateWhite & 0x0010) != 0 ? "X" : "";
		string valueOf6() => (BoardstateBlack & 0x0020) != 0 ? "O" : (BoardstateWhite & 0x0020) != 0 ? "X" : "";
		string valueOf7() => (BoardstateBlack & 0x0040) != 0 ? "O" : (BoardstateWhite & 0x0040) != 0 ? "X" : "";
		string valueOf8() => (BoardstateBlack & 0x0080) != 0 ? "O" : (BoardstateWhite & 0x0080) != 0 ? "X" : "";
		string valueOf9() => (BoardstateBlack & 0x0100) != 0 ? "O" : (BoardstateWhite & 0x0100) != 0 ? "X" : "";

		void select1() { if(_Turn == Turn.O) BoardstateBlack |= 0x0001; else BoardstateWhite |= 0x0001; }
		void select2() { if(_Turn == Turn.O) BoardstateBlack |= 0x0002; else BoardstateWhite |= 0x0002; }
		void select3() { if(_Turn == Turn.O) BoardstateBlack |= 0x0004; else BoardstateWhite |= 0x0004; }
		void select4() { if(_Turn == Turn.O) BoardstateBlack |= 0x0008; else BoardstateWhite |= 0x0008; }
		void select5() { if(_Turn == Turn.O) BoardstateBlack |= 0x0010; else BoardstateWhite |= 0x0010; }
		void select6() { if(_Turn == Turn.O) BoardstateBlack |= 0x0020; else BoardstateWhite |= 0x0020; }
		void select7() { if(_Turn == Turn.O) BoardstateBlack |= 0x0040; else BoardstateWhite |= 0x0040; }
		void select8() { if(_Turn == Turn.O) BoardstateBlack |= 0x0080; else BoardstateWhite |= 0x0080; }
		void select9() { if(_Turn == Turn.O) BoardstateBlack |= 0x0100; else BoardstateWhite |= 0x0100; }

		#endregion

		#region Lazy Win Detection

		/// <summary>
		/// Values: <br/>
		/// -1: Game is still going <br/>
		/// 0: Draw <br/>
		/// 1: Player 1 won <br/>
		/// 2: Player 2 won <br/>
		/// </summary>
		/// <returns></returns>
		int playerWon()
		{
			// Check win for white
			bool maskApplies(short pos, short mask) => (pos & mask) == mask;

			foreach (var mask in masks) if (maskApplies(BoardstateWhite, mask)) return 1;
			foreach (var mask in masks) if (maskApplies(BoardstateBlack, mask)) return 2;

			// Check if board is full
			if (maskApplies(Boardstate, 0x01FF)) return 0;
			return -1;
		}

		private const short mask_row1 = 0x0007; // 000000111 | 0 0000 0111
		private const short mask_row2 = 0x0038; // 000111000 | 0 0011 1000
		private const short mask_row3 = 0x01C0; // 111000000 | 1 1100 0000
		private const short mask_col1 = 0x0124; // 100100100 | 1 0010 0100
		private const short mask_col2 = 0x0092; // 010010010 | 0 1001 0010
		private const short mask_col3 = 0x0049; // 001001001 | 0 0100 1001
		private const short mask_diag1 = 0x0111; // 100010001 | 1 0001 0001
		private const short mask_diag2 = 0x0054; // 001010100 | 0 0101 0100

		private readonly short[] masks =
			{ mask_row1, mask_row2, mask_row3, mask_col1, mask_col2, mask_col3, mask_diag1, mask_diag2 };

		#endregion

		enum Turn
		{
			X, O
		}
	}
}
