﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FileStreamClient
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			progressBar1.Minimum = 0;
			progressBar1.Maximum = 100;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Program.Connect(textBox1.Text, Int32.Parse(textBox2.Text));
		}
	}
}
