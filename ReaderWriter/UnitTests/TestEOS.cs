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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat.EOSReaderWriter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class TestEOS
    {
        [TestMethod]
        public void TestReadEVB()
        {
            string testfile = new DirectoryInfo(Path.Combine([Directory.GetCurrentDirectory(), "TestFiles", "ACAM_test.evb"])).FullName;
            using var reader = new EOSFileReader();

            // Insert custom evb file location here
            reader.OpenJob(testfile);

            // --- Example tests ---
            // Test if layer number equals the build jobs layer numbers.
            Assert.AreEqual(1025, reader.EVBNumLayers);

            // Test if vector count in layer 42 equals the specified value.
            Assert.AreEqual(24653,(int) reader.EVBVectorCount(42));

            // Check the StartX of the 17th vector in layer 42.
            Assert.AreEqual(72.77269533081055, reader.ReadEVBLayer(42)[16].StartX);
        }
    }
}