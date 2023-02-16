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
		}

		public void newTurn(Turn turn)
		{
			tf_Turn.Text = "Current Turn: " + turn.ToString();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			this.Controls.Add(new Chessboard(1000));
		}
	}
}
