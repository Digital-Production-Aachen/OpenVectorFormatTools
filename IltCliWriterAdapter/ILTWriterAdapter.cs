using IltCliWriterAdapter.CLI;
using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.FileReaderWriterFactory;
using OpenVectorFormat.ILTFileReader;
using OpenVectorFormat.ILTFileReader.Controller;
using OpenVectorFormat.OVFReaderWriter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace IltCliWriterAdapter
{
    public class ILTWriterAdapter : FileWriter
    {
        private IFileReaderWriterProgress progress;
        private string filename;
        public override OpenVectorFormat.Job JobShell { get { return _jobShell; } }
        private OpenVectorFormat.Job _jobShell;
        public override FileWriteOperation FileOperationInProgress { get { return _fileOperationInProgress; } }
        private FileWriteOperation _fileOperationInProgress = FileWriteOperation.None;
        private StreamWriter _fs;


        public async override Task AppendVectorBlockAsync(OpenVectorFormat.VectorBlock block)
        {
            //await Task.Run(() => _addVectorBlock(block));
        }

        public async override Task AppendWorkPlaneAsync(OpenVectorFormat.WorkPlane workPlane)
        {
            await Task.Run(() =>
            {
                for (uint i = 0; i < workPlane.Repeats + 1; i++)
                {
                    for (int i_vectorblock = 0; i_vectorblock < workPlane.VectorBlocks.Count; i_vectorblock++)
                    {
                        //_addVectorBlock(workPlane.VectorBlocks[i_vectorblock]);
                    }
                }
            });
        }

        public void Write(Job job, FileInfo targetFile)
        {
            var layerstyle = CliFileAccess.BinaryWriteStyle.LONG;
            var hatchstyle = CliFileAccess.BinaryWriteStyle.SHORT;
            var polystyle = CliFileAccess.BinaryWriteStyle.LONG;


            var geo = new Geometry();
            geo.Layers = new List<ILayer>();
            geo.Layers.Add(new Layer(0));

            //Fill Geometry
            #region FillGeometry
            //var hatches = new List<IHatch>();
            //for (int z = 0; z < 5; z++)
            //{
            //    for (int i = 0; i < 10; i++)
            //    {
            //        hatches.Add(new Hatch(new EuclidKoordinates(0, i), new EuclidKoordinates(1, i)));
            //    }
            //    geo.Layers.Add(new Layer(new Hatches(hatches), z));
            //    hatches = new List<IHatch>();
            //}

            var layer_i = 100;
            var workPlane = job.WorkPlanes[layer_i];


            var hatches = new List<IHatch>();
            var polylines = new List<Polyline>();
            foreach (var vectorBlock in workPlane.VectorBlocks.ToList())
            {
                var metaData = vectorBlock.LpbfMetadata;
                var partKey = vectorBlock.MetaData.PartKey;

                if (metaData.StructureType == OpenVectorFormat.VectorBlock.Types.StructureType.Part)
                {
                    //Contour
                    if (vectorBlock.VectorDataCase == OpenVectorFormat.VectorBlock.VectorDataOneofCase.LineSequence)
                    {
                        var points = vectorBlock.Hatches.Points;
                        var polyline = new Polyline(new EuclidKoordinates(points[i], points[i + 1]), 0);
                    }
                    //Hatches
                    else if (vectorBlock.VectorDataCase == OpenVectorFormat.VectorBlock.VectorDataOneofCase.Hatches)
                    {
                        var points = vectorBlock.Hatches.Points;

                        for (int i = 0; i < points.Count; i += 4)
                        {
                            hatches.Add(new Hatch(new EuclidKoordinates(points[i], points[i + 1]), 
                                new EuclidKoordinates(points[i + 2], points[i + 3])));
                        }                  }
                    else
                    {
                        throw new Exception("New Vector Data Case " + vectorBlock.VectorDataCase.ToString());
                    }
                }
                else
                {
                    throw new Exception("new StructureType to handle");
                }


            }

            var layer = new Layer(layer_i);
            layer.VectorBlocks.ToList().AddRange(polylines);
            layer.VectorBlocks.ToList().Add(new Hatches(hatches));
            geo.Layers.Add(layer);
            #endregion

            var cli = new CliFile();
            cli.Geometry = geo;
            cli.Parts = new List<IPart>();
            cli.Parts.Add(new CLI.Part());
            cli.CreateHeaderAllHatch(0.01f);


            //Write
            var cliwriter = new CliFileAccess();
            cliwriter.WriteFile(targetFile.FullName, cli, layerstyle, hatchstyle, polystyle);

        }

        public override void Dispose()
        {
            if (_fileOperationInProgress != FileWriteOperation.None)
            {
                _fs.Close();
            }
        }

        public override async Task SimpleJobWriteAsync(OpenVectorFormat.Job job, string filename, IFileReaderWriterProgress progress)
        {
            CheckConsistence(job.NumWorkPlanes, job.WorkPlanes.Count);
            for (int i = 0; i < job.NumWorkPlanes; i++)
            {
                CheckConsistence(job.WorkPlanes[i].NumBlocks, job.WorkPlanes[i].VectorBlocks.Count);
            }
            _jobShell = job;
            _fileOperationInProgress = FileWriteOperation.CompleteWrite;
            _fs = new StreamWriter(filename);
            this.filename = filename;
            this.progress = progress;
            foreach (OpenVectorFormat.WorkPlane wp in job.WorkPlanes)
            {
                await AppendWorkPlaneAsync(wp);
            }
            _fs.Close();
            _fileOperationInProgress = FileWriteOperation.None;
        }

        public override void StartWritePartial(OpenVectorFormat.Job jobShell, string filename, IFileReaderWriterProgress progress)
        {
            _jobShell = jobShell;
            _fileOperationInProgress = FileWriteOperation.PartialWrite;
            _fs = new StreamWriter(filename);
            this.filename = filename;
            this.progress = progress;
        }

        private void CheckConsistence(int number1, int number2)
        {
            if (number1 != number2)
            {
                Dispose();
                throw new IOException("inconsistence in file detected");
            }
        }
    }
}
