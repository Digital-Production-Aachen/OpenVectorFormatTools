/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2021 Digital-Production-Aachen

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
using System.Text;
using System.Linq;
using System.Threading;
using OpenVectorFormat.AbstractReaderWriter;

namespace OpenVectorFormat.FileReaderWriterFactory
{
    public class FileConverter
    {
        /// <summary>
        /// Convert a given vector file (a supported reader must be available) into the target format.
        /// </summary>
        /// <param name="file">a file to load in a supported format</param>
        /// <param name="targetFormatExtension">extension with dot, e.g. ".cli"</param>
        public static async System.Threading.Tasks.Task ConvertAsync(FileInfo file, FileInfo targetFile, IFileReaderWriterProgress progress)
        {
            if(!FileReaderFactory.SupportedFileFormats.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("no reader available for extension " + file.Extension);
            }
            if (!FileWriterFactory.SupportedFileFormats.Contains(targetFile.Extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("no writer available for target extension " + targetFile.Extension);
            }
            using (var reader = FileReaderFactory.CreateNewReader(file.Extension))
            {
                await reader.OpenJobAsync(file.FullName, progress);
                using (var writer = FileWriterFactory.CreateNewWriter(targetFile.Extension))
                {
                    writer.StartWritePartial(reader.JobShell, targetFile.FullName, progress);

                    for (int i = 0; i < reader.JobShell.NumWorkPlanes; i++)
                    {
                        await writer.AppendWorkPlaneAsync(await reader.GetWorkPlaneAsync(i));
                    }
                }
            }
        }
    }
}
