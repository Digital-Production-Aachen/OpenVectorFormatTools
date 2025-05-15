/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ILTFileReader.OVFToCLIAdapter
{
    internal class OVFCliHatches : IHatches
    {
        private VectorBlock hatchBlock;

        public OVFCliHatches(VectorBlock hatchBlock)
        {
            this.hatchBlock = hatchBlock ?? throw new ArgumentNullException(nameof(hatchBlock));
            if(hatchBlock.VectorDataCase != VectorBlock.VectorDataOneofCase.Hatches)
                throw new ArgumentException($"invalid vector data case {hatchBlock.VectorDataCase}, must be Hatches");
        }

        public IList<IHatch> hatches
        {
            get
            {
                var points = hatchBlock.Hatches.Points;
                var result = new List<IHatch>(points.Count / 4);
                for(int i=0; i< points.Count; i += 4)
                {
                    var start = new Point2D(points[i], points[i + 1]);
                    var end = new Point2D(points[i + 2], points[i + 3]);
                    result.Add(new Hatch(start, end));
                }
                return result;
            }
        }

        public Span<float> Coordinates => hatchBlock.Hatches.Points.AsSpan();

        public int Id => hatchBlock.MetaData == null ? 1 : hatchBlock.MetaData.PartKey;

        public int N => hatchBlock.Hatches.Points.Count / 2;

        public int Power => throw new NotImplementedException();

    }
}
