using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OVFFileConverterGUI
{
    public partial class ConverterForm : Form, IFileReaderWriterProgress
    {
        public ConverterForm()
        {
            InitializeComponent();
        }

        public bool IsCancelled { get; set; }
        public bool IsFinished { get; set; }

        public void Update(string message, int progressPerCent)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new ProgressbarUpdateDelegate(Update), new object[] { message, progressPerCent });
                return;
            }
            progressBar1.Value = progressPerCent;
        }

        private delegate void ProgressbarUpdateDelegate(string message, int value);

        private void button1_Click(object sender, EventArgs e)
        {
            string path;
            string pathOut;
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "choose file to convert";
            fileDialog.Filter = "slice data file (CLI, ILT, ASP, OVF)|*.cli;*.ilt;*.ovf;*.asp;";
            var result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                path = String.Copy(fileDialog.FileName);
            }
            else
            {
                return;
            }
            SaveFileDialog targetFileDialog = new SaveFileDialog();
            targetFileDialog.Title = "choose target file";
            targetFileDialog.Filter = "cli file|*.cli;*.CLI|ilt file|.ilt;*.ILT|ovf file|*.ovf;*.OVF|asp file|*.asp;*.ASP|all files|*.*";
            result = targetFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                pathOut = String.Copy(targetFileDialog.FileName);
            }
            else
            {
                return;
            }
            OpenVectorFormat.FileReaderWriterFactory.FileConverter.ConvertAsync(new System.IO.FileInfo(path), new System.IO.FileInfo(pathOut), this);
        }
    }
}
