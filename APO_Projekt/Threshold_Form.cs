﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APO_Projekt
{
    public partial class Threshold_Form : Form
    {
        private byte value = (byte) 127;
        private PictureWindow pw;
        public Threshold_Form(PictureWindow pw)
        {
            InitializeComponent();
            this.pw = pw;
        }

        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            value = (byte) trackBar.Value;
            labelValue.Text = "Value: " + value.ToString();
        }

        private void btnDo_Click(object sender, EventArgs e)
        {
            Operations.thresholding(pw.Bitmap, value);
            pw.resetLutTables();
            pw.resetBitmap();
            Close();
        }
    }
}
