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

﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Transactions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Google.Protobuf.Collections;
using OpenVectorFormat;
using OpenVectorFormat.Utils;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class VectorTranslate
    {
        [Params(1, 60 ,100, 10000, 1000000)]
        public int numVectors { get; set; }
        [Params(2, 3)]
        public int dims { get; set; }
        public VectorBlock vectorBlock;
        public Vector2 translation = new Vector2(4, 5);

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
        public void TranslateSpanSIMDVectorWFallback()
        {
            var coords = vectorBlock.RawCoordinates();
            if (coords.Count > 190 && Vector.IsHardwareAccelerated)
            {
                TranslateSpanSIMDVector();
            }
            else
            {
                AddToVector2(coords, translation);
            }
        }
    }

    [MemoryDiagnoser]
    public class VectorRotate
    {
        [Params(1, 10, 20, 100, 10000, 1000000)]
        public int numVectors { get; set; }
        [Params(2, 3)]
        public int dims { get; set; }
        public VectorBlock vectorBlock;
        public const float rotation = MathF.PI / 6;

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
            var sin = MathF.Sin(angleDeg);
            var cos = MathF.Cos(angleDeg);
            var nsin = -sin;
            for (int i = 0; i < coords.Count - 1; i += dims)
            {
                float xNew = coords[i] * cos + coords[i + 1] * nsin;
                float yNew = coords[i] * sin + coords[i + 1] * cos;
                coords[i] = xNew; coords[i + 1] = yNew;
            }
        }

        private static float[,] RotationMatrix2D(float angleRad)
        {
            return new float[2, 2] {
                        { MathF.Cos(angleRad), -MathF.Sin(angleRad) },
                        { MathF.Sin(angleRad),  MathF.Cos(angleRad) },
                    };
        }

        struct Vec2FromVec3 { public Vector2 vec2; float unused; };

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

        [Benchmark]
        public void RotateSpanSIMDVector()
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
                        var sum = Avx2.Multiply(vec256Span[i], vec2);
                        var sumPermuted = Avx2.PermuteVar8x32(sum, shuffleMask);
                        vec256Span[i] = Fma.MultiplyAdd(vec256Span[i], vec1, sumPermuted);
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

                    int chunkSize = Vector256<float>.Count;

                    var vec1 = Vector256.Create(cos);
                    var vec2 = Vector256.Create(sin, nsin, sin, nsin, sin, nsin, sin, nsin);
                    Vector256<int> shuffleMask = Vector256.Create(1, 0, 3, 2, 5, 4, 7, 6);

                    var vec256Span = MemoryMarshal.Cast<float, Vector256<float>>(coordSpan);

                    for (int i = 0; i < vec256Span.Length; i++)
                    {
                        var sum = Avx2.Multiply(vec256Span[i], vec2);
                        var sumPermuted = Avx2.PermuteVar8x32(sum, shuffleMask);
                        vec256Span[i] = Fma.MultiplyAdd(vec256Span[i], vec1, sumPermuted);
                    }

                    coordSpan = coordSpan.Slice(vec256Span.Length * chunkSize);
                }
                //var vecSpan = MemoryMarshal.Cast<float, Vector<float>>(coordSpan);
                //int chunkSize = Vector<float>.Count;
                //var inputVec = new float[chunkSize * 3];

                //for (int i = 0; i < chunkSize * 3; i += 3)
                //{
                //    inputVec[i] = translation.X;
                //    inputVec[i + 1] = translation.Y;
                //    //inputVec[i + 2] = 0;
                //}

                //var addVec1 = new Vector<float>(inputVec, 0);
                //var addVec2 = new Vector<float>(inputVec, chunkSize);
                //var addVec3 = new Vector<float>(inputVec, chunkSize * 2);

                //for (int i = 0; i < vecSpan.Length - 2; i += 3)
                //{
                //    vecSpan[i] += addVec1;
                //    vecSpan[i + 1] += addVec2;
                //    vecSpan[i + 2] += addVec3;
                //}

                //var rest = vecSpan.Length % 3;
                //if (rest == 1)
                //{
                //    vecSpan[vecSpan.Length - 1] += addVec1;
                //}
                //else if (rest == 2)
                //{
                //    vecSpan[vecSpan.Length - 2] += addVec1;
                //    vecSpan[vecSpan.Length - 1] += addVec2;
                //}

                var vec3Span = MemoryMarshal.Cast<float, Vec2FromVec3>(coordSpan);
                for (int i = 0; i < vec3Span.Length; i++)
                {
                    vec3Span[i].vec2 = Vector2.Transform(vec3Span[i].vec2, rot);
                }
            }
        }

        //[Benchmark]
        //public void TranslateSpanSIMDVectorWFallback()
        //{
        //    var coords = vectorBlock.RawCoordinates();
        //    if (coords.Count > 190 && Vector.IsHardwareAccelerated)
        //    {
        //        TranslateSpanSIMDVector();
        //    }
        //    else
        //    {
        //        AddToVector2(coords, translation);
        //    }
        //}
    }

    public class VectorTransformBenchmark
    {
        public static void Main(string[] args)
        {
            //debugVec2Trans();
            debugVec2Rot();
            //debugVec3();
            var summary = BenchmarkRunner.Run<VectorTranslate>();
            //var summary2 = BenchmarkRunner.Run<VectorRotate>();
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
            bench.RotateSpanSIMDVector();
            var trans2 = bench.vectorBlock;
            //if (!trans2.Equals(transBase)) throw new Exception($"{trans2.LineSequence.Points}\r{transBase.LineSequence.Points}");
        }

        private static void debugVec3()
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

    }
}