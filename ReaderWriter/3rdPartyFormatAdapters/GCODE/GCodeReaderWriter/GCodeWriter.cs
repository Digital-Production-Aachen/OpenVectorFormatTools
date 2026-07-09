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

using GCodeReaderWriter.Commands;
using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.OVFReaderWriter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenVectorFormat.GCodeReaderWriter
{
    public class GCodeWriter : FileWriter
    {
        public new static List<string> SupportedFileFormats { get; } = new List<string> { ".gcode", ".gco" };

        private IFileReaderWriterProgress _progress;
        private string _filename;
        private StreamWriter _fs;

        public override Job JobShell { get { return _jobShell; } }
        private Job _jobShell;
        float[] _lastPt = null;
        float _currentZ;

        private readonly OvfToGCodeCommandFactory _factory;

        public GCodeWriter() : this(new OvfToGCodeCommandFactory()) { }

        public GCodeWriter(OvfToGCodeCommandFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public override FileWriteOperation FileOperationInProgress { get { return _fileOperationInProgress; } }
        private FileWriteOperation _fileOperationInProgress = FileWriteOperation.None;

        private MarkingParams _lastWrittenParams = null;

        public void ProcessOVFtoGCode(OVFFileReader ovfReader, string gcodeOutputPath)
        {
            Job job = ovfReader.CacheJobToMemory();

            if (job == null)
                throw new InvalidOperationException("Failed to load job from OVF file.");
            if (job.NumWorkPlanes <= 0)
                throw new InvalidOperationException("No WorkPlanes found in job.");

            this.SimpleJobWrite(job, gcodeOutputPath);
        }

        public override void AppendWorkPlane(WorkPlane workPlane)
        {
            _currentZ = workPlane.ZPosInMm;

            for (uint repeatIndex = 0; repeatIndex < workPlane.Repeats + 1; repeatIndex++)
            {
                for (int blockIndex = 0; blockIndex < workPlane.VectorBlocks.Count; blockIndex++)
                {
                    VectorBlock block = workPlane.VectorBlocks[blockIndex];

                    bool isFirstBlockInPlane = (repeatIndex == 0 && blockIndex == 0);

                    bool injectZ = isFirstBlockInPlane &&
                           (block.VectorDataCase == VectorBlock.VectorDataOneofCase.PointSequence ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.Arcs ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence);

                    AddVectorBlock(block, injectZ);
                }
            }
        }

        public override void AppendVectorBlock(VectorBlock block)
        {
            AddVectorBlock(block, false);
        }

        public override void Dispose()
        {
            if (_fileOperationInProgress != FileWriteOperation.None)
            {
                _fs.Close();
            }
        }

        private void AddVectorBlock(VectorBlock block, bool injectZ)
        {
            var ctx = new GCodeWriterContext(_currentZ, injectZ);
            for (ulong i = 0; i < block.Repeats + 1; i++)
            {
                foreach (var cmd in _factory.CreateCommands(block, ctx))
                {
                    if (cmd is LinearInterpolationCmd linCmd)
                    {
                        float[] target = MoveTarget(linCmd);
                        if (!linCmd.isOperation)
                        {
                            if (_lastPt != null && target.SequenceEqual(_lastPt))
                                continue;
                        }
                        _lastPt = target;
                    }
                    _fs.WriteLine(cmd.ToString());
                }
            }
        }

        private static float[] MoveTarget(LinearInterpolationCmd cmd)
        {
            return cmd.zPosition.HasValue
                ? new[] { cmd.xPosition.Value, cmd.yPosition.Value, cmd.zPosition.Value }
                : new[] { cmd.xPosition.Value, cmd.yPosition.Value };
        }

        public override void SimpleJobWrite(Job job, string filename, IFileReaderWriterProgress progress = null)
        {
            CheckConsistence(job.NumWorkPlanes, job.WorkPlanes.Count);
            for (int i = 0; i < job.NumWorkPlanes; i++)
            {
                int expected = job.WorkPlanes[i].NumBlocks;
                int actual = job.WorkPlanes[i].VectorBlocks.Count;

                if (expected != actual)
                {
                    throw new IOException($"Inconsistency detected in WorkPlane {i}: Expected {expected}, Actual {actual}");
                }
            }
            _jobShell = job;
            _fileOperationInProgress = FileWriteOperation.CompleteWrite;
            _fs = new StreamWriter(filename);
            this._filename = filename;
            this._progress = progress;
            foreach (WorkPlane wp in job.WorkPlanes)
            {
                AppendWorkPlane(wp);
            }
            _fs.Close();
            _fileOperationInProgress = FileWriteOperation.None;
        }
        public override void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress = null)
        {
            _jobShell = jobShell;
            _fileOperationInProgress = FileWriteOperation.PartialWrite;
            _fs = new StreamWriter(filename);
            this._filename = filename;
            this._progress = progress;
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
