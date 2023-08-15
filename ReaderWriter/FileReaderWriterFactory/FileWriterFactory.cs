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
using System.Linq;
using ILTFileReaderAdapter.OVFToCLIAdapter;
using OpenVectorFormat.AbstractReaderWriter;

namespace OpenVectorFormat.FileReaderWriterFactory
{
    public class FileWriterFactory
    {
        public static FileWriter CreateNewWriter(string extension)
        {
            FileWriter newFileWriter;
            if (OVFReaderWriter.OVFFileWriter.SupportedFileFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                newFileWriter = new OVFReaderWriter.OVFFileWriter();
            }
            else if (ASPFileReaderWriter.ASPFileWriter.SupportedFileFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                newFileWriter = new ASPFileReaderWriter.ASPFileWriter();
            }
            else if (CLIWriterAdapter.SupportedFileFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                newFileWriter = new CLIWriterAdapter();
            }
            else
            {
                throw new ArgumentException("format " + extension + " is not supported");
            }
            return newFileWriter;
        }

        public static List<string> SupportedFileFormats
        {
            get {
                List<string> formats = new List<string>();
                formats.AddRange(OVFReaderWriter.OVFFileWriter.SupportedFileFormats);
                formats.AddRange(ASPFileReaderWriter.ASPFileWriter.SupportedFileFormats);
                return formats;
            }
        }
    }
}
