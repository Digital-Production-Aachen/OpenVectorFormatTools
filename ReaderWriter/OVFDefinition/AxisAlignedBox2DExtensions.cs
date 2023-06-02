using System;
using System.Collections.Generic;
using System.Text;

namespace OpenVectorFormat
{
    public static class AxisAlignedBox2DExtensions
    {
        public static AxisAlignedBox2D EmptyAAB2D()
        {
            return new AxisAlignedBox2D()
            {
                XMin = float.MaxValue,
                YMin = float.MaxValue,
                XMax = float.MinValue,
                YMax = float.MinValue,
            };
        }

        public static void Contain(this AxisAlignedBox2D bounds, AxisAlignedBox2D otherBounds)
        {
            bounds.XMin = Math.Min(bounds.XMin, otherBounds.XMin);
            bounds.YMin = Math.Min(bounds.YMin, otherBounds.YMin);
            bounds.XMax = Math.Max(bounds.XMax, otherBounds.XMax);
            bounds.YMax = Math.Max(bounds.YMax, otherBounds.YMax);
        }
    }
}
