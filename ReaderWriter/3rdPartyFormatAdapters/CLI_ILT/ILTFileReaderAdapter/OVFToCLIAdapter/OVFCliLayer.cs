using ILTFileReader.OVFToCLIAdapter;
using OpenVectorFormat;
using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Text;

namespace ILTFileReaderAdapter.OVFToCLIAdapter
{
    internal class OVFCliLayer : ILayer
    {
        private WorkPlane wp;

        public OVFCliLayer(WorkPlane wp)
        {
            this.wp = wp ?? throw new ArgumentNullException(nameof(wp));
        }

        public float Height => wp.ZPosInMm;

        public IList<IVectorBlock> VectorBlocks
        {
            get
            {
                var result = new List<IVectorBlock>(wp.NumBlocks);
                foreach (var block in wp.VectorBlocks)
                {
                    if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                    {
                        result.Add(new OVFCliPolyline(block));
                    }
                    else if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches)
                    {
                        result.Add(new OVFCliHatches(block));
                    }
                }
                return result;
            }
        }
    }
}
