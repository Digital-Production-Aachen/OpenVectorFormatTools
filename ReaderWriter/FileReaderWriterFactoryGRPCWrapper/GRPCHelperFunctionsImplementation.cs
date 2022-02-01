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



using System;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcReaderWriterInterface;
using OpenVectorFormat.FileReaderWriterFactory;

namespace OpenVectorFormat.FileHandlerFactoryGRPCWrapper
{
    /// <summary>
    /// Implements server calls for grpc connection
    /// </summary>
    partial class GRPCWrapperFunctionsImplementation : VectorFileHandler.VectorFileHandlerBase
    {
        /// <summary>
        /// Check if a format is supported
        /// </summary>
        /// <param name="request">Message with extension as string</param>
        /// <param name="context"></param>
        /// <returns>IsFormatSupportedOutput message detailing if Reading / Writing of format is supported, and string of all supported formats.</returns>
        public override Task<IsFormatSupportedReply> IsFormatSupported(IsFormatSupportedRequest request, ServerCallContext context)
        {
            //Console.WriteLine("\"IsFormatSupported\" called with format " + request.FileExtension);
            string fileExtension = request.FileExtension;
            string[] tmpArray = fileExtension.Split('.');
            fileExtension = tmpArray[tmpArray.Length - 1];

            IsFormatSupportedReply ret = new IsFormatSupportedReply();

            ret.ReadSupport = FileReaderFactory.SupportedFileFormats.Contains("." + fileExtension);
            ret.WriteSupport = FileWriterFactory.SupportedFileFormats.Contains("." + fileExtension);
            ret.AllReadSupportedFormats = string.Join(";", FileReaderFactory.SupportedFileFormats);
            ret.AllWriteSupportedFormats = string.Join(";", FileWriterFactory.SupportedFileFormats);

            return Task.FromResult(ret);
        }
    }
}
