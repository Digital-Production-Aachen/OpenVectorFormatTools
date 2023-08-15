using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IltCliWriterAdapter.CLI
{
    public class Hatches : IHatches
    {   
        public Hatches(List<IHatch> parahatches)
        {
            hatches = parahatches;
        }
        public IList<IHatch> hatches { get; set; }

        public float[] Coordinates => hatches.SelectMany(x => new[] { x.Start.X, x.Start.Y, x.End.X, x.End.Y }).ToArray();

        public int Id { get; set; } = 1;

        public int N => hatches.Count();

        public double JumpLengthNoSkywriting
        {
            get
            {
                if (hatches.Count() < 2)
                {
                    return 0;
                }
                var temp = hatches.Select(x => new { x.Start, x.End }).ToList();
                double length = Math.Sqrt(temp[0].Start.X * temp[0].Start.X + temp[0].Start.Y + temp[0].Start.Y);
                for (int i = 0; i < temp.Count() - 1; i++)
                {
                    var v1Ende = new EuclidKoordinates(temp[i].End);
                    var v2Start = new EuclidKoordinates(temp[i + 1].Start);

                    var distance = v2Start - v1Ende;


                    length += Math.Sqrt(distance.X * distance.X + distance.Y * distance.Y);
                }

                return length;
            }
        }
    }
}
