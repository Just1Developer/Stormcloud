using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessV1
{
	public partial class Form1 : Form
	{
		public static Form1 self;

		public Label tf_Turn, tf_Result, l_GameMode, l_LegalMovesEnabled, l_SearchForChecks, l_AllowSelfTakes, l_FlipBoard;
		public CheckBox cb_LegalMovesEnabled, cb_SearchForChecks, cb_AllowSelfTakes, cb_FlipBoard;

		IChessboard _Chessboard;
		public Button UndoButton, ResetButton, RefreshSizeButton;
		public ComboBox GameMode;

		public Form1()
		{
			self = this;
			Chessboard.Init();
			ChessGraphics.Init();
			InitializeComponent();
			tf_Turn = new Label();
			tf_Turn.Font = new Font(tf_Turn.Font.FontFamily, 15f);
			tf_Turn.AutoSize = true;
			tf_Turn.Location = new Point(1040, 150);
			tf_Turn.Text = "Current Turn: white";
			Controls.Add(tf_Turn);

			tf_Result = new Label();
			tf_Result.Font = new Font(tf_Result.Font.FontFamily, 35f);
			tf_Result.AutoSize = true;
			tf_Result.Location = new Point(340, 450);
			tf_Result.Text = "";
			Controls.Add(tf_Result);

			// Checkboxes

			l_LegalMovesEnabled = new Label();
			l_LegalMovesEnabled.Font = new Font(tf_Result.Font.FontFamily, 13f);
			l_LegalMovesEnabled.AutoSize = true;
			l_LegalMovesEnabled.Location = new Point(1060, 250);
			l_LegalMovesEnabled.Text = "Legal Moves Only";
			Controls.Add(l_LegalMovesEnabled);

			cb_LegalMovesEnabled = new CheckBox();
			cb_LegalMovesEnabled.AutoSize = true;
			cb_LegalMovesEnabled.Location = new Point(1040, l_LegalMovesEnabled.Location.Y + 4);
			cb_LegalMovesEnabled.Checked = true;
			cb_LegalMovesEnabled.CheckedChanged += (s, e) => _Chessboard.LegalMovesEnabled = cb_LegalMovesEnabled.Checked;
			Controls.Add(cb_LegalMovesEnabled);


			l_SearchForChecks = new Label();
			l_SearchForChecks.Font = new Font(tf_Result.Font.FontFamily, 13f);
			l_SearchForChecks.AutoSize = true;
			l_SearchForChecks.Location = new Point(1060, 290);
			l_SearchForChecks.Text = "Look for Checks";
			Controls.Add(l_SearchForChecks);

			cb_SearchForChecks = new CheckBox();
			cb_SearchForChecks.AutoSize = true;
			cb_SearchForChecks.Location = new Point(1040, l_SearchForChecks.Location.Y + 4);
			cb_SearchForChecks.CheckedChanged += (s, e) => _Chessboard.ScanForChecks = cb_SearchForChecks.Checked;
			Controls.Add(cb_SearchForChecks);


			l_AllowSelfTakes = new Label();
			l_AllowSelfTakes.Font = new Font(tf_Result.Font.FontFamily, 13f);
			l_AllowSelfTakes.AutoSize = true;
			l_AllowSelfTakes.Location = new Point(1060, 330);
			l_AllowSelfTakes.Text = "Allow Self-Takes";
			Controls.Add(l_AllowSelfTakes);

			cb_AllowSelfTakes = new CheckBox();
			cb_AllowSelfTakes.AutoSize = true;
			cb_AllowSelfTakes.Location = new Point(1040, l_AllowSelfTakes.Location.Y + 4);
			cb_AllowSelfTakes.CheckedChanged += (s, e) => _Chessboard.AllowSelfTakes = cb_AllowSelfTakes.Checked;
			Controls.Add(cb_AllowSelfTakes);


			l_FlipBoard = new Label();
			l_FlipBoard.Font = new Font(tf_Result.Font.FontFamily, 13f);
			l_FlipBoard.AutoSize = true;
			l_FlipBoard.Location = new Point(1060, 380);
			l_FlipBoard.Text = "Flip board";
			Controls.Add(l_FlipBoard);

			cb_FlipBoard = new CheckBox();
			cb_FlipBoard.AutoSize = true;
			cb_FlipBoard.Location = new Point(1040, l_FlipBoard.Location.Y + 4);
			cb_FlipBoard.Checked = true;
			cb_FlipBoard.CheckedChanged += (s, e) => _Chessboard.EnableFlipBoard = cb_FlipBoard.Checked;
			Controls.Add(cb_FlipBoard);

			// Combo Box

			l_GameMode = new Label();
			l_GameMode.Font = new Font(tf_Result.Font.FontFamily, 11f);
			l_GameMode.AutoSize = true;
			l_GameMode.Location = new Point(1005, 440);
			l_GameMode.Text = "Switch GameMode (Warning: Resets the Board)";
			Controls.Add(l_GameMode);

			GameMode = new ComboBox();
			GameMode.Font = new Font(tf_Turn.Font.FontFamily, 12f);
			GameMode.AutoSize = false;
			GameMode.Size = new Size(220, GameMode.Size.Height);
			GameMode.Location = new Point(1040, 470);
			GameMode.Items.Add("Normal");
			GameMode.Items.Add("Blitz (WIP)");
			GameMode.Items.Add("Rapid (WIP)");
			GameMode.Items.Add("Variant: Atomic");
			GameMode.Items.Add("Variant: Il Vaticano (WIP)");
			GameMode.SelectedIndex = 0;
			GameMode.SelectedIndexChanged += (s, e) =>
			{
				switch (GameMode.SelectedIndex)
				{
					case 0: _Chessboard.ChessMode = ChessMode.Normal; _Chessboard.Reset(); break;
					case 1: _Chessboard.ChessMode = ChessMode.Blitz; _Chessboard.Reset(); break;
					case 2: _Chessboard.ChessMode = ChessMode.Rapid; _Chessboard.Reset(); break;
					case 3: _Chessboard.ChessMode = ChessMode.Atomic; _Chessboard.Reset(); break;
					case 4: _Chessboard.ChessMode = ChessMode.Il_Vaticano; _Chessboard.Reset(); break;
				}
				_Chessboard.Focus();
			};
			Controls.Add(GameMode);

			UndoButton = new Button();
			UndoButton.Font = new Font(tf_Turn.Font.FontFamily, 15f);
			UndoButton.AutoSize = true;
			UndoButton.Location = new Point(1040, 550);
			UndoButton.Text = "Undo Last Move";
			UndoButton.Enabled = false;
			UndoButton.Click += (s, e) => { UndoButton.Enabled = _Chessboard.UndoLastMove(); };
			Controls.Add(UndoButton);

			ResetButton = new Button();
			ResetButton.Font = new Font(tf_Turn.Font.FontFamily, 15f);
			ResetButton.AutoSize = true;
			ResetButton.Location = new Point(1040, 650);
			ResetButton.Text = "Reset Board";
			ResetButton.Click += (s, e) => { _Chessboard.Reset(); };
			Controls.Add(ResetButton);

			RefreshSizeButton = new Button();
			RefreshSizeButton.Font = new Font(tf_Turn.Font.FontFamily, 13f);
			RefreshSizeButton.AutoSize = true;
			RefreshSizeButton.Text = "Adjust Boardsize";
			RefreshSizeButton.Click += (s, e) => { _Chessboard.DisplaySize = (int)Math.Min(this.Width * 0.9, this.Height * 0.9); };
			Controls.Add(RefreshSizeButton);
		}

		public void newTurn(Turn turn)
		{
			tf_Turn.Text = "Current Turn: " + turn.ToString();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			_Chessboard = new Chessboard2(1000);
			this.Controls.Add((Chessboard2) _Chessboard);
			RefreshSizeButton.PerformClick();

			new UnitTest();
		}
	}
}
