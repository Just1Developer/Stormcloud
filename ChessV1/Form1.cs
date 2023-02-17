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

		public Label tf_Turn;

		Chessboard _Chessboard;
		public Button UndoButton;

		public Form1()
		{
			self = this;
			Chessboard.Init();
			InitializeComponent();
			tf_Turn = new Label();
			tf_Turn.Font = new Font(tf_Turn.Font.FontFamily, 15f);
			tf_Turn.AutoSize = true;
			tf_Turn.Location = new Point(1040, 150);
			tf_Turn.Text = "Current Turn: white";
			Controls.Add(tf_Turn);

			UndoButton = new Button();
			UndoButton.Font = new Font(tf_Turn.Font.FontFamily, 15f);
			UndoButton.AutoSize = true;
			UndoButton.Location = new Point(1040, 550);
			UndoButton.Text = "Undo Last Move";
			UndoButton.Enabled = false;
			UndoButton.Click += (s, e) => { UndoButton.Enabled = _Chessboard.UndoLastMove(); };
			Controls.Add(UndoButton);
		}

		public void newTurn(Turn turn)
		{
			tf_Turn.Text = "Current Turn: " + turn.ToString();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			_Chessboard = new Chessboard(1000);
			this.Controls.Add(_Chessboard);
		}
	}
}
