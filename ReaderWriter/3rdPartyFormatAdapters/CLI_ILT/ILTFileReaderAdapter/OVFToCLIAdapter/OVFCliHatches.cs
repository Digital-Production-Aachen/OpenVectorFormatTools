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
