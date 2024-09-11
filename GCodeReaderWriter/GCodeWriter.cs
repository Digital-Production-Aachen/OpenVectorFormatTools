/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2024 Digital-Production-Aachen

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

﻿using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCodeReaderWriter
{
    public class GCodeWriter : FileWriter
    {
        public override FileWriteOperation FileOperationInProgress => throw new NotImplementedException();

        public override Job JobShell => throw new NotImplementedException();

        public override void AppendVectorBlock(VectorBlock block)
        {
            throw new NotImplementedException();
        }

        public override void AppendWorkPlane(WorkPlane workPlane)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void SimpleJobWrite(Job job, string filename, IFileReaderWriterProgress progress = null)
        {
            throw new NotImplementedException();
        }

        public override void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress = null)
        {
            throw new NotImplementedException();
        }
    }
}
