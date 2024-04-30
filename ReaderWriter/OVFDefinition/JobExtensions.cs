/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2024 Digital-Production-Aachen

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

---- Copyright End ----
*/

﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Adds all work planes from index 0 to job.NumWorkplanes to job.WorkPlanes in parallel
        /// using the provided work plane getter function.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="GetWorkPlane"></param>
        public static void AddAllWorkPlanesParallel(this Job job, Func<int, WorkPlane> GetWorkPlane)
        {
            ConcurrentBag<WorkPlane> wpBag = new ConcurrentBag<WorkPlane>();
            Parallel.For(0, job.NumWorkPlanes, j =>
            {
                var wpNum = j;
                wpBag.Add(GetWorkPlane(wpNum));
            });

            job.WorkPlanes.AddRange(wpBag.OrderBy(x => x.WorkPlaneNumber));
        }
    }
}
