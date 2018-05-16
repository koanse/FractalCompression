using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace FractCompression
{
    public partial class MainForm : Form
    {
        Compression compression;
        Decompression decompression;
        Bitmap image;
        
        public MainForm()
        {
            InitializeComponent();
            compression = new Compression();
            decompression = new Decompression();
            decompression.repeats = 20;
        }
        private void OpenImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "Все файлы (*.*)|*.*";
                if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
                image = new Bitmap(openFileDialog1.FileName, false);
                image = new Bitmap(image, 512, 512);
                label1.Image = image;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка открытия файла");
            }
        }
        private void CompressionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                saveFileDialog1.Filter = "Архивы (*.frc)|*.frc";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    ComprSettingsForm comprSettingsForm = new ComprSettingsForm();
                    if (comprSettingsForm.ShowDialog() != DialogResult.OK) return;
                    compression.doCmp2x2 = comprSettingsForm.checkBox1.Checked;
                    compression.doCmp4x4 = comprSettingsForm.checkBox2.Checked;
                    compression.doCmp8x8 = comprSettingsForm.checkBox3.Checked;
                    compression.SetImage(image);
                    if (comprSettingsForm.radioButton1.Checked == true)
                        compression.linearCriterion = false;
                    else compression.linearCriterion = true;
                    ProgressForm progressForm = new ProgressForm(compression, decompression);
                    progressForm.ShowDialog();

                    FileStream stream = new FileStream(saveFileDialog1.FileName,
                        FileMode.Create, FileAccess.Write);
                    BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII);
                    compression.WriteToFile(writer);
                    stream.Close();

                    decompression.IterFuncSys = compression.IterFuncSys;
                    decompression.Decompress();
                    Image im = decompression.GetImage();
                    ResultForm resultForm = new ResultForm();
                    resultForm.label1.Image = im;
                    resultForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка компресии");
            }
        }
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void OpenArchiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "Архивы (*.frc)|*.frc";
                if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
                FileStream stream = new FileStream(openFileDialog1.FileName,
                    FileMode.Open, FileAccess.Read);
                BinaryReader reader = new BinaryReader(stream, Encoding.ASCII);
                
                IteratingFunction[] ifsOrig = decompression.IterFuncSys;
                decompression.ReadFromFile(reader);
                IteratingFunction[] ifsRead = decompression.IterFuncSys;
                stream.Close();
                decompression.Decompress();
                ResultForm resultForm = new ResultForm();
                resultForm.label1.Image = decompression.GetImage();
                resultForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка открытия файла");
            }
        }
        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                uint i = uint.Parse(toolStripTextBox1.Text);
                if (i > 1000) throw new Exception();
                decompression.repeats = (int)i;
                toolStripTextBox1.Text = i.ToString();
            }
            catch
            {
                toolStripTextBox1.Text = "20";
                decompression.repeats = 20;
            }
        }
        private void DecompressionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                decompression.Decompress();
                Image im = decompression.GetImage();
                ResultForm resultForm = new ResultForm();
                resultForm.label1.Image = im;
                resultForm.Show();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка декомпрессии");
            }
        }
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Фрактальная компрессия изображений", "Версия 1.0 - 4/04/2008");
        }
    }
}