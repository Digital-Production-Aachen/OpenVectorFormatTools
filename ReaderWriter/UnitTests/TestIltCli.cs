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



using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Collections.Generic;
using OpenVectorFormat.ILTFileReader.Controller;
using OpenVectorFormat.ILTFileReader;
using OpenVectorFormat.Plausibility;
using FluentAssertions;
using OpenVectorFormat.OVFReaderWriter;
using OpenVectorFormat.FileReaderWriterFactory;
using ILTFileReaderAdapter.OVFToCLIAdapter;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class TestIltCli
    {
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [DynamicData("CliFiles")]
        [DataTestMethod]
        public void TestCliFiles(FileInfo fileName)
        {
            CliFileAccess cliFile = new CliFileAccess();
            cliFile.OpenFile(fileName.FullName);
            TestCLIFile(cliFile);
        }

        [DynamicData("CliFiles")]
        [DataTestMethod]
        public void TestCliFilesAddParams(FileInfo fileName)
        {
            var targetFile = new FileInfo(Path.GetTempFileName() + ".ovf");
            var converter = SetupConverter();

            converter.ConvertAddParams(fileName, targetFile, new FileReaderWriterFactory.FileReaderWriterProgress());
            CheckJob(targetFile);
        }

        [DynamicData("OvfFiles")]
        [DataTestMethod]
        public void TestWriteCliFile(FileInfo fileName)
        {
            var cliFile = new FileInfo(Path.GetTempFileName() + ".cli");

            var progress = new FileReaderWriterProgress();
            var dataFormat = DataFormatType.binary;
            CliFormatSettings.Instance.dataFormatType = dataFormat;
            FileReaderWriterFactory.FileConverter.ConvertAsync(fileName, cliFile, progress).GetAwaiter().GetResult();

            //Test
            CliFileAccess cliFileTest = new CliFileAccess();
            cliFileTest.OpenFile(cliFile.FullName);
            Assert.AreEqual(cliFileTest.Header.DataFormat, dataFormat);
            TestCLIFile(cliFileTest);
        }

        [DynamicData("OvfFiles")]
        [DataTestMethod]
        public void TestWriteCliFileASCII(FileInfo fileName)
        {
            var cliFile = new FileInfo(Path.GetTempFileName() + ".cli");

            var progress = new FileReaderWriterProgress();
            var dataFormat = DataFormatType.ASCII;
            CliFormatSettings.Instance.dataFormatType = dataFormat;
            FileReaderWriterFactory.FileConverter.ConvertAsync(fileName, cliFile, progress).GetAwaiter().GetResult();

            //Test
            CliFileAccess cliFileTest = new CliFileAccess();
            cliFileTest.OpenFile(cliFile.FullName);
            Assert.AreEqual(cliFileTest.Header.DataFormat, dataFormat);
            TestCLIFile(cliFileTest);
        }

        [DynamicData("OvfFiles")]
        [DataTestMethod]
        public void TestWriteCliFileForEOS(FileInfo fileName)
        {
            var cliFile = new FileInfo(Path.GetTempFileName() + ".cli");

            var progress = new FileReaderWriterProgress();
            CliFormatSettings.Instance.FormatForEOS = true;
            FileReaderWriterFactory.FileConverter.ConvertAsync(fileName, cliFile, progress).GetAwaiter().GetResult();

            //Test
            CliFileAccess cliFileTest = new CliFileAccess();
            cliFileTest.OpenFile(cliFile.FullName);

            Assert.AreEqual(CliFormatSettings.Instance.FormatForEOS, true);
            TestCLIFile(cliFileTest);
        }

        [DynamicData("OvfFiles")]
        [DataTestMethod]
        public void TestWriteCliFileForCliPlus(FileInfo fileName)
        {
            var cliFile = new FileInfo(Path.GetTempFileName() + ".cli");

            var progress = new FileReaderWriterProgress();
            var dataFormat = DataFormatType.ASCII;
            CliFormatSettings.Instance.dataFormatType = dataFormat;
            CliFileAccess.CliPlus = true;
            FileReaderWriterFactory.FileConverter.ConvertAsync(fileName, cliFile, progress).GetAwaiter().GetResult();

            //Test
            CliFileAccess cliFileTest = new CliFileAccess();
            cliFileTest.OpenFile(cliFile.FullName);
            Assert.AreEqual(cliFileTest.Header.DataFormat, dataFormat);
            TestCLIFile(cliFileTest);
        }

        private FileReaderWriterFactory.FileConverter SetupConverter()
        {
            FileReaderWriterFactory.FileConverter converter = new FileReaderWriterFactory.FileConverter();
            converter.SupportPostfix = "_support";
            converter.FallbackContouringParams = new MarkingParams() { LaserSpeedInMmPerS = 400, LaserPowerInW = 150 };
            converter.FallbackHatchingParams = new MarkingParams() { LaserSpeedInMmPerS = 900, LaserPowerInW = 250 };
            converter.FallbackSupportContouringParams = new MarkingParams() { LaserSpeedInMmPerS = 600, LaserPowerInW = 250 };
            converter.FallbackSupportHatchingParams = new MarkingParams() { LaserSpeedInMmPerS = 1500, LaserPowerInW = 400 };
            return converter;
        }

        [DataTestMethod]
        [DataRow("Box_support_solid_ascii_with_params.cli", 3)] // 
        [DataRow("netfabb_ascii_with_params.ilt", 4)] // 1 "standard" parameter set, 3 in modified file
        public void TestCLIMarkingParams(string fileName, int expectedMarkingParams)
        {
            var fileInfo = new FileInfo(Path.Combine(dir.FullName, "marking_params", fileName));
            using var reader = FileReaderWriterFactory.FileReaderFactory.CreateNewReader(fileInfo.Extension);
            reader.OpenJob(fileInfo.FullName);
            var job = reader.CacheJobToMemory();

            CheckerConfig config = new CheckerConfig
            {
                CheckLineSequencesClosed = CheckAction.DONTCHECK,
                CheckMarkingParamsKeys = CheckAction.CHECKERROR,
                CheckPartKeys = CheckAction.CHECKERROR,
                CheckPatchKeys = CheckAction.DONTCHECK,
                CheckVectorBlocksNonEmpty = CheckAction.CHECKERROR,
                CheckWorkPlanesNonEmpty = CheckAction.DONTCHECK,
                ErrorHandling = ErrorHandlingMode.THROWEXCEPTION
            };

            CheckerResult checkResult = PlausibilityChecker.CheckJob(job, config).GetAwaiter().GetResult();
            Assert.AreEqual(OverallResult.ALLSUCCEDED, checkResult.Result);
            Assert.AreEqual(0, checkResult.Errors.Count);
            Assert.AreEqual(0, checkResult.Warnings.Count);
            job.MarkingParamsMap.Count.Should().Be(expectedMarkingParams);
        }

        [DynamicData("CliFiles")]
        [DataTestMethod]
        public void TestCliFilesAddParamsToMemory(FileInfo fileName)
        {
            var converter = SetupConverter();

            var job = converter.ConvertAddParams(fileName, new FileReaderWriterFactory.FileReaderWriterProgress());
            CheckJob(job);
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

        [DynamicData("IltFiles")]
        [DataTestMethod]
        public void TestIltFiles(FileInfo fileName)
        {
            IltFileAccess iltFile = new IltFileAccess();
            iltFile.OpenFile(fileName.FullName);
            Assert.AreNotEqual(0, iltFile.ModelSections.Count);
            // treat every ModelSection in the .ilt file as an .cli file and use the designated test for it
            for (int i = 0; i < iltFile.ModelSections.Count; i++)
            {
                TestCLIFile(iltFile.ModelSections[i]);
            }
        }

        private void TestCLIFile(ICLIFile cliFile)
        {
            //Fail in case that there are no Parts...
            Assert.AreNotEqual(0, cliFile.Parts.Count);
            //...or WorkPlanes...
            //Assert.AreNotEqual(0, cliFile.Header.NumLayers); => TODO
            //...or the information in the header is inconsistent
            //Assert.AreEqual(cliFile.Header.NumLayers, cliFile.Geometry.Layers.Count); => TODO
            for (int i = 0; i < cliFile.Geometry.Layers.Count; i++)
            {
                ILayer layer = cliFile.Geometry.Layers[i];
                foreach (IVectorBlock vBlock in layer.VectorBlocks)
                {
                    //check if Vectorblocks are not empty
                    Assert.AreNotEqual(0, vBlock.Coordinates.Length);
                    //Test if start and end coordinates match
                    if (vBlock is IPolyline polyline)
                    {
                        if ((polyline.Dir == Direction.counterClockwise) || (polyline.Dir == Direction.clockwise))
                        {
                            Assert.AreEqual(polyline.Points[0], polyline.Points[polyline.N - 1]);
                        }
                    }
                }
            }
        }

        public static List<object[]> IltFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.ilt"); //getting all .ilt files
                List<object[]> files = new List<object[]>(testFiles.Length);
                for (int i = 0; i < testFiles.Length; i++)
                {
                    files.Add(new object[] { testFiles[i] });
                }
                return files;
            }
        }

        public static List<object[]> CliFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.cli"); //getting all .cli files
                List<object[]> files = new List<object[]>(testFiles.Length);
                for (int i = 0; i < testFiles.Length; i++)
                {
                    files.Add(new object[] { testFiles[i] });
                }
                return files;
            }
        }
        public static List<object[]> OvfFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.ovf"); //getting all .ovf files
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