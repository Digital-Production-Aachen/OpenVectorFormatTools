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



using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.OVFReaderWriter;
using OVFReaderWriter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class TestOVF
    {
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [TestMethod]
        public void TestOVFSummary()
        {
            string fileName = Path.Combine(dir.FullName, "SupportPart.ovf");
            StringBuilder sb = new();
            int indent = 0;
            using (OVFFileReader reader = new())
            {
                reader.OpenJob(fileName);
                Console.WriteLine(OVFStatistics.CreateSummary(reader));
                Console.WriteLine(OVFStatistics.GetJobShellInfoAsJSON(reader));
            }
        }

        [TestMethod]
        public void TestSimpleWriteSimpleReadAsync()
        {
            string test_filename = "SimpleWriteTest.ovf";
            Job job = SetupTestJob();
            Job originalJobToCompareTo = job.Clone();
            // Write test job to disk
            using (FileWriter simpleWriter = new OVFFileWriter())
            {
                IFileReaderWriterProgress progWrite = new FileReaderWriterProgressDummy();
                simpleWriter.SimpleJobWrite(job, test_filename, progWrite);
            }

            // read test job from disk
            using (FileReader simpleReader = new OVFFileReader())
            {
                IFileReaderWriterProgress progRead = new FileReaderWriterProgressDummy();
                simpleReader.OpenJob(test_filename, progRead);
                Job readJob = simpleReader.CacheJobToMemory();

                readJob.JobMetaData = null;
                foreach (var workplane in readJob.WorkPlanes)
                {
                    workplane.MetaData = null;
                }

                Assert.AreEqual(originalJobToCompareTo, readJob);
            }
        }

        [TestMethod]
        public async Task TestAsyncWriteReadAsync()
        {
            string test_filename = "AsyncWriteTest.ovf";
            Job job = SetupTestJob();
            Job originalJobToCompareTo = job.Clone();

            using (OVFFileWriter testWriter = new OVFFileWriter())
            {
                IFileReaderWriterProgress progWrite = new FileReaderWriterProgressDummy();
                testWriter.StartWritePartial(job, test_filename, progWrite);

                for (int i = 0; i < job.NumWorkPlanes; i++)
                {
                    WorkPlane workPlaneShell = job.WorkPlanes[(int)i].Clone();
                    workPlaneShell.VectorBlocks.Clear();
                    testWriter.AppendWorkPlane(workPlaneShell);
                    for (int j = 0; j < workPlaneShell.NumBlocks; j++)
                    {
                        testWriter.AppendVectorBlock(job.WorkPlanes[i].VectorBlocks[j]);
                    }
                }
            }

            OVFFileReader testReader = new OVFFileReader
            {
                AutomatedCachingThresholdBytes = 0 // forces partial reading
            };
            IFileReaderWriterProgress progRead = new FileReaderWriterProgressDummy();
            testReader.OpenJob(test_filename, progRead);
            Job readJob = testReader.CacheJobToMemory();

            readJob.JobMetaData = null;
            foreach (var workplane in readJob.WorkPlanes)
            {
                workplane.MetaData = null;
            }

            Assert.AreEqual(originalJobToCompareTo, readJob);
            testReader.Dispose();
        }

        [TestMethod]
        public void VectorCount_WithSynchronizationBlock_ReturnsZero()
        {
            var block = new VectorBlock
            {
                SyncBlock = new VectorBlock.Types.SynchronizationBlock
                {
                    VectorBlockIndexToWaitOn = 0
                }
            };

            int count = block.VectorCount();

            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void SimpleJobWrite_WithSynchronizationBlock_WritesReadableFile()
        {
            /*
             Create job in memory
            → write OVF file
            → close writer
            → open file with OVF reader
            → read blocks back
            → verify SyncBlock and ExposurePause
            → close reader
            → delete file*/

            string filePath = Path.Combine(Path.GetTempPath(), $"SynchronizationTest_{Guid.NewGuid():N}.ovf");
            var job = new Job();

            var workPlane = new WorkPlane
            {
                WorkPlaneNumber = 0
            };

            // Block 0: ordinary geometry.
            var lineSequence = new VectorBlock.Types.LineSequence();
            lineSequence.Points.Add(
            new float[]
            {
                0f, 0f,
                10f, 0f
            });

            workPlane.VectorBlocks.Add(new VectorBlock
            {
                LineSequence = lineSequence,
                LaserIndex = 0
            });

            // Block 1: laser 1 waits for block 0.
            workPlane.VectorBlocks.Add(new VectorBlock
            {
                SyncBlock = new VectorBlock.Types.SynchronizationBlock { VectorBlockIndexToWaitOn = 0 },
                LaserIndex = 1
            });

            // Block 2: laser 1 waits another 250 ms.
            workPlane.VectorBlocks.Add(new VectorBlock
            {
                ExposurePause = new VectorBlock.Types.ExposurePause
                {
                    PauseInUs = 250_000
                },
                LaserIndex = 1
            });

            workPlane.NumBlocks = workPlane.VectorBlocks.Count;

            job.WorkPlanes.Add(workPlane);
            job.NumWorkPlanes = job.WorkPlanes.Count;

            // Act: write the complete OVF file.
            using (var writer = new OVFFileWriter())
            {
                writer.SimpleJobWrite(job, filePath, new FileReaderWriterProgressDummy());
            }

            // Read the file back.
            using (var reader = new OVFFileReader())
            {
                reader.OpenJob(filePath, new FileReaderWriterProgressDummy());

                WorkPlane result = reader.GetWorkPlane(0);

                Assert.AreEqual(3, result.VectorBlocks.Count);

                VectorBlock geometryBlock = result.VectorBlocks[0];
                VectorBlock synchronizationBlock = result.VectorBlocks[1];
                VectorBlock pauseBlock = result.VectorBlocks[2];

                Assert.AreEqual(VectorBlock.VectorDataOneofCase.LineSequence, geometryBlock.VectorDataCase);
                Assert.AreEqual(VectorBlock.VectorDataOneofCase.SyncBlock, synchronizationBlock.VectorDataCase);

                Assert.AreEqual(0, synchronizationBlock.SyncBlock.VectorBlockIndexToWaitOn);
                Assert.AreEqual(1, synchronizationBlock.LaserIndex);
                Assert.AreEqual(VectorBlock.VectorDataOneofCase.ExposurePause, pauseBlock.VectorDataCase);
                Assert.AreEqual((ulong)250_000, pauseBlock.ExposurePause.PauseInUs);
                Assert.AreEqual(1, pauseBlock.LaserIndex);
            }

            // Confirms that writer and reader released the file.
            File.Delete(filePath);
            Assert.IsFalse(File.Exists(filePath));
        }


        // DEBUGGING
        //[TestMethod]
        //public void TestWPMetaDataNull()
        //{
        //    var workPlane = new WorkPlane();
        //    workPlane.MetaData = new WorkPlane.Types.WorkPlaneMetaData();
        //    workPlane.MetaData.Bounds = null;
        //    var jobShell = new Job();
        //    jobShell.JobMetaData = new Job.Types.JobMetaData();
        //    jobShell.JobMetaData.Bounds = null;
        //    using (var writer = new OVFFileWriter())
        //    {
        //        writer.StartWritePartial(jobShell, Path.GetTempPath() + "TestWPMetaDataNull.ovf");
        //        writer.AppendWorkPlane(workPlane);
        //    }
        //}

        private Job SetupTestJob()
        {
            OpenVectorFormat.Job job = new OpenVectorFormat.Job();
            int numWorkPlanes = 100;
            int numBlocksPerWorkPlane = 100;
            job.NumWorkPlanes = numWorkPlanes;

            for (int i = 0; i < numWorkPlanes; i++)
            {
                OpenVectorFormat.WorkPlane workPlane = new OpenVectorFormat.WorkPlane
                {
                    NumBlocks = numBlocksPerWorkPlane,
                    WorkPlaneNumber = i
                };
                job.WorkPlanes.Add(workPlane);
                for (int j = 0; j < numBlocksPerWorkPlane; j++)
                {
                    OpenVectorFormat.VectorBlock block = new OpenVectorFormat.VectorBlock
                    {
                        MetaData = new VectorBlock.Types.VectorBlockMetaData()
                    };
                    block.MetaData.PartKey = j+10;
                    job.WorkPlanes[i].VectorBlocks.Add(block);
                }
            }
            return job;
        }
        
    }
}
