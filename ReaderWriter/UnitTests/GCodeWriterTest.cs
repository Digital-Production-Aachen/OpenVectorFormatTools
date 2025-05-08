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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.GCodeReaderWriter;
using OpenVectorFormat;
using GCodeReaderWriter;
using OpenVectorFormat.OVFReaderWriter;
using OpenVectorFormat.ReaderWriter.UnitTests;
using System.IO;

namespace UnitTests
{
    [TestClass]
    public class GCodeWriterTest
    {
        private string ovfFilePath = "\\bunny.ovf";
        private string gcodeOutputPath = "\\output.gcode";
        private OVFFileReader ovfReader;
        private GCodeWriter gcodeWriter;

        [TestInitialize]
        public void Setup()
        {
            Assert.IsTrue(File.Exists(ovfFilePath), "OVF file not found");
            ovfReader = new OVFFileReader();
            gcodeWriter = new GCodeWriter();
        }

        [TestMethod]
        public void Test_OVF_To_GCode()
        {
            ovfReader.OpenJob(ovfFilePath);
            Job job = ovfReader.JobShell;

            Assert.IsNotNull(job, "Job is null");
            Assert.IsTrue(job.NumWorkPlanes > 0, "No WorkPlanes in the OVF");

            List<WorkPlaneData> workPlanesData = new List<WorkPlaneData>();

            for (int i = 0; i < job.NumWorkPlanes; i++)
            {
                var workPlane = ovfReader.GetWorkPlane(i);
                Assert.IsNotNull(workPlane, $"WorkPlane {i} is null");


                job.WorkPlanes.Add(workPlane);

                List<VectorBlockData> vectorBlocksData = new List<VectorBlockData>();


                for (int j = 0; j < workPlane.NumBlocks; j++)
                {
                    var vectorBlock = ovfReader.GetVectorBlock(i, j);
                    Assert.IsNotNull(vectorBlock, $"VectorBlock {j} in WorkPlane {i}not null");


                    vectorBlocksData.Add(new VectorBlockData
                    {
                        BlockIndex = j,
                        BlockData = vectorBlock.ToString()
                    });

                    Console.WriteLine($"Processed VectorBlock {j} in WorkPlane {i}");
                }






                workPlanesData.Add(new WorkPlaneData
                {
                    WorkPlaneIndex = i,
                    NumBlocks = workPlane.NumBlocks,
                    VectorBlocks = vectorBlocksData
                });

                Console.WriteLine($"WorkPlane {i} with {workPlane.NumBlocks} VectorBlocks added to job.");
            }

            Console.WriteLine($"job.NumWorkPlanes = {job.NumWorkPlanes}");
            Console.WriteLine($"job.WorkPlanes.Count = {job.WorkPlanes.Count}");

            gcodeWriter.SimpleJobWrite(job, gcodeOutputPath);

            Assert.IsTrue(File.Exists(gcodeOutputPath), "GCode file not created");
            Assert.IsTrue(new FileInfo(gcodeOutputPath).Length > 0, "GCode file is empty");

            Console.WriteLine("GCode successfully saved to: " + gcodeOutputPath);


            //try
            //{
            //    Console.WriteLine($"job.NumWorkPlanes = {job.NumWorkPlanes}");
            //    Console.WriteLine($"job.WorkPlanes.Count = {job.WorkPlanes.Count}");

            //    gcodeWriter.SimpleJobWrite(job, gcodeOutputPath);

            //    Assert.IsTrue(File.Exists(gcodeOutputPath), "GCode file not created");
            //    Assert.IsTrue(new FileInfo(gcodeOutputPath).Length > 0, "GCode file is empty");

            //    Console.WriteLine("GCode successfully saved to: " + gcodeOutputPath);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Saving error: {ex.Message}");
            //}
        }

        [TestCleanup]
        public void Cleanup()
        {
            ovfReader?.Dispose();
            gcodeWriter?.Dispose();


        }

        public class WorkPlaneData
        {
            public int WorkPlaneIndex { get; set; }
            public int NumBlocks { get; set; }
            public List<VectorBlockData> VectorBlocks { get; set; }
        }

        public class VectorBlockData
        {
            public int BlockIndex { get; set; }
            public string BlockData { get; set; }
        }
    }
}