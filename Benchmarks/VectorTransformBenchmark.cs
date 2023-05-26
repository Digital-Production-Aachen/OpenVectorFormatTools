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
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Google.Protobuf.Collections;
using OpenVectorFormat;
using OpenVectorFormat.Utils;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class VectorTransform
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

    public class VectorTransformBenchmark
    {
        public static void Main(string[] args)
        {
            debugVec2();
            debugVec3();
            var summary = BenchmarkRunner.Run<VectorTransform>(/*new DebugInProcessConfig()*/);
        }

        private static void debugVec2()
        {
            var bench = new VectorTransform();
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

        private static void debugVec3()
        {
            var bench = new VectorTransform();
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