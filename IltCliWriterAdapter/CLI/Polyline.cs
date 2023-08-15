using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IltCliWriterAdapter.CLI
{
    public class Polyline : IPolyline
    {
        public Polyline(EuclidKoordinates start, int id)
        {
            Points = new List<IPoint2D>();
            _coordinates = new List<float>();


            Points.Add(start);
            _coordinates.Add(start.X);
            _coordinates.Add(start.Y);
            Id = id;

        }
        //public void AddVectorByPoint(EuclidKoordinates koordinates)
        //{
        //    Points.Add(koordinates);
        //    _coordinates.Add(koordinates.X);
        //    _coordinates.Add(koordinates.Y);
        //}

        public void AddVectorByPoint(IPoint2D point)
        {
            var lastpoint = Points.Last();

            var distance = Math.Sqrt((point.X - lastpoint.X) * (point.X - lastpoint.X) + (point.Y - lastpoint.Y) * (point.Y - lastpoint.Y));

            length += distance;

            Points.Add(point);
            _coordinates.Add(point.X);
            _coordinates.Add(point.Y);
        }

        public double length { get; set; }
        public Direction Dir { get; set; } = Direction.openLine;

        public IList<IPoint2D> Points { get; private set; }

        public float[] Coordinates => _coordinates.ToArray();
        private List<float> _coordinates { get; set; }

        public int Id { get; private set; }

        public int N => Points.Count();
    }
}
