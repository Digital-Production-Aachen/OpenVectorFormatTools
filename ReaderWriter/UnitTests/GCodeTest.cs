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
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class GCodeTest
    {
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeFile(FileInfo fileName)
        {
            string[] fileContent = File.ReadAllLines(fileName.FullName);

            Assert.IsTrue(IsValidGCode(fileContent));

            static bool IsValidGCode(string[] commandLines)
            {
                Regex gcodePattern = new Regex(@"^[GMT]\w*\s*(-?\d+(\.\d+)?)?\s*(;.*)?$");

                foreach (string commandLine in commandLines)
                {
                    string[] commandParts = commandLine.Split(' ');
                    
                    string trimmedLine = commandLine.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || !gcodePattern.IsMatch(trimmedLine))  // Ignore empty lines
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeCmdLineToObject(FileInfo fileName)
        {
            string[] testCommands = File.ReadAllLines(fileName.FullName);

            GCodeCommandList gCodeCommandList = new GCodeCommandList(testCommands);

            /*
            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine(gCodeCommandList[i].GetType());
                //Console.WriteLine(gCodeCommandList[i].ToString());
            }
            */
            Assert.AreEqual(testCommands.Length, gCodeCommandList.Count);
        }
        /*
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
        */
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
