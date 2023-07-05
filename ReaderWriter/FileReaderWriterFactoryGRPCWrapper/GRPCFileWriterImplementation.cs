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



using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcReaderWriterInterface;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.FileReaderWriterFactory;

namespace OpenVectorFormat.FileHandlerFactoryGRPCWrapper
{
    /// <summary>
    /// Implements server calls for grpc connection
    /// </summary>
    partial class GRPCWrapperFunctionsImplementation : VectorFileHandler.VectorFileHandlerBase
    {
        /// <summary>
        /// Simple, all-at-once writing of job
        /// </summary>
        /// <param name="saveRequest">Input message with savepath and <see cref="Job"/></param>
        /// <param name="context"></param>
        /// <returns>SimpleJobWriteOutput message</returns>
        public async override Task<SimpleJobWriteReply> SimpleJobWrite(SimpleJobWriteRequest saveRequest, ServerCallContext context)
        {
            Console.WriteLine("\"SimpleJobWrite\" called for " + saveRequest.JobUri);
            string filename = saveRequest.JobUri;
            string extension = Path.GetExtension(filename);
            if (!FileWriterFactory.SupportedFileFormats.Contains(extension))
            {
                string supFormats = string.Join(";", FileWriterFactory.SupportedFileFormats);
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Writing failed: FileFormat " + extension + " is not supported!\n Supported formats are " + supFormats));
            }
            FileWriter writer = FileWriterFactory.CreateNewWriter(extension);
            FileReaderWriterProgress progress = new FileReaderWriterProgress();
            try
            {
                await writer.SimpleJobWriteAsync(saveRequest.Job, saveRequest.JobUri, progress);
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Writing the file " + filename + " failed. Error message: " + ex.Message));
            }
            SimpleJobWriteReply ret = new SimpleJobWriteReply();
            ret.InfoMessage = "File written to " + filename + "!";
            writer.Dispose();
            return await Task.FromResult(ret);
        }

        public override async Task PartialWrite(Grpc.Core.IAsyncStreamReader<PartialWriteRequest> requestStream, Grpc.Core.IServerStreamWriter<PartialWriteReply> responseStream, Grpc.Core.ServerCallContext context)
        {
            bool disposeWriter = false;

            bool writerStarted = false;
            string jobURI = null;
            FileWriter writer = null;
            FileReaderWriterProgress progress = null;

            while (await requestStream.MoveNext() && !disposeWriter)
            {
                PartialWriteRequest inputMsg = requestStream.Current;
                PartialWriteReply outputMsg = new PartialWriteReply();
                outputMsg.RequestId = inputMsg.RequestId;
                
                switch (inputMsg.SelectedCommandMode)
                {
                    case PartialWriteCommandMode.StartWritePartial:
                        Console.WriteLine("\t\"StartWritePartial\" command called for " + inputMsg.JobUri);

                        if (inputMsg.JobUri == string.Empty)
                        {
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Creating writer failed: jobURI is empty."));
                        }


                        jobURI = inputMsg.JobUri;
                        string extension = Path.GetExtension(jobURI);
                        if (!FileWriterFactory.SupportedFileFormats.Contains(extension))
                        {
                            string supFormats = string.Join(";", FileWriterFactory.SupportedFileFormats);
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Creating writer failed: FileFormat " + extension + " is not supported!\n Supported formats are " + supFormats));
                        }
                        writer = FileWriterFactory.CreateNewWriter(extension);
                        progress = new FileReaderWriterProgress();
                        try
                        {
                            writer.StartWritePartial(inputMsg.JobShell, jobURI, progress);
                        }
                        catch (Exception ex)
                        {
                            writer?.Dispose();
                            throw new RpcException(new Status(StatusCode.Internal, "Opening file " + jobURI + " for writing failed: " + ex.Message));
                        }
                        writerStarted = true;
                        break;

                    case PartialWriteCommandMode.AddPlanePartial:
                        // Console.WriteLine("\t\"AddPlanePartial\" command called");
                        if (!writerStarted)
                        {
                            writer?.Dispose();
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No active writer found. Start with \"StartWritePartial\" command."));
                        }

                        try
                        {
                            await writer.AppendWorkPlaneAsync(inputMsg.WorkPlane);
                        }
                        catch (Exception ex)
                        {
                            writer?.Dispose();
                            throw new RpcException(new Status(StatusCode.Internal, "Adding plane to " + jobURI + " failed: " + ex.Message));
                        }
                        break;

                    case PartialWriteCommandMode.AddVectorBlockPartial:
                        // Console.WriteLine("\t\"AddVectorBlockPartial\" command called");
                        if (!writerStarted)
                        {
                            writer?.Dispose();
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No active writer found. Start with \"StartWritePartial\" command."));
                        }

                        try
                        {
                            await writer.AppendVectorBlockAsync(inputMsg.VectorBlock);
                        }
                        catch (Exception ex)
                        {
                            writer?.Dispose();
                            throw new RpcException(new Status(StatusCode.Internal, "Adding vectorblock to " + jobURI + " failed: " + ex.Message));
                        }
                        break;

                    default:
                        writer?.Dispose();
                        throw new RpcException(new Status(StatusCode.FailedPrecondition, "Unrecognized PartialWriteCommandMode " + inputMsg.SelectedCommandMode));
                }

                if (inputMsg.ReflectRequest) { outputMsg.Request = inputMsg; }
                await responseStream.WriteAsync(outputMsg);
            }

            writer?.Dispose();
        }
    }
}
