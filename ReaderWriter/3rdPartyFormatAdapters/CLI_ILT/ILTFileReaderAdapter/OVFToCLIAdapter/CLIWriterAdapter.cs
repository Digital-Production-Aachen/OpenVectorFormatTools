using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.ILTFileReader.Controller;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static OpenVectorFormat.ILTFileReader.Controller.CliFileAccess;

namespace ILTFileReaderAdapter.OVFToCLIAdapter
{
    public class CLIWriterAdapter : FileWriter
    {
        public override FileWriteOperation FileOperationInProgress => _fileOperationInProgress;

        private FileWriteOperation _fileOperationInProgress = FileWriteOperation.None;
        public BinaryWriteStyle HatchesStyle { get; set; } = BinaryWriteStyle.SHORT;
        public BinaryWriteStyle PolylineStyle { get; set; } = BinaryWriteStyle.LONG;
        public BinaryWriteStyle LayerStyle { get; set; } = BinaryWriteStyle.LONG;

        private static List<string> fileFormats = new List<string>() { ".cli" };

        /// <summary>
        /// List of file format extensions supported by this file reader.
        /// </summary>
        public static new List<string> SupportedFileFormats => fileFormats;

        public override Job JobShell => throw new NotImplementedException();

        public override Task AppendVectorBlockAsync(VectorBlock block)
        {
            throw new NotImplementedException();
        }

        public override Task AppendWorkPlaneAsync(WorkPlane workPlane)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override Task SimpleJobWriteAsync(Job job, string filename, IFileReaderWriterProgress progress)
        {
            var adapter = new CliFileAccess();
            adapter.WriteFile(filename, new OVFCliJob(job) , LayerStyle, HatchesStyle, PolylineStyle);
            return Task.CompletedTask;
        }

        public override void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress)
        {
            throw new NotImplementedException();
        }
    }
}
