/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2023 Digital-Production-Aachen

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

using Google.Protobuf.Collections;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using System.Transactions;
using static OpenVectorFormat.SIMDVectorOperations;

namespace OpenVectorFormat
{
    /// <summary>
    /// Extension methods for convenient handling of OVF vector blocks in C# and with System.Numerics types.
    /// </summary>
    public static class VectorBlockExtensions
    {
        /// <summary>
        /// Clones all data of the vector block without copying the coordinate data.
        /// Avoids unnecessary copies of data compared to calling clone and ClearVectorData on the vector block.
        /// </summary>
        /// <param name="blockToClone"></param>
        /// <returns></returns>
        public static VectorBlock CloneWithoutVectorData(this VectorBlock blockToClone)
        {
            var clone = new VectorBlock();

            clone.MarkingParamsKey = blockToClone.MarkingParamsKey;
            clone.LaserIndex = blockToClone.LaserIndex;
            clone.Repeats = blockToClone.Repeats;
            clone.MetaData = blockToClone.MetaData?.Clone();

            switch (blockToClone.ProcessMetaDataCase)
            {
                case VectorBlock.ProcessMetaDataOneofCase.None:
                    break;
                case VectorBlock.ProcessMetaDataOneofCase.LpbfMetadata:
                    clone.LpbfMetadata = blockToClone.LpbfMetadata?.Clone();
                    break;
                case VectorBlock.ProcessMetaDataOneofCase.MicroStructuringMetadata:
                    clone.MicroStructuringMetadata = blockToClone.MicroStructuringMetadata?.Clone();
                    break;
                case VectorBlock.ProcessMetaDataOneofCase.PolishingMetadata:
                    clone.PolishingMetadata = blockToClone.PolishingMetadata?.Clone();
                    break;
            }

            return clone;
        }

        /// <summary>
        /// Clones all data of the vector block, excluding the vectordata oneof.
        /// Replaces the vectordata oneof with a Reference to the original blocks vectordata oneof.
        /// This shallow copy can have  all other vector block data and meta data altered without side effects.
        /// Changes to coordinate data have side effects. The shallow copy is much faster than a deep copy
        /// which can be obtained using default VectorBlock clone.
        /// </summary>
        /// <param name="blockToClone"></param>
        /// <returns></returns>
        public static VectorBlock ShallowCopy(this VectorBlock blockToClone)
        {
            var shallowCopy = CloneWithoutVectorData(blockToClone);
            switch (blockToClone.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.None:
                    break;
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    shallowCopy.LineSequence = blockToClone.LineSequence;
                    break;
                case VectorBlock.VectorDataOneofCase.Hatches:
                    shallowCopy.Hatches = blockToClone.Hatches;
                    break;
                case VectorBlock.VectorDataOneofCase.PointSequence:
                    shallowCopy.PointSequence = blockToClone.PointSequence;
                    break;
                case VectorBlock.VectorDataOneofCase.Arcs:
                    shallowCopy.Arcs = blockToClone.Arcs;
                    break;
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    shallowCopy.Ellipses = blockToClone.Ellipses;
                    break;
                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                    shallowCopy.LineSequence3D = blockToClone.LineSequence3D;
                    break;
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                    shallowCopy.Hatches3D = blockToClone.Hatches3D;
                    break;
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                    shallowCopy.PointSequence3D = blockToClone.PointSequence3D;
                    break;
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                    shallowCopy.Arcs3D = blockToClone.Arcs3D;
                    break;
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    shallowCopy.ExposurePause = blockToClone.ExposurePause;
                    break;
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    shallowCopy.LineSequenceParaAdapt = blockToClone.LineSequenceParaAdapt;
                    break;
                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    shallowCopy.HatchParaAdapt = blockToClone.HatchParaAdapt;
                    break;
                default:
                    throw new NotImplementedException($"shallow copy not implemented for VectorDataCase {blockToClone.VectorDataCase}");
            }
            return shallowCopy;
        }


        /// <summary>
        /// Translates a vector block in the x/y plane.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="vectorBlock">vector block to translate</param>
        /// <param name="translation">translation vector</param>
        public static void Translate(this VectorBlock vectorBlock, Vector2 translation)
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    TranslateAsVector2(vectorBlock.RawCoordinates().AsSpan(), translation);
                    break;

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    TranslateAsVector3(vectorBlock.RawCoordinates().AsSpan(), translation);
                    break;

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        TranslateAsVector3(item.PointsWithParas.AsSpan(), translation);
                    }
                    break;

                case VectorBlock.VectorDataOneofCase.None:
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    break;
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        /// <summary>
        /// Translates a vector block in the x/y plane.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="vectorBlock"></param>
        /// <param name="translationX"></param>
        /// <param name="translationY"></param>
        public static void Translate(this VectorBlock vectorBlock, float translationX, float translationY) => vectorBlock.Translate(new Vector2(translationX, translationY));

        /// <summary>
        /// Translates a vector blocks in the x/y plane.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="vectorBlocks"></param>
        /// <param name="translation"></param>
        public static void Translate(this IEnumerable<VectorBlock> vectorBlocks, Vector2 translation)
        {
            foreach (var block in vectorBlocks) { block.Translate(translation); }
        }

        /// <summary>
        /// Rotates the VectorBlock data counterclockwise
        /// by angleRad [radians] around the origin.
        /// Uses AVX2 hardware acceleration if available.
        /// </summary>
        /// <param name="vectorBlock"></param>
        /// <param name="angleRad"> angle in radians</param>
        /// <exception cref="NotImplementedException"></exception>
        public static void Rotate(this VectorBlock vectorBlock, float angleRad)
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    RotateVector2(vectorBlock.RawCoordinates(), angleRad, 2);
                    break;

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    RotateVector2(vectorBlock.RawCoordinates(), angleRad, 3);
                    break;

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        RotateVector2(item.PointsWithParas, angleRad, 3);
                    }
                    break;

                case VectorBlock.VectorDataOneofCase.None:
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    break;
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        /// <summary>
        /// Rotates the VectorBlocks data counterclockwise
        /// by angleRad [radians] around the origin.
        /// Uses AVX2 hardware acceleration if available.
        /// </summary>
        /// <param name="vectorBlocks"></param>
        /// <param name="angleRad"></param>
        public static void Rotate(this IEnumerable<VectorBlock> vectorBlocks, float angleRad)
        {
            foreach (var block in vectorBlocks) { block.Rotate(angleRad); }
        }

        /// <summary>
        /// Calculates the 2D (x any y) axis aligned bounding box of the coordinates of the vector block.
        /// Uses SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="vectorBlock"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public static AxisAlignedBox2D Bounds2D(this VectorBlock vectorBlock)
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    return Bounds2DFromCoordinates(vectorBlock.RawCoordinates(), 2);

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    return Bounds2DFromCoordinates(vectorBlock.RawCoordinates(), 3);

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    var bounds = AxisAlignedBox2DExtensions.EmptyAAB2D();
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        if (item.PointsWithParas.Count > 0)
                        {
                            var lsBounds = Bounds2DFromCoordinates(item.PointsWithParas, 3);
                            bounds.Contain(lsBounds);
                        }
                    }
                    return bounds;

                case VectorBlock.VectorDataOneofCase.None:
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    throw new ArgumentException($"vector block type {vectorBlock.VectorDataCase} does not have coordinates");
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        /// <summary>
        /// Calculates the 2D (x any y) axis aligned bounding box of the coordinates of the vector blocks.
        /// Uses SIMD hardware acceleration if available.
        /// Returns empty (min/max float) bounds if all blocks are empty.
        /// </summary>
        /// <param name="vectorBlocks"></param>
        /// <returns></returns>
        public static AxisAlignedBox2D Bounds2D(this IEnumerable<VectorBlock> vectorBlocks)
        {
            var bounds = AxisAlignedBox2DExtensions.EmptyAAB2D();
            foreach (var block in vectorBlocks)
            {
                //skip empty blocks and don't attempt vector block types that do not have coordinates
                if (block.VectorCount() > 0)
                {
                    bounds.Contain(block.Bounds2D());
                }
            }
            return bounds;
        }

        /// <summary>
        /// Computes and stores the vector blocks axis aligned bounding box into their meta data.
        /// Skips empty blocks.
        /// </summary>
        /// <param name="vectorBlocks"></param>
        public static void StoreVectorBlockBoundsInMetaData(this IEnumerable<VectorBlock> vectorBlocks)
        {
            foreach (var block in vectorBlocks)
            {
                if (block.VectorCount() > 0)
                {
                    block.StoreVectorBlockBoundsInMetaData();
                }
            }
        }

        /// <summary>
        /// Computes and stores the vector blocks axis aligned bounding box into its meta data.
        /// </summary>
        /// <param name="vectorBlock"></param>
        public static void StoreVectorBlockBoundsInMetaData(this VectorBlock vectorBlock)
        {
            if (vectorBlock.MetaData == null) vectorBlock.MetaData = new VectorBlock.Types.VectorBlockMetaData();
            vectorBlock.MetaData.Bounds = vectorBlock.Bounds2D();
        }

        /// <summary>
        /// Returns the count of 2D or 3D vectors stored in the vector block,
        /// depending on the vector block type.
        /// </summary>
        /// <param name="vectorBlock"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static int VectorCount(this VectorBlock vectorBlock)
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    return vectorBlock.LineSequence.Points.Count / 2;
                case VectorBlock.VectorDataOneofCase.Hatches:
                    return vectorBlock.Hatches.Points.Count / 2;
                case VectorBlock.VectorDataOneofCase.PointSequence:
                    return vectorBlock.PointSequence.Points.Count / 2;
                case VectorBlock.VectorDataOneofCase.Arcs:
                    return vectorBlock.Arcs.Centers.Count / 2;
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    return vectorBlock.Ellipses.EllipsesArcs.Centers.Count / 2;
                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                    return vectorBlock.LineSequence3D.Points.Count / 3;
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                    return vectorBlock.Hatches3D.Points.Count / 3;
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                    return vectorBlock.PointSequence3D.Points.Count / 3;
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                    return vectorBlock.Arcs3D.Centers.Count / 3;
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    return vectorBlock.LineSequenceParaAdapt.PointsWithParas.Count / 3;
                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    int counter = 0;
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        counter += item.PointsWithParas.Count;
                    }
                    return counter;
                case VectorBlock.VectorDataOneofCase.None:
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    return 0;
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        public static int VectorCount(this IEnumerable<VectorBlock> vectorBlocks) => vectorBlocks.Sum(block => block.VectorCount());

        /// <summary>
        /// Retrieves the raw coordinate data of the vector block.
        /// This are 2D or 3D coordinates of either the line points or centers (for arcs/ellipses),
        /// depending on VectorDataOneofCase.
        /// Returns empty RepeatedFileds for cases with no data, and a new merged RepeatedField instead of a
        /// reference for HatchAsLinesequence.
        /// </summary>
        /// <param name="vectorBlock"></param>
        /// <returns></returns>
        public static RepeatedField<float> RawCoordinates(this VectorBlock vectorBlock)
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    return vectorBlock.LineSequence.Points;
                case VectorBlock.VectorDataOneofCase.Hatches:
                    return vectorBlock.Hatches.Points;
                case VectorBlock.VectorDataOneofCase.PointSequence:
                    return vectorBlock.PointSequence.Points;
                case VectorBlock.VectorDataOneofCase.Arcs:
                    return vectorBlock.Arcs.Centers;
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    return vectorBlock.Ellipses.EllipsesArcs.Centers;
                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                    return vectorBlock.LineSequence3D.Points;
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                    return vectorBlock.Hatches3D.Points;
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                    return vectorBlock.PointSequence3D.Points;
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                    return vectorBlock.Arcs3D.Centers;
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    return vectorBlock.LineSequenceParaAdapt.PointsWithParas;
                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    //there is no better design for this that came to my mind...
                    var temp = new RepeatedField<float>();
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        temp.AddRange(item.PointsWithParas);
                    }
                    return temp;
                case VectorBlock.VectorDataOneofCase.None:
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    //return empty repeated field
                    return new RepeatedField<float>();
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

#if NETCOREAPP3_0_OR_GREATER
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Vec256FromVec3
        {
            public Vector256<float> vec256;
            private float unused;
        }
#endif

        private static void RotateVector2(RepeatedField<float> coordinates, float angleRad, int dims)
        {
            if (coordinates.Count % dims != 0) throw new ArgumentException($"coordinates count is {coordinates.Count} but must be a multiple of {dims}");

#if NETCOREAPP3_0_OR_GREATER
            if (Avx2.IsSupported & coordinates.Count > 80 * dims)
            {
                var coordSpan = ProtoUtils.AsSpan<float>(coordinates);
                RotateAsVector2(coordSpan, angleRad, dims);
                return;
            }
#endif

            var sin = (float)Math.Sin(angleRad);
            var cos = (float)Math.Cos(angleRad);
            var nsin = -sin;

            for (int i = 0; i < coordinates.Count - 1; i += dims)
            {
                float xNew = coordinates[i] * cos + coordinates[i + 1] * nsin;
                float yNew = coordinates[i] * sin + coordinates[i + 1] * cos;
                coordinates[i] = xNew; coordinates[i + 1] = yNew;
            }
        }

        private static AxisAlignedBox2D Bounds2DFromCoordinates(RepeatedField<float> coordinates, int dims)
        {
            if (coordinates.Count == 0)
                return AxisAlignedBox2DExtensions.EmptyAAB2D();
            else if (coordinates.Count > 100 * dims)
            {
                var coordSpan = coordinates.AsSpan();
                return SIMDVectorOperations.Bounds2D(coordSpan, dims);
            }
            else
            {
                var bounds = new AxisAlignedBox2D()
                {
                    XMin = coordinates[0],
                    YMin = coordinates[1],
                    XMax = coordinates[0],
                    YMax = coordinates[1],
                };
                for (int i = dims; i < coordinates.Count - 1; i += dims)
                {
                    if (coordinates[i] < bounds.XMin) bounds.XMin = coordinates[i];
                    if (coordinates[i + 1] < bounds.YMin) bounds.YMin = coordinates[i + 1];
                    if (coordinates[i] > bounds.XMax) bounds.XMax = coordinates[i];
                    if (coordinates[i + 1] > bounds.YMax) bounds.YMax = coordinates[i + 1];
                }
                return bounds;
            }
        }

        /// <summary>
        /// Converts the raw point data of this vectorBlock into a List of Vector2 structs.
        /// For 3D point types the z value is discarded.
        /// For types without data an empty list is returned.
        /// </summary>
        /// <param name="vectorBlock"></param>
        /// <returns></returns>
        public static List<Vector2> ToVector2(this VectorBlock vectorBlock)
        {
            var list = new List<Vector2>();
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    return ToVector2(vectorBlock.RawCoordinates());

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    return Points3DToVector2(vectorBlock.RawCoordinates());

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    var tempList = new List<Vector2>();
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        tempList.AddRange(ToVector2(item.PointsWithParas));
                    }
                    return tempList;

                case VectorBlock.VectorDataOneofCase.ExposurePause:
                case VectorBlock.VectorDataOneofCase.None:
                    return new List<Vector2>();
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        /// <summary>
        /// Converts the raw point data of this vectorBlock into a List of Vector3 structs.
        /// For 2D point types the z value is set to 0.
        /// For types without data an empty list is returned.
        /// </summary>
        /// <param name="vectorBlock"></param>
        /// <returns></returns>
        public static List<Vector3> ToVector3(this VectorBlock vectorBlock)
        {
            var list = new List<Vector2>();
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    return Points2DToVector3(vectorBlock.RawCoordinates());

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    return ToVector3(vectorBlock.RawCoordinates());

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    var tempList = new List<Vector3>();
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        tempList.AddRange(ToVector3(item.PointsWithParas));
                    }
                    return tempList;

                case VectorBlock.VectorDataOneofCase.ExposurePause:
                case VectorBlock.VectorDataOneofCase.None:
                    return new List<Vector3>();
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        /// <summary>
        /// Converts raw coordinates used in OVF vector blocks into a list of Vector3 structs,
        /// assuming the raw data is (x,y,z).
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<Vector3> ToVector3(this RepeatedField<float> points)
        {
            var list = new List<Vector3>(points.Count / 3);

            for (int i = 2; i < points.Count; i += 3)
            {
                var end = new Vector3(points[i - 2], points[i - 1], points[i]);
                list.Add(end);
            }

            return list;
        }

        /// <summary>
        /// Converts raw coordinates used in OVF vector blocks into a list of Vector2 structs,
        /// assuming the raw data is (x,y).
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<Vector2> ToVector2(this RepeatedField<float> points)
        {
            var list = new List<Vector2>(points.Count / 2);

            for (int i = 1; i < points.Count; i += 2)
            {
                var end = new Vector2(points[i - 1], points[i]);
                list.Add(end);
            }

            return list;
        }

        private static List<Vector2> Points3DToVector2(RepeatedField<float> points)
        {
            var list = new List<Vector2>(points.Count / 3);

            for (int i = 2; i < points.Count; i += 3)
            {
                //skip z
                var end = new Vector2(points[i - 2], points[i - 1]);
                list.Add(end);
            }

            return list;
        }

        private static List<Vector3> Points2DToVector3(RepeatedField<float> points)
        {
            var list = new List<Vector3>(points.Count / 2);

            for (int i = 1; i < points.Count; i += 2)
            {
                var end = new Vector3(points[i - 1], points[i], 0);
                list.Add(end);
            }

            return list;
        }
    }
}