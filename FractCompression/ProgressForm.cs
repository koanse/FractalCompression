using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FractCompression
{
    public partial class ProgressForm : Form
    {
        Compression compression;
        Decompression decompression;
        public delegate void ProgressFormMethod();
        ProgressFormMethod myDelegate;
        public ProgressForm(Compression c, Decompression d)
        {
            InitializeComponent();
            compression = c;
            decompression = d;
            c.backgroundWorker = backgroundWorker1;
            myDelegate = new ProgressFormMethod(SetLabelText);
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            compression.PreProcess();
            this.Invoke(myDelegate);
            compression.Compress();
        }
        private void ProgressForm_Shown(object sender, EventArgs e)
        {
            label1.Text = "Подготовка...";
            backgroundWorker1.RunWorkerAsync();
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Close();
        }
        private void ProgressForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }
        private void SetLabelText()
        {
            label1.Text = "Компрессия...";
        }
    }
}