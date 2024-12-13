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
using OpenVectorFormat;
using OpenVectorFormat.ILTFileReader;
using OpenVectorFormat.Utils;
using System;

namespace ILTFileReaderAdapter.OVFToCLIAdapter
{
    internal class OVFCliLineSequence3D : IVectorBlock
    {
        private VectorBlock hatchBlock;

        public Span<float> Coordinates => hatchBlock.LineSequence3D.Points.AsSpan();

        public int Id => hatchBlock.MetaData == null ? 1 : hatchBlock.MetaData.PartKey;

        public int N => hatchBlock.LineSequence3D.Points.Count / 2;


        public OVFCliLineSequence3D(VectorBlock hatchBlock)
        {
            this.hatchBlock = hatchBlock ?? throw new ArgumentNullException(nameof(hatchBlock));
            if (hatchBlock.VectorDataCase != VectorBlock.VectorDataOneofCase.LineSequence3D)
                throw new ArgumentException($"invalid vector data case {hatchBlock.VectorDataCase}, must be LineSequence 3D");
        }
    }
}