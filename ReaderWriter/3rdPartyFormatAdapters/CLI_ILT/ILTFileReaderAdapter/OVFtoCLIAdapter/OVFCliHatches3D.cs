using OpenVectorFormat;
using OpenVectorFormat.ILTFileReader;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ILTFileReaderAdapter.OVFtoCLIAdapter
{
    internal class OVFCliHatches3D : IVectorBlock
    {
        private VectorBlock hatchBlock;

        public Span<float> Coordinates => hatchBlock.Hatches3D.Points.AsSpan();

        public int Id => hatchBlock.MetaData == null ? 1 : hatchBlock.MetaData.PartKey;

        public int N => hatchBlock.Hatches3D.Points.Count / 2;


        public OVFCliHatches3D(VectorBlock hatchBlock)
        {
            this.hatchBlock = hatchBlock ?? throw new ArgumentNullException(nameof(hatchBlock));
            if (hatchBlock.VectorDataCase != VectorBlock.VectorDataOneofCase.Hatches3D)
                throw new ArgumentException($"invalid vector data case {hatchBlock.VectorDataCase}, must be Hatches 3D");
        }
    }
}
