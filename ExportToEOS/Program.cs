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
            dir = new DirectoryInfo(@"C:\Users\Domin\Desktop\source\EOS\");
            outputDir = new DirectoryInfo(@"C:\Users\Domin\Desktop\sink\");

            foreach (var file in dir.GetFiles())
            {
                if(file.Extension == ".ovf")
                {
                    string name = Path.GetFileNameWithoutExtension(file.FullName);

                    //Hatch and Contour
                    RUN_LLL(file, name);
                    RUN_SLL(file, name);
                    RUN_LSL(file, name);
                    RUN_SSL(file, name);
                    RUN_LLS(file, name);
                    RUN_SLS(file, name);
                    RUN_LSS(file, name);
                    RUN_SSS(file, name);

                    RUN_PolylineToHatch(file, name, false, false, false);
                    RUN_PolylineToHatch(file, name, true, false, false);
                    RUN_PolylineToHatch(file, name, false, true, false);
                    RUN_PolylineToHatch(file, name, true, true, false);
                    RUN_PolylineToHatch(file, name, false, false, true);
                    RUN_PolylineToHatch(file, name, true, false, true);
                    RUN_PolylineToHatch(file, name, false, true, true);
                    RUN_PolylineToHatch(file, name, true, true, true);
                }
            }
        }
        //Contour and Hatch
        private static void RUN_LLL(FileInfo file, string name)
        {
            CLIWriterAdapter.HatchesStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.PolylineStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.LayerStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;

            var outputFile = new FileInfo(dir.FullName + name + "_LLL.ovf");
            WriteOVF(file, outputFile, true, true);
            WriteCLI(outputFile, outputDir, name + "_LLL");
            outputFile.Delete();
        }
        private static void RUN_SLL(FileInfo file, string name)
        {
            CLIWriterAdapter.HatchesStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;
            CLIWriterAdapter.PolylineStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.LayerStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;

            var outputFile = new FileInfo(dir.FullName + name + "_SLL.ovf");
            WriteOVF(file, outputFile, true, true);
            WriteCLI(outputFile, outputDir, name + "_SLL");
            outputFile.Delete();
        }
        private static void RUN_LSL(FileInfo file, string name)
        {
            CLIWriterAdapter.HatchesStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.PolylineStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;
            CLIWriterAdapter.LayerStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;

            var outputFile = new FileInfo(dir.FullName + name + "_LSL.ovf");
            WriteOVF(file, outputFile, true, true);
            WriteCLI(outputFile, outputDir, name + "_LSL");
            outputFile.Delete();
        }
        private static void RUN_SSL(FileInfo file, string name)
        {
            CLIWriterAdapter.HatchesStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;
            CLIWriterAdapter.PolylineStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;
            CLIWriterAdapter.LayerStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;

            var outputFile = new FileInfo(dir.FullName + name + "_SSL.ovf");
            WriteOVF(file, outputFile, true, true);
            WriteCLI(outputFile, outputDir, name + "_SSL");
            outputFile.Delete();
        }
        private static void RUN_LLS(FileInfo file, string name)
        {
            CLIWriterAdapter.HatchesStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.PolylineStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.LayerStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;

            var outputFile = new FileInfo(dir.FullName + name + "_LLS.ovf");
            WriteOVF(file, outputFile, true, true);
            WriteCLI(outputFile, outputDir, name + "_LLS");
            outputFile.Delete();
        }
        private static void RUN_SLS(FileInfo file, string name)
        {
            CLIWriterAdapter.HatchesStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;
            CLIWriterAdapter.PolylineStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.LayerStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;

            var outputFile = new FileInfo(dir.FullName + name + "_SLS.ovf");
            WriteOVF(file, outputFile, true, true);
            WriteCLI(outputFile, outputDir, name + "_SLS");
            outputFile.Delete();
        }
        private static void RUN_LSS(FileInfo file, string name)
        {
            CLIWriterAdapter.HatchesStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.LONG;
            CLIWriterAdapter.PolylineStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;
            CLIWriterAdapter.LayerStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;

            var outputFile = new FileInfo(dir.FullName + name + "_LSS.ovf");
            WriteOVF(file, outputFile, true, true);
            WriteCLI(outputFile, outputDir, name + "_LSS");
            outputFile.Delete();
        }
        private static void RUN_SSS(FileInfo file, string name)
        {
            CLIWriterAdapter.HatchesStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;
            CLIWriterAdapter.PolylineStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;
            CLIWriterAdapter.LayerStyle = OpenVectorFormat.ILTFileReader.Controller.CliFileAccess.BinaryWriteStyle.SHORT;

            var outputFile = new FileInfo(dir.FullName + name + "_SSS.ovf");
            WriteOVF(file, outputFile, true, true);
            WriteCLI(outputFile, outputDir, name + "_SSS");
            outputFile.Delete();
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
            outputFile.Delete();


        }



        private static void WriteOVF(FileInfo origin, FileInfo target, bool contour, bool hatches)
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

                    foreach (var vectorBlock in slice.VectorBlocks.ToList())
                    {
                        var metaData = vectorBlock.LpbfMetadata;

                        if (metaData.StructureType == StructureType.Part)
                        {

                            if (contour && vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                            {
                                workPlane.VectorBlocks.Add(vectorBlock);
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