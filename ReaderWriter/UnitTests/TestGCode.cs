/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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

using GCodeReaderWriter.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat.GCodeReaderWriter;
using OpenVectorFormat.OVFReaderWriter;
using OpenVectorFormat.Plausibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnitTests;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class TestGCode
    {
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeToCommandObject(FileInfo fileInfo)
        {
            string[] testCommands = File.ReadAllLines(fileInfo.FullName);

            using (var reader = FileReaderWriterFactory.FileReaderFactory.CreateNewReader(fileInfo.Extension))
            {
                reader.OpenJob(fileInfo.FullName, new FileReaderWriterFactory.FileReaderWriterProgress());
                var job = reader.CacheJobToMemory();

                Assert.AreEqual(job.VectorCount(), CheckGCode(fileInfo).MovementCommandCount);
            }
        }

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeReaderAddParams(FileInfo fileInfo)
        {
            var targetFile = new FileInfo(Path.GetTempFileName() + ".ovf");
            var converter = SetupConverter();

            converter.ConvertAddParams(fileInfo, targetFile, new FileReaderWriterFactory.FileReaderWriterProgress());
            CheckJob(targetFile);
        }

        [DynamicData("GCodeFiles")]
        [TestMethod]
        public void TestGCodeReaderAddParamsToMemory(FileInfo fileInfo)
        {
            var converter = SetupConverter();

            var job = converter.ConvertAddParams(fileInfo, new FileReaderWriterFactory.FileReaderWriterProgress());
            CheckJob(job);
        }

        [DynamicData("OVFFiles")]
        [TestMethod]
        public void TestGCodeWriterOVFToGCode(FileInfo fileInfo)
        {
            var converter = SetupConverter();
            FileInfo gCodeOutputPath = new FileInfo(Path.Combine(Path.GetTempPath(), "writer_test.gcode"));

            FileReaderWriterFactory.FileConverter.Convert(fileInfo, gCodeOutputPath, new FileReaderWriterFactory.FileReaderWriterProgress());

            Assert.IsTrue(gCodeOutputPath.Exists, "G-code output file was not created.");
            (bool isGCodeValid, int movementCommandCount) = CheckGCode(gCodeOutputPath);

            Assert.IsTrue(isGCodeValid, "G-code output is not valid.");

            OVFFileReader testReader = new OVFFileReader
            {
                AutomatedCachingThresholdBytes = 0
            };
            testReader.OpenJob(fileInfo.FullName, new FileReaderWriterProgressDummy());
            Job testJob = testReader.CacheJobToMemory();
            Assert.AreEqual(movementCommandCount, testJob.VectorCount());
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

        static (bool IsValid, int MovementCommandCount) CheckGCode(FileInfo fileName)
        {
            string[] fileContent = File.ReadAllLines(fileName.FullName);
            int movementCommandCount = 0;
            var movePattern = new Regex(@"^\s*[Gg]0?[0-4]\b", RegexOptions.Compiled);

            foreach (string commandLine in fileContent)
            {
                string commandString = commandLine.Split(';')[0].Trim();
                if (string.IsNullOrEmpty(commandString))
                {
                    continue;
                }
                string[] commandParts = commandString.Split(' ');

                bool isMove = false;
                bool hasSpatialCoord = false;
                foreach (string commandPart in commandParts)
                {
                    if (!char.IsLetter(commandPart[0]) || (commandPart.Length > 1 && !float.TryParse(commandPart.Substring(1), out _)))
                    {
                        return (IsValid: false, MovementCommandCount: movementCommandCount);
                    }

                    if (movePattern.IsMatch(commandPart)) isMove = true;
                    char code = char.ToUpperInvariant(commandPart[0]);
                    if (code == 'X' || code == 'Y' || code == 'Z') hasSpatialCoord = true;
                }

                if (isMove && hasSpatialCoord) movementCommandCount++;
            }
            return (IsValid: true, MovementCommandCount: movementCommandCount);
        }

        //[TestMethod]
        //public void TestGCodeOnBunny()
        //{
        //    var fileInfo = new FileInfo(Path.Combine(dir.FullName, "bunny.ovf"));
        //    var converter = SetupConverter();
        //    FileInfo gCodeOutputPath = new FileInfo(Path.Combine(Path.GetTempPath(), "writer_test.gcode"));

        //    OVFFileReader ovf_reader = new OVFFileReader();
        //    ovf_reader.OpenJob(fileInfo.FullName, new FileReaderWriterFactory.FileReaderWriterProgress());
        //    Job originalJob = ovf_reader.CacheJobToMemory();

        //    FileReaderWriterFactory.FileConverter.Convert(fileInfo, gCodeOutputPath, new FileReaderWriterFactory.FileReaderWriterProgress());

        //    Assert.IsTrue(gCodeOutputPath.Exists, "G-code output file was not created.");
        //    (bool isGCodeValid, int movementCommandCount) = CheckGCode(gCodeOutputPath);

        //    Assert.IsTrue(isGCodeValid, "G-code output is not valid.");

        //    OVFFileReader testReader = new OVFFileReader
        //    {
        //        AutomatedCachingThresholdBytes = 0
        //    };
        //    testReader.OpenJob(fileInfo.FullName, new FileReaderWriterProgressDummy());
        //    Job testJob = testReader.CacheJobToMemory();

        //    originalJob.JobMetaData.Bounds = null;
        //    testJob.JobMetaData.Bounds = null;

        //    originalJob.JobParameters = null;
        //    foreach (var workplane in originalJob.WorkPlanes)
        //    {
        //        workplane.MetaData = null;
        //    }

        //    bool failed = false;
        //    if (!originalJob.Equals(testJob))
        //    {
        //        var nonEqual = AbstractVectorFileHandlerUtils.NonEqualFieldsDebug(originalJob, testJob);
        //        Debug.Print($"WorkPlaneStats differs:\r{String.Join("\r", nonEqual)}\r");
        //        failed = true;
        //    }
        //    Assert.IsFalse(failed);
        //}

        public static List<object[]> GCodeFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.gcode"); // grab all .gcode test files
                List<object[]> files = new List<object[]>(testFiles.Length);
                for (int i = 0; i < testFiles.Length; i++)
                {
                    files.Add(new object[] { testFiles[i] });
                }
                return files;
            }
        }

        public static List<object[]> OVFFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.ovf"); // grab all .ovf test files
                List<object[]> files = new List<object[]>(testFiles.Length);
                for (int i = 0; i < testFiles.Length; i++)
                {
                    files.Add(new object[] { testFiles[i] });
                }
                return files;
            }
        }

        // Tests reader extension of example command by introducing a new subclass, overriding a Parse* handler.
        private sealed class CountingGCodeReader : GCodeReader
        {
            public int LinearParseCount;

            protected override void ParseLinear(LinearInterpolationCmd cmd)
            {
                LinearParseCount++;
                base.ParseLinear(cmd);
            }
        }

        // Tests writer extension of example command by introducing a new subclass, overriding a move helper.
        private sealed class MarkingGCodeWriter : GCodeWriter
        {
            public const string Marker = "OVERRIDE_MARKER";

            protected override LinearInterpolationCmd TravelMove(float x, float y, float? z = null)
                => new LinearInterpolationCmd(PrepCode.G, 0, false, x, y, z, null, null, null, Marker);
        }

        // Tests reader extension of command handler by introducing a new subclass, overriding
        [TestMethod]
        public void TestGCodeReaderHandlerOverride()
        {
            FileInfo file = dir.GetFiles("*.gcode").First();

            int baseVectorCount;
            using (var baseReader = new GCodeReader())
            {
                baseReader.OpenJob(file.FullName, new FileReaderWriterFactory.FileReaderWriterProgress());
                baseVectorCount = baseReader.CacheJobToMemory().VectorCount();
            }

            using (var reader = new CountingGCodeReader())
            {
                reader.OpenJob(file.FullName, new FileReaderWriterFactory.FileReaderWriterProgress());
                Job job = reader.CacheJobToMemory();

                // Make sure the overridden handler was called at least once.
                Assert.IsTrue(reader.LinearParseCount > 0);

                // Make sure extension commands do not alter vector count.
                Assert.AreEqual(baseVectorCount, job.VectorCount()); 
            }
        }

        [TestMethod]
        public void TestGCodeWriterHandlerOverride()
        {
            Job job = TestJob();
            string outFile = Path.Combine(Path.GetTempPath(), "override_test.gcode");

            // Test that the base writer does not include the marker but the overridden writer does.
            new GCodeWriter().SimpleJobWrite(job, outFile);
            Assert.IsFalse(File.ReadAllText(outFile).Contains(MarkingGCodeWriter.Marker));

            new MarkingGCodeWriter().SimpleJobWrite(job, outFile);
            Assert.IsTrue(File.ReadAllText(outFile).Contains(MarkingGCodeWriter.Marker));
        }

        private static Job TestJob()
        {
            var job = new Job { NumWorkPlanes = 1 };
            var wp = new WorkPlane { ZPosInMm = 1f, WorkPlaneNumber = 0, NumBlocks = 1 };
            var vb = new VectorBlock { LineSequence = new VectorBlock.Types.LineSequence() };
            vb.LineSequence.Points.Add(0f);
            vb.LineSequence.Points.Add(0f);
            vb.LineSequence.Points.Add(10f);
            vb.LineSequence.Points.Add(10f);
            wp.VectorBlocks.Add(vb);
            job.WorkPlanes.Add(wp);
            return job;
        }
    }
}
