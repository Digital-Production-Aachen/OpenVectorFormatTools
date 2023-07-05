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
using OpenVectorFormat.AbstractReaderWriter;

namespace OpenVectorFormat.FileReaderWriterFactory
{
    public class FileReaderWriterProgress : IFileReaderWriterProgress
    {
        private bool _isCancelled;
        private bool _isFinished;
        public bool IsCancelled { get => _isCancelled; set => _isCancelled = value; }
        public bool IsFinished { get => _isFinished; set => _isFinished = value; }

        public void Update(string message, int progressPerCent)
        {
            Console.WriteLine(message + " progress [%]: " + progressPerCent);
        }
    }
}
