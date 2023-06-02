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

﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Transactions;
#endif
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Google.Protobuf.Collections;
using OpenVectorFormat;
using OpenVectorFormat.Utils;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net70, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class VectorTranslate
    {
        [Params(1, 20, 40, 60, 80, 100, 10000, 1000000)]
        public int numVectors { get; set; }
        [Params(2, 3)]
        public int dims { get; set; }

        public VectorBlock vectorBlock;
        public Vector2 translation = new Vector2(4, 5);
        private float[] spanArray;

        [GlobalSetup]
        public void GlobalSetup()
        {
            vectorBlock = new VectorBlock();
            if (dims == 2)
            {
                vectorBlock.LineSequence = new VectorBlock.Types.LineSequence();
                vectorBlock.LineSequence.Points.Capacity = numVectors * 2;
                Random random = new Random();
                for (int i = 0; i < numVectors; i++)
                {
                    vectorBlock.LineSequence.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence.Points.Add((float)(random.NextDouble() * 10f));
                }
                spanArray = vectorBlock.LineSequence.Points.ToArray();
            }
            else
            {
                vectorBlock.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                vectorBlock.LineSequence3D.Points.Capacity = numVectors * 3;
                Random random = new Random();
                for (int i = 0; i < numVectors; i++)
                {
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                }
                spanArray = vectorBlock.LineSequence3D.Points.ToArray();
            }
        }

        [Benchmark(Baseline = true)]
        public void TranslateBaseline()
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    AddToVector2(vectorBlock.RawCoordinates(), translation);
                    break;

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    AddToVector3(vectorBlock.RawCoordinates(), translation);
                    break;

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        AddToVector3(item.PointsWithParas, translation);
                    }
                    break;

                case VectorBlock.VectorDataOneofCase.None:
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    break;
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        private static void AddToVector2(RepeatedField<float> coordinates, Vector2 translation)
        {
            for (int i = 0; i < coordinates.Count - 1; i += 2)
            {
                coordinates[i] += translation.X;
                coordinates[i + 1] += translation.Y;
            }
        }

        private static void AddToVector3(RepeatedField<float> coordinates, Vector2 translation)
        {
            if (coordinates.Count % 3 != 0) throw new ArgumentException($"count of coordinates must be a multiple of 3");

            for (int i = 0; i < coordinates.Count - 2; i += 3)
            {
                coordinates[i] += translation.X;
                coordinates[i + 1] += translation.Y;
            }
        }

        [Benchmark]
        public void TranslateSpanSIMDSystemNumericsVector()
        {
            var coordSpan = vectorBlock.RawCoordinates().AsSpan();
            if (dims == 2)
            {
                var vec2Span = MemoryMarshal.Cast<float, Vector2>(coordSpan);
                for (int i = 0; i < vec2Span.Length; i++)
                {
                    vec2Span[i] += translation;
                }
            }
            else
            {
                Vector3 trans3 = new Vector3(translation.X, translation.Y, 0);
                var vec2Span = MemoryMarshal.Cast<float, Vector3>(coordSpan);
                for (int i = 0; i < vec2Span.Length; i++)
                {
                    vec2Span[i] += trans3;
                }
            }
        }

        [Benchmark]
        public void TranslateSpanSIMDVector()
        {
            if (dims == 2) {
                var coordSpan = vectorBlock.RawCoordinates().AsSpan();
                var vecSpan = MemoryMarshal.Cast<float, Vector<float>>(coordSpan);
                int chunkSize = Vector<float>.Count;
                var inputVec = new float[chunkSize];

                for (int i = 0; i < chunkSize - 1; i += 2)
                {
                    inputVec[i] = translation.X;
                    inputVec[i + 1] = translation.Y;
                }

                var addVec = new Vector<float>(inputVec);

                for (int i = 0; i < vecSpan.Length; i++)
                {
                    vecSpan[i] += addVec;
                }

                var restCoord = coordSpan.Slice(vecSpan.Length * chunkSize);

                var vec2Span = MemoryMarshal.Cast<float, Vector2>(restCoord);
                for (int i = 0; i < vec2Span.Length; i++)
                {
                    vec2Span[i] += translation;
                }
            }
            else
            {
                var coordSpan = vectorBlock.RawCoordinates().AsSpan();
                var vecSpan = MemoryMarshal.Cast<float, Vector<float>>(coordSpan);
                int chunkSize = Vector<float>.Count;
                var inputVec = new float[chunkSize * 3];

                for (int i = 0; i < chunkSize * 3; i += 3)
                {
                    inputVec[i    ] = translation.X;
                    inputVec[i + 1] = translation.Y;
                    //inputVec[i + 2] = 0;
                }

                var addVec1 = new Vector<float>(inputVec, 0);
                var addVec2 = new Vector<float>(inputVec, chunkSize);
                var addVec3 = new Vector<float>(inputVec, chunkSize * 2);

                for (int i = 0; i < vecSpan.Length - 2; i += 3)
                {
                    vecSpan[i]     += addVec1;
                    vecSpan[i + 1] += addVec2;
                    vecSpan[i + 2] += addVec3;
                }

                var rest = vecSpan.Length % 3;
                if(rest == 1)
                {
                    vecSpan[vecSpan.Length - 1] += addVec1;
                }
                else if (rest == 2){
                    vecSpan[vecSpan.Length - 2] += addVec1;
                    vecSpan[vecSpan.Length - 1] += addVec2;
                }

                var restCoord = coordSpan.Slice(vecSpan.Length * chunkSize);
                var offset = (vecSpan.Length * chunkSize) % 3;
                for (int i = offset; i < restCoord.Length + offset; i++)
                {
                    restCoord[i - offset] += i % 3 == 0 ? translation.X : i % 3 == 1 ? translation.Y : 0;
                }
            }
        }

        [Benchmark]
        public void TranslateFinal()
        {
            vectorBlock.Translate(translation);
        }

        [Benchmark]
        public void TranslateFloatSpan()
        {
            var span = spanArray.AsSpan<float>();
            if(dims == 2)
            {
                SIMDVectorOperations.TranslateAsVector2(span, translation);
            }
            else
            {
                SIMDVectorOperations.TranslateAsVector3(span, translation);
            }
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net70, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class VectorBlockBounds
    {
        [Params(1, 20, 40, 60, 80, 100, 10000, 1000000)]
        public int numVectors { get; set; }
        [Params(2, 3)]
        public int dims { get; set; }

        public VectorBlock vectorBlock;
        private float[] spanArray;

        [GlobalSetup]
        public void GlobalSetup()
        {
            vectorBlock = new VectorBlock();
            if (dims == 2)
            {
                vectorBlock.LineSequence = new VectorBlock.Types.LineSequence();
                vectorBlock.LineSequence.Points.Capacity = numVectors * 2;
                Random random = new Random();
                for (int i = 0; i < numVectors; i++)
                {
                    vectorBlock.LineSequence.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence.Points.Add((float)(random.NextDouble() * 10f));
                }
                spanArray = vectorBlock.LineSequence.Points.ToArray();
            }
            else
            {
                vectorBlock.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                vectorBlock.LineSequence3D.Points.Capacity = numVectors * 3;
                Random random = new Random();
                for (int i = 0; i < numVectors; i++)
                {
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                }
                spanArray = vectorBlock.LineSequence3D.Points.ToArray();
            }
        }

        [Benchmark(Baseline = true)]
        public AxisAlignedBox2D BoundsBaseline()
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    return BoundsVector2(vectorBlock.RawCoordinates(), 2);

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    return BoundsVector2(vectorBlock.RawCoordinates(), 3);

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    return null;
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        return BoundsVector2(item.PointsWithParas, 3);
                    }
                    break;

                case VectorBlock.VectorDataOneofCase.None:
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    throw new ArgumentException($"vector block type does not have coordinates: {vectorBlock.VectorDataCase}");
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        private static AxisAlignedBox2D BoundsVector2(RepeatedField<float> coordinates, int dims)
        {
            if (coordinates.Count % dims != 0) throw new ArgumentException($"count of coordinates must be a multiple of 2");

            var bounds = new AxisAlignedBox2D();

            bounds.XMin = coordinates[0];
            bounds.YMin = coordinates[1];
            bounds.XMax = coordinates[0];
            bounds.YMax = coordinates[1];
            for (int i = dims; i < coordinates.Count - 1; i += dims)
            {
                bounds.XMin = Math.Min(bounds.XMin, coordinates[i]);
                bounds.YMin = Math.Min(bounds.YMin, coordinates[i + 1]);
                bounds.XMax = Math.Max(bounds.XMax, coordinates[i]);
                bounds.YMax = Math.Max(bounds.YMax, coordinates[i + 1]);
            }
            return bounds;
        }

        [Benchmark]
        public AxisAlignedBox2D BoundsSimpleLoop()
        {
            var coordinates = vectorBlock.RawCoordinates();
            if (coordinates.Count % dims != 0) throw new ArgumentException($"count of coordinates must be a multiple of 2");

            var bounds = new AxisAlignedBox2D();

            bounds.XMin = coordinates[0];
            bounds.YMin = coordinates[1];
            bounds.XMax = coordinates[0];
            bounds.YMax = coordinates[1];
            for (int i = dims; i < coordinates.Count - 1; i += dims)
            {
                if (coordinates[i] < bounds.XMin)     bounds.XMin = coordinates[i];
                if (coordinates[i + 1] < bounds.YMin) bounds.YMin = coordinates[i + 1];
                if (coordinates[i] > bounds.XMax)     bounds.XMax = coordinates[i];
                if (coordinates[i + 1] > bounds.YMax) bounds.YMax = coordinates[i + 1];
            }
            return bounds;
        }

        [Benchmark]
        public AxisAlignedBox2D BoundsSpanSIMDVector()
        {
            var coordinates = vectorBlock.RawCoordinates();
            int nonSIMDStartIdx = dims;
            var bounds = new AxisAlignedBox2D();

            if (dims == 2 && coordinates.Count >= Vector<float>.Count)
            {
                var coordSpan = vectorBlock.RawCoordinates().AsSpan();
                var vecSpan = MemoryMarshal.Cast<float, Vector<float>>(coordSpan);
                int chunkSize = Vector<float>.Count;

                var minVector = vecSpan[0];
                var maxVector = vecSpan[0];

                for (int i = 1; i < vecSpan.Length; i++)
                {
                    minVector = Vector.Min(minVector, vecSpan[i]);
                    maxVector = Vector.Max(maxVector, vecSpan[i]);
                }

                nonSIMDStartIdx = vecSpan.Length * chunkSize;

                bounds.XMin = minVector[0];
                bounds.YMin = minVector[1];
                bounds.XMax = maxVector[0];
                bounds.YMax = maxVector[1];

                for (int i = dims; i < chunkSize - 1; i += dims)
                {
                    bounds.XMin = Math.Min(bounds.XMin, minVector[i]);
                    bounds.YMin = Math.Min(bounds.YMin, minVector[i + 1]);
                    bounds.XMax = Math.Max(bounds.XMax, maxVector[i]);
                    bounds.YMax = Math.Max(bounds.YMax, maxVector[i + 1]);
                }
            }
            else if(coordinates.Count >= 24) { 
                var coordSpan = vectorBlock.RawCoordinates().AsSpan();
                var vecSpan = MemoryMarshal.Cast<float, Vector<float>>(coordSpan);
                //var vecSpanShifted = MemoryMarshal.Cast<float, Vector<float>>(coordSpan.Slice(1));
                int chunkSize = Vector<float>.Count;

                var mask = new int[chunkSize * 3];

                for (int i = 0; i < mask.Length; i += 3)
                {
                    mask[i] = -1;
                    mask[i + 1] = 0;
                    mask[i + 2] = 0;
                }

                var mask1 = new Vector<int>(mask);
                var mask2 = new Vector<int>(mask, chunkSize);
                var mask3 = new Vector<int>(mask, chunkSize * 2);

                var minXVector = Vector.ConditionalSelect(mask1, vecSpan[0], vecSpan[1]);
                var minYVector = Vector.ConditionalSelect(mask2, vecSpan[0], vecSpan[1]);
                minXVector = Vector.ConditionalSelect(mask3, vecSpan[2], minXVector);
                minYVector = Vector.ConditionalSelect(mask1, vecSpan[2], minYVector);
                var maxXVector = minXVector;
                var maxYVector = minYVector;

                for (int i = 3; i < vecSpan.Length - 2; i += 3)
                {
                    var vectorX = Vector.ConditionalSelect(mask1, vecSpan[0], vecSpan[1]);
                    var vectorY = Vector.ConditionalSelect(mask2, vecSpan[0], vecSpan[1]);
                    vectorX = Vector.ConditionalSelect(mask3, vecSpan[2], vectorX);
                    vectorY = Vector.ConditionalSelect(mask1, vecSpan[2], vectorY);
                    minXVector = Vector.Min(minXVector, vectorX);
                    minYVector = Vector.Min(minYVector, vectorY);
                    maxXVector = Vector.Max(maxXVector, vectorX);
                    maxYVector = Vector.Max(maxYVector, vectorY);
                }

                nonSIMDStartIdx = (vecSpan.Length - vecSpan.Length % 3) * chunkSize;

                bounds.XMin = minXVector[0];
                bounds.YMin = minYVector[0];
                bounds.XMax = maxXVector[0];
                bounds.YMax = maxYVector[0];

                for (int i = 1; i < chunkSize; i++)
                {
                    bounds.XMin = Math.Min(bounds.XMin, minXVector[i]);
                    bounds.YMin = Math.Min(bounds.YMin, minYVector[i]);
                    bounds.XMax = Math.Max(bounds.XMax, maxXVector[i]);
                    bounds.YMax = Math.Max(bounds.YMax, maxYVector[i]);
                }
            }
            else
            {
                bounds.XMin = coordinates[0];
                bounds.YMin = coordinates[1];
                bounds.XMax = coordinates[0];
                bounds.YMax = coordinates[1];
            }

            for (int i = nonSIMDStartIdx; i < coordinates.Count - 1; i += dims)
            {
                if (coordinates[i] < bounds.XMin) bounds.XMin = coordinates[i];
                if (coordinates[i + 1] < bounds.YMin) bounds.YMin = coordinates[i + 1];
                if (coordinates[i] > bounds.XMax) bounds.XMax = coordinates[i];
                if (coordinates[i + 1] > bounds.YMax) bounds.YMax = coordinates[i + 1];
            }

            return bounds;
        }

        [Benchmark]
        public AxisAlignedBox2D BoundsFinal()
        {
            return vectorBlock.Bounds2D();
        }

        [Benchmark]
        public AxisAlignedBox2D BoundsFloatSpan()
        {
            var span = spanArray.AsSpan<float>();
            return SIMDVectorOperations.Bounds2D(span, dims);
        }

        [Benchmark]
        public AxisAlignedBox2D BoundsSpanAVX256()
        {
            var coordinates = vectorBlock.RawCoordinates();
            int nonSIMDStartIdx = 1;
            //if(coordinates.Count > 10)
            var coordSpan = vectorBlock.RawCoordinates().AsSpan();
            var vecSpan = MemoryMarshal.Cast<float, Vector256<float>>(coordSpan);
            int chunkSize = Vector<float>.Count;

            var minVector = vecSpan[0];
            var maxVector = vecSpan[0];

            for (int i = 1; i < vecSpan.Length; i++)
            {
                minVector = Avx2.Min(minVector, vecSpan[i]);
                maxVector = Avx2.Max(maxVector, vecSpan[i]);
            }

            nonSIMDStartIdx = vecSpan.Length * chunkSize;

            var bounds = new AxisAlignedBox2D();

            bounds.XMin = minVector.GetElement(0);
            bounds.YMin = minVector.GetElement(1);
            bounds.XMax = maxVector.GetElement(0);
            bounds.YMax = maxVector.GetElement(1);

            for (int i = dims; i < chunkSize - 1; i += dims)
            {
                bounds.XMin = Math.Min(bounds.XMin, minVector.GetElement(i));
                bounds.YMin = Math.Min(bounds.YMin, minVector.GetElement(i+1));
                bounds.XMax = Math.Max(bounds.XMax, maxVector.GetElement(i));
                bounds.YMax = Math.Max(bounds.YMax, maxVector.GetElement(i+1));
            }

            for (int i = nonSIMDStartIdx; i < coordinates.Count - 1; i += dims)
            {
                bounds.XMin = Math.Min(bounds.XMin, coordinates[i]);
                bounds.YMin = Math.Min(bounds.YMin, coordinates[i + 1]);
                bounds.XMax = Math.Max(bounds.XMax, coordinates[i]);
                bounds.YMax = Math.Max(bounds.YMax, coordinates[i + 1]);
            }

            return bounds;
        }
        //[Benchmark]
        //public void BoundsFinal()
        //{

        //}
    }

    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net70, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class VectorRotate
    {
        [Params(1, 20, 40, 60, 80, 100, 10000, 1000000)]
        public int numVectors { get; set; }
        [Params(2, 3)]
        public int dims { get; set; }

        public const float rotation = (float)Math.PI / 6;

        public VectorBlock vectorBlock;
        private float[] spanArray;

        [GlobalSetup]
        public void GlobalSetup()
        {
            vectorBlock = new VectorBlock();
            if (dims == 2)
            {
                vectorBlock.LineSequence = new VectorBlock.Types.LineSequence();
                vectorBlock.LineSequence.Points.Capacity = numVectors * 2;
                Random random = new Random();
                for (int i = 0; i < numVectors; i++)
                {
                    vectorBlock.LineSequence.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence.Points.Add((float)(random.NextDouble() * 10f));
                }
                spanArray = vectorBlock.LineSequence.Points.ToArray();
            }
            else
            {
                vectorBlock.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                vectorBlock.LineSequence3D.Points.Capacity = numVectors * 3;
                Random random = new Random();
                for (int i = 0; i < numVectors; i++)
                {
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                }
                spanArray = vectorBlock.LineSequence3D.Points.ToArray();
            }
        }

        [Benchmark(Baseline = true)]
        public void RotateBaseline()
        {
            switch (vectorBlock.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                case VectorBlock.VectorDataOneofCase.Hatches:
                case VectorBlock.VectorDataOneofCase.PointSequence:
                case VectorBlock.VectorDataOneofCase.Arcs:
                case VectorBlock.VectorDataOneofCase.Ellipses:
                    RotateVector2(vectorBlock.RawCoordinates(), rotation, 2);
                    break;

                case VectorBlock.VectorDataOneofCase.LineSequence3D:
                case VectorBlock.VectorDataOneofCase.Hatches3D:
                case VectorBlock.VectorDataOneofCase.PointSequence3D:
                case VectorBlock.VectorDataOneofCase.Arcs3D:
                case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                    RotateVector2(vectorBlock.RawCoordinates(), rotation, 3);
                    break;

                case VectorBlock.VectorDataOneofCase.HatchParaAdapt:
                    foreach (var item in vectorBlock.HatchParaAdapt.HatchAsLinesequence)
                    {
                        RotateVector2(item.PointsWithParas, rotation, 3);
                    }
                    break;

                case VectorBlock.VectorDataOneofCase.None:
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    break;
                default:
                    throw new NotImplementedException($"unknown VectorDataCase: {vectorBlock.VectorDataCase}");
            }
        }

        private static void RotateVector2(RepeatedField<float> coords, float angleDeg, int dims)
        {
            var sin = (float) Math.Sin(angleDeg);
            var cos = (float) Math.Cos(angleDeg);
            var nsin = -sin;
            for (int i = 0; i < coords.Count - 1; i += dims)
            {
                float xNew = coords[i] * cos + coords[i + 1] * nsin;
                float yNew = coords[i] * sin + coords[i + 1] * cos;
                coords[i] = xNew; coords[i + 1] = yNew;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Vec2FromVec3
        {
            public Vector2 vec2;
            private float unused;
        }

#if NETCOREAPP3_0_OR_GREATER
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Vec256FromVec3
        {
            public Vector256<float> vec256;
            private float unused;
        }
#endif

        [Benchmark]
        public void RotateSpanSIMDSystemNumericsVector()
        {
            var coordSpan = vectorBlock.RawCoordinates().AsSpan();
            var rot = Matrix3x2.CreateRotation(rotation);
            if (dims == 2)
            {
                var vec2Span = MemoryMarshal.Cast<float, Vector2>(coordSpan);
                for (int i = 0; i < vec2Span.Length; i++)
                {
                    vec2Span[i] = Vector2.Transform(vec2Span[i], rot);
                }
            }
            else
            {
                var vec3Span = MemoryMarshal.Cast<float, Vec2FromVec3>(coordSpan);
                for (int i = 0; i < vec3Span.Length; i++)
                {
                    vec3Span[i].vec2 = Vector2.Transform(vec3Span[i].vec2, rot);
                }
            }
        }

#if NETCOREAPP3_0_OR_GREATER
        [Benchmark]
        public void RotateSpanAVX256()
        {
            var rot = Matrix3x2.CreateRotation(rotation);
            var coordSpan = vectorBlock.RawCoordinates().AsSpan();
            if (dims == 2)
            {
                if (Avx2.IsSupported)
                {
                    var sin = rot.M12;
                    var cos = rot.M11;
                    var nsin = rot.M21;

                    int chunkSize = Vector256<float>.Count;

                    var vec1 = Vector256.Create(cos);
                    var vec2 = Vector256.Create(sin, nsin, sin, nsin, sin, nsin, sin, nsin);
                    Vector256<int> shuffleMask = Vector256.Create(1, 0, 3, 2, 5, 4, 7, 6);

                    var vec256Span = MemoryMarshal.Cast<float, Vector256<float>>(coordSpan);

                    for (int i = 0; i < vec256Span.Length; i++)
                    {
                        var sumCos = Avx2.Multiply(vec256Span[i], vec1);
                        var sumSin = Avx2.Multiply(vec256Span[i], vec2);
                        var sumSinShuffled = Avx2.PermuteVar8x32(sumSin, shuffleMask);
                        vec256Span[i] = Avx2.Add(sumCos, sumSinShuffled);
                    }

                    coordSpan = coordSpan.Slice(vec256Span.Length * chunkSize);
                }

                var vec2Span = MemoryMarshal.Cast<float, Vector2>(coordSpan);
                for (int i = 0; i < vec2Span.Length; i++)
                {
                    vec2Span[i] = Vector2.Transform(vec2Span[i], rot);
                }
            }
            else
            {
                if (Avx2.IsSupported)
                {
                    var sin = rot.M12;
                    var cos = rot.M11;
                    var nsin = rot.M21;

                    int chunkSize = Vector256<float>.Count + 1;

                    var vec1 = Vector256.Create(cos,  cos, 1, cos,  cos, 1, cos, cos );
                    var vec2 = Vector256.Create(sin, nsin, 0, sin, nsin, 0, sin, nsin);
                    Vector256<int> shuffleMask = Vector256.Create(1, 0, 2, 4, 3, 5, 7, 6);

                    var vec256Span = MemoryMarshal.Cast<float, Vec256FromVec3>(coordSpan);
                    var numChunks = coordSpan.Length / chunkSize;
                    for (int i = 0; i < numChunks; i++)
                    {
                        var sumCos = Avx2.Multiply(vec256Span[i].vec256, vec1);
                        var sumSin = Avx2.Multiply(vec256Span[i].vec256, vec2);
                        var sumSinShuffled = Avx2.PermuteVar8x32(sumSin, shuffleMask);
                        vec256Span[i].vec256 = Avx2.Add(sumCos, sumSinShuffled);
                    }

                    coordSpan = coordSpan.Slice(numChunks * chunkSize);
                }

                var vec3Span = MemoryMarshal.Cast<float, Vec2FromVec3>(coordSpan);
                for (int i = 0; i < coordSpan.Length; i+=3)
                {
                    vec3Span[i].vec2 = Vector2.Transform(vec3Span[i].vec2, rot);
                }
            }
        }
#endif

        [Benchmark]
        public void RotateFinal()
        {
            vectorBlock.Rotate(rotation);
        }

        [Benchmark]
        public void RotateFloatSpan()
        {
            SIMDVectorOperations.RotateAsVector2(spanArray.AsSpan<float>(), rotation, dims);
        }
    }

    public class VectorTransformBenchmark
    {
        public static void Main(string[] args)
        {
            debugVec2Trans();
            debugVec3Trans();
            debugVec2Rot();
            debugVec3Rot();
            debugVec2Bounds();
            debugVec3Bounds();
            Type[] benchmarks = { typeof(VectorBlockBounds) };//typeof(VectorBlockBounds) };//, typeof(VectorTranslate), typeof(VectorRotate) };
            var summary = BenchmarkRunner.Run(benchmarks);
        }

        public static (VectorBlock, float[]) GlobalSetup(int dims, int numVectors)
        {
            float[] spanArray;
            var vectorBlock = new VectorBlock();
            if (dims == 2)
            {
                vectorBlock.LineSequence = new VectorBlock.Types.LineSequence();
                vectorBlock.LineSequence.Points.Capacity = numVectors * 2;
                Random random = new Random();
                for (int i = 0; i < numVectors; i++)
                {
                    vectorBlock.LineSequence.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence.Points.Add((float)(random.NextDouble() * 10f));
                }
                spanArray = vectorBlock.LineSequence.Points.ToArray();
            }
            else
            {
                vectorBlock.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                vectorBlock.LineSequence3D.Points.Capacity = numVectors * 3;
                Random random = new Random();
                for (int i = 0; i < numVectors; i++)
                {
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                    vectorBlock.LineSequence3D.Points.Add((float)(random.NextDouble() * 10f));
                }
                spanArray = vectorBlock.LineSequence3D.Points.ToArray();
            }
            return (vectorBlock, spanArray);
        }

        private static void debugVec2Trans()
        {
            var bench = new VectorTranslate();
            bench.numVectors = 10;
            bench.dims = 2;
            bench.GlobalSetup();
            var origBlock = bench.vectorBlock.Clone();

            bench.TranslateBaseline();
            var transBase = bench.vectorBlock;

            bench.vectorBlock = origBlock.Clone();
            bench.TranslateSpanSIMDSystemNumericsVector();
            var trans1 = bench.vectorBlock;
            if (!trans1.Equals(transBase)) throw new Exception($"{trans1.LineSequence.Points}\r{transBase.LineSequence.Points}");

            bench.vectorBlock = origBlock.Clone();
            bench.TranslateSpanSIMDVector();
            var trans2 = bench.vectorBlock;
            if (!trans2.Equals(transBase)) throw new Exception($"{trans2.LineSequence.Points}\r{transBase.LineSequence.Points}");
        }

        private static void debugVec2Rot()
        {
            var bench = new VectorRotate();
            bench.numVectors = 10;
            bench.dims = 2;
            bench.GlobalSetup();
            var origBlock = bench.vectorBlock.Clone();

            bench.RotateBaseline();
            var transBase = bench.vectorBlock;

            bench.vectorBlock = origBlock.Clone();
            bench.RotateSpanSIMDSystemNumericsVector();
            var trans1 = bench.vectorBlock;
            if (!trans1.Equals(transBase)) throw new Exception($"{trans1.LineSequence.Points}\r{transBase.LineSequence.Points}");

            bench.vectorBlock = origBlock.Clone();
            bench.RotateFinal();
            var trans2 = bench.vectorBlock;
            if (!trans2.Equals(transBase)) throw new Exception($"{trans2.LineSequence.Points}\r{transBase.LineSequence.Points}");
        }

        private static void debugVec3Rot()
        {
            var bench = new VectorRotate();
            bench.numVectors = 10;
            bench.dims = 3;
            bench.GlobalSetup();
            var origBlock = bench.vectorBlock.Clone();

            bench.RotateBaseline();
            var transBase = bench.vectorBlock;

            bench.vectorBlock = origBlock.Clone();
            bench.RotateSpanSIMDSystemNumericsVector();
            var trans1 = bench.vectorBlock;
            if (!trans1.Equals(transBase)) throw new Exception($"{trans1.LineSequence3D.Points}\r{transBase.LineSequence3D.Points}");

            bench.vectorBlock = origBlock.Clone();
            bench.RotateFinal();
            var trans2 = bench.vectorBlock;
            if (!trans2.Equals(transBase)) throw new Exception($"{trans2.LineSequence3D.Points}\r{transBase.LineSequence3D.Points}");
        }

        private static void debugVec3Trans()
        {
            var bench = new VectorTranslate();
            bench.numVectors = 12;
            bench.dims = 3;
            bench.GlobalSetup();
            var origBlock = bench.vectorBlock.Clone();

            bench.TranslateBaseline();
            var transBase = bench.vectorBlock;

            bench.vectorBlock = origBlock.Clone();
            bench.TranslateSpanSIMDVector();
            var trans2 = bench.vectorBlock;
            if (!trans2.Equals(transBase)) throw new Exception($"{trans2.LineSequence3D.Points}\r{transBase.LineSequence3D.Points}");
        }

        private static void debugVec2Bounds()
        {
            var bench = new VectorBlockBounds();
            bench.numVectors = 10;
            bench.dims = 2;
            bench.GlobalSetup();

            var bounds1 = bench.BoundsBaseline();

            var bounds2 = bench.BoundsSpanSIMDVector();

            if (!bounds1.Equals(bounds2)) throw new Exception($"{bounds1}\r{bounds2}");

            var bounds3 = bench.BoundsFinal();

            if (!bounds1.Equals(bounds3)) throw new Exception($"{bounds1}\r{bounds3}");
        }

        private static void debugVec3Bounds()
        {
            var bench = new VectorBlockBounds();
            bench.numVectors = 12;
            bench.dims = 3;
            bench.GlobalSetup();

            var bounds1 = bench.BoundsBaseline();

            var bounds2 = bench.BoundsSpanSIMDVector();

            if (!bounds1.Equals(bounds2)) throw new Exception($"{bounds1}\r{bounds2}");

            var bounds3 = bench.BoundsFinal();

            if (!bounds1.Equals(bounds3)) throw new Exception($"{bounds1}\r{bounds3}");
        }

    }
}