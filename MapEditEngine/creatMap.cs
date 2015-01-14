using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TextDx5
{
    public partial class creatMap : Form
    {
        public creatMap()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox4.Text == "" || textBox1.Text == "" || textBox2.Text == "" || textBox3.Text == "")
            {
                MessageBox.Show("请全部输入所有参数!");
            }
            else
            {
                string[] a = new string[4];
                a[0] = textBox1.Text;
                a[1] = textBox2.Text;
                a[2] = textBox3.Text;
                a[3] = textBox4.Text;
                using (mainForm mainf = new mainForm())
                {
                    mainf.LoadMapArgs(a,true);
                }
                this.Dispose();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            textBox3.Text = openFileDialog1.FileName;
        }
    }
}