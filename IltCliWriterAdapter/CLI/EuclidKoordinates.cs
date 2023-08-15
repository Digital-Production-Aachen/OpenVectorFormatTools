using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Text;

namespace IltCliWriterAdapter.CLI
{
    public class EuclidKoordinates : IPoint2D
    {
        public EuclidKoordinates(float x, float y, float z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public EuclidKoordinates(IPoint2D point)
        {
            X = point.X;
            Y = point.Y;
            Z = 0;
        }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        float IPoint2D.X => (float)X;

        float IPoint2D.Y => (float)Y;

        public EuclidKoordinates AddX(float x)
        {
            return new EuclidKoordinates(X + x, Y, Z);
        }
        public EuclidKoordinates AddY(float y)
        {
            return new EuclidKoordinates(X, Y + y, Z);
        }
        public EuclidKoordinates AddZ(float z)
        {
            return new EuclidKoordinates(X, Y, Z + z);
        }

        public void SetLength(double length)
        {
            var actualLength = Math.Sqrt(X * X + Y * Y + Z * Z);
            float factor = (float)(length / actualLength);

            this.X *= factor;
            this.Y *= factor;
            this.Z *= factor;
        }

        public override string ToString()
        {
            return string.Concat("x: ", this.X, "; y: ", this.Y, "; z: ", this.Z);
        }

        public static EuclidKoordinates operator +(EuclidKoordinates v1, EuclidKoordinates v2)
        {
            return new EuclidKoordinates(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }
        public static EuclidKoordinates operator -(EuclidKoordinates v1, EuclidKoordinates v2)
        {
            return new EuclidKoordinates(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }
        public static EuclidKoordinates operator *(EuclidKoordinates v1, int multiplikator)
        {
            return new EuclidKoordinates(v1.X * multiplikator, v1.Y * multiplikator, v1.Z * multiplikator);
        }
    }
}
