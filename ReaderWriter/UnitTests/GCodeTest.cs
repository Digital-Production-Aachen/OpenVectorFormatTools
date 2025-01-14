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
using OpenVectorFormat.GCodeReaderWriter;
using System.IO;
using OpenVectorFormat.FileReaderWriterFactory;
using OpenVectorFormat.Plausibility;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class GCodeTest
    {
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));


        // Consider moving this to GCodeWriter
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
        public void TestGCodeToObject(FileInfo fileName)
        {
            string[] testCommands = File.ReadAllLines(fileName.FullName);

            GCodeCommandList gCodeCommandList = new GCodeCommandList(testCommands);
            Assert.AreEqual(19335, gCodeCommandList.OfType<LinearInterpolationCmd>().ToList().Count);
            Assert.AreEqual(185, gCodeCommandList.OfType<MiscCommand>().ToList().Count);
        }

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeFilesAddParams(FileInfo fileName)
        {
            var targetFile = new FileInfo(Path.GetTempFileName() + ".ovf");
            var converter = SetupConverter();

            converter.ConvertAddParams(fileName, targetFile, new FileReaderWriterFactory.FileReaderWriterProgress());
            CheckJob(targetFile);
        }

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeAddParamsToMemory(FileInfo fileName)
        {
            var converter = SetupConverter();

            var job = converter.ConvertAddParams(fileName, new FileReaderWriterFactory.FileReaderWriterProgress());
            CheckJob(job);
        }

        private FileReaderWriterFactory.FileConverter SetupConverter()
        {
            FileReaderWriterFactory.FileConverter converter = new FileReaderWriterFactory.FileConverter();
            converter.SupportPostfix = "_support";
            converter.FallbackContouringParams = new MarkingParams() { LaserSpeedInMmPerS = 100, LaserPowerInW = 0 };
            converter.FallbackHatchingParams = new MarkingParams() { LaserSpeedInMmPerS = 100, LaserPowerInW = 0 };
            converter.FallbackSupportContouringParams = new MarkingParams() { LaserSpeedInMmPerS = 100, LaserPowerInW = 0 };
            converter.FallbackSupportHatchingParams = new MarkingParams() { LaserSpeedInMmPerS = 100, LaserPowerInW = 0 };
            return converter;
        }

        private void CheckJob(FileInfo testFile)
        {
            using (var reader = FileReaderWriterFactory.FileReaderFactory.CreateNewReader(testFile.Extension))
            {
                reader.OpenJob(testFile.FullName, new FileReaderWriterFactory.FileReaderWriterProgress());
                var job = reader.CacheJobToMemory();

                CheckJob(job);
            }
        }

        private void CheckJob(Job job)
        {
            CheckerConfig config = new CheckerConfig
            {
                CheckLineSequencesClosed = CheckAction.DONTCHECK,
                CheckMarkingParamsKeys = CheckAction.CHECKERROR,
                CheckPartKeys = CheckAction.CHECKERROR,
                CheckPatchKeys = CheckAction.DONTCHECK,
                CheckVectorBlocksNonEmpty = CheckAction.CHECKERROR,
                CheckWorkPlanesNonEmpty = CheckAction.CHECKERROR,

                ErrorHandling = ErrorHandlingMode.THROWEXCEPTION
            };

            CheckerResult checkResult = PlausibilityChecker.CheckJob(job, config).GetAwaiter().GetResult();
            Assert.AreEqual(OverallResult.ALLSUCCEDED, checkResult.Result);
            Assert.AreEqual(0, checkResult.Errors.Count);
            Assert.AreEqual(0, checkResult.Warnings.Count);
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
