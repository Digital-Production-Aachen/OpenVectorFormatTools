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



using Grpc.Net.Client;
using GrpcReaderWriterInterface;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using OpenVectorFormat.FileReaderWriterFactory;
using OpenVectorFormat.AbstractReaderWriter;
using Grpc.Core;
using System.Linq;
using System.Diagnostics;
using UnitTests;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class TestGRPCWrapper
    {
        [TestMethod]
        public void TestSupportedFormat()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            string ip = "127.0.0.1";
            uint port = 50051;
            using GrpcChannel channel = GrpcChannel.ForAddress("http://" + ip + ":" + port.ToString());

            VectorFileHandler.VectorFileHandlerClient client = new VectorFileHandler.VectorFileHandlerClient(channel);
            foreach (string extension in FileWriterFactory.SupportedFileFormats)
            {
                IsFormatSupportedReply localreply = client.IsFormatSupported(new IsFormatSupportedRequest { FileExtension = extension });
                Assert.IsTrue(localreply.WriteSupport);
            }

            foreach (string extension in FileReaderFactory.SupportedFileFormats)
            {
                IsFormatSupportedReply localreply = client.IsFormatSupported(new IsFormatSupportedRequest { FileExtension = extension });
                Assert.IsTrue(localreply.ReadSupport);
            }
            IsFormatSupportedReply reply = client.IsFormatSupported(new IsFormatSupportedRequest { FileExtension = "abc" });
            Assert.AreEqual(reply.AllReadSupportedFormats, string.Join(";", FileReaderFactory.SupportedFileFormats));
            Assert.AreEqual(reply.AllWriteSupportedFormats, string.Join(";", FileWriterFactory.SupportedFileFormats));

            channel.ShutdownAsync().Wait();
        }

        // for this test files of all supported formats have to be placed under TestFiles
        // either directly in this Repo or by a relative link (to e.g. submodules containing the test files (add existing element)
        // finally all test files have to be marked to be copied to the run folder (right click/Properties/Copy if newer)
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [DynamicData("TestFiles")]
        [TestMethod]
        public async System.Threading.Tasks.Task TestWriteReadAsync(FileInfo testFile)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            string ip = "127.0.0.1";
            uint port = 50051;
            using GrpcChannel channel = GrpcChannel.ForAddress("http://" + ip + ":" + port.ToString(),
                new GrpcChannelOptions
                    {
                        MaxReceiveMessageSize = null,
                        MaxSendMessageSize = null,
                    }
            );

            VectorFileHandler.VectorFileHandlerClient client = new VectorFileHandler.VectorFileHandlerClient(channel);

            // read file from disc directly
            FileReader originalReader = FileReaderFactory.CreateNewReader(testFile.Extension);
            originalReader.OpenJob(testFile.FullName, new FileReaderWriterProgress());

            // read file via grpc (simple)
            SimpleJobReadReply simpleReadReply = client.SimpleJobRead(new SimpleJobReadRequest { JobUri = testFile.FullName });
            Assert.AreEqual(originalReader.CacheJobToMemory(), simpleReadReply.Job);

            // read file vie grpc (partial) not ready yet
            using (var call = client.PartialRead())
            {
                // open reader
                PartialReadRequest request = new PartialReadRequest();
                request.SelectedCommandMode = PartialReadCommandMode.OpenJob;
                request.JobUri = testFile.FullName;
                request.ReflectRequest = true;
                await call.RequestStream.WriteAsync(request);
                await call.ResponseStream.MoveNext();
                PartialReadReply partialReply = call.ResponseStream.Current;
                Assert.AreEqual(request, partialReply.Request);

                // get jobfile shell
                // NOTE - job file shell does NOT get assertet yet. Just a successfull command call is asserted.
                PartialReadRequest getJobShellRequest = new PartialReadRequest();
                getJobShellRequest.SelectedCommandMode = PartialReadCommandMode.GetJobShell;
                getJobShellRequest.ReflectRequest = true;
                await call.RequestStream.WriteAsync(getJobShellRequest);
                await call.ResponseStream.MoveNext();
                PartialReadReply getJobShellReply = call.ResponseStream.Current;
                Assert.AreEqual(getJobShellRequest, getJobShellReply.Request);

                // get all planes and assert they are good.
                int numPlanes = getJobShellReply.JobShell.NumWorkPlanes;
                for (int iWorkPlanes = 0; iWorkPlanes < numPlanes; iWorkPlanes++)
                {
                    PartialReadRequest getPlaneRequest = new PartialReadRequest();
                    getPlaneRequest.SelectedCommandMode = PartialReadCommandMode.GetPlane;
                    getPlaneRequest.PlaneIndex = iWorkPlanes;
                    getPlaneRequest.ReflectRequest = true;
                    await call.RequestStream.WriteAsync(getPlaneRequest);
                    await call.ResponseStream.MoveNext();
                    PartialReadReply getPlaneReply = call.ResponseStream.Current;
                    Assert.AreEqual(originalReader.GetWorkPlane(iWorkPlanes), getPlaneReply.WorkPlane);
                    Assert.AreEqual(getPlaneRequest, getPlaneReply.Request);

                    // additionally, get all vector blocks separetely
                    int numBlocks = getPlaneReply.WorkPlane.NumBlocks;
                    for (int iBlocks = 0; iBlocks < numBlocks; iBlocks++)
                    {
                        PartialReadRequest getBlockRequest = new PartialReadRequest();
                        getBlockRequest.SelectedCommandMode = PartialReadCommandMode.GetVectorBlock;
                        getBlockRequest.PlaneIndex = iWorkPlanes;
                        getBlockRequest.VectorBlockIndex = iBlocks;
                        getBlockRequest.ReflectRequest = true;
                        await call.RequestStream.WriteAsync(getBlockRequest);
                        await call.ResponseStream.MoveNext();
                        PartialReadReply getBlockReply = call.ResponseStream.Current;
                        Assert.AreEqual(originalReader.GetVectorBlock(iWorkPlanes, iBlocks), getBlockReply.VectorBlock);
                        Assert.AreEqual(getBlockRequest, getBlockReply.Request);
                    }
                }

                call.Dispose();
            }

            // test writing & re-reading for supported formats
            foreach (string extension in FileWriterFactory.SupportedFileFormats)
            {

                FileInfo target = new FileInfo(Path.GetTempFileName() + extension);

                Console.WriteLine("Converting from {0} to {1}", testFile.Extension, target.Extension);

                SimpleJobWriteReply reply2 = client.SimpleJobWrite(new SimpleJobWriteRequest { JobUri = target.FullName, Job = originalReader.CacheJobToMemory() });

                // read written file back directly
                FileReader convertedReader = FileReaderFactory.CreateNewReader(target.Extension);
                convertedReader.OpenJob(target.FullName, new FileReaderWriterProgress());

                Job originalJob = originalReader.CacheJobToMemory();
                Job convertedJob = convertedReader.CacheJobToMemory();


                #region Debug
                if (target.Extension == ".cli")
                {
                    for (int i = 0; i < originalJob.PartsMap.Count; i++)
                    {
                        var partOriginal = originalJob.PartsMap.Values.ToList()[i];
                        var partConverted = convertedJob.PartsMap.Values.ToList()[i];
                        if (partOriginal.GeometryInfo != null)
                        {
                            if (partConverted.GeometryInfo == null) partConverted.GeometryInfo = partOriginal.GeometryInfo.Clone();
                            //partConverted.GeometryInfo.BuildHeightInMm = partOriginal.GeometryInfo.BuildHeightInMm;
                            partConverted.ParentPartName = partOriginal.ParentPartName;
                            partConverted.Material = partOriginal.Material;
                        }
                    }
                    convertedJob.MarkingParamsMap.Clear();
                    foreach (var key in originalJob.MarkingParamsMap.Keys)
                    {
                        convertedJob.MarkingParamsMap.Add(key, originalJob.MarkingParamsMap[key]);
                    }
                    //convertedJob.JobMetaData.Version = originalJob.JobMetaData.Version;
                    //convertedJob.JobMetaData.Bounds = originalJob.JobMetaData.Bounds;
                    convertedJob.JobMetaData = originalJob.JobMetaData?.Clone();

                    for (int i = 0; i < Math.Min(originalJob.WorkPlanes.Count, convertedJob.WorkPlanes.Count); i++)
                    {
                        //if (originalJob.WorkPlanes[i].MetaData != null)
                        //{
                        //    convertedJob.WorkPlanes[i].MetaData = originalJob.WorkPlanes[i].MetaData?.Clone();
                        //}
                        var wp1 = originalJob.WorkPlanes[i];
                        var wp2 = convertedJob.WorkPlanes[i];

                        for (int j = 0; j < Math.Min(originalJob.WorkPlanes[i].VectorBlocks.Count, convertedJob.WorkPlanes[i].VectorBlocks.Count); j++)
                        {
                            var vb1 = originalJob.WorkPlanes[i].VectorBlocks[j];
                            var vb2 = convertedJob.WorkPlanes[i].VectorBlocks[j];

                            vb2.MetaData = vb1.MetaData?.Clone();
                            vb1.MarkingParamsKey = 0;
                            vb1.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Part;
                            vb1.LpbfMetadata.SkinCoreStrategyArea = VectorBlock.Types.LPBFMetadata.Types.SkinCoreStrategyArea.OuterHull;

                            vb2.LpbfMetadata = vb1.LpbfMetadata?.Clone();
                        }
                    }

                    //Delete 3D Data from asp
                    if (testFile.Extension == ".asp")
                    {
                        for (int i = 0; i < originalJob.WorkPlanes.Count; i++)
                        {
                            var removeOutOfList = new List<VectorBlock>();
                            for (int j = 0; j < originalJob.WorkPlanes[i].VectorBlocks.Count; j++)
                            {
                                var vb = originalJob.WorkPlanes[i].VectorBlocks[j];
                                if (vb.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence3D ||
                                    vb.VectorDataCase == VectorBlock.VectorDataOneofCase.PointSequence3D ||
                                    vb.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches3D)
                                {
                                    removeOutOfList.Add(vb);
                                }
                            }
                            removeOutOfList.ForEach(vb => originalJob.WorkPlanes[i].VectorBlocks.Remove(vb));
                            originalJob.WorkPlanes[i].NumBlocks = originalJob.WorkPlanes[i].VectorBlocks.Count;
                        }
                    }
                }
                #endregion


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

                if (target.Extension != testFile.Extension)
                {
                    // all formats except ovf are unable to store meta data
                    var job = originalJob;
                    if (target.Extension == ".ovf")
                        job = convertedJob;

                    originalJob.JobMetaData.Bounds = null;
                    convertedJob.JobMetaData.Bounds = null;

                    job.JobParameters = null;
                    foreach (var workplane in job.WorkPlanes)
                    {
                        workplane.MetaData = null;
                    }
                }

                bool failed = false;
                if (!originalJob.Equals(convertedJob))
                {
                    var nonEqual = AbstractVectorFileHandlerUtils.NonEqualFieldsDebug(originalJob, convertedJob);
                    Debug.Print($"WorkPlaneStats differs:\r{String.Join("\r", nonEqual)}\r");
                    failed = true;
                }
                Assert.IsFalse(failed);

            }
            channel.ShutdownAsync().Wait();
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