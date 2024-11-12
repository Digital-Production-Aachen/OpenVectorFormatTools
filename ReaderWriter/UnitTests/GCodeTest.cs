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
using OpenVectorFormat.ASPFileReaderWriter;

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

            string testCommandLinear = File.ReadAllLines(fileName.FullName)[19];
            GCodeState gCodeState = new GCodeState(testCommandLinear);

            LinearInterpolationCmd assertCmd = new LinearInterpolationCmd(PrepCode.G, 0, new Dictionary<char, float> { { 'F', 1800 }, { 'X', 110.414f }, { 'Y', 94.025f }, { 'E', 0.02127f } });
            Assert.AreEqual(gCodeState.gCodeCommand.gCode, assertCmd.gCode );
            Assert.AreEqual(gCodeState.gCodeCommand.GetType(), assertCmd.GetType());

            object[] stateUpdates = gCodeState.Update(File.ReadAllLines(fileName.FullName)[22]);

            Assert.IsNotNull(stateUpdates[0]);
            Assert.IsNull(stateUpdates[1]);
            Assert.IsNull(stateUpdates[2]);
        }

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeGrouping(FileInfo fileName)
        {
            string[] testCommands = File.ReadAllLines(fileName.FullName);
            GCodeReader gCodeReader = new GCodeReader();

            GCodeCommandList gCodeCommandList = new GCodeCommandList(testCommands);

            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine(gCodeCommandList[i].GetType());
                //Console.WriteLine(gCodeCommandList[i].ToString());
            }

            IEnumerable<IGrouping<Type, GCodeCommand>> gCodeTypeGrouping = gCodeCommandList.GroupBy(gCodeCommand => gCodeCommand.GetType()).ToList();
            Console.WriteLine(gCodeTypeGrouping.ToString());

            Assert.AreEqual(3, gCodeTypeGrouping.Count());
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
