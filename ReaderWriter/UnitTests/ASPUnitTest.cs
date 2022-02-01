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



using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat.ASPFileReaderWriter;
using OpenVectorFormat.AbstractReaderWriter;
using System.Collections.Generic;
using System.IO;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    [TestClass]
    public class TestReadWrite
    {

        public static DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles"));

        [DynamicData("ASPFiles")]
        [TestMethod]
        public async System.Threading.Tasks.Task ASPReadTestAsync(FileInfo fileName)
        {
            // Job job;
            using (ASPFileReader simpleReader = new ASPFileReader())
            {
                IFileReaderWriterProgress progRead = new FileReaderWriterProgressDummy();

                /*
                Console.WriteLine(fileName.FullName);
                Console.WriteLine("start loading");
                Console.WriteLine(DateTime.Now.ToString());
                */

                await simpleReader.OpenJobAsync(fileName.FullName, progRead);

                /*
                job = simpleReader.CompleteJob;
                Console.WriteLine("finished loading");
                Console.WriteLine(DateTime.Now.ToString());
                Console.WriteLine("num marking params");
                Console.WriteLine(simpleReader.JobShell.MarkingParamsMap.Count.ToString());
                Console.WriteLine("num workplanes");
                Console.WriteLine(simpleReader.JobShell.NumWorkPlanes.ToString());
                Console.WriteLine("num blocks workplane 0");
                Console.WriteLine(simpleReader.CompleteJob.WorkPlanes[0].VectorBlocks.Count.ToString());
                int count = 0;
                foreach (VectorBlock vb in simpleReader.CompleteJob.WorkPlanes[0].VectorBlocks)
                {
                    Console.WriteLine(count++);
                    Console.WriteLine(vb.VectorDataCase.ToString());
                }
                */
            }

            /*
            ASPFileWriter writer = new ASPFileWriter();
            Console.WriteLine("start writing");
            Console.WriteLine(DateTime.Now.ToString());
            await writer.SimpleJobWriteAsync(job, @"C:\Users\ameyer\Documents\Data\tmp\bigtest.asp", new FileHandlerProgress());
            writer.Dispose();
            Console.WriteLine("finish writing");
            Console.WriteLine(DateTime.Now.ToString());
            */

        }

        public static List<object[]> ASPFiles
        {
            get
            {
                FileInfo[] testFiles = dir.GetFiles("*.asp"); //getting all .asp files
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
