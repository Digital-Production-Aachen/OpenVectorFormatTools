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
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.OVFReaderWriter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Job _jobShell;
        private FileWriteOperation _fileOperationInProgress = FileWriteOperation.None;

        /// <inheritdoc/>
        public override Job JobShell { get { return _jobShell; } }

        /// <inheritdoc/>
        public override FileWriteOperation FileOperationInProgress { get { return _fileOperationInProgress; } }

        // Current writer states.
        float[] _lastPosition = null;
        float _currentZ;
        float? _lastX;
        float? _lastY;

        /// <summary>
        /// Processes the OVF file and writes the corresponding GCode to the specified output path.
        /// </summary>
        public void ProcessOVFtoGCode(OVFFileReader ovfReader, string gcodeOutputPath)
        {
            Job job = ovfReader.CacheJobToMemory();

            if (job == null)
                throw new InvalidOperationException("Failed to load job from OVF file.");
            if (job.NumWorkPlanes <= 0)
                throw new InvalidOperationException("No WorkPlanes found in job.");

            this.SimpleJobWrite(job, gcodeOutputPath);
        }

        /// <inheritdoc/>
        public override void AppendWorkPlane(WorkPlane workPlane)
        {
            _currentZ = workPlane.ZPosInMm;

            for (uint repeatIndex = 0; repeatIndex < workPlane.Repeats + 1; repeatIndex++)
            {
                for (int blockIndex = 0; blockIndex < workPlane.VectorBlocks.Count; blockIndex++)
                {
                    VectorBlock block = workPlane.VectorBlocks[blockIndex];

                    bool isFirstBlockInPlane = (repeatIndex == 0 && blockIndex == 0);

                    // Inject a Z move before the first block in a workplane if the block translates to a movement.
                    bool injectZ = isFirstBlockInPlane &&
                           (block.VectorDataCase == VectorBlock.VectorDataOneofCase.PointSequence ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.Arcs ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence);

                    HandleVectorBlock(block, injectZ);
                }
            }
        }

        /// <inheritdoc/>
        public override void AppendVectorBlock(VectorBlock block)
        {
            HandleVectorBlock(block, false);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (_fileOperationInProgress != FileWriteOperation.None)
            {
                _fs.Close();
            }
        }

        private void HandleVectorBlock(VectorBlock block, bool injectZ)
        {
            // Save the previous point before each block, so that the first move of a new block can be compared
            // against the last point of the previous block.
            _lastX = _lastPosition?[0];
            _lastY = _lastPosition?[1];

            for (ulong i = 0; i < block.Repeats + 1; i++)
            {
                foreach (var cmd in WriteVectorBlockData(block, injectZ))
                    Write(cmd);
            }
        }

        private void Write(GCodeCommand cmd)
        {
            // Make sure a travel move whose X/Y equals the last point is skipped.
            // Z-only moves have no X/Y component and are always written.
            if (cmd is LinearInterpolationCmd linCmd &&
                (linCmd.xPosition.HasValue || linCmd.yPosition.HasValue))
            {
                float[] target = { linCmd.xPosition.Value, linCmd.yPosition.Value };
                if (!linCmd.isOperation && _lastPosition != null && target.SequenceEqual(_lastPosition))
                    return;
                _lastPosition = target;
            }
            _fs.WriteLine(cmd.ToString());
        }

        // Command handlers translate OVF vector data into GCodeCommands.
        // Override these (or the small move helpers below) in a subclass to change the interpretation.
        protected virtual IEnumerable<GCodeCommand> WriteVectorBlockData(VectorBlock block, bool injectZ)
        {
            switch (block.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.PointSequence:
                    return WritePointSequence(block, injectZ);
                case VectorBlock.VectorDataOneofCase.Hatches:
                    return WriteHatches(block, injectZ);
                case VectorBlock.VectorDataOneofCase.Arcs:
                    return WriteArcs(block, injectZ);
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    return WriteExposurePause(block, injectZ);
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    return WriteLineSequence(block, injectZ);
                default:
                    throw new NotImplementedException($"VectorDataCase {block.VectorDataCase} is not supported.");
            }
        }

        /// <summary>
        /// Writes point sequence data as a series of travel moves.
        /// </summary>
        protected virtual IEnumerable<GCodeCommand> WritePointSequence(VectorBlock block, bool injectZ)
        {
            var pts = block.PointSequence.Points;
            for (int i = 0; i < pts.Count; i += 2)
            {
                if (injectZ && i == 0)
                    yield return InjectedTravel(pts[i], pts[i + 1]);
                else
                    yield return TravelMove(pts[i], pts[i + 1]);
            }
        }

        /// <summary>
        /// Writes hatch data as a series of moves.
        /// </summary>
        protected virtual IEnumerable<GCodeCommand> WriteHatches(VectorBlock block, bool injectZ)
        {
            var pts = block.Hatches.Points;
            for (int i = 0; i < pts.Count; i += 4)
            {
                if (injectZ && i == 0)
                    yield return InjectedTravel(pts[i], pts[i + 1]);
                else
                    yield return TravelMove(pts[i], pts[i + 1]);

                yield return FeedMove(pts[i + 2], pts[i + 3]);
            }
        }

        /// <summary>
        /// Writes arc data as a series of circular interpolation moves.
        /// </summary>
        protected virtual IEnumerable<GCodeCommand> WriteArcs(VectorBlock block, bool injectZ)
        {
            double angle = block.Arcs.Angle;
            float startX = block.Arcs.StartDx;
            float startY = block.Arcs.StartDy;
            var centers = block.Arcs.Centers;

            // If the first move in a workplane is an arc,
            // inject a Z move to the workplane height before the arc.
            if (injectZ)
                yield return ZMove(_currentZ);

            for (int i = 0; i < centers.Count; i += 2)
            {
                float cx = centers[i], cy = centers[i + 1];
                double iOffset = cx - startX;
                double jOffset = cy - startY;
                double deltaX = startX - cx, deltaY = startY - cy;
                double radius = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                double angleStart = Math.Atan2(deltaY, deltaX);
                double angleFinal = NormalizeAngle(angleStart - angle);
                double endX = cx + radius * Math.Cos(angleFinal);
                double endY = cy + radius * Math.Sin(angleFinal);

                if (angle > 0)
                    yield return new CircularInterpolationCmd(PrepCode.G, 2, true,
                        (float)endX, (float)endY, (float)iOffset, (float)jOffset, null, null);
                else if (angle < 0)
                    yield return new CircularInterpolationCmd(PrepCode.G, 3, false,
                        (float)endX, (float)endY, (float)iOffset, (float)jOffset, null, null);
                else
                    throw new ArgumentException("Arc angle must be non-zero.");
            }
        }

        /// <summary>
        /// Writes an exposure pause command as a G4 command.
        /// </summary>
        protected virtual IEnumerable<GCodeCommand> WriteExposurePause(VectorBlock block, bool injectZ)
        {
            yield return new PauseCommand(PrepCode.G, 4, (int)block.ExposurePause.PauseInUs);
        }

        /// <summary>
        /// Writes line sequence data as a series of moves.
        /// </summary>
        protected virtual IEnumerable<GCodeCommand> WriteLineSequence(VectorBlock block, bool injectZ)
        {
            var pts = block.LineSequence.Points;
            if (injectZ)
                yield return InjectedTravel(pts[0], pts[1]);
            else
                yield return TravelMove(pts[0], pts[1]);

            for (int i = 2; i < pts.Count; i += 2)
                yield return FeedMove(pts[i], pts[i + 1]);
        }

        /// <summary>
        /// First travel move of a workplane: writes a standalone Z move when X/Y is
        /// unchanged from the previous point, otherwise write a combined X/Y/Z travel move.
        /// </summary>
        protected virtual LinearInterpolationCmd InjectedTravel(float x, float y)
            => (_lastX == x && _lastY == y)
                ? ZMove(_currentZ)
                : TravelMove(x, y, _currentZ);

        protected virtual LinearInterpolationCmd ZMove(float z)
            => new LinearInterpolationCmd(PrepCode.G, 0, false, null, null, z);

        protected virtual LinearInterpolationCmd TravelMove(float x, float y, float? z = null)
            => new LinearInterpolationCmd(PrepCode.G, 0, false, x, y, z);

        protected virtual LinearInterpolationCmd FeedMove(float x, float y, float? z = null)
            => new LinearInterpolationCmd(PrepCode.G, 1, true, x, y, z);

        private static double NormalizeAngle(double angle)
        {
            angle %= 2 * Math.PI;
            if (angle > Math.PI) angle -= 2 * Math.PI;
            else if (angle <= -Math.PI) angle += 2 * Math.PI;
            return angle;
        }

        /// <inheritdoc/>
        public override void SimpleJobWrite(Job job, string filename, IFileReaderWriterProgress progress = null)
        {
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

        /// <inheritdoc/>
        public override void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress = null)
        {
            _jobShell = jobShell;
            _fileOperationInProgress = FileWriteOperation.PartialWrite;
            _fs = new StreamWriter(filename);
            this._filename = filename;
            this._progress = progress;
        }
    }
}
