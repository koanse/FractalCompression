using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FractCompression
{
    public partial class ComprSettingsForm : Form
    {
        public ComprSettingsForm()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked == false && checkBox2.Checked == false
                && checkBox3.Checked == false)
            {
                MessageBox.Show("Нужно выбрать хотя бы один способ сравнения", "Ошибка");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}