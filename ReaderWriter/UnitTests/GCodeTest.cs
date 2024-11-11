/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2024 Digital-Production-Aachen

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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenVectorFormat.GCodeReaderWriter;
using System.IO;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class GCodeTest
    {
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeFiles(FileInfo fileName)
        {
            var gCodeReader = new GCodeReader();

            string testCommand = File.ReadAllLines(fileName.FullName)[21];
            GCodeState gCodeState = new GCodeState(testCommand);

            LinearInterpolationCmd assertCmd = new LinearInterpolationCmd(PrepCode.G, 1, new Dictionary<char, float> { { 'F', 1800 }, { 'X', 110.414f }, { 'Y', 94.025f }, { 'E', 0.02127f } });
            Assert.AreEqual(gCodeState.gCodeCommand.gCode, assertCmd.gCode );
            Assert.AreEqual(gCodeState.gCodeCommand.GetType(), assertCmd.GetType());
        }
        public static List<object[]> GCodeFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.gcode"); //getting all .cli files
                List<object[]> files = new List<object[]>(testFiles.Length);
                for (int i = 0; i < testFiles.Length; i++)
                {
                    files.Add(new object[] { testFiles[i] });
                }
                return files;
            }
        }
    }
}
