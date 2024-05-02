using FluentAssertions;
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
