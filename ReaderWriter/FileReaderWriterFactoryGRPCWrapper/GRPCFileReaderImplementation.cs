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



using Grpc.Core;
using GrpcReaderWriterInterface;
using System;
using System.IO;
using System.Threading.Tasks;
using OpenVectorFormat.FileReaderWriterFactory;
using OpenVectorFormat.AbstractReaderWriter;

namespace OpenVectorFormat.FileHandlerFactoryGRPCWrapper
{
    /// <summary>
    /// Implements server calls for grpc connection
    /// </summary>
    partial class GRPCWrapperFunctionsImplementation : VectorFileHandler.VectorFileHandlerBase
    {
        /// <summary>
        /// Simple all-at-once reading of job
        /// </summary>
        /// <param name="readRequest">Input Message with flename</param>
        /// <param name="context"></param>
        /// <returns>SimpleJobReadOutput with message and <see cref="Job"/></returns>
        public override async Task<SimpleJobReadReply> SimpleJobRead(SimpleJobReadRequest readRequest, ServerCallContext context)
        {
            Console.WriteLine("\"SimpleJobRead\" called for " + readRequest.JobUri);
            string filename = readRequest.JobUri;

            string extension = Path.GetExtension(filename);
            if (!FileReaderFactory.SupportedFileFormats.Contains(extension))
            {
                string supFormats = string.Join(";", FileReaderFactory.SupportedFileFormats);
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Reading failed: FileFormat " + extension + " is not supported!\n Supported formats are " + supFormats));
            }

            if (!File.Exists(filename))
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Reading failed: File " + filename + " does not exist."));
            }
            FileReader reader = FileReaderFactory.CreateNewReader(extension);
            reader.OpenJob(filename, new FileReaderWriterProgress());
            SimpleJobReadReply ret = new SimpleJobReadReply();
            ret.InfoMessage = "File read from " + filename + "!";
            ret.Job = reader.CacheJobToMemory();
            reader.Dispose();
            return ret;
        }

        public override async Task PartialRead(Grpc.Core.IAsyncStreamReader<PartialReadRequest> requestStream, Grpc.Core.IServerStreamWriter<PartialReadReply> responseStream, Grpc.Core.ServerCallContext context)
        {
            bool disposeReader = false;
            bool readerStarted = false;
            string jobURI = null;
            FileReader reader= null;
            FileReaderWriterProgress progress = null;
            while (await requestStream.MoveNext() && !disposeReader)
            {

                PartialReadRequest inputMsg = requestStream.Current;
                PartialReadReply outputMsg = new PartialReadReply();
                outputMsg.RequestId = inputMsg.RequestId;
                switch (inputMsg.SelectedCommandMode)
                {
                    case PartialReadCommandMode.OpenJob:
                        Console.WriteLine("\"OpenJob\" command called for " + inputMsg.JobUri);

                        jobURI = inputMsg.JobUri;
                        string extension = Path.GetExtension(jobURI);
                        if (!FileReaderFactory.SupportedFileFormats.Contains(extension))
                        {
                            string supFormats = string.Join(";", FileReaderFactory.SupportedFileFormats);
                            throw new RpcException(new Status(StatusCode.InvalidArgument, "Reading failed: FileFormat " + extension + " is not supported!\n Supported formats are " + supFormats));
                        }

                        if (!File.Exists(jobURI))
                        {
                            throw new RpcException(new Status(StatusCode.InvalidArgument, "File " + jobURI + " does not exist!"));
                        }

                        reader = FileReaderFactory.CreateNewReader(extension);
                        reader.AutomatedCachingThresholdBytes = Config.AutomatedCachingThresholdBytes;
                        progress = new FileReaderWriterProgress();
                        try
                        {
                            reader.OpenJob(jobURI, progress);
                        }
                        catch (Exception ex)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.Internal, "Opening file " + jobURI + " for reading failed: " + ex.Message));
                        }
                        readerStarted = true;
                        break;

                    case PartialReadCommandMode.UnloadJobFromMemory:
                        // Console.WriteLine("\"UnloadJobFromMemory\" command called");

                        if (!readerStarted)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No active reader found. Start with \"OpenJobfile\" command."));
                        }

                        try
                        {
                            reader.UnloadJobFromMemory();
                        }
                        catch (Exception ex)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.Internal, "UnloadJobFromMemory failed: " + ex.Message));
                        }
                        break;

                    case PartialReadCommandMode.GetJobShell:
                        // Console.WriteLine("\"GetJobShell\" command called");

                        if (!readerStarted)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No active reader found. Start with \"OpenJobfile\" command."));
                        }

                        try
                        {
                            outputMsg.JobShell = reader.JobShell.Clone();
                        }
                        catch (Exception ex)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.Internal, "GetJobShell failed: " + ex.Message));
                        }
                        break;

                    case PartialReadCommandMode.GetPlane:
                        // Console.WriteLine("\"GetPlane\" command called");

                        if (!readerStarted)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No active reader found. Start with \"OpenJobfile\" command."));
                        }

                        try
                        {
                            outputMsg.WorkPlane = reader.GetWorkPlane(inputMsg.PlaneIndex);
                        }
                        catch (Exception ex)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.Internal, "GetPlane failed: " + ex.Message));
                        }
                        break;

                    case PartialReadCommandMode.GetVectorBlock:
                        // Console.WriteLine("\"GetVectorBlock\" command called");

                        if (!readerStarted)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No active reader found. Start with \"OpenJobfile\" command."));
                        }

                        try
                        {
                            outputMsg.VectorBlock = reader.GetVectorBlock(inputMsg.PlaneIndex, inputMsg.VectorBlockIndex);
                        }
                        catch (Exception ex)
                        {
                            reader?.Dispose();
                            throw new RpcException(new Status(StatusCode.Internal, "GetVectorBlock failed: " + ex.Message));
                        }
                        break;

                    //default:
                    //    reader?.Dispose();
                    //    throw new RpcException(new Status(StatusCode.FailedPrecondition, "Unrecognized PartialReadCommandMode " + inputMsg.SelectedCommandMode));

                }

                if (inputMsg.ReflectRequest) { outputMsg.Request = inputMsg; }
                await responseStream.WriteAsync(outputMsg);
                
            }
            reader?.Dispose();
        }
    }
}
