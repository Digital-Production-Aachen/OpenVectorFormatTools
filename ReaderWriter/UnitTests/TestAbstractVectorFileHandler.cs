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



using OpenVectorFormat.FileReaderWriterFactory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpenVectorFormat.AbstractReaderWriter;
using System.Numerics;
using Google.Protobuf;
using System.Linq;
using Google.Protobuf.Reflection;
using System.Diagnostics;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class AbstractVectorFileHandler
    {
        // for this test files of all supported formats have to be placed under TestFiles
        // either directly in this Repo or by a relative link (to e.g. submodules containing the test files (add existing element)
        // finally all test files have to be marked to be copied to the run folder (right click/Properties/Copy if newer)
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [DynamicData("TestFiles")]
        [DataTestMethod]
        public async System.Threading.Tasks.Task SimpleWriteAndRead(FileInfo testFile)
        {
            // Checks if simply reading a job, writing it with the SimpleWrite function, and reading it again corrupts the job.
            if (FileReaderFactory.SupportedFileFormats.Contains(testFile.Extension) && FileWriterFactory.SupportedFileFormats.Contains(testFile.Extension))
            {
                IFileReaderWriterProgress progress = new FileReaderWriterProgress();
                FileReader fileReader = FileReaderFactory.CreateNewReader(testFile.Extension);
                await fileReader.OpenJobAsync(testFile.FullName, progress);
                Job originalJob = await fileReader.CacheJobToMemoryAsync();
                fileReader.Dispose();
                Job originalJobToCompareTo = originalJob.Clone();

                FileInfo target = new FileInfo(Path.GetTempFileName() + testFile.Extension);
                FileWriter fileWriter = FileWriterFactory.CreateNewWriter(target.Extension);
                await fileWriter.SimpleJobWriteAsync(originalJob, target.FullName, progress);

                FileReader fileReader2 = FileReaderFactory.CreateNewReader(testFile.Extension);
                await fileReader2.OpenJobAsync(target.FullName, progress);
                Job readJob = await fileReader2.CacheJobToMemoryAsync();
                Job orgJob = ASPHelperUtils.HandleJobCompareWithASPTarget(originalJobToCompareTo, readJob);
                Assert.AreEqual(orgJob, readJob);
            }
        }

        [DynamicData("TestFiles")]
        [DataTestMethod]
        public async Task TestConvertAndCompareAsync(FileInfo testFile)
        {
            Console.WriteLine("TestConvertAndCompare");

            IFileReaderWriterProgress progress = new FileReaderWriterProgress();
            foreach (string extension in FileWriterFactory.SupportedFileFormats)
            {
                FileInfo target = new FileInfo(Path.GetTempFileName() + extension);
                Debug.WriteLine("Converting from {0} to {1}", testFile.Extension, target.Extension);

                FileConverter.ConvertAsync(testFile, target, progress).Wait();
                FileReader originalReader = FileReaderFactory.CreateNewReader(testFile.Extension);
                FileReader convertedReader = FileReaderFactory.CreateNewReader(target.Extension);
                await originalReader.OpenJobAsync(testFile.FullName, progress);
                await convertedReader.OpenJobAsync(target.FullName, progress);

                Job originalJob = await originalReader.CacheJobToMemoryAsync();
                Job convertedJob = await convertedReader.CacheJobToMemoryAsync();

                if (target.Extension == ".asp")
                {
                    // ASP has no concept of workplanes, so only single-workplane jobs can be restored properly.
                    // After conversion, all workplanes are merged into one for ASP.
                    if (originalJob.WorkPlanes.Count > 1)
                    {
                        continue;
                    }
                    convertedJob = ASPHelperUtils.HandleJobCompareWithASPTarget(originalJob, convertedJob);
                }
                else if(target.Extension == ".cli")
                {
                    //TODO: Check what can be included as meta information into CLI
                    originalJob.MarkingParamsMap.Clear();
                    //originalJob.PartsMap.Clear();
                    for (int i = 0; i < originalJob.PartsMap.Count; i++)
                    {
                        var partOriginal = originalJob.PartsMap.Values.ToList()[i];
                        var partConverted = convertedJob.PartsMap.Values.ToList()[i];
                        if (partOriginal.GeometryInfo != null)
                        {
                            if (partConverted.GeometryInfo == null) partConverted.GeometryInfo = new Part.Types.GeometryInfo();
                            partConverted.GeometryInfo.BuildHeightInMm = partOriginal.GeometryInfo.BuildHeightInMm;
                        }
                    }
                    foreach (var wp in originalJob.WorkPlanes)
                    {
                        foreach (var vb in wp.VectorBlocks)
                        {
                            vb.MarkingParamsKey = 0;
                            if(vb.LpbfMetadata == null) vb.LpbfMetadata = new VectorBlock.Types.LPBFMetadata();
                            vb.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Part;
                        }
                    }

                    for (int i = 0; i < originalJob.WorkPlanes.Count; i++)
                    {
                        for (int j = 0; j < originalJob.WorkPlanes[i].VectorBlocks.Count; j++)
                        {
                            var vbOrigine = originalJob.WorkPlanes[i].VectorBlocks[j];
                            if(convertedJob.WorkPlanes.Count < i && convertedJob.WorkPlanes[i].VectorBlocks.Count < j)
                            {
                                var vbConvert = convertedJob.WorkPlanes[i].VectorBlocks[j];

                                //if(vbOrigine.LpbfMetadata.PartArea != vbConvert.LpbfMetadata.PartArea)
                                //{
                                //    bool dd = true;
                                //}
                                if (testFile.Extension == ".ilt")
                                {
                                    vbConvert.LpbfMetadata.PartArea = vbOrigine.LpbfMetadata.PartArea;
                                    vbConvert.LpbfMetadata.SkinType = vbOrigine.LpbfMetadata.SkinType;
                                }
                            }
                        }
                    }
                    convertedJob.MarkingParamsMap.Clear();
                    //convertedJob.PartsMap.Clear();
                    convertedJob.JobMetaData.Version = originalJob.JobMetaData.Version;

                }

                convertedJob.JobMetaData.Bounds = null;
                convertedJob.JobMetaData.JobName = originalJob.JobMetaData.JobName;
                foreach (var workplane in convertedJob.WorkPlanes)
                {
                    workplane.MetaData = null;
                }

                if (originalJob.JobMetaData.Version != convertedJob.JobMetaData.Version)
                {
                    Console.WriteLine("");
                }

                bool failed = false;
                if (!originalJob.Equals(convertedJob))
                {
                    var nonEqual = AbstractVectorFileHandlerUtiils.NonEqualFieldsDebug(originalJob, convertedJob);
                    Debug.Print($"WorkPlaneStats differs:\r{String.Join("\r", nonEqual)}\r");
                    failed = true;
                }
                Assert.IsFalse(failed);

                Vector2 translation = new Vector2(4, 5);
                float rotation =(float) Math.PI / 8;

                for (int i = 0; i < originalJob.WorkPlanes.Count; i++)
                {
                    var wp1 = originalJob.WorkPlanes[i];
                    var wp2 = convertedJob.WorkPlanes[i];
                    for (int j = 0; j < wp1.VectorBlocks.Count; j++)
                    {
                        var vb1 = wp1.VectorBlocks[j];
                        vb1.Translate(translation);
                        vb1.Rotate(rotation);
                        var vb2 = wp2.VectorBlocks[j];
                        vb2.Translate(translation);
                        vb2.Rotate(rotation);
                    }
                }

                Assert.AreEqual(originalJob, convertedJob);

                originalReader.Dispose();
                convertedReader.Dispose();
            }
        }

        /// <summary>
        /// tests some minimum capabilities of the format that all readers should implement
        /// </summary>
        /// <param name="testFile"></param>
        [DynamicData("TestFiles")]
        [TestMethod]
        public async System.Threading.Tasks.Task TestReadAsync(FileInfo testFile)
        {
            IFileReaderWriterProgress progress = new FileReaderWriterProgress();
            using (FileReader reader = FileReaderFactory.CreateNewReader(testFile.Extension))
            {
                await reader.OpenJobAsync(testFile.FullName, progress);
                Assert.IsTrue(reader.JobShell.NumWorkPlanes > 0); // job must have at least one workPlane
                //Job-WorkPlanes are allowed to be empty, must be retrieved by getWorkPlane
                Dictionary<Tuple<double, double, double, double, double, double>, long> workPlanePositions = new Dictionary<Tuple<double, double, double, double, double, double>, long>();
                for (int i = 0; i < reader.JobShell.NumWorkPlanes; i++)
                {
                    var workPlane = await reader.GetWorkPlaneAsync(i);
                    Tuple<double, double, double, double, double, double> workPlanePos = new Tuple<double, double, double, double, double, double>(workPlane.XPosInMm, workPlane.XRotInDeg, workPlane.YPosInMm, workPlane.YRotInDeg, workPlane.ZPosInMm, workPlane.ZRotInDeg);
                    Assert.IsFalse(workPlanePositions.TryGetValue(workPlanePos, out long workPlaneNumber),
                        "coordinates " + workPlanePos.ToString() + "already used by workPlane " + workPlaneNumber + ". " +
                        "WorkPlanes with same coordinates must be merged");
                    workPlanePositions.Add(workPlanePos, workPlane.WorkPlaneNumber);
                    TestWorkPlane(workPlane);
                }
            }
        }

        private void TestWorkPlane(WorkPlane workPlane)
        {
            // workPlane must have at least one block. Empty workPlanes should be discarded by the reader, test should indicate that data parsing failed.
            Assert.IsTrue(workPlane.NumBlocks > 0);
            Assert.IsNotNull(workPlane.VectorBlocks);
            Assert.AreEqual(workPlane.NumBlocks, workPlane.VectorBlocks.Count);
            foreach (var vb in workPlane.VectorBlocks)
            {
                TestVectorBlock(vb);
            }
        }

        private void TestVectorBlock(VectorBlock vectorBlock)
        {
            Assert.IsNotNull(vectorBlock);
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.None:
                    Assert.Fail("vectorData empty");
                    break;
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    Assert.IsTrue(vectorBlock.LineSequence.Points.Count > 0);
                    Assert.IsTrue(vectorBlock.LineSequence.Points.Count % 2 == 0);
                    break;
                case VectorBlock.VectorDataOneofCase.Hatches:
                    Assert.IsTrue(vectorBlock.Hatches.Points.Count > 0);
                    Assert.IsTrue(vectorBlock.Hatches.Points.Count % 2 == 0);
                    break;
                case VectorBlock.VectorDataOneofCase.PointSequence:
                    Assert.IsTrue(vectorBlock.PointSequence.Points.Count > 0);
                    break;
                case VectorBlock.VectorDataOneofCase.Arcs:
                    Assert.IsTrue(vectorBlock.Arcs.Centers.Count > 0);
                    Assert.IsTrue(vectorBlock.Arcs.Centers.Count % 2 == 0);
                    break;
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    Assert.IsTrue(vectorBlock.Ellipses.EllipsesArcs.Centers.Count > 0);
                    Assert.IsTrue(vectorBlock.Ellipses.EllipsesArcs.Centers.Count % 2 == 0);
                    break;
                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                    Assert.IsTrue(vectorBlock.LineSequence3D.Points.Count > 0);
                    Assert.IsTrue(vectorBlock.LineSequence3D.Points.Count % 3 == 0);
                    break;
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                    Assert.IsTrue(vectorBlock.Hatches3D.Points.Count > 0);
                    Assert.IsTrue(vectorBlock.Hatches3D.Points.Count % 3 == 0);
                    break;
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                    Assert.IsTrue(vectorBlock.PointSequence3D.Points.Count > 0);
                    Assert.IsTrue(vectorBlock.PointSequence3D.Points.Count % 3 == 0);
                    break;
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                    Assert.IsTrue(vectorBlock.Arcs3D.Centers.Count > 0);
                    Assert.IsTrue(vectorBlock.Arcs3D.Centers.Count % 3 == 0);
                    break;
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    Assert.IsTrue(vectorBlock.ExposurePause.PauseInUs > 0);
                    break;
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    Assert.IsTrue(vectorBlock.LineSequenceParaAdapt.PointsWithParas.Count > 0);
                    Assert.IsTrue(vectorBlock.LineSequenceParaAdapt.PointsWithParas.Count % 3 == 0);
                    break;
            }
        }

        public static List<object[]> TestFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles(); //getting all .cli files
                List<object[]> files = new List<object[]>(testFiles.Length);
                for (int i = 0; i < testFiles.Length; i++)
                {
                    // if (testFiles[i].Extension == ".cli" || testFiles[i].Extension == ".ilt") { continue; }
                    files.Add(new object[] { testFiles[i] });
                }
                return files;
            }
        }
    }
}
