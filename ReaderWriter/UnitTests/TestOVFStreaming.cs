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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat;
using OpenVectorFormat.OVFReaderWriter;
using OpenVectorFormat.Plausibility;
using OpenVectorFormat.Streaming;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace UnitTests
{
    [TestClass]
    public class TestOVFStreaming
    {

        /// <summary>
        /// Test OVFStreamingMerger by merging one part and support and applying the Plausibilitychecker.
        /// </summary>
        [TestMethod]
        public void TestMergePartConfig()
        {
            string sourceDir = new[] {"..", "..", "..", "TestFiles"}.Aggregate(Path.Combine); 
            string partFile = "bunny";
            string supportFile = "bunny (solidsupport)";

            using (OVFFileReader partReader = new OVFFileReader())
            using (OVFFileReader supportReader = new OVFFileReader())
            {
                // load part and support ovf
                partReader.OpenJobAsync(Path.Combine(sourceDir, partFile) + ".ovf", null).GetAwaiter().GetResult();
                supportReader.OpenJobAsync(Path.Combine(sourceDir, supportFile) + ".ovf", null).GetAwaiter().GetResult();

                // run plausibility checks on input
                PlausibilityChecker.CheckJob(partReader.CacheJobToMemoryAsync().GetAwaiter().GetResult(), new CheckerConfig())
                    .GetAwaiter().GetResult();
                PlausibilityChecker.CheckJob(supportReader.CacheJobToMemoryAsync().GetAwaiter().GetResult(), new CheckerConfig())
                    .GetAwaiter().GetResult();

                // merge part with supports
                var merger = new OVFStreamingMerger(partReader);
                merger.AddFileReaderToMerge(new FileReaderToMerge() { fr = supportReader, markAsSupport = true });

                // run plausibility checks on result
                var job = merger.CacheJobToMemoryAsync().GetAwaiter().GetResult();
                PlausibilityChecker.CheckJob(job, new CheckerConfig()).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Test OVFStreamingMerger by merging several instances of the same part together and
        /// applying the Plausibilitychecker.
        /// </summary>
        [TestMethod]
        public void TestMergeInstances()
        {
            string sourceDir = new[] { "..", "..", "..", "TestFiles" }.Aggregate(Path.Combine);
            string partFile = "bunny";
            string supportFile = "bunny (solidsupport)";
            (float x, float y, float rot)[] positions = new (float, float, float)[]
            {
                (100, 0, 0),
                (200, 100, (float)Math.PI/2),
                (300, 0, 0),
            };

            using (OVFFileReader partReader = new OVFFileReader())
            using (OVFFileReader supportReader = new OVFFileReader())
            {
                // load part and support ovf
                partReader.OpenJobAsync(Path.Combine(sourceDir, partFile) + ".ovf", null).GetAwaiter().GetResult();
                supportReader.OpenJobAsync(Path.Combine(sourceDir, supportFile) + ".ovf", null).GetAwaiter().GetResult();

                // run plausibility checks on input
                PlausibilityChecker.CheckJob(partReader.CacheJobToMemoryAsync().GetAwaiter().GetResult(), new CheckerConfig())
                    .GetAwaiter().GetResult();
                PlausibilityChecker.CheckJob(supportReader.CacheJobToMemoryAsync().GetAwaiter().GetResult(), new CheckerConfig())
                    .GetAwaiter().GetResult();

                // merge part with supports
                var partSupportReader = new OVFStreamingMerger(partReader);
                partSupportReader.AddFileReaderToMerge(new FileReaderToMerge() { fr = supportReader, markAsSupport = true });

                // merge parts at different positions
                OVFStreamingMerger jobMerger = null;
                for (int i = 0; i < positions.Length; i++)
                {
                    var pos = positions[i];
                    var toMerge = new FileReaderToMerge()
                    {
                        fr = partSupportReader,
                        translationX = pos.x,
                        translationY = pos.y,
                        rotationInRad = pos.rot,
                        markAsSupport = false
                    };
                    if (i == 0) jobMerger = new OVFStreamingMerger(toMerge);
                    else if (i > 0) jobMerger?.AddFileReaderToMerge(toMerge);
                }

                // run plausibility checks on result
                var job = jobMerger.CacheJobToMemoryAsync().GetAwaiter().GetResult();
                PlausibilityChecker.CheckJob(job, new CheckerConfig()).GetAwaiter().GetResult();

                // check layer heights
                Assert.AreEqual(jobMerger.JobShell.NumWorkPlanes, partSupportReader.JobShell.NumWorkPlanes);

                // check bounding boxes
                for (int wpnr = 0; wpnr < jobMerger.JobShell.NumWorkPlanes; wpnr++)
                {
                    AxisAlignedBox2D aggregateAABB = null;
                    var jobAABB = jobMerger.GetWorkPlaneAsync(wpnr).GetAwaiter().GetResult().Bounds2D();
                    for (int i = 0; i < positions.Length; i++)
                    {
                        var pos = positions[i];
                        var wp = partSupportReader.GetWorkPlaneAsync(wpnr).GetAwaiter().GetResult();
                        var wpCopy = wp.Clone();
                        wpCopy.Rotate(pos.rot);
                        wpCopy.Translate(new Vector2(pos.x, pos.y));
                        var aabb = wpCopy.Bounds2D();
                        if (i == 0) aggregateAABB = aabb;
                        else aggregateAABB.Contain(aabb);
                    }
                    //Console.WriteLine($"XMin: {aggregateAABB.XMin} {jobAABB.XMin}");
                    //Console.WriteLine($"XMax: {aggregateAABB.XMax} {jobAABB.XMax}");
                    //Console.WriteLine($"YMin: {aggregateAABB.YMin} {jobAABB.YMin}");
                    //Console.WriteLine($"YMax: {aggregateAABB.YMax} {jobAABB.YMax}");
                    Assert.IsTrue(ApproxEquals(aggregateAABB.XMin, jobAABB.XMin));
                    Assert.IsTrue(ApproxEquals(aggregateAABB.XMax, jobAABB.XMax));
                    Assert.IsTrue(ApproxEquals(aggregateAABB.YMin, jobAABB.YMin));
                    Assert.IsTrue(ApproxEquals(aggregateAABB.YMax, jobAABB.YMax));
                }
            }
        }

        private bool ApproxEquals(float value, float other, float tolerance = 1e-6f)
        {
            return Math.Abs(value - other) < tolerance;
        }
    }
}
