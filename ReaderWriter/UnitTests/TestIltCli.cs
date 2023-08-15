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



using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using OpenVectorFormat.ILTFileReader.Controller;
using OpenVectorFormat.ILTFileReader;
using OpenVectorFormat.Plausibility;
using OpenVectorFormat.FileReaderWriterFactory;
using System.Runtime.Intrinsics.Arm;
using OpenVectorFormat.OVFReaderWriter;
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
        public void TestWriteCliFiles(FileInfo fileName)
        {
            var reader = new ILTFileReaderAdapter.ILTFileReaderAdapter(new CliFileAccess());
            var progress = new FileReaderWriterProgress();
            reader.OpenJobAsync(fileName.FullName, progress).GetAwaiter().GetResult();
            var job = reader.CacheJobToMemoryAsync().GetAwaiter().GetResult();

            CLIWriterAdapter cliWriter = new CLIWriterAdapter();
            cliWriter.SimpleJobWriteAsync(job, fileName.FullName + ".cli", progress).Wait();
        }

        [DynamicData("CliFiles")]
        [DataTestMethod]
        public void TestCliFilesAddParams(FileInfo fileName)
        {
            var targetFile = new FileInfo(Path.GetTempFileName() + ".ovf");
            var converter = SetupConverter();

            converter.ConvertAsyncAddParams(fileName, targetFile, new FileReaderWriterFactory.FileReaderWriterProgress()).Wait();
            CheckJob(targetFile);
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

        [DynamicData("CliFiles")]
        [DataTestMethod]
        public void TestCliFilesAddParamsToMemory(FileInfo fileName)
        {
            var converter = SetupConverter();

            var job = converter.ConvertAsyncAddParams(fileName, new FileReaderWriterFactory.FileReaderWriterProgress());
            CheckJob(job);
        }

        private void CheckJob(FileInfo testFile)
        {
            using (var reader = FileReaderWriterFactory.FileReaderFactory.CreateNewReader(testFile.Extension))
            {
                reader.OpenJobAsync(testFile.FullName, new FileReaderWriterFactory.FileReaderWriterProgress());
                var job = reader.CacheJobToMemoryAsync().GetAwaiter().GetResult();

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
            Assert.AreNotEqual(0, cliFile.Header.NumLayers);
            //...or the information in the header is inconsistent
            Assert.AreEqual(cliFile.Header.NumLayers, cliFile.Geometry.Layers.Count);
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
    }
}