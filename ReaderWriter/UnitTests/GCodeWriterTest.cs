/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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

using System;
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
using System.Text.RegularExpressions;

namespace UnitTests
{
    [TestClass]
    public class GCodeWriterTest
    {
        private string ovfFilePath = "C:\\Users\\sambi\\Source\\Repos\\OpenVectorFormatTools\\ReaderWriter\\UnitTests\\TestFiles\\bunny.ovf";
        private string gcodeOutputPath = "C:\\Users\\sambi\\Documents\\temp\\output3.gcode";
        private OVFFileReader ovfReader;
        private GCodeWriter gcodeWriter;

        [TestInitialize]
        public void Setup()
        {
            Assert.IsTrue(File.Exists(ovfFilePath), "OVF file not found");

            ovfReader = new OVFFileReader();
            ovfReader.OpenJob(ovfFilePath);

            gcodeWriter = new GCodeWriter();
        }

        [TestMethod]
        public void Test_OVF_To_GCode()
        {
            gcodeWriter.ProcessOVFtoGCode(ovfReader, gcodeOutputPath);
            Assert.IsTrue(File.Exists(gcodeOutputPath), "G-code output file was not created");

            string[] lines = File.ReadAllLines(gcodeOutputPath);
            Assert.IsTrue(lines.Length > 0, "Output file is empty");

            var job = ovfReader.CacheJobToMemory();
            int numberWorkplanes = job.NumWorkPlanes;
            int numberZCoordinate = lines.Count(line => line.Contains("Z") && Regex.IsMatch(line, @"Z[-+]?\d+(\.\d+)?"));

            Console.WriteLine($"[INFO] WorkPlanes: {numberWorkplanes}, Z commands found in G-code: {numberZCoordinate}");
            Assert.AreEqual(numberWorkplanes, numberZCoordinate, $"Expected {numberWorkplanes} Z commands (WorkPlanes), but found {numberZCoordinate}.");

            string firstLine = lines[0].Trim();
            string expectedFirstLine = "G0 X-18.0231 Y-4.8820124 Z3";

            Console.WriteLine($"[INFO] First line: \"{firstLine}\"");
            Assert.AreEqual(expectedFirstLine, firstLine, $"First G-code line mismatch. Expected \"{expectedFirstLine}\", but got \"{firstLine}\".");
        }

        [TestCleanup]
        public void Cleanup()
        {
            ovfReader?.Dispose();
            gcodeWriter?.Dispose();
        }
    }
}