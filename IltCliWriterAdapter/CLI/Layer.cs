using OpenVectorFormat.ILTFileReader;
using System.Collections.Generic;


namespace IltCliWriterAdapter.CLI
{
    public class Layer : ILayer
    {
        public Layer(IVectorBlock vectorBlock, float height)
        {
            Height = height;
            VectorBlocks = new List<IVectorBlock>();
            VectorBlocks.Add(vectorBlock);
        }
        public Layer(List<IVectorBlock> vectorBlocks, float height)
        {
            Height = height;
            VectorBlocks = new List<IVectorBlock>();

            foreach (IVectorBlock item in vectorBlocks)
            {
                VectorBlocks.Add(item);
            }
        }
        public Layer(float height)
        {
            Height = height;
            VectorBlocks = new List<IVectorBlock>();
        }
        public float Height { get; set; }

        public IList<IVectorBlock> VectorBlocks { get; set; }
    }
}
