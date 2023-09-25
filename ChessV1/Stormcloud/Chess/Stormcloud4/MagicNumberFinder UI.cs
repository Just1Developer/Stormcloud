using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessV1.Stormcloud.Chess.Stormcloud4
{
	partial class MagicNumberFinder : Form
	{
		private static Button buttonStartSearch;
		private static Button buttonStopSearch;
		private static Button buttonNextIndex;
		private static bool searching = false;

		public static MagicNumberFinder UI;

		public MagicNumberFinder()
		{
			UI = this;
			InitializeComponent();
			CheckForIllegalCrossThreadCalls = false;
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.Size = new Size(1200, 2 * Padding + 8 * Fieldsize);
			buttonStartSearch = new Button();
			buttonStartSearch.Bounds = new Rectangle(2 * Padding + 8 * Fieldsize, 2 * Fieldsize, 200, 80);
			buttonStartSearch.Text = "Start Search";
			buttonStopSearch = new Button();
			buttonStopSearch.Bounds = new Rectangle(2 * Padding + 8 * Fieldsize, 4 * Fieldsize, 200, 80);
			buttonStopSearch.Text = "Stop Search";
			buttonStopSearch.Enabled = false;

			buttonNextIndex = new Button();
			buttonNextIndex.Bounds = new Rectangle(2 * Padding + 8 * Fieldsize, 6 * Fieldsize, 200, 80);
			buttonNextIndex.Text = "Constellations: Start";
			
			Controls.Add(buttonStartSearch);
			Controls.Add(buttonStopSearch);
			Controls.Add(buttonNextIndex);
		}

		public static void Repaint()
		{
			UI.Refresh();
		}

		private void MagicNumberFinder_UI_Load(object sender, EventArgs e)
		{

			buttonStartSearch.Click += (s, e) =>
			{
				buttonStartSearch.Enabled = false;
				buttonStopSearch.Enabled = true;
				StartTesting();
			};
			buttonStopSearch.Click += (s, e) =>
			{
				searching = false;
				buttonStartSearch.Enabled = true;
				buttonStopSearch.Enabled = false;
			};
			buttonNextIndex.Click += (s, e) =>
			{
				NextBlockerConstellations();
			};
			this.MouseUp += (s, e) =>
			{
				if (e.Button == MouseButtons.Left) MouseUpCalled(e.Location);
				else if (e.Button == MouseButtons.Right) MouseUpCalledRightclick(e.Location);
			};

		}

		private static ulong HighlightBitboard;
		private static ulong BlockerBitboard;
		private static byte selectedX, selectedY;

		private const int Fieldsize = 90;
		private const int Padding = 70;

		private ulong[] AllBlockerConstellations = { 0 };
		private int AllBlockerConstellationsIndex = 0;

		public static ConcurrentDictionary<int, double> Runs = new();
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Brush colorOf(sbyte reverseIndex)
			{
				bool light = (reverseIndex & 1) == ((reverseIndex / 8) & 1);

				if(((HighlightBitboard >> reverseIndex) & 1) == 1) return light ? Brushes.Crimson : Brushes.Brown;
				return light ? Brushes.Bisque : Brushes.DarkSalmon;
			}

			for (sbyte i = 63; i >= 0; i--)
			{
				g.FillRectangle(colorOf(i), new Rectangle(
					Padding + Fieldsize * (i % 8),
					Padding + Fieldsize * (i / 8),
					Fieldsize, Fieldsize));
				if (((BlockerBitboard >> i) & 1) == 1)
				{
					g.DrawString("X", new Font(FontFamily.GenericMonospace, 64f, FontStyle.Bold), Brushes.Gold,
						new Point(Padding + Fieldsize * (i % 8) + 6, Padding + Fieldsize * (i / 8)));
				}

				if (Runs.ContainsKey(i))
				{
					double runs = Runs[i];
					g.DrawString($"{(runs >= 10 ? (int) runs : runs)}m".Replace(",", "."), new Font(FontFamily.GenericMonospace, 16f, FontStyle.Bold), Brushes.CadetBlue,
						new Point(Padding + Fieldsize * (i % 8), Padding + Fieldsize * (i / 8)));
				}
			}
			base.OnPaint(e);
		}

		private void MouseUpCalled(Point mouseLoc)
		{
			byte x = (byte) (7 - (mouseLoc.X - Padding) / Fieldsize);
			byte y = (byte) ((mouseLoc.Y - Padding) / Fieldsize);

			if (x > 7 || y > 7 || mouseLoc.X < Padding || mouseLoc.Y < Padding)
			{
				HighlightBitboard = 0;
				selectedX = 0;
				selectedY = 0;
				Refresh();
				return;
			}

			// reverseIndex: 0 = a1, 63 = h8
			//byte reverseIndex = (byte) (0b111111 - ((7 - x) * 8 + x));	// <- wrong

			//ulong positionMask = ~(1UL << reverseIndex);
			//HighlightBitboard = (RookBBSpecial_GetBitboardFileOnly0to7((byte) (7-x)) | RookBBSpecial_GetBitboardRankOnly0to7(y)) & positionMask;

			selectedX = (byte)(7 - x);
			selectedY = y;

			//Log($"SelectedX: {selectedX}, SelectedY: {selectedY}");

			AllBlockerConstellations = GetAllBlockerPositionsFromSquare(selectedY, selectedX).ToArray();
			AllBlockerConstellationsIndex = -1;

			HighlightBitboard = GetCutoffInDirectionsLegalMovesPos(BlockerBitboard, selectedY, selectedX);
			int field = selectedY * 8 + selectedX;
			//HighlightBitboard = MagicC.rmask(field);
			Refresh();

			//Log($"Clicked {String_file(x)}{String_rank(y)}  |  x: {x}, y: {y}");
			//Log($"Clicked {String_file(selectedX)}{String_rank(selectedY)}  |  x: {selectedX}, y: {selectedY}");
			//Log($"ReverseIndex: {reverseIndex}");
		}

		private void MouseUpCalledRightclick(Point mouseLoc)
		{
			byte x = (byte) (7 - (mouseLoc.X - Padding) / Fieldsize);
			byte y = (byte) ((mouseLoc.Y - Padding) / Fieldsize);

			if (x > 7 || y > 7 || mouseLoc.X < Padding || mouseLoc.Y < Padding)
			{
				HighlightBitboard = 0;
				BlockerBitboard = 0;
				selectedX = 0;
				selectedY = 0;
				Refresh();
				return;
			}

			// reverseIndex: 0 = a1, 63 = h8
			byte reverseIndex = (byte) (0b111111 - ((7 - y) * 8 + x));

			ulong positionMask = 1UL << reverseIndex;
			BlockerBitboard ^= positionMask;
			HighlightBitboard = GetCutoffInDirectionsLegalMovesPos(BlockerBitboard, selectedY, selectedX);
			Refresh();

			//Log($"Clicked {String_file(x)}{String_rank(y)}  |  x: {x}, y: {y}");
			//Log($"ReverseIndex: {reverseIndex}");
		}

		private bool runnin = false;
		System.Timers.Timer Timer;
		private void NextBlockerConstellations()
		{
			if (runnin)
			{
				Timer.Enabled = false;
				Timer.Stop();
				runnin = false;
				buttonNextIndex.Text = "Constellations: Start";
				return;
			}
			runnin = true;
			buttonNextIndex.Text = "Constellations: Stop";
			Timer = new System.Timers.Timer();
			Timer.Interval = 100;
			Timer.Elapsed += (s, e) => NextBlockerConstellation();
			Timer.Start();
		}
		private void NextBlockerConstellation()
		{
			AllBlockerConstellationsIndex++;
			if (AllBlockerConstellationsIndex >= AllBlockerConstellations.Length) AllBlockerConstellationsIndex = 0;
			BlockerBitboard = AllBlockerConstellations[AllBlockerConstellationsIndex];
			HighlightBitboard = GetCutoffInDirectionsLegalMovesPos(BlockerBitboard, selectedY, selectedX);
			Refresh();
		}

		static char String_file(int x) => (char) ('a' + x);
		static char String_rank(int y) => (char) ('8' - y);
	}
}
