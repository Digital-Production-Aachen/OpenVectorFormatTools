/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Transactions;

namespace OpenVectorFormat
{
    public static class WorkPlaneExtensions
    {
        /// <summary>
        /// Clones all data of the work plane without copying the vector blocks data.
        /// Avoids unnecessary copies of data compared to calling clone and clone.VectorBlocks.Clear on the work plane.
        /// </summary>
        /// <param name="blockToClone"></param>
        /// <returns></returns>
        public static WorkPlane CloneWithoutVectorData(this WorkPlane wpToClone)
        {
            var clone = new WorkPlane();

            clone.XPosInMm = wpToClone.XPosInMm;
            clone.YPosInMm = wpToClone.YPosInMm;
            clone.ZPosInMm = wpToClone.ZPosInMm;
            clone.XRotInDeg = wpToClone.XRotInDeg;
            clone.YRotInDeg = wpToClone.YRotInDeg;
            clone.ZRotInDeg = wpToClone.ZRotInDeg;
            clone.NumBlocks = wpToClone.NumBlocks;
            clone.Repeats = wpToClone.Repeats;
            clone.WorkPlaneNumber = wpToClone.WorkPlaneNumber;
            clone.MachineType = wpToClone.MachineType;
            clone.AdditionalAxisPositions.AddRange(wpToClone.AdditionalAxisPositions);
            clone.MetaData = wpToClone.MetaData != null ? wpToClone.MetaData.Clone() : null;

            return clone;
        }

        /// <summary>
        /// Translates all vector block coordinates of the work plane in the x/y plane.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <param name="translation"></param>
        public static void Translate(this WorkPlane workPlane, Vector2 translation) => workPlane.VectorBlocks.Translate(translation);

        /// <summary>
        /// Translates all vector block coordinates of all work plane in the x/y plane.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <param name="translation"></param>
        public static void Translate(this IEnumerable<WorkPlane> workPlanes, Vector2 translation)
        {
            foreach(var wp in workPlanes) { Translate(wp, translation); }
        }

        /// <summary>
        /// Rotates all vector block coordinates of the work plane counterclockwise
        /// by angleRad [radians] around the origin.
        /// Uses AVX2 hardware acceleration if available.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <param name="angleRad"></param>
        public static void Rotate(this WorkPlane workPlane, float angleRad) => workPlane.VectorBlocks.Rotate(angleRad);

        /// <summary>
        /// Rotates all vector block coordinates of all work plane counterclockwise
        /// by angleRad [radians] around the origin.
        /// Uses AVX2 hardware acceleration if available.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <param name="angleRad"></param>
        public static void Rotate(this IEnumerable<WorkPlane> workPlanes, float angleRad)
        {
            foreach (var wp in workPlanes) { Rotate(wp, angleRad); }
        }

        /// <summary>
        /// Calculates the 2D (x any y) axis aligned bounding box of all coordinates
        /// of the work planes vector blocks.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <returns></returns>
        public static AxisAlignedBox2D Bounds2D(this WorkPlane workPlane) => workPlane.VectorBlocks.Bounds2D();

        /// <summary>
        /// Calculates the 2D (x any y) axis aligned bounding box of all coordinates
        /// of all work planes vector blocks.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <returns></returns>
        public static AxisAlignedBox2D Bounds2D(this IEnumerable<WorkPlane> workPlanes)
        {
            var bounds = AxisAlignedBox2DExtensions.EmptyAAB2D();
            foreach (var wp in workPlanes)
            {
                bounds.Contain(wp.Bounds2D());
            }
            return bounds;
        }

        /// <summary>
        /// Returns the sum of counts of 2D or 3D vectors
        /// stored in the work planes vector blocks,
        /// depending on the vector block type.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <returns></returns>
        public static int VectorCount(this WorkPlane workPlane) => workPlane.VectorBlocks.VectorCount();

        /// <summary>
        /// Returns the sum of counts of 2D or 3D vectors
        /// stored in the work planes vector blocks,
        /// depending on the vector block type.
        /// </summary>
        /// <param name="workPlane"></param>
        /// <returns></returns>
        public static int VectorCount(this IEnumerable<WorkPlane> workPlanes) => workPlanes.Sum(wp=>wp.VectorCount());

        /// <summary>
        /// Computes and stores the vector blocks axis aligned bounding box into their meta data.
        /// Skips empty blocks.
        /// </summary>
        /// <param name="vectorBlocks"></param>
        public static void StoreVectorBlockBoundsInMetaData(this WorkPlane workPlane)
        {
            workPlane.VectorBlocks.StoreVectorBlockBoundsInMetaData();
        }

        /// <summary>
        /// Computes and stores the vector blocks axis aligned bounding box into their meta data.
        /// Skips empty blocks.
        /// </summary>
        /// <param name="vectorBlocks"></param>
        public static void StoreVectorBlockBoundsInMetaData(this IEnumerable<WorkPlane> workPlanes)
        {
            foreach (var wp in workPlanes) { wp.StoreVectorBlockBoundsInMetaData(); }
        }
    }
}
