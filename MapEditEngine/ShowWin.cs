using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TextDx5
{
    public partial class ShowWin : Form
    {
        public ShowWin()
        {
            InitializeComponent();
        }

        public void showW(Image i)
        {
            pictureBox1.Image = i;
            pictureBox1.Update();
        }
    }
}