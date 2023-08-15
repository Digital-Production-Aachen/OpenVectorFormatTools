using OpenVectorFormat.ILTFileReader;
using System.Collections.Generic;


namespace IltCliWriterAdapter.CLI
{
    public class Geometry : IGeometry
    {
        public IList<ILayer> Layers { get; set; }
    }
}
