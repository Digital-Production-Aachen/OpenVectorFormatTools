using ILTFileReader.OVFToCLIAdapter;
using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.ILTFileReader;
using OpenVectorFormat.ILTFileReader.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static OpenVectorFormat.ILTFileReader.Controller.CliFileAccess;

namespace ILTFileReaderAdapter.OVFToCLIAdapter
{
    public class CLIWriterAdapter : FileWriter
    {
        public override FileWriteOperation FileOperationInProgress => _fileOperationInProgress;

        private FileWriteOperation _fileOperationInProgress = FileWriteOperation.None;
        public static BinaryWriteStyle HatchesStyle { get; set; } = BinaryWriteStyle.SHORT; //EOS => Short
        public static BinaryWriteStyle PolylineStyle { get; set; } = BinaryWriteStyle.LONG;
        public static BinaryWriteStyle LayerStyle { get; set; } = BinaryWriteStyle.LONG;

        public float units { get; set; } = 1;

        private Job jobShell;
        private IFileReaderWriterProgress progress;

        private static List<string> fileFormats = new List<string>() { ".cli" };
        private BinaryWriter binaryWriter;
        private CliFileAccess cliAdapter;

        /// <summary>
        /// List of file format extensions supported by this file reader.
        /// </summary>
        public static new List<string> SupportedFileFormats => fileFormats;

        public override Job JobShell => jobShell;

        public override Task AppendVectorBlockAsync(VectorBlock block)
        {
            IVectorBlock blockAdapter;
            switch (block.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.Hatches:
                    blockAdapter = new OVFCliHatches(block);
                    break;
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    blockAdapter = new OVFCliPolyline(block);
                    break;
                default:
                    throw new ArgumentException($"vector block type {block.VectorDataCase} is not supported by cli, only Hatches and LineSequence");
            }
            cliAdapter.AppendVectorBlock(binaryWriter, blockAdapter);
            return Task.CompletedTask;
        }

        public override Task AppendWorkPlaneAsync(WorkPlane workPlane)
        {
            cliAdapter.AppendLayer(binaryWriter, new OVFCliLayer(workPlane));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            binaryWriter?.Dispose();
        }

        public override Task SimpleJobWriteAsync(Job job, string filename, IFileReaderWriterProgress progress)
        {
            var adapter = new CliFileAccess() { units = units };
            adapter.WriteFile(filename, new OVFCliJob(job) { Units = units } , LayerStyle, HatchesStyle, PolylineStyle, units != 1);
            return Task.CompletedTask;
        }

        public override void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress)
        {
            this.jobShell = jobShell;
            this.progress = progress;
            using (var sW = new StreamWriter(filename, false))
            {
                WriteHeader(sW, new OVFCliJob(jobShell) { Units = units });
            }
            binaryWriter = new BinaryWriter(new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.None));
            cliAdapter = new CliFileAccess() { HatchesStyle = HatchesStyle, PolylineStyle = PolylineStyle, LayerStyle = LayerStyle, units = units };
        }
    }
}
