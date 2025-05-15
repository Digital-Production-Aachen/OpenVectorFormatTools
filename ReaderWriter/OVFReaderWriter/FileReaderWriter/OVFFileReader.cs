/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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



using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;

namespace OpenVectorFormat.OVFReaderWriter
{
    internal enum FileReadOperation
    {
        None = 1,
        Undefined = 2,
        CompleteRead = 3,
        Streaming = 4
    }

    public class OVFFileReader : FileReader
    {
        private IFileReaderWriterProgress progress;
        /// <summary>
        /// stream with access to the full MemoryMappedFile
        /// </summary>
        private Stream _globalstream;
        /// <summary>
        /// shared file stream the memory mapped file is created from
        /// </summary>
        private FileStream _fileStream;
        private long _streamlength;
        private string _filename;
        private MemoryMappedFile _mmf;

        // Look-up-table for workPlane positions in filestream.
        private JobLUT _jobLUT;

        // List of Look-up-tables for vectorblock positions in the filestream for each workplane.
        private WorkPlaneLUT[] _workPlaneLUTs;

        /// <inheritdoc/>
        public override Job JobShell => _jobShell.Clone();
        private Job _jobShell;
        private Job _job;
        private int _numberOfLayers;
        private int _numberOfCachedLayers = 0;


        private CacheState _cacheState = CacheState.NotCached;

        private FileReadOperation _fileOperationInProgress = FileReadOperation.None;

        /// <inheritdoc/>
        public new static List<string> SupportedFileFormats { get; } = new List<string>() { ".ovf" };

        /// <inheritdoc/>
        public override CacheState CacheState => _cacheState;

        /// <inheritdoc/>
        public override void OpenJob(string filename, IFileReaderWriterProgress progress = null)
        {
            if (_fileOperationInProgress != FileReadOperation.None)
            {
                throw new InvalidOperationException("Another FileLoadingOperation is currently running. Please wait until it is finished.");
            }
            _fileOperationInProgress = FileReadOperation.Undefined;

            this.progress = progress;

            _fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            _streamlength = _fileStream.Length;
            if (_fileStream.Length < 12)
            {
                _fileStream.Dispose();
                throw new IOException("binary file is empty!");
            }
            else
            {
                _mmf = MemoryMappedFile.CreateFromFile(_fileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);

                _globalstream = _mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);

                byte[] magicNumberBuffer = new byte[Contract.magicNumber.Length];
                _globalstream.Read(magicNumberBuffer, 0, magicNumberBuffer.Length);
                byte[] LUTIndexBuffer = new byte[sizeof(Int64)];
                _globalstream.Read(LUTIndexBuffer, 0, LUTIndexBuffer.Length);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(LUTIndexBuffer);
                }
                long jobLUTindex = BitConverter.ToInt64(LUTIndexBuffer, 0);

                if (!magicNumberBuffer.SequenceEqual(Contract.magicNumber) || jobLUTindex >= _globalstream.Length || jobLUTindex < 0 || jobLUTindex == Contract.defaultLUTIndex)
                {
                    _globalstream.Close();
                    throw new IOException("binary file is not an OVF file or corrupted!");
                }
                else
                {
                    if (jobLUTindex <= AutomatedCachingThresholdBytes)
                    {
                        _fileOperationInProgress = FileReadOperation.CompleteRead;
                    }
                    else
                    {
                        _fileOperationInProgress = FileReadOperation.Streaming;
                    }

                    var task = Task.Run(() =>
                    {
                        _readJobShell(jobLUTindex);
                        if (this.progress != null) this.progress.IsFinished = false;
                        _workPlaneLUTs = new WorkPlaneLUT[_jobShell.NumWorkPlanes];
                        for (int i_plane = 0; i_plane < _jobShell.NumWorkPlanes; i_plane++)
                        {
                            _readWorkPlaneLUT(i_plane);
                        }
                    });

                    task.GetAwaiter().GetResult();

                    if (_fileOperationInProgress == FileReadOperation.CompleteRead)
                    {
                        CacheJobToMemory();
                        _cacheState = CacheState.CompleteJobCached;
                    }
                    else
                    {
                        _cacheState = CacheState.JobShellCached;
                    }
                }
            }

            void _readWorkPlaneLUT(int i_workPlane)
            {
                long wpLUTIndexIndex = _jobLUT.WorkPlanePositions[i_workPlane];
                _globalstream.Position = wpLUTIndexIndex;

                byte[] LUTIndexBuffer = new byte[sizeof(Int64)];
                _globalstream.Read(LUTIndexBuffer, 0, LUTIndexBuffer.Length);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(LUTIndexBuffer);
                }
                long wpLUTindex = BitConverter.ToInt64(LUTIndexBuffer, 0);
                _globalstream.Position = wpLUTindex;
                WorkPlaneLUT wpLUT = WorkPlaneLUT.Parser.ParseDelimitedFrom(_globalstream);

                foreach (long pos in wpLUT.VectorBlocksPositions)
                {
                    if (pos > _globalstream.Length)
                    {
                        Dispose();
                        throw new IOException("invalid vectorblock position detected in file");
                    }
                }

                _workPlaneLUTs[i_workPlane] = wpLUT;
            }

            void _readJobShell(Int64 LUTindex)
            {
                // Read LUT
                _globalstream.Position = LUTindex;
                _jobLUT = JobLUT.Parser.ParseDelimitedFrom(_globalstream);
                _globalstream.Position = _jobLUT.JobShellPosition;
                _jobShell = Job.Parser.ParseDelimitedFrom(_globalstream);
                CheckConsistence(_jobShell.NumWorkPlanes, _jobLUT.WorkPlanePositions.Count);
                _numberOfLayers = _jobShell.NumWorkPlanes;
                foreach (long pos in _jobLUT.WorkPlanePositions)
                {
                    if (pos > _globalstream.Length)
                    {
                        Dispose();
                        throw new IOException("invalid workPlane position detected in file");
                    }
                }
            }

            _filename = filename;
        }

        /// <inheritdoc/>
        public override Job CacheJobToMemory()
        {
            if (_cacheState == CacheState.CompleteJobCached && _job != null)
            {
                return _job;
            }
            else
            {
                _globalstream.Dispose();
                if (this.progress != null) progress.IsCancelled = false;
                if (this.progress != null) progress.IsFinished = false;

                _job = _jobShell.Clone();
                _numberOfLayers = _job.NumWorkPlanes;
                _job.AddAllWorkPlanesParallel(GetWorkPlane);

                _cacheState = CacheState.CompleteJobCached;
                return _job;
            }
        }

        /// <inheritdoc/>
        public override WorkPlane GetWorkPlane(int i_workPlane)
        {
            if (_jobShell.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane " + i_workPlane.ToString() + " out of range for jobfile with " + _jobShell.NumWorkPlanes.ToString() + " workPlanes!");
            }
            if (_cacheState == CacheState.CompleteJobCached)
            {
                return _job.WorkPlanes[i_workPlane];
            }
            else
            {
                WorkPlaneLUT wpLUT = _workPlaneLUTs[i_workPlane];
                using (var stream = CreateLocalStream(i_workPlane))
                {

                    WorkPlane wp = GetWorkPlaneShell(i_workPlane, stream);

                    for (int i_block = 0; i_block < wp.NumBlocks; i_block++)
                    {
                        wp.VectorBlocks.Add(GetVectorBlock(i_workPlane, i_block, stream));
                    }

                    _numberOfCachedLayers++;
                    UpdateStatus();
                    return wp;
                }
            }
        }
        private void UpdateStatus()
        {
            progress?.Update("reading workPlane " + _numberOfCachedLayers, (int)(((double)(_numberOfCachedLayers * 100)) / _numberOfLayers));
        }
        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            return GetWorkPlaneShell(i_workPlane, null);
        }

        private WorkPlane GetWorkPlaneShell(int i_workPlane, Stream stream)
        {
            if (_jobShell.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane " + i_workPlane.ToString() + " out of range for jobfile with " + _jobShell.NumWorkPlanes.ToString() + " workPlanes!");
            }

            if (CacheState == CacheState.CompleteJobCached)
            {
                return _job.WorkPlanes[i_workPlane].CloneWithoutVectorData();
            }
            else
            {
                bool closeStream = false;
                if (stream == null)
                {
                    closeStream = true;
                    stream = CreateLocalStream(i_workPlane);
                }
                WorkPlaneLUT wpLUT = _workPlaneLUTs[i_workPlane];
                stream.Position = wpLUT.WorkPlaneShellPosition - GetStreamOffset(i_workPlane);
                var wp = WorkPlane.Parser.ParseDelimitedFrom(stream);
                if (closeStream) stream.Dispose();
                return wp;
            }
        }
        private Stream CreateLocalStream(int i_workPlane)
        {
            long end;
            if (i_workPlane + 1 == _jobShell.NumWorkPlanes)
            {
                end = _streamlength;
            }
            else
            {
                end = _jobLUT.WorkPlanePositions[i_workPlane + 1] - 1;
            }

            var start = _jobLUT.WorkPlanePositions[i_workPlane];
            var stream = _mmf.CreateViewStream(start, end - start, MemoryMappedFileAccess.Read);

            return stream;
        }
        private long GetStreamOffset(int i_workPlane)
        {
            return _jobLUT.WorkPlanePositions[i_workPlane];
        }

        /// <inheritdoc/>
        public override VectorBlock GetVectorBlock(int i_workPlane, int i_vectorblock)
        {
            return GetVectorBlock(i_workPlane, i_vectorblock, null);
        }
        private VectorBlock GetVectorBlock(int i_workPlane, int i_vectorblock, Stream stream)
        {
            if (_jobShell.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane" + i_workPlane.ToString() + " out of range for job with " + _jobShell.NumWorkPlanes.ToString() + " workplanes!");
            }
            WorkPlaneLUT wpLut = _workPlaneLUTs[i_workPlane];
            if (wpLut.VectorBlocksPositions.Count < i_vectorblock)
            {
                throw new ArgumentOutOfRangeException("i_vectorblock " + i_vectorblock.ToString() + " out of range for workPlane with " + wpLut.VectorBlocksPositions.Count.ToString() + " blocks!");
            }

            if (CacheState == CacheState.CompleteJobCached)
            {
                return _job.WorkPlanes[i_workPlane].VectorBlocks[i_vectorblock];
            }
            else
            {
                bool closeStream = false;
                if (stream == null)
                {
                    closeStream = true;
                    stream = CreateLocalStream(i_workPlane);
                }
                VectorBlock vb = new VectorBlock();
                stream.Position = wpLut.VectorBlocksPositions[i_vectorblock] - GetStreamOffset(i_workPlane);

                vb = VectorBlock.Parser.ParseDelimitedFrom(stream);
                if (closeStream) stream.Dispose();
                return vb;
            }
        }

        private void CheckConsistence(int number1, int number2)
        {
            if (number1 != number2)
            {
                Dispose();
                throw new IOException("inconsistence in file detected");
            }
        }

        /// <inheritdoc/>
        public override void UnloadJobFromMemory()
        {
            _cacheState = CacheState.NotCached;
            _job = null;
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            _job = null;
            _globalstream?.Dispose();
            _mmf?.Dispose();
            _fileStream?.Dispose();
            _fileOperationInProgress = FileReadOperation.None;
            _cacheState = CacheState.NotCached;
        }

        public override void CloseFile()
        {
            _fileStream?.Close();
        }
    }
}
