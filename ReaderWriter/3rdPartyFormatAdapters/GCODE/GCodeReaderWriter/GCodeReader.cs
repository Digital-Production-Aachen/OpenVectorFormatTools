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
using Google.Protobuf.Collections;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace OpenVectorFormat.GCodeReaderWriter
{
    public class GCodeReader : FileReader
    {
        /// <inheritdoc/>
        public new static List<string> SupportedFileFormats { get; } = new List<string> { ".gcode", ".gco" };

        public Job job;
        /// <inheritdoc/>
        public override CacheState CacheState => _cacheState;

        private CacheState _cacheState = CacheState.NotCached;
        private string _filename;
        private IFileReaderWriterProgress _progress;

        // Current reader states.
        private WorkPlane _currentWP;
        private VectorBlock _currentVB;
        private MarkingParams _currentMP;
        private MapField<int, MarkingParams> _mpMap;
        private Dictionary<MarkingParams, int> _cachedMP;
        private int _nextMpKey;
        private Vector3 _position;
        private bool _absolutePositioning;
        private bool _vbLocked;
        private bool _vbEmpty;

        /// <inheritdoc/>
        public override Job JobShell
        {
            get
            {
                EnsureLoaded();
                var shell = new Job();
                ProtoUtils.CopyWithExclude(job, shell, new List<int> { Job.WorkPlanesFieldNumber });
                return shell;
            }
        }

        /// <inheritdoc/>
        public override Job CacheJobToMemory()
        {
            if (_cacheState == CacheState.CompleteJobCached) return job;
            if (File.Exists(_filename))
            {
                ParseGCodeFile(_progress);
                return job;
            }
            throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
        }

        public override void CloseFile() => UnloadJobFromMemory();

        /// <inheritdoc/>
        public override void Dispose() => UnloadJobFromMemory();

        /// <inheritdoc/>
        public override VectorBlock GetVectorBlock(int i_workPlane, int i_vectorblock)
        {
            EnsureLoaded();
            return job.WorkPlanes[i_workPlane].VectorBlocks[i_vectorblock];
        }

        /// <inheritdoc/>
        public override WorkPlane GetWorkPlane(int i_workPlane)
        {
            EnsureLoaded();
            return job.WorkPlanes[i_workPlane];
        }

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            EnsureLoaded();
            if (i_workPlane >= job.NumWorkPlanes)
                throw new ArgumentOutOfRangeException(nameof(i_workPlane),
                    $"i_workPlane {i_workPlane} out of range for jobfile with {job.NumWorkPlanes} workPlanes!");
            return job.WorkPlanes[i_workPlane].CloneWithoutVectorData();
        }

        /// <inheritdoc/>
        public override void OpenJob(string filename, IFileReaderWriterProgress progress = null)
        {
            _progress = progress;
            _filename = filename;
            _cacheState = CacheState.NotCached;

            if (!SupportedFileFormats.Contains(Path.GetExtension(filename)))
                throw new ArgumentException(
                    $"{Path.GetExtension(filename)} is not supported by GCodeReader. " +
                    $"Supported formats are: {string.Join(";", SupportedFileFormats)}");

            job = new Job
            {
                JobMetaData = new Job.Types.JobMetaData
                {
                    JobCreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    JobName = Path.GetFileNameWithoutExtension(filename)
                }
            };

            ParseGCodeFile(progress);
        }

        /// <inheritdoc/>
        public override void UnloadJobFromMemory()
        {
            job = null;
            _cacheState = CacheState.NotCached;
        }

        private void ParseGCodeFile(IFileReaderWriterProgress progress = null)
        {
            _mpMap = new MapField<int, MarkingParams>();
            _cachedMP = new Dictionary<MarkingParams, int>();   // MarkingParams overrides Equals/GetHashCode field-wise
            _currentMP = new MarkingParams();
            _nextMpKey = 0;
            _position = new Vector3(0, 0, 0);
            _absolutePositioning = true;
            _vbLocked = true;
            _vbEmpty = true;

            _currentWP = new WorkPlane { WorkPlaneNumber = 0, Repeats = 0 };
            _currentVB = NewEmptyVectorBlock();

            var converter = new GCodeConverter();

            using (var fs = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sr = new StreamReader(fs))
            {
                long totalLength = fs.Length;
                int lastPercent = -1;
                int lineNumber = 0;
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    lineNumber++;
                    GCodeCommand command;
                    try
                    {
                        command = converter.ParseLineToCommandObject(line);
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"GCodeReader: skipping line {lineNumber}: {ex.Message}");
                        continue;
                    }

                    if (command != null)
                    {
                        ParseCommandObjectToJob(command);
                        _vbLocked = false;
                    }

                    if (progress != null && totalLength > 0)
                    {
                        int percent = (int)(fs.Position * 100 / totalLength);
                        if (percent != lastPercent)
                        {
                            progress.Update($"Parsed line {lineNumber}", percent);
                            lastPercent = percent;
                        }
                    }
                }
            }

            // Write last workplane.
            NewWorkPlane();
            job.NumWorkPlanes = job.WorkPlanes.Count;

            job.MarkingParamsMap.MergeFrom(_mpMap);

            var part = new Part
            {
                GeometryInfo = new Part.Types.GeometryInfo
                {
                    BuildHeightInMm = Math.Round((double)_position.Z, 2)
                }
            };
            job.PartsMap.Add(0, part);

            _cacheState = CacheState.CompleteJobCached;
        }

        protected virtual void ParseCommandObjectToJob(GCodeCommand command)
        {
            switch (command)
            {
                case LinearInterpolationCmd linear: ParseLinear(linear); break;
                case CircularInterpolationCmd circular: ParseCircular(circular); break;
                case PauseCommand pause: ParsePause(pause); break;
                case PositioningToggleCommand toggle: ParseToggle(toggle); break;
                case ToolChangeCommand toolChange: ParseToolChange(toolChange); break;
                case MonitoringCommand monitor: ParseMonitoring(monitor); break;
                case MiscCommand misc: ParseMisc(misc); break;
            }
        }

        // Comand parsers decide how to interpret the gcode commands.
        // Override these in a subclass to change the interpretation, e.g. for different machine types or to support more gcode commands.
        protected virtual void ParseLinear(LinearInterpolationCmd cmd)
        {
            if (cmd.zPosition.HasValue && cmd.zPosition.Value != _position.Z && !_vbEmpty)
                NewWorkPlane();

            if (!cmd.isOperation)
            {
                float lastJumpSpeed = _currentMP.JumpSpeedInMmS;
                if (!_vbLocked) NewVectorBlock();
                _currentMP.JumpSpeedInMmS = cmd.feedRate ?? lastJumpSpeed;
            }
            else
            {
                UpdateSpeed(isOperation: true, newSpeed: cmd.feedRate);
            }

            // Z is only a workplane signal; a move without X/Y (pure-Z) adds no geometry.
            if (cmd.xPosition.HasValue || cmd.yPosition.HasValue)
            {
                if (_currentVB.LineSequence == null)
                    _currentVB.LineSequence = new VectorBlock.Types.LineSequence();

                float x = _absolutePositioning
                    ? (cmd.xPosition ?? _position.X)
                    : (_position.X + (cmd.xPosition ?? 0f));
                float y = _absolutePositioning
                    ? (cmd.yPosition ?? _position.Y)
                    : (_position.Y + (cmd.yPosition ?? 0f));

                _currentVB.LineSequence.Points.Add(x);
                _currentVB.LineSequence.Points.Add(y);

                _vbEmpty = false;
            }

            UpdatePosition(cmd);
        }

        protected virtual void ParseCircular(CircularInterpolationCmd cmd)
        {
            if (cmd.zPosition.HasValue && cmd.zPosition.Value != _position.Z && !_vbEmpty)
                NewWorkPlane();

            var target = new Vector2(
                _absolutePositioning ? (cmd.xPosition ?? _position.X) : (_position.X + (cmd.xPosition ?? 0f)),
                _absolutePositioning ? (cmd.yPosition ?? _position.Y) : (_position.Y + (cmd.yPosition ?? 0f)));

            // I/J are offsets from the start point.
            var center = new Vector2(
                _position.X + (cmd.xCenterRel ?? 0f),
                _position.Y + (cmd.yCenterRel ?? 0f));

            // Convert from target point based arc definition to center point + fixed angle based definition
            var vCP = new Vector2(_position.X - center.X, _position.Y - center.Y);
            var vCT = target - center;

            float dot = Vector2.Dot(Vector2.Normalize(vCP), Vector2.Normalize(vCT));
            dot = Math.Max(-1f, Math.Min(1f, dot));
            float angleAbs = (float)(Math.Acos(dot) * 180.0 / Math.PI);
            float angle = cmd.isClockwise ? angleAbs : -angleAbs;

            if (_currentVB.Arcs != null && _currentVB.Arcs.Angle != angle && _currentVB.Arcs.Angle != 0 && !_vbLocked)
            {
                NewVectorBlock();
            }

            UpdateSpeed(isOperation: true, newSpeed: cmd.feedRate);

            if (_currentVB.Arcs == null)
            {
                _currentVB.Arcs = new VectorBlock.Types.Arcs
                {
                    Angle = angle,
                    StartDx = _position.X - center.X,
                    StartDy = _position.Y - center.Y
                };
            }
            _currentVB.Arcs.Centers.Add(center.X);
            _currentVB.Arcs.Centers.Add(center.Y);

            UpdatePosition(cmd);
            _vbEmpty = false;
        }

        protected virtual void ParsePause(PauseCommand cmd)
        {
            NewVectorBlock();
            _currentVB.ExposurePause = new VectorBlock.Types.ExposurePause
            {
                PauseInUs = (ulong)cmd.duration * 1000UL
            };
            _vbEmpty = false;
            NewVectorBlock();
        }

        protected virtual void ParseToggle(PositioningToggleCommand cmd)
        {
            _absolutePositioning = cmd.isAbsolute;
        }

        protected virtual void ParseToolChange(ToolChangeCommand cmd)
        {
            return;
        }

        protected virtual void ParseMonitoring(MonitoringCommand cmd)
        {
            return;
        }

        protected virtual void ParseMisc(MiscCommand cmd)
        {
            return;
        }

        // State helpers.

        protected virtual void UpdatePosition(MovementCommand cmd)
        {
            _position = new Vector3(
                cmd.xPosition ?? _position.X,
                cmd.yPosition ?? _position.Y,
                cmd.zPosition ?? _position.Z);
        }

        protected virtual void UpdateSpeed(bool isOperation, float? newSpeed)
        {
            if (newSpeed == null) return;

            if (isOperation)
            {
                if (_currentMP.LaserSpeedInMmPerS != newSpeed.Value)
                {
                    if (_currentMP.LaserSpeedInMmPerS != 0f && !_vbLocked) NewVectorBlock();
                    _currentMP.LaserSpeedInMmPerS = newSpeed.Value;
                }
            }
            else
            {
                if (_currentMP.JumpSpeedInMmS != newSpeed.Value)
                {
                    if (_currentMP.JumpSpeedInMmS != 0f && !_vbLocked) NewVectorBlock();
                    _currentMP.JumpSpeedInMmS = newSpeed.Value;
                }
            }
        }

        protected virtual int WriteCurrentMarkingParams()
        {
            if (!_cachedMP.TryGetValue(_currentMP, out int key))
            {
                key = _nextMpKey++;
                _mpMap.Add(key, _currentMP);
                _cachedMP.Add(_currentMP, key);
            }
            _currentMP = new MarkingParams();
            return key;
        }

        /// <summary>
        /// Creates new vector block and new <see cref="MarkingParams"/>.
        /// </summary>
        protected virtual void NewVectorBlock()
        {
            // If the last vector block is not empty, write it to the current work plane and create a new marking params key.
            if (!_vbEmpty)
            {
                _currentVB.MarkingParamsKey = WriteCurrentMarkingParams();
                _currentWP.VectorBlocks.Add(_currentVB);
                _currentWP.NumBlocks++;
            }

            _currentVB = NewEmptyVectorBlock();
            _vbLocked = true;
            _vbEmpty = true;
        }

        /// <summary>
        /// Creates new work plane and new <see cref="VectorBlock"/>.
        /// </summary>
        protected virtual void NewWorkPlane()
        {
            NewVectorBlock();
            _currentWP.ZPosInMm = _position.Z;
            job.WorkPlanes.Add(_currentWP);
            _currentWP = new WorkPlane { WorkPlaneNumber = job.WorkPlanes.Count };
        }

        private static VectorBlock NewEmptyVectorBlock() => new VectorBlock
        {
            MetaData = new VectorBlock.Types.VectorBlockMetaData { PartKey = 0 },
            LpbfMetadata = new VectorBlock.Types.LPBFMetadata
            {
                StructureType = VectorBlock.Types.StructureType.Part
            }
        };

        private void EnsureLoaded()
        {
            if (_cacheState != CacheState.CompleteJobCached)
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
        }
    }
}
