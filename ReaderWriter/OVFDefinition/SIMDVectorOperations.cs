﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Transactions;

namespace OpenVectorFormat
{
    public static class SIMDVectorOperations
    {
        /// <summary>
        /// Interprets a span of floats as an array of Vector2 structs [x0 y0 x1 y1 ...]
        /// and translates all the coordinates by translation, using SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="translation"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void TranslateAsVector2(Span<float> coordinates, Vector2 translation)
        {
            if (coordinates.Length % 2 != 0) throw new ArgumentException($"count of coordinates must be even, but is {coordinates.Length}");
            //did some benchmarks (on AVX2 capable hardware) to estimate the threshold when the overhead of
            //getting the span with reflection is compensated by SIMD speedup => ~190
            if (coordinates.Length > 190 && Vector.IsHardwareAccelerated && Vector<float>.Count % 2 == 0)
            {
                var vecSpan = MemoryMarshal.Cast<float, Vector<float>>(coordinates);
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

                var restCoord = coordinates.Slice(vecSpan.Length * chunkSize);

                var vec2Span = MemoryMarshal.Cast<float, Vector2>(restCoord);
                for (int i = 0; i < vec2Span.Length; i++)
                {
                    vec2Span[i] += translation;
                }
            }
            else
            {
                for (int i = 0; i < coordinates.Length - 1; i += 2)
                {
                    coordinates[i] += translation.X;
                    coordinates[i + 1] += translation.Y;
                }
            }
        }

        /// <summary>
        /// Interprets a span of floats as an array of Vector3 structs [x0 y0 z0 x1 y1 z1 ...]
        /// and translates the x and y coordinates by translation, using SIMD hardware acceleration if available.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="translation"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void TranslateAsVector3(Span<float> coordinates, Vector2 translation)
        {
            if (coordinates.Length % 3 != 0) throw new ArgumentException($"count of coordinates must be divisible by 3, but is {coordinates.Length}");

            if (coordinates.Length > 300 && Vector.IsHardwareAccelerated)
            {
                int chunkSize = Vector<float>.Count;
                var vecSpan = MemoryMarshal.Cast<float, Vector<float>>(coordinates);

                var inputVec1 = new float[chunkSize];
                var inputVec2 = new float[chunkSize];
                var inputVec3 = new float[chunkSize];

                for (int i = 0; i < chunkSize; i += 3)
                {
                    inputVec1[i] = translation.X;
                    if (i + 1 < chunkSize) inputVec1[i + 1] = translation.Y;

                    if (i + 1 < chunkSize) inputVec2[i + 1] = translation.X;
                    if (i + 2 < chunkSize) inputVec2[i + 2] = translation.Y;

                    if (i + 2 < chunkSize) inputVec3[i + 2] = translation.X;
                    inputVec3[i] = translation.Y;
                }

                var addVec1 = new Vector<float>(inputVec1);
                var addVec2 = new Vector<float>(inputVec2);
                var addVec3 = new Vector<float>(inputVec3);

                for (int i = 0; i < vecSpan.Length - 2; i += 3)
                {
                    vecSpan[i] += addVec1;
                    vecSpan[i + 1] += addVec2;
                    vecSpan[i + 2] += addVec3;
                }

                var rest = vecSpan.Length % 3;
                if (rest == 1)
                {
                    vecSpan[vecSpan.Length - 1] += addVec1;
                }
                else if (rest == 2)
                {
                    vecSpan[vecSpan.Length - 2] += addVec1;
                    vecSpan[vecSpan.Length - 1] += addVec2;
                }

                var restCoord = coordinates.Slice(vecSpan.Length * chunkSize);
                var offset = (vecSpan.Length * chunkSize) % 3;
                for (int i = offset; i < restCoord.Length + offset; i++)
                {
                    restCoord[i - offset] += i % 3 == 0 ? translation.X : i % 3 == 1 ? translation.Y : 0;
                }
            }
            else
            {
                for (int i = 0; i < coordinates.Length - 1; i += 3)
                {
                    coordinates[i] += translation.X;
                    coordinates[i + 1] += translation.Y;
                }
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

        /// <summary>
        /// Interprets a span of floats as an array of Vector2 structs [x0 y0 x1 y1 ...]
        /// or higher dimension vector structs like Vector3 [x0 y0 z0 x1 y1 z1 ...]
        /// and translates x and y the coordinates by translation.
        /// Uses SIMD hardware acceleration if Avx2 is available and dimension is 2 or 3.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="angleRad"></param>
        /// <param name="dims">the dimension of the input vector structs</param>
        /// <exception cref="ArgumentException"></exception>
        public static void RotateAsVector2(Span<float> coordinates, float angleRad, int dims = 2)
        {
            if (coordinates.Length % dims != 0) throw new ArgumentException($"coordinates count is {coordinates.Length} but must be a multiple of {dims}");

            var sin = (float)Math.Sin(angleRad);
            var cos = (float)Math.Cos(angleRad);
            var nsin = -sin;
            int noSIMDStartIndex = 0;

#if NETCOREAPP3_0_OR_GREATER
            //the variable size Vector does not have permute/shuffle methods that we need for a matrix multiplication
            //so instead we use Avx2 with Vector256 if available (and did not bother to create a separate Vector128 code path)
            if (Avx2.IsSupported & coordinates.Length > 80 * dims)
            {
                if (dims == 2)
                {
                    int chunkSize = Vector256<float>.Count;
                    var vec1 = Vector256.Create(cos);
                    var vec2 = Vector256.Create(sin, nsin, sin, nsin, sin, nsin, sin, nsin);
                    Vector256<int> shuffleMask = Vector256.Create(1, 0, 3, 2, 5, 4, 7, 6);

                    var vec256Span = MemoryMarshal.Cast<float, Vector256<float>>(coordinates);
                    for (int i = 0; i < vec256Span.Length; i++)
                    {
                        var sumCos = Avx2.Multiply(vec256Span[i], vec1);
                        var sumSin = Avx2.Multiply(vec256Span[i], vec2);
                        var sumSinShuffled = Avx2.PermuteVar8x32(sumSin, shuffleMask);
                        vec256Span[i] = Avx2.Add(sumCos, sumSinShuffled);
                    }
                    noSIMDStartIndex = vec256Span.Length * chunkSize;
                }
                else if (dims == 3)
                {
                    int chunkSize = Vector256<float>.Count + 1;
                    var vec1 = Vector256.Create(cos, cos, 1, cos, cos, 1, cos, cos);
                    var vec2 = Vector256.Create(sin, nsin, 0, sin, nsin, 0, sin, nsin);
                    Vector256<int> shuffleMask = Vector256.Create(1, 0, 2, 4, 3, 5, 7, 6);

                    //we process 9 floats at once with one Vector256. For this we throw away every 9th float
                    //so the resulting vector is [x0 y0 z0 x1 y1 z1 x2 y2] [z2]
                    //because we only need x and y for the calculation
                    var vec256Span = MemoryMarshal.Cast<float, Vec256FromVec3>(coordinates);
                    for (int i = 0; i < vec256Span.Length; i++)
                    {
                        var sumCos = Avx2.Multiply(vec256Span[i].vec256, vec1);
                        var sumSin = Avx2.Multiply(vec256Span[i].vec256, vec2);
                        var sumSinShuffled = Avx2.PermuteVar8x32(sumSin, shuffleMask);
                        vec256Span[i].vec256 = Avx2.Add(sumCos, sumSinShuffled);
                    }
                    noSIMDStartIndex = vec256Span.Length * chunkSize;
                }
            }
#endif

            for (int i = noSIMDStartIndex; i < coordinates.Length - 1; i += dims)
            {
                float xNew = coordinates[i] * cos + coordinates[i + 1] * nsin;
                float yNew = coordinates[i] * sin + coordinates[i + 1] * cos;
                coordinates[i] = xNew; coordinates[i + 1] = yNew;
            }
        }
    }
}
