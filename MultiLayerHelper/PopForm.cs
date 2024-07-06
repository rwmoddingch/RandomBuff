using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiLayerHelper
{
    public partial class PopForm : Form
    {
        public PopForm()
        {
            InitializeComponent();
        }


        public float depth = 0.5f;

        private void button1_Click(object sender, EventArgs e)
        {
            depth = float.Parse(textBox1.Text);
            Close();
        }

        private void textBox1_Validating(object sender, CancelEventArgs e)
        {
            

            string content = ((TextBox)sender).Text;

            if (!float.TryParse(content,out var value))
                textBox1.Text = "0.5";
            else
                textBox1.Text = MathF.Max(MathF.Min(value, 1), 0).ToString();

        }
    }
}
