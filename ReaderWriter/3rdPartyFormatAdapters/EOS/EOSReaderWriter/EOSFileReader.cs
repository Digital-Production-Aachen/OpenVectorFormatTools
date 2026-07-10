using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace EOSReaderWriter
{
    public class EOSFileReader : FileReader
    {
        private WorkPlane _workPlane;
        private VectorBlock _currentVectorBlock;
        private CacheState _cacheState = CacheState.NotCached;
        private string _filename;

        private readonly BinaryReader _reader; // for EVB file reading

        /// <inheritdoc/>
        public new static List<string> SupportedFileFormats { get; } = new List<string>() { ".openjz", ".evb" };

        /// <inheritdoc/>
        public override CacheState CacheState => _cacheState;

        public Job CompleteJob { get; private set; }

        public override Job JobShell
        {
            get
            {
                if (_cacheState == CacheState.CompleteJobCached)
                {
                    Job jobShell = new Job();
                    ProtoUtils.CopyWithExclude(CompleteJob, jobShell, new List<int> { Job.WorkPlanesFieldNumber });
                    return jobShell;
                }
                else
                {
                    throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
                }
            }
        }

        public override Job CacheJobToMemory()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override VectorBlock GetVectorBlock(int i_workPlane, int i_vectorblock)
        {
            throw new NotImplementedException();
        }

        public override WorkPlane GetWorkPlane(int i_workPlane)
        {
            throw new NotImplementedException();
        }

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            throw new NotImplementedException();
        }

        public override void OpenJob(string filename, IFileReaderWriterProgress progress = null)
        {
            if (!SupportedFileFormats.Contains(Path.GetExtension(filename)))
            {
                throw new Exception(Path.GetExtension(filename) + " is not supported by ASPFileReader. Supported formats are: " + string.Join(";", SupportedFileFormats));
            }
            CompleteJob = new Job();
            DateTime fileCreationTime = File.GetCreationTime(filename);
            CompleteJob.JobMetaData = new Job.Types.JobMetaData
            {
                JobCreationTime = 0,
                JobName = Path.GetFileNameWithoutExtension(filename)
            };

            _filename = filename;
        }

        public override void UnloadJobFromMemory()
        {
            throw new NotImplementedException();
        }
    }
}
