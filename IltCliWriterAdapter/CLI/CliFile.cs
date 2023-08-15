using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IltCliWriterAdapter.CLI
{
    class CliFile : ICLIFile
    {
        //private static readonly float _Units = 1;
        public CliFile()
        {
            Geometry = new Geometry();
        }
        public void CreateHeaderAllHatch(float _Units = 1)
        {
            var allHatches = Geometry.Layers.SelectMany(x => x.VectorBlocks).Select(x => x as IHatches).SelectMany(x => x.hatches).ToList();
            Header = new Header
                (
                    020220,
                    new Dimension
                    (
                        0,
                        Geometry.Layers.Max(x => x.Height),
                        allHatches.SelectMany(z => new[] { z.End.X, z.Start.X }).Min(),
                        allHatches.SelectMany(z => new[] { z.End.X, z.Start.X }).Max(),
                        allHatches.SelectMany(z => new[] { z.End.Y, z.Start.Y }).Min(),
                        allHatches.SelectMany(z => new[] { z.End.Y, z.Start.Y }).Max()
                    ),
                    Geometry.Layers.Count(),
                    _Units,
                    new UserData(null, 10, "Daniel L."),
                    1
                );
        }

        public void CreateHeaderAllPolylines(float _Units = 1)
        {
            var allPolylines = Geometry.Layers.SelectMany(x => x.VectorBlocks).Select(x => x as Polyline).SelectMany(x => x.Points).ToList();
            Header = new Header
                (
                    020220,
                    new Dimension
                    (
                        0,
                        Geometry.Layers.Max(x => x.Height),
                        allPolylines.Min(x => x.X),
                        allPolylines.Max(x => x.X),
                        allPolylines.Min(x => x.Y),
                        allPolylines.Max(x => x.Y)
                    ),
                    Geometry.Layers.Count(),
                    _Units,
                    new UserData(null, 10, "Daniel L."),
                    1
                );
        }
        public IGeometry Geometry { get; set; }

        public IHeader Header { get; set; }

        public IList<IPart> Parts { get; set; }
    }
}
