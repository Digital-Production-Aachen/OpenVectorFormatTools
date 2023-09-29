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
using OpenVectorFormat.OVFReaderWriter;
using OpenVectorFormat.Plausibility;
using OpenVectorFormat.Streaming;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class TestOVFStreaming
    {
        [TestMethod]
        public void TestMergePartConfig()
        {
            Console.WriteLine("Test");
            string sourceDir = @"..\..\..\TestFiles\";
            string partFile = "bunny";
            string supportFile = "bunny (solidsupport)";
            
            using (OVFFileReader partReader = new OVFFileReader())
            using (OVFFileReader supportReader = new OVFFileReader())
            {
                // run plausibility checks on input
                PlausibilityCheckOVFFile.CheckJobFile(sourceDir + partFile + ".ovf", new CheckerConfig()).GetAwaiter().GetResult();
                PlausibilityCheckOVFFile.CheckJobFile(sourceDir + supportFile + ".ovf", new CheckerConfig()).GetAwaiter().GetResult();
                
                // load part and support ovf
                partReader.OpenJobAsync(sourceDir + partFile + ".ovf", null).GetAwaiter().GetResult();
                supportReader.OpenJobAsync(sourceDir + supportFile + ".ovf", null).GetAwaiter().GetResult();
                
                // merge part with supports
                //var merger = new OVFStreamingMerger(partReader);
                //merger.AddFileReaderToMerge(new FileReaderToMerge() { fr = supportReader, markAsSupport = true });
            }
        }
    }
}
