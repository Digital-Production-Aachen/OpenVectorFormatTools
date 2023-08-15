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

        public int Id => polyBlock.MetaData.PartKey;

        public int N => polyBlock.LineSequence.Points.Count / 2;
    }
}
