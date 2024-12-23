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

        private Job jobShell;
        private IFileReaderWriterProgress progress;

        private static List<string> fileFormats = new List<string>() { ".cli" };
        private BinaryWriter binaryWriter;
        private StreamWriter steamWriter;
        private CliFileAccess cliAdapter = new CliFileAccess();


        /// <summary>
        /// List of file format extensions supported by this file reader.
        /// </summary>
        public static new List<string> SupportedFileFormats => fileFormats;

        public override Job JobShell => jobShell;

        public override void Dispose()
        {
            binaryWriter?.Dispose();
            steamWriter?.Dispose();
        }

        public override Task SimpleJobWriteAsync(Job job, string filename, IFileReaderWriterProgress progress)
        {

            var map = new Dictionary<int, Tuple<float, float>>();

            if (CliPlus)
            {
                foreach (var part in job.MarkingParamsMap)
                {
                    map.Add(part.Key, Tuple.Create(part.Value.LaserPowerInW, part.Value.LaserSpeedInMmPerS));
                }
            }

            var adapter = new CliFileAccess(map);
            adapter.WriteFile(filename, new OVFCliJob(job) { Units = CliFormatSettings.Instance.Units });

            return Task.CompletedTask;
        }

        public override void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress)
        {
            this.jobShell = jobShell;
            this.progress = progress;

            var map = new Dictionary<int, Tuple<float, float>>();

            if (CliPlus)
            {
                foreach (var part in jobShell.MarkingParamsMap)
                {
                    map.Add(part.Key, Tuple.Create(part.Value.LaserPowerInW, part.Value.LaserSpeedInMmPerS));
                }
            }

            using (var sW = new StreamWriter(filename, false))
            {
                WriteHeader(sW, new OVFCliJob(jobShell) { Units = CliFormatSettings.Instance.Units });
            }
            
            cliAdapter = new CliFileAccess(map) ;

            if (CliFormatSettings.Instance.dataFormatType == DataFormatType.binary)
                binaryWriter = new BinaryWriter(new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.None));
            else
                steamWriter = new StreamWriter(new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.ASCII);
        }

        public override void SimpleJobWrite(Job job, string filename, IFileReaderWriterProgress progress = null)
        {
            var map = new Dictionary<int, Tuple<float, float>>();

            if (CliPlus)
            {
                foreach (var part in job.MarkingParamsMap)
                {
                    map.Add(part.Key, Tuple.Create(part.Value.LaserPowerInW, part.Value.LaserSpeedInMmPerS));
                }
            }

            var adapter = new CliFileAccess(map);
            adapter.WriteFile(filename, new OVFCliJob(job) { Units = CliFormatSettings.Instance.Units });
        }

        public override void AppendWorkPlane(WorkPlane workPlane)
        {
            if(CliFormatSettings.Instance.dataFormatType == DataFormatType.binary)
                cliAdapter.AppendLayer(binaryWriter, new OVFCliLayer(workPlane));
            else
                cliAdapter.AppendLayer(steamWriter, new OVFCliLayer(workPlane));
        }

        public override void AppendVectorBlock(VectorBlock block)
        {
            throw new NotImplementedException();
        }

        public void WriteStartGeometry()
        {
            if(CliFormatSettings.Instance.dataFormatType == DataFormatType.ASCII)
            {
                steamWriter.WriteLine("");
                steamWriter.WriteLine("$$GEOMETRYSTART");
            }
        }

        public void WriteEndGeometry()
        {
            if (CliFormatSettings.Instance.dataFormatType == DataFormatType.ASCII)
            {
                steamWriter.WriteLine("");
                steamWriter.WriteLine("$$GEOMETRYEND");
            }
                
        }
    }
}
