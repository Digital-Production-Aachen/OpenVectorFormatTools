/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2023 Digital-Production-Aachen

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

---- Copyright End ----
*/

﻿using OpenVectorFormat.AbstractReaderWriter;
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

        private async void button1_Click(object sender, EventArgs e)
        {
            string path;
            string pathOut;
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "choose file to convert";
            fileDialog.Filter = "slice data file (CLI, ILT, ASP, OVF)|*.cli;*.ilt;*.ovf;*.asp;";
            var result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                path = fileDialog.FileName;
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
                pathOut = targetFileDialog.FileName;
            }
            else
            {
                return;
            }
            try
            {
                OpenVectorFormat.FileReaderWriterFactory.FileConverter.Convert(new System.IO.FileInfo(path), new System.IO.FileInfo(pathOut), this);
                progressBar1.Value = 100;
            }
            catch(NotSupportedException ex)
            {
                MessageBox.Show(ex.Message);
                progressBar1.Value = 0;
            }
        }
    }
}
