/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2021 Digital-Production-Aachen

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
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat.ILTFileReader.Model;
using OpenVectorFormat.ILTFileReader.Controller;
using System.Diagnostics;
using System.Collections.Generic;
using OpenVectorFormat.ILTFileReader;

namespace UnitTestILTFileReader
{
    [TestClass]
    public class FileAccessTest
    {
        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Resources"));

        [DynamicData("CliFiles")]
        [DataTestMethod]
        public void TestCliFiles(FileInfo fileName)
        {
            CliFileAccess cliFile = new CliFileAccess();
            cliFile.OpenFile(fileName.FullName);
            TestCLIFile(cliFile);
        }

        [DynamicData("CliFiles")]
        [DataTestMethod]
        public void TestWriteCliFiles(FileInfo fileName)
        {
            CliFileAccess cliFile = new CliFileAccess();
            cliFile.OpenFile(fileName.FullName);
            var subDir = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(fileName.FullName), "generatedFiles"));
            var oldFileName = Path.Combine(subDir.FullName, Path.GetFileNameWithoutExtension(fileName.Name));

            var copyFile = new FileInfo(oldFileName + "_copy_lll" + fileName.Extension);
            cliFile.WriteFile(copyFile.FullName, cliFile, CliFileAccess.BinaryWriteStyle.LONG, CliFileAccess.BinaryWriteStyle.LONG, CliFileAccess.BinaryWriteStyle.LONG);
            TestCliFiles(copyFile);

            copyFile = new FileInfo(oldFileName + "_copy_sll" + fileName.Extension);
            cliFile.WriteFile(copyFile.FullName, cliFile, CliFileAccess.BinaryWriteStyle.SHORT, CliFileAccess.BinaryWriteStyle.LONG, CliFileAccess.BinaryWriteStyle.LONG);
            TestCliFiles(copyFile);

            copyFile = new FileInfo(oldFileName + "_copy_lsl" + fileName.Extension);

            cliFile.WriteFile(copyFile.FullName, cliFile, CliFileAccess.BinaryWriteStyle.LONG, CliFileAccess.BinaryWriteStyle.SHORT, CliFileAccess.BinaryWriteStyle.LONG);
            TestCliFiles(copyFile);

            copyFile = new FileInfo(oldFileName + "_copy_lls" + fileName.Extension);
            cliFile.WriteFile(copyFile.FullName, cliFile, CliFileAccess.BinaryWriteStyle.LONG, CliFileAccess.BinaryWriteStyle.LONG, CliFileAccess.BinaryWriteStyle.SHORT);
            TestCliFiles(copyFile);

            copyFile = new FileInfo(oldFileName + "_copy_ssl" + fileName.Extension);
            cliFile.WriteFile(copyFile.FullName, cliFile, CliFileAccess.BinaryWriteStyle.SHORT, CliFileAccess.BinaryWriteStyle.SHORT, CliFileAccess.BinaryWriteStyle.LONG);
            TestCliFiles(copyFile);

            copyFile = new FileInfo(oldFileName + "_copy_lss" + fileName.Extension);
            cliFile.WriteFile(copyFile.FullName, cliFile, CliFileAccess.BinaryWriteStyle.LONG, CliFileAccess.BinaryWriteStyle.SHORT, CliFileAccess.BinaryWriteStyle.SHORT);
            TestCliFiles(copyFile);

            copyFile = new FileInfo(oldFileName + "_copy_sss" + fileName.Extension);
            cliFile.WriteFile(copyFile.FullName, cliFile, CliFileAccess.BinaryWriteStyle.SHORT, CliFileAccess.BinaryWriteStyle.SHORT, CliFileAccess.BinaryWriteStyle.SHORT);
            TestCliFiles(copyFile);
        }

        [DynamicData("IltFiles")]
        [DataTestMethod]
        public void TestIltFiles(FileInfo fileName)
        {
            IltFileAccess iltFile = new IltFileAccess();
            iltFile.OpenFile(fileName.FullName);
            Assert.AreNotEqual(0, iltFile.ModelSections.Count);
            /*treat every ModelSection in the .ilt file as an .cli file
              and use the tesignated test for it*/
            for (int i = 0; i < iltFile.ModelSections.Count; i++)
            {
                TestCLIFile(iltFile.ModelSections[i]);
            }
        }

        private void TestCLIFile(ICLIFile cliFile)
        {
            //Fail in case that there are no Parts...
            Assert.AreNotEqual(0, cliFile.Parts.Count);
            //...or Layers...
            Assert.AreNotEqual(0, cliFile.Header.NumLayers);
            //...or the information in the header is inconsistent
            Assert.AreEqual(cliFile.Header.NumLayers, cliFile.Geometry.Layers.Count);
            for (int i = 0; i < cliFile.Geometry.Layers.Count; i++)
            {
                ILayer layer = cliFile.Geometry.Layers[i];
                foreach (IVectorBlock vBlock in layer.VectorBlocks)
                {
                    //check if Vectorblocks are not empty
                    Assert.AreNotEqual(0, vBlock.Coordinates);
                    //Test if start and end coordinates match
                    if (vBlock is IPolyline)
                    {
                        IPolyline polyline = (IPolyline)vBlock;
                        if ((polyline.Dir == Direction.counterClockwise) || (polyline.Dir == Direction.clockwise))
                        {
                            Assert.AreEqual(polyline.Points[0], polyline.Points[polyline.N - 1]);
                        }
                    }
                }
            }
        }
        public static List<object[]> IltFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.ilt"); //getting all .ilt files
                List<object[]> files = new List<object[]>(testFiles.Length);
                for (int i = 0; i < testFiles.Length; i++)
                {
                    files.Add(new object[] { testFiles[i] });
                }
                return files;
            }
        }

        public static List<object[]> CliFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.cli"); //getting all .cli files
                List<object[]> files = new List<object[]>(testFiles.Length);
                for (int i = 0; i < testFiles.Length; i++)
                {
                    files.Add(new object[] { testFiles[i] });
                }
                return files;
            }
        }
    }
}
