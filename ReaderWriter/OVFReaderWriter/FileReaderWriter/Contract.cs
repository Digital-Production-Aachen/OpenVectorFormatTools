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

namespace OpenVectorFormat.OVFReaderWriter
{
    /// <summary>
    /// Contract variables for the OVF format.
    /// OVF file layout: [MagicNumber][LUTIndex   ][WorkPlane1][...][WorkPlaneN][JobShell][LUT]
    /// </summary>
    class Contract
    {
        internal static readonly byte[] magicNumber = { 0x4c, 0x56, 0x46, 0x21 };
        
        // only Int64 and not UInt64 because the length of a filestream is given by Int64 (long) in C#.
        internal const Int64 defaultLUTIndex = 0;
    }
}
