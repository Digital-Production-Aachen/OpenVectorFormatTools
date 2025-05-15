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



using Google.Protobuf;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpenVectorFormat.OVFReaderWriter
{
    public class OVFFileWriter : FileWriter
    {
        private IFileReaderWriterProgress progress;
        private string filename;
        private FileStream _fs;

        /// /// <summary>
        /// jobShell being written to. (just the shell without workPlanes)
        /// For OVF, edits to the JobShell reference this getter returns are possible at any point.
        /// OVFFileWriter will write them to the target file when dispose is called.
        /// </summary>
        public override Job JobShell { get { return _jobShell; } }
        private Job _jobShell;

        #region WorkPlane Properties for partial writing

        // Index in the file header where the index on the workPlane LUT will be saved. Should(!) be 2, since this index follows directly after the Int16 indicating FileWriteOperation.PartialWrite.
        private Int64 _LUTPositionIndex = 0;

        // workPlaneLUT containing the start indices of each workPlane in filestream
        private JobLUT _jobLUT;

        // shell object of the current workPlane
        private WorkPlane _currentWorkPlaneShell;
        #endregion

        /// <inheritdoc/>
        public override FileWriteOperation FileOperationInProgress { get { return _fileOperationInProgress; } }
        private FileWriteOperation _fileOperationInProgress = FileWriteOperation.None;

        public new static List<string> SupportedFileFormats { get; } = new List<string>() { ".ovf" };

        #region Partial Write. LUTs are included in file to enable partial reading.

        /// <inheritdoc/>
        public override void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress = null)
        {
            _fileOperationInProgress = FileWriteOperation.PartialWrite;

            // remove workplanes if present.
            // create copy of jobshell, since the provided jobShell is a reference to the callers' object. Removing potentially present workplanes in the provdied jobShell also removes them for the caller.
            Job newJobShell = new Job();
            ProtoUtils.CopyWithExclude(jobShell, newJobShell, new List<int> { Job.WorkPlanesFieldNumber });
            _jobShell = newJobShell;
            _jobShell.NumWorkPlanes = 0;
            if(_jobShell.JobMetaData == null) _jobShell.JobMetaData = new Job.Types.JobMetaData();
            _jobShell.JobMetaData.Bounds = AxisAlignedBox2DExtensions.EmptyAAB2D();

            this.progress = progress;
            this.filename = filename;

            // Create jobfile and write header
            _fs = File.Create(filename);
            lock (_fs)
            {
                _fs.Write(Contract.magicNumber, 0, Contract.magicNumber.Length);
                _LUTPositionIndex = _fs.Position;
                
                byte[] _LutPositionIndexBytes = ConvertToByteArrayLittleEndian(Contract.defaultLUTIndex);
                _fs.Write(_LutPositionIndexBytes, 0, _LutPositionIndexBytes.Length);

                // Initialize workPlane lookup table
                _jobLUT = new JobLUT();
            }
        }

        /// <inheritdoc/>
        public override void AppendWorkPlane(WorkPlane workPlaneShell)
        {
            if (_fileOperationInProgress != FileWriteOperation.PartialWrite)
            {
                throw new InvalidOperationException("Adding workPlanes is only possible after initializing a file opertaion with StartWriteAsync");
            }
            // finish the previous workPlane
            FinishWorkPlane();
            // hold the workPlane in RAM until writing is done
            _currentWorkPlaneShell = workPlaneShell;
        }

        private void FinishWorkPlane()
        {
            if (_currentWorkPlaneShell != null)
            {
                if(_fs == null)
                {
                    return;
                }
                lock (_fs)
                {
                    _fs.Position = _fs.Length;

                    _jobLUT.WorkPlanePositions.Add(_fs.Position);

                    long _workPlaneLUTIndex = _fs.Position;

                    byte[] byteToWrite = ConvertToByteArrayLittleEndian((long)0);
                    _fs.WriteAsync(byteToWrite, 0, byteToWrite.Length);

                    WorkPlaneLUT wpLUT = new WorkPlaneLUT();

                    for (int i_block = 0; i_block < _currentWorkPlaneShell.VectorBlocks.Count; i_block++)
                    {
                        wpLUT.VectorBlocksPositions.Add(_fs.Position);
                        _currentWorkPlaneShell.VectorBlocks[i_block].WriteDelimitedTo(_fs);
                    }

                    _currentWorkPlaneShell.WorkPlaneNumber = _jobShell.NumWorkPlanes;
                    _currentWorkPlaneShell.NumBlocks = _currentWorkPlaneShell.VectorBlocks.Count;
                    if (_currentWorkPlaneShell.MetaData == null) _currentWorkPlaneShell.MetaData = new WorkPlane.Types.WorkPlaneMetaData();
                    _currentWorkPlaneShell.MetaData.Bounds = _currentWorkPlaneShell.Bounds2D();
                    _jobShell.JobMetaData.Bounds.Contain(_currentWorkPlaneShell.MetaData.Bounds);

                    var tempShell = _currentWorkPlaneShell.CloneWithoutVectorData();
                    wpLUT.WorkPlaneShellPosition = _fs.Position;
                    tempShell.WriteDelimitedTo(_fs);

                    long lutPosition = _fs.Position;

                    wpLUT.WriteDelimitedTo(_fs);
                    _fs.Position = _workPlaneLUTIndex;
                    byte[] lutPosBytes = ConvertToByteArrayLittleEndian(lutPosition);
                    _fs.Write(lutPosBytes, 0, lutPosBytes.Length);
                    _fs.Position = _fs.Length;

                    _jobShell.NumWorkPlanes++;
                }
            }
        }

        /// <inheritdoc/>
        public override void AppendVectorBlock(VectorBlock block)
        {
            if (_fileOperationInProgress != FileWriteOperation.PartialWrite)
            {
                throw new InvalidOperationException("Adding a VectorBlock is only possible after initializing a file opertaion with StartWriteAsync");
            }

            if (_currentWorkPlaneShell == null)
            {
                throw new InvalidOperationException("No workPlane added yet! A workPlane must be added before adding VectorBlocks");
            }
            _currentWorkPlaneShell.VectorBlocks.Add(block);
        }
        #endregion

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (_fileOperationInProgress == FileWriteOperation.PartialWrite && _fs != null && _fs.CanWrite)
            {
                // [WorkPlaneN][JobShell][LUT]
                // write last workPlane
                FinishWorkPlane();
                lock (_fs)
                {
                    // write shell
                    _jobLUT.JobShellPosition = _fs.Position;
                    _jobShell.WriteDelimitedTo(_fs);
                    // write LUT
                    long LUTPosition = _fs.Position;
                    _jobLUT.WriteDelimitedTo(_fs);
                    //update LUT position to finalize file and make it valid
                    _fs.Position = _LUTPositionIndex;
                    byte[] lutPosBytes = ConvertToByteArrayLittleEndian(LUTPosition);
                    _fs.Write(lutPosBytes, 0, lutPosBytes.Length);
                    if(progress != null) progress.IsFinished = true;
                }
            }
            _fs?.Dispose();
            _fs = null;
            _currentWorkPlaneShell = null;
            _jobLUT = null;
            _fileOperationInProgress = FileWriteOperation.None;
        }

        /// <inheritdoc/>
        public override void SimpleJobWrite(Job job, string filename, IFileReaderWriterProgress progress = null)
        {
            CheckConsistence(job.NumWorkPlanes, job.WorkPlanes.Count);

            StartWritePartial(job, filename, progress);

            // Add all workPlanes already contained in the job
            foreach (var workPlane in job.WorkPlanes)
            {
                progress.Update("workPlane " + workPlane.WorkPlaneNumber + " added! ", (int)((double)workPlane.WorkPlaneNumber / (double)job.WorkPlanes.Count * 100));
                AppendWorkPlane(workPlane);
            }
            Dispose();
            progress.IsFinished = true;
        }

        private void CheckConsistence(int number1, int number2)
        {
            if (number1 != number2)
            {
                Dispose();
                throw new IOException("inconsistence in file detected");
            }
        }

        private byte[] ConvertToByteArrayLittleEndian(long val)
        {
            byte[] bytes_val = BitConverter.GetBytes(val);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes_val);
            }
            return bytes_val;
        }
    }
}
