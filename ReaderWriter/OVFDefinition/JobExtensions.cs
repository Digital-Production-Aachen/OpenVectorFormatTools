using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace OpenVectorFormat
{
    public static class JobExtensions
    {
        /// <summary>
        /// Translates all vector block coordinates of the work plane in the x/y plane.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <param name="translation"></param>
        public static void Translate(this Job job, Vector2 translation)
        {
            Parallel.ForEach(job.WorkPlanes, (wp) => wp.Translate(translation));
        }

        /// <summary>
        /// Rotates all vector block coordinates of the work plane counterclockwise
        /// by angleRad [radians] around the origin.
        /// Uses AVX2 hardware acceleration if available.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <param name="angleRad"></param>
        public static void Rotate(this Job job, float angleRad)
        {
            Parallel.ForEach(job.WorkPlanes, (wp) => wp.Rotate(angleRad));
        }

        /// <summary>
        /// Calculates the 2D (x any y) axis aligned bounding box of all coordinates
        /// of the jobs work planes vector blocks.
        /// Uses SIMD hardware acceleration if available.
        /// 
        /// </summary>
        /// <param name="workPlane"></param>
        /// <returns></returns>
        public static AxisAlignedBox2D Bounds2D(this Job job)
        {
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;

            Parallel.ForEach(job.WorkPlanes, (wp) =>
            {
                var wpBounds = wp.Bounds2D();
                InterlockedHelpers.AssignIfNewValueSmaller(ref xMin, wpBounds.XMin);
                InterlockedHelpers.AssignIfNewValueSmaller(ref yMin, wpBounds.YMin);
                InterlockedHelpers.AssignIfNewValueBigger(ref xMax, wpBounds.XMax);
                InterlockedHelpers.AssignIfNewValueBigger(ref yMax, wpBounds.YMax);
            });
            return new AxisAlignedBox2D() { YMax = yMax, XMax = xMax, XMin = xMin, YMin = yMin };
        }

        /// <summary>
        /// Returns the sum of counts of 2D or 3D vectors
        /// stored in the all the jobs work planes vector blocks,
        /// depending on the vector block type.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <returns></returns>
        public static int VectorCount(this Job job) => job.WorkPlanes.VectorCount();
    }
}
