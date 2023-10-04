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

﻿using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.FileReaderWriterFactory;
using System.IO;
using System.Threading.Tasks;

namespace Streaming
{
    public static class OVFStreamingWriter
    {
        public static async Task StreamToFile(FileReader fileToWrite, string targetFile)
        {
            var fileInfo = new FileInfo(targetFile);
            using (var writer = FileWriterFactory.CreateNewWriter(fileInfo.Extension))
            {
                writer.StartWritePartial(fileToWrite.JobShell, targetFile, null);
                for (int i = 0; i < fileToWrite.JobShell.NumWorkPlanes; i++)
                {
                    var wp = fileToWrite.GetWorkPlaneShell(i);
                    await writer.AppendWorkPlaneAsync(wp);
                }
            }
        }
    }
}
