using ILTFileReaderAdapter.OVFToCLIAdapter;
using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.FileReaderWriterFactory;
using OpenVectorFormat.OVFReaderWriter;
using System;
using System.Xml.Linq;
using static OpenVectorFormat.VectorBlock.Types;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static DirectoryInfo dir;
        static DirectoryInfo outputDir;

        static void Main(string[] args)
        {
            dir = new DirectoryInfo(@"C:\Users\Domin\Desktop\source\ACAM24\OVFs\");
            outputDir = new DirectoryInfo(@"C:\Users\Domin\Desktop\sink\");

            foreach (var file in dir.GetFiles())
            {
                if(file.Extension == ".ovf")
                {
                    string name = Path.GetFileNameWithoutExtension(file.FullName);

                    RUN_PolylineToHatch(file, name, true, false, false);
                }
            }
        }
        private static void RUN_PolylineToHatch(FileInfo file, string name, bool HatchesStyle, bool PolylineStyle, bool LayerStyle)
        {
            CLIWriterAdapter.HatchesStyle = HatchesStyle ? OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT : OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.PolylineStyle = PolylineStyle ? OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT : OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.LayerStyle = LayerStyle ? OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT : OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;


            var endOfName = "_poly2Hatch";
            endOfName += HatchesStyle ? "S" : "L";
            endOfName += PolylineStyle ? "S" : "L";
            endOfName += LayerStyle ? "S" : "L";

            var outputFile = new FileInfo(dir.FullName + name + endOfName + ".ovf");

            var reader = new OVFFileReader();
            var progress = new FileReaderWriterProgress();
            reader.OpenJobAsync(file.FullName, progress).GetAwaiter().GetResult();
            var job = reader.CacheJobToMemoryAsync().GetAwaiter().GetResult();

            //Writer
            using (var writer = new OVFFileWriter())
            {
                writer.StartWritePartial(job, outputFile.FullName, progress);

                for (int i = 0; i < job.NumWorkPlanes; i++)
                {
                    var slice = reader.GetWorkPlaneAsync(i).GetAwaiter().GetResult();
                    var workPlane = new WorkPlane();
                    workPlane.MetaData = slice.MetaData;

                    foreach (var vectorBlock in slice.VectorBlocks.ToList())
                    {
                        var metaData = vectorBlock.LpbfMetadata;

                        if (metaData.StructureType == StructureType.Part)
                        {

                            if (vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                            {
                                var vb = new VectorBlock();
                                vb.Hatches = new Hatches();
                                for (int j = 0; j < vectorBlock.LineSequence.Points.ToList().Count-3; j+=2)
                                {
                                    var x_1 = vectorBlock.LineSequence.Points.ToList()[j];
                                    var y_1 = vectorBlock.LineSequence.Points.ToList()[j + 1];

                                    vb.Hatches.Points.Add(x_1);
                                    vb.Hatches.Points.Add(y_1);

                                    var x_2 = vectorBlock.LineSequence.Points.ToList()[j + 2];
                                    var y_2 = vectorBlock.LineSequence.Points.ToList()[j + 3];

                                    vb.Hatches.Points.Add(x_2);
                                    vb.Hatches.Points.Add(y_2);
                                }
                                //var p3 = vectorBlock.LineSequence.Points.ToList()[vectorBlock.LineSequence.Points.ToList().Count-2];
                                //var p4 = vectorBlock.LineSequence.Points.ToList()[vectorBlock.LineSequence.Points.ToList().Count - 1];

                                //vb.Hatches.Points.Add(p3);
                                //vb.Hatches.Points.Add(p4);

                                vb.MetaData = vectorBlock.MetaData;
                                workPlane.VectorBlocks.Add(vb);
                            }
                            else if(vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches)
                            {
                                workPlane.VectorBlocks.Add(vectorBlock);
                            }

                        }
                        else
                        {
                            throw new Exception();
                        }
                    }

                    workPlane.NumBlocks = workPlane.VectorBlocks.Count;
                    workPlane.ZPosInMm = slice.ZPosInMm;
                    writer.AppendWorkPlaneAsync(workPlane).GetAwaiter().GetResult();
                }
            }



            WriteCLI(outputFile, outputDir, name + endOfName);
            SplitOVF(file, outputFile);
            outputFile.Delete();


        }
        private static void SplitOVF(FileInfo origin, FileInfo outputFile)

        {

            var name = Path.GetFileNameWithoutExtension(outputFile.FullName);

            var contour1 = new FileInfo(outputFile.DirectoryName + @"/" + name + "_contour1.ovf");
            //var contour2 = new FileInfo(outputFile.DirectoryName + @"/" + name + "_contour2.ovf");
            //var tracingContours = new FileInfo(outputFile.DirectoryName + @"/" + name + "_tracngContrours.ovf");

            var hatch = new FileInfo(outputFile.DirectoryName + @"/" + name + "_hatch.ovf");

            //WriteOVF(origin, contour2, true, false, false);
            WriteOVF(origin, contour1, true, true, false);
            //WriteOVF(origin, tracingContours, false, true, false, true);

            WriteOVF(origin, hatch, false, false, true);



            WriteCLI(contour1, outputDir, name + "_contour1");
            //WriteCLI(contour2, outputDir, name + "_contour2");
            //WriteCLI(tracingContours, outputDir, name + "_tracngContrours");

            WriteCLI(hatch, outputDir, name + "_hatch");

            contour1.Delete();
            //contour2.Delete();

            hatch.Delete();

        }


        private static void WriteOVF(FileInfo origin, FileInfo target, bool contour1, bool contour2, bool hatches, bool continiusContours = false)
        {
            var reader = new OVFFileReader();
            var progress = new FileReaderWriterProgress();
            reader.OpenJobAsync(origin.FullName, progress).GetAwaiter().GetResult();
            var job = reader.CacheJobToMemoryAsync().GetAwaiter().GetResult();

            //Writer
            using (var writer = new OVFFileWriter())
            {
                writer.StartWritePartial(job, target.FullName, progress);

                for (int i = 0; i < job.NumWorkPlanes; i++)
                {
                    var slice = reader.GetWorkPlaneAsync(i).GetAwaiter().GetResult();
                    var workPlane = new WorkPlane();
                    workPlane.MetaData = slice.MetaData;
                    int contourCounter = 0;

                    foreach (var vectorBlock in slice.VectorBlocks.ToList())
                    {
                        var metaData = vectorBlock.LpbfMetadata;

                        if (metaData.StructureType == StructureType.Part)
                        {

                            if ((contour1 || contour2) && vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                            {
                                if(contourCounter %2 == 0 && contour1) workPlane.VectorBlocks.Add(vectorBlock);
                                if(contourCounter % 2 == 1 && contour2) workPlane.VectorBlocks.Add(vectorBlock);
                                if(contour1 && contour2) contourCounter++;
                            }
                            if (hatches && vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches)
                            {
                                workPlane.VectorBlocks.Add(vectorBlock);
                            }

                        }
                        else
                        {
                            throw new Exception();
                        }
                    }

                    workPlane.NumBlocks = workPlane.VectorBlocks.Count;
                    workPlane.ZPosInMm = slice.ZPosInMm;
                    writer.AppendWorkPlaneAsync(workPlane).GetAwaiter().GetResult();
                }
            }


            if (continiusContours)
            {
                //Writer
                using (var writer = new OVFFileWriter())
                {
                    writer.StartWritePartial(job, target.FullName, progress);

                    for (int i = 0; i < job.NumWorkPlanes; i++)
                    {
                        var slice = reader.GetWorkPlaneAsync(i).GetAwaiter().GetResult();
                        var workPlane = new WorkPlane();
                        workPlane.MetaData = slice.MetaData;
                        int contourCounter = 0;

                        foreach (var vectorBlock in slice.VectorBlocks.ToList())
                        {
                            var metaData = vectorBlock.LpbfMetadata;

                            if (metaData.StructureType == StructureType.Part)
                            {

                                if ((contour1 || contour2) && vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                                {
                                    workPlane.VectorBlocks.Add(vectorBlock);
                                }

                            }
                            else
                            {
                                throw new Exception();
                            }
                        }

                        workPlane.NumBlocks = workPlane.VectorBlocks.Count;
                        workPlane.ZPosInMm = slice.ZPosInMm;
                        writer.AppendWorkPlaneAsync(workPlane).GetAwaiter().GetResult();
                    }
                }
            }
            else
            {
                //Writer
                using (var writer = new OVFFileWriter())
                {
                    writer.StartWritePartial(job, target.FullName, progress);

                    for (int i = 0; i < job.NumWorkPlanes; i++)
                    {
                        var slice = reader.GetWorkPlaneAsync(i).GetAwaiter().GetResult();
                        var workPlane = new WorkPlane();
                        workPlane.MetaData = slice.MetaData;
                        int contourCounter = 0;

                        foreach (var vectorBlock in slice.VectorBlocks.ToList())
                        {
                            var metaData = vectorBlock.LpbfMetadata;

                            if (metaData.StructureType == StructureType.Part)
                            {

                                if ((contour1 || contour2) && vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                                {
                                    if (contourCounter % 2 == 0 && contour1) workPlane.VectorBlocks.Add(vectorBlock);
                                    if (contourCounter % 2 == 1 && contour2) workPlane.VectorBlocks.Add(vectorBlock);
                                    contourCounter++;
                                }
                                if (hatches && vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches)
                                {
                                    workPlane.VectorBlocks.Add(vectorBlock);
                                }

                            }
                            else
                            {
                                throw new Exception();
                            }
                        }

                        workPlane.NumBlocks = workPlane.VectorBlocks.Count;
                        workPlane.ZPosInMm = slice.ZPosInMm;
                        writer.AppendWorkPlaneAsync(workPlane).GetAwaiter().GetResult();
                    }
                }
            }
        }
        private static void WriteCLI(FileInfo ovfFile, DirectoryInfo outPutDir, string name)
        {
            using (var reader = new OVFFileReader())
            {
                var progress = new FileReaderWriterProgress();
                reader.OpenJobAsync(ovfFile.FullName, progress).GetAwaiter().GetResult();
                var job = reader.CacheJobToMemoryAsync().GetAwaiter().GetResult();

                CLIWriterAdapter cliWriter = new CLIWriterAdapter() { units = 1 / 200f };
                cliWriter.SimpleJobWriteAsync(job, new FileInfo(outPutDir.FullName + name + ".cli").FullName, progress).Wait();
                reader.Dispose();
            }
        }
    }
}