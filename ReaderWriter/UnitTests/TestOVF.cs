/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2022 Digital-Production-Aachen

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



using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.OVFReaderWriter;
using System.Threading.Tasks;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class TestOVF
    {
        [TestMethod]
        public async Task TestSimpleWriteSimpleReadAsync()
        {
            string test_filename = "SimpleWriteTest.ovf";
            Job job = SetupTestJob();
            Job originalJobToCompareTo = job.Clone();
            // Write test job to disk
            using (FileWriter simpleWriter = new OVFFileWriter())
            {
                IFileReaderWriterProgress progWrite = new FileReaderWriterProgressDummy();
                await simpleWriter.SimpleJobWriteAsync(job, test_filename, progWrite);
            }

            // read test job from disk
            using (FileReader simpleReader = new OVFFileReader())
            {
                IFileReaderWriterProgress progRead = new FileReaderWriterProgressDummy();
                await simpleReader.OpenJobAsync(test_filename, progRead);
                Job readJob = await simpleReader.CacheJobToMemoryAsync();
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
                    await testWriter.AppendWorkPlaneAsync(workPlaneShell);
                    for (int j = 0; j < workPlaneShell.NumBlocks; j++)
                    {
                        await testWriter.AppendVectorBlockAsync(job.WorkPlanes[i].VectorBlocks[j]);
                    }
                }
            }

            OVFFileReader testReader = new OVFFileReader
            {
                AutomatedCachingThresholdBytes = 0 // forces partial reading
            };
            IFileReaderWriterProgress progRead = new FileReaderWriterProgressDummy();
            await testReader.OpenJobAsync(test_filename, progRead);
            Job readJob = await testReader.CacheJobToMemoryAsync();
            Assert.AreEqual(originalJobToCompareTo, readJob);
            testReader.Dispose();
        }

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
