/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2022 Digital-Production-Aachen

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
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Transactions;

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
            //cases none and exposure pause don't contain coordinates
            Utils.ProtoUtils.CopyWithExclude(blockToClone, clone, new List<int>() {
                VectorBlock.LineSequenceFieldNumber,
                VectorBlock.HatchesFieldNumber,
                VectorBlock.PointSequenceFieldNumber,
                VectorBlock.ArcsFieldNumber,
                VectorBlock.EllipsesFieldNumber,
                VectorBlock.LineSequence3DFieldNumber,
                VectorBlock.Hatches3DFieldNumber,
                VectorBlock.PointSequence3DFieldNumber,
                VectorBlock.Arcs3DFieldNumber,
                VectorBlock.LineSequenceParaAdaptFieldNumber,
                VectorBlock.HatchParaAdaptFieldNumber
            });
            //ensure the field is empty for future extensions as well
            //new definitions will not be excluded and degrade performance because of unnecessary copies
            clone.ClearVectorData();
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
        /// </summary>
        /// <param name="vectorBlock">vector block to translate</param>
        /// <param name="translation">translation vector</param>
        public static void Translate(this VectorBlock vectorBlock, float translationX, float translationY)
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    AddToVector2(vectorBlock.RawCoordinates(), translationX, translationY);
                    break;

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    AddToVector3(vectorBlock.RawCoordinates(), translationX, translationY);
                    break;

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        AddToVector3(item.PointsWithParas, translationX, translationY);
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

        private static void AddToVector2(RepeatedField<float> coordinates, float translationX, float translationY)
        {
            for (int i = 0; i < coordinates.Count; i++)
            {
                coordinates[i] += i % 2 == 0 ? translationX : translationY;
            }
        }

        private static void AddToVector3(RepeatedField<float> coordinates, float translationX, float translationY)
        {
            for (int i = 0; i < coordinates.Count; i++)
            {
                coordinates[i] += i % 3 == 0 ? translationX : i % 3 == 1 ? translationY : 0;
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
            var list = new List<Vector3>();

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
            var list = new List<Vector2>();

            for (int i = 1; i < points.Count; i += 2)
            {
                var end = new Vector2(points[i - 1], points[i]);
                list.Add(end);
            }

            return list;
        }

        private static List<Vector2> Points3DToVector2(RepeatedField<float> points)
        {
            var list = new List<Vector2>();

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
            var list = new List<Vector3>();

            for (int i = 1; i < points.Count; i += 2)
            {
                var end = new Vector3(points[i - 1], points[i], 0);
                list.Add(end);
            }

            return list;
        }
    }
}