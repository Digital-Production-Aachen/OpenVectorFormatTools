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



using OpenVectorFormat.ILTFileReader.Controller;
using OpenVectorFormat.ILTFileReaderAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.ASPFileReaderWriterAdapter;

namespace OpenVectorFormat.FileReaderWriterFactory
{
    public class FileReaderFactory
    {
        public static FileReader CreateNewReader(string extension)
        {
            FileReader newFileReader;
            if (IltFileAccess.SupportedFileFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                newFileReader = new ILTFileReaderAdapter.ILTFileReaderAdapter(new IltFileAccess());//inject dependency
            }
            else if (CliFileAccess.SupportedFileFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                newFileReader = new ILTFileReaderAdapter.ILTFileReaderAdapter(new CliFileAccess());//inject dependency
            }
            else if (OVFReaderWriter.OVFFileReader.SupportedFileFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                newFileReader = new OVFReaderWriter.OVFFileReader();
            }
            else if (ASPFileReaderWriterAdapter.ASPFileReader.SupportedFileFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                newFileReader = new ASPFileReaderWriterAdapter.ASPFileReader();
            }
            else
            {
                throw new ArgumentException("format " + extension + " is not supported");
            }
            return newFileReader;
        }

        public static List<string> SupportedFileFormats
        {
            get {
                List<string> formats = new List<string>();
                formats.AddRange(OVFReaderWriter.OVFFileReader.SupportedFileFormats);
                formats.AddRange(IltFileAccess.SupportedFileFormats);
                formats.AddRange(CliFileAccess.SupportedFileFormats);
                formats.AddRange(ASPFileReaderWriterAdapter.ASPFileReader.SupportedFileFormats);
                return formats;
            }
        }
    }
}
