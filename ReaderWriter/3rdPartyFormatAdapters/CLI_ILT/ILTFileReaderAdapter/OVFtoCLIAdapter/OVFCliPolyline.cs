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
    internal class OVFCliPolyline : IPolyline
    {
        private VectorBlock polyBlock;

        public Direction Dir => Direction.clockwise;

        public OVFCliPolyline(VectorBlock polyBlock)
        {
            this.polyBlock = polyBlock ?? throw new ArgumentNullException(nameof(polyBlock));
            if (polyBlock.VectorDataCase != VectorBlock.VectorDataOneofCase.LineSequence)
                throw new ArgumentException($"invalid vector data case {polyBlock.VectorDataCase}, must be LineSequence");
        }

        public IList<IPoint2D> Points
        {
            get
            {
                var points = polyBlock.LineSequence.Points;
                var result = new List<IPoint2D>(points.Count / 2);
                for(int i=0; i<Points.Count; i += 2)
                {
                    result.Add(new Point2D(points[i], points[i + 1]));
                }
                return result;
            }
        }

        public Span<float> Coordinates => polyBlock.LineSequence.Points.AsSpan();

        public int Id => polyBlock.MetaData == null ? 1 : polyBlock.MetaData.PartKey;

        public int N => polyBlock.LineSequence.Points.Count / 2;

        public int Power => throw new NotImplementedException();

        Span<float> IVectorBlock.Coordinates => polyBlock.LineSequence.Points.AsSpan();
    }
}
