﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChessV1.Stormcloud.Chess.Stormcloud4.UI;

namespace ChessUI
{
	public partial class Form1 : Form
	{
		public static Form1 self;

		public Label tf_Turn, tf_Result, l_GameMode, l_LegalMovesEnabled, l_SearchForChecks, l_AllowSelfTakes, l_FlipBoard;
		public CheckBox cb_LegalMovesEnabled, cb_SearchForChecks, cb_AllowSelfTakes, cb_FlipBoard;

		private int DisplaySize = 1000;
		private int MyPadding = 150;

		IChessboard _Chessboard;
		private ChessboardUI SC4UI;
		public Button UndoButton, ResetButton, RefreshSizeButton;
		public ComboBox GameMode;

		public Label Catfish_UI_Title, Catfish_UI_BestMove_Title, Catfish_UI_BestMove, Catfish_UI_BestMoveScore;
		public TextBox TextFieldExport1;

		public Form1()
		{
			self = this;
			Chessboard.Init();
			ChessGraphics.Init();
			InitializeComponent();
			tf_Turn = new Label();
			tf_Turn.Font = new Font(tf_Turn.Font.FontFamily, 15f);
			tf_Turn.AutoSize = true;
			tf_Turn.Location = new Point(MyPadding + DisplaySize + 40, 150);
			tf_Turn.Text = "Current Turn: white";
			Controls.Add(tf_Turn);

			tf_Result = new Label();
			tf_Result.Font = new Font(tf_Result.Font.FontFamily, 35f);
			tf_Result.AutoSize = true;
			tf_Result.Location = new Point(340, 450);
			tf_Result.Text = "";
			Controls.Add(tf_Result);

			TextFieldExport1 = new TextBox();
			TextFieldExport1.Font = new Font(tf_Result.Font.FontFamily, 35f);
			TextFieldExport1.AutoSize = true;
			TextFieldExport1.Location = new Point(MyPadding + DisplaySize + 40, 150);
			Controls.Add(TextFieldExport1);

			// Checkboxes

			l_LegalMovesEnabled = new Label();
			l_LegalMovesEnabled.Font = new Font(tf_Result.Font.FontFamily, 13f);
			l_LegalMovesEnabled.AutoSize = true;
			l_LegalMovesEnabled.Location = new Point(MyPadding + DisplaySize + 60, 250);
			l_LegalMovesEnabled.Text = "Legal Moves Only";
			Controls.Add(l_LegalMovesEnabled);

			cb_LegalMovesEnabled = new CheckBox();
			cb_LegalMovesEnabled.AutoSize = true;
			cb_LegalMovesEnabled.Location = new Point(MyPadding + DisplaySize + 40, l_LegalMovesEnabled.Location.Y + 4);
			cb_LegalMovesEnabled.Checked = true;
			cb_LegalMovesEnabled.CheckedChanged += (s, e) => _Chessboard.LegalMovesEnabled = cb_LegalMovesEnabled.Checked;
			Controls.Add(cb_LegalMovesEnabled);


			l_SearchForChecks = new Label();
			l_SearchForChecks.Font = new Font(tf_Result.Font.FontFamily, 13f);
			l_SearchForChecks.AutoSize = true;
			l_SearchForChecks.Location = new Point(MyPadding + DisplaySize + 60, 290);
			l_SearchForChecks.Text = "Look for Checks";
			Controls.Add(l_SearchForChecks);

			cb_SearchForChecks = new CheckBox();
			cb_SearchForChecks.AutoSize = true;
			cb_SearchForChecks.Location = new Point(MyPadding + DisplaySize + 40, l_SearchForChecks.Location.Y + 4);
			cb_SearchForChecks.CheckedChanged += (s, e) => _Chessboard.ScanForChecks = cb_SearchForChecks.Checked;
			Controls.Add(cb_SearchForChecks);


			l_AllowSelfTakes = new Label();
			l_AllowSelfTakes.Font = new Font(tf_Result.Font.FontFamily, 13f);
			l_AllowSelfTakes.AutoSize = true;
			l_AllowSelfTakes.Location = new Point(MyPadding + DisplaySize + 60, 330);
			l_AllowSelfTakes.Text = "Allow Self-Takes";
			Controls.Add(l_AllowSelfTakes);

			cb_AllowSelfTakes = new CheckBox();
			cb_AllowSelfTakes.AutoSize = true;
			cb_AllowSelfTakes.Location = new Point(MyPadding + DisplaySize + 40, l_AllowSelfTakes.Location.Y + 4);
			cb_AllowSelfTakes.CheckedChanged += (s, e) => _Chessboard.AllowSelfTakes = cb_AllowSelfTakes.Checked;
			Controls.Add(cb_AllowSelfTakes);


			l_FlipBoard = new Label();
			l_FlipBoard.Font = new Font(tf_Result.Font.FontFamily, 13f);
			l_FlipBoard.AutoSize = true;
			l_FlipBoard.Location = new Point(MyPadding + DisplaySize + 60, 380);
			l_FlipBoard.Text = "Flip board";
			Controls.Add(l_FlipBoard);

			cb_FlipBoard = new CheckBox();
			cb_FlipBoard.AutoSize = true;
			cb_FlipBoard.Location = new Point(MyPadding + DisplaySize + 40, l_FlipBoard.Location.Y + 4);
			cb_FlipBoard.Checked = true;
			cb_FlipBoard.CheckedChanged += (s, e) => _Chessboard.EnableFlipBoard = cb_FlipBoard.Checked;
			Controls.Add(cb_FlipBoard);

			// Combo Box

			l_GameMode = new Label();
			l_GameMode.Font = new Font(tf_Result.Font.FontFamily, 11f);
			l_GameMode.AutoSize = true;
			l_GameMode.Location = new Point(MyPadding + DisplaySize + 05, 440);
			l_GameMode.Text = "Switch GameMode (Warning: Resets the Board)";
			Controls.Add(l_GameMode);

			GameMode = new ComboBox();
			GameMode.Font = new Font(tf_Turn.Font.FontFamily, 12f);
			GameMode.AutoSize = false;
			GameMode.Size = new Size(220, GameMode.Size.Height);
			GameMode.Location = new Point(MyPadding + DisplaySize + 40, 470);
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
			UndoButton.Location = new Point(MyPadding + DisplaySize + 40, 550);
			UndoButton.Text = "Undo Last Move";
			UndoButton.Enabled = false;
			UndoButton.Click += (s, e) => { UndoButton.Enabled = _Chessboard.UndoLastMove(); };
			Controls.Add(UndoButton);

			ResetButton = new Button();
			ResetButton.Font = new Font(tf_Turn.Font.FontFamily, 15f);
			ResetButton.AutoSize = true;
			ResetButton.Location = new Point(MyPadding + DisplaySize + 40, 650);
			ResetButton.Text = "Reset Board";
			ResetButton.Click += (s, e) => { if(_Chessboard != null) _Chessboard.Reset(); else if(SC4UI != null) SC4UI.ResetBoard(); };
			Controls.Add(ResetButton);

			RefreshSizeButton = new Button();
			RefreshSizeButton.Font = new Font(tf_Turn.Font.FontFamily, 13f);
			RefreshSizeButton.AutoSize = true;
			RefreshSizeButton.Text = "Adjust Boardsize";
			RefreshSizeButton.Click += (s, e) =>
			{
				DisplaySize = (int)Math.Min(this.Width * 0.9, this.Height * 0.9);
				if (_Chessboard != null) _Chessboard.DisplaySize = DisplaySize;
				else if (SC4UI != null) SC4UI.DisplaySize = DisplaySize;
			};
			Controls.Add(RefreshSizeButton);

			// CATFISH UI

			Catfish_UI_Title = new Label();
			Catfish_UI_Title.Font = new Font(tf_Result.Font.FontFamily, 25f);
			Catfish_UI_Title.AutoSize = true;
			Catfish_UI_Title.Location = new Point(MyPadding + DisplaySize + 20, 720);
			Catfish_UI_Title.Text = "-={[ Catfish Engine ]}=-";
			Controls.Add(Catfish_UI_Title);

			Catfish_UI_BestMove_Title = new Label();
			Catfish_UI_BestMove_Title.Font = new Font(tf_Result.Font.FontFamily, 19f);
			Catfish_UI_BestMove_Title.AutoSize = true;
			Catfish_UI_BestMove_Title.Location = new Point(MyPadding + DisplaySize + 30, Catfish_UI_Title.Location.Y + Catfish_UI_Title.Height + 20);
			Catfish_UI_BestMove_Title.Text = "Best Move: ";
			Controls.Add(Catfish_UI_BestMove_Title);

			Catfish_UI_BestMove = new Label();
			Catfish_UI_BestMove.Font = new Font(tf_Result.Font.FontFamily, 18f, FontStyle.Bold);
			Catfish_UI_BestMove.AutoSize = true;
			Catfish_UI_BestMove.Location = new Point(Catfish_UI_BestMove_Title.Location.X + Catfish_UI_BestMove_Title.Width, Catfish_UI_BestMove_Title.Location.Y + ((Catfish_UI_BestMove_Title.Height - Catfish_UI_BestMove.Height) / 2));
			Catfish_UI_BestMove.Text = "None";
			Catfish_UI_BestMove.ForeColor = System.Drawing.Color.FromArgb(153, 197, 255);
			Controls.Add(Catfish_UI_BestMove);

			Catfish_UI_BestMoveScore = new Label();
			Catfish_UI_BestMoveScore.Font = new Font(tf_Result.Font.FontFamily, 13f);
			Catfish_UI_BestMoveScore.AutoSize = true;
			Catfish_UI_BestMoveScore.Location = new Point(Catfish_UI_BestMove_Title.Location.X + Catfish_UI_BestMove_Title.Width / 4, Catfish_UI_BestMove.Location.Y + Catfish_UI_BestMove.Height + 10);
			Catfish_UI_BestMoveScore.Text = "Score: 0";
			Controls.Add(Catfish_UI_BestMoveScore);

			Timer t = new Timer();
			t.Tick += (s, e) =>
			{
				Catfish_UI_BestMove.Text = bestMove;
				Catfish_UI_BestMoveScore.Text = $"Eval: {(bestMoveScore > 0 ? $"+{bestMoveScore}" : $"{bestMoveScore}")} | Depth: {bestMoveDepth}";
				Refresh();
			};
			t.Interval = 150;
			t.Start();
		}

		public static string bestMove = "None";
		public static double bestMoveScore = 0;
		public static int bestMoveDepth = 0;

		public void SetBestMove(string move, double score, int Depth)
		{
			Catfish_UI_BestMove.Text = move;
			Catfish_UI_BestMoveScore.Text = $"Material: {(score > 0 ? $"+{score}" : $"{score}")}";
			//SetScore(score);
			//Catfish_UI_BestMoveScore.Text = $"Score: {(double) (score / 100.0)}  |  Depth: {Depth}";
		}

		public void SetScore(double score)
		{
			Catfish_UI_BestMoveScore.Text = $"Material: {(score > 0 ? $"+{score}" : $"{score}")}";
		}

		public void SetPosKey(string key)
		{
			TextFieldExport1.Text = key;
		}

		public void newTurn(Turn turn)
		{
			tf_Turn.Text = "Current Turn: " + turn.ToString();
		}

		public void newTurn(string turn)
		{
			tf_Turn.Text = "Current Turn: " + turn;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			//_Chessboard = new Chessboard(DisplaySize);
			//this.Controls.Add((Panel)_Chessboard);
			SC4UI = new ChessV1.Stormcloud.Chess.Stormcloud4.UI.ChessboardUI(500);
			Controls.Add((Panel)SC4UI);
			RefreshSizeButton.PerformClick();

			new UnitTest();
		}
	}
}
