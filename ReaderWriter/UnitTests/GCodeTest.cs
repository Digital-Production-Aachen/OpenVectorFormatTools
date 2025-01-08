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
            bool isContentValid = IsValidGCode(fileContent);
            Assert.IsTrue(isContentValid);

            static bool IsValidGCode(string[] commandLines)
            {
                foreach (string commandLine in commandLines)
                {
                    string commandString = commandLine.Split(';')[0].Trim();
                    if (string.IsNullOrEmpty(commandString))
                    {
                        continue;
                    }
                    string[] commandParts = commandString.Split(' ');

                    foreach (string commandPart in commandParts)
                    {
                        if (!char.IsLetter(commandPart[0]) || (commandPart.Length>1 && !float.TryParse(commandPart.Substring(1), out _)))
                        {
                            return false;
                        }
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
            Assert.AreEqual(testCommands.Length, gCodeCommandList.Count);
        }

        public static List<object[]> GCodeFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.gcode"); //getting all .gcode files
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
