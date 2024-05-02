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

﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat;
using OpenVectorFormat.FileReaderWriterFactory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class TestComputeMetaData
    {
        private static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [TestMethod]
        public void TestComputeMetaDataBunny()
        {
            var testFileName = Path.Combine(dir.FullName, "bunny.ovf");
            using var reader = FileReaderFactory.CreateNewReader(Path.GetExtension(testFileName));
            reader.OpenJob(testFileName);
            var jobShell = reader.JobShell;

            int numLayers = jobShell.NumWorkPlanes;
            double jumpLength = 0, markLength = 0;
            for(int i = 0; i < numLayers; i++)
            {
                var wp = reader.GetWorkPlane(i);
                if (wp.VectorBlocks.Count == 0) continue;
                foreach(var vectorBlock in wp.VectorBlocks)
                {
                    var metaData = vectorBlock.ComputeAndStoreMetaData();
                    jumpLength += metaData.TotalJumpDistanceInMm;
                    markLength += metaData.TotalScanDistanceInMm;
                }
            }

            const double precision = 0.1;
            markLength.Should().BeApproximately(831978.62, precision);
            //jumpLength.Should().BeApproximately(928193.66, precision);//ups this jump distance includes jumps between blocks and skywriting jumps
            jumpLength.Should().BeApproximately(821578.01, precision);
        }
    }
}
