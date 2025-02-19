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

﻿using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.IO;
using System.Collections.Generic;
using OpenVectorFormat.Utils;
using System.Linq;
using System.Numerics;
using Google.Protobuf.Collections;
using OVFDefinition;
using System.Diagnostics;

namespace OpenVectorFormat.GCodeReaderWriter
{
    public class GCodeReader : FileReader
    {
        private WorkPlane _currentWP;
        private VectorBlock _currentVB;
        private IFileReaderWriterProgress _progress;
        private CacheState _cacheState = CacheState.NotCached;
        private string _filename;
        private bool _fileLoadingFinished;

        public new static List<String> SupportedFileFormats { get; } = new List<string>() { ".gcode", ".gco" };

        public override CacheState CacheState => _cacheState;

        public Job job;

        public override Job JobShell
        {
            get
            {
                if (_cacheState == CacheState.CompleteJobCached)
                {
                    Job jobShell = new Job();
                    ProtoUtils.CopyWithExclude(job, jobShell, new List<int> { Job.WorkPlanesFieldNumber });
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
            if (_cacheState == CacheState.CompleteJobCached)
            {
                return job;
            }
            else if (File.Exists(_filename))
            {
                ParseGCodeFile();
                return job;
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        public override void CloseFile()
        {
            UnloadJobFromMemory();
        }

        public override void Dispose()
        {
            UnloadJobFromMemory();
        }

        public override VectorBlock GetVectorBlock(int i_workPlane, int i_vectorblock)
        {
            if (_cacheState == CacheState.CompleteJobCached)
            {
                return job.WorkPlanes[i_workPlane].VectorBlocks[i_vectorblock];
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        public override WorkPlane GetWorkPlane(int i_workPlane)
        {
            if (_cacheState == CacheState.CompleteJobCached)
            {
                return job.WorkPlanes[i_workPlane];
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            if (job.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane " + i_workPlane.ToString() + " out of range for jobfile with " + job.NumWorkPlanes.ToString() + " workPlanes!");
            }

            if (_cacheState == CacheState.CompleteJobCached)
            {
                return job.WorkPlanes[i_workPlane].CloneWithoutVectorData();
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        public override void OpenJob(string filename, IFileReaderWriterProgress progress = null)
        {
            this._progress = progress;
            this._filename = filename;
            _cacheState = CacheState.NotCached;
            if (!SupportedFileFormats.Contains(Path.GetExtension(filename)))
            {
                throw new ArgumentException(Path.GetExtension(filename) + " is not supported by GCodeReader. Supported formats are: " + string.Join(";", SupportedFileFormats));
            }
            job = new Job
            {
                JobMetaData = new Job.Types.JobMetaData
                {
                    JobCreationTime = 0,
                    JobName = Path.GetFileNameWithoutExtension(filename)
                }
            };

            this._filename = filename;

            ParseGCodeFile();
        }

        public void ParseGCodeFile()
        {
            MapField<int, MarkingParams> MPsMap = new MapField<int, MarkingParams>();
            Dictionary<MarkingParams, int> cachedMP = new Dictionary<MarkingParams, int>();

            MarkingParams currentMP = new MarkingParams();
            List<VectorBlock> addedVectorBlocks = new List<VectorBlock>();

            Vector3 position = new Vector3(0, 0, 0);
            float angle = 0f;

            GCodeCommandList gCodeCommands = new GCodeCommandList(File.ReadAllLines(_filename));

            bool VBlocked = true;
            int MPKey = 0;

            _currentWP = new WorkPlane
            {
                WorkPlaneNumber = 0
            };
            job.NumWorkPlanes = 0;
            _currentWP.Repeats = 0;

            _currentVB = new VectorBlock
            {
                MarkingParamsKey = 0,
                MetaData = new VectorBlock.Types.VectorBlockMetaData
                {
                    PartKey = 0
                }
            };

            foreach (GCodeCommand currentGCodeCommand in gCodeCommands)
            {
                switch (currentGCodeCommand)
                {
                    case MovementCommand movementCmd:
                        processMovementCmd(movementCmd);
                        break;
                    case PauseCommand pauseCmd:
                        processPauseCmd(pauseCmd);
                        break;
                    case ToolChangeCommand toolChangeCmd:
                        processToolChandeCmd(toolChangeCmd);
                        break;
                    case MonitoringCommand monitoringCmd:
                        processMonitoringCmd(monitoringCmd);
                        break;
                    case ProgramLogicsCommand programLogicsCmd:
                        processProgramLogicsCmd(programLogicsCmd);
                        break;
                    case MiscCommand miscCmd:
                        processMiscCmd(miscCmd);
                        break;
                }

                VBlocked = false;
            }

            NewWorkPlane();
            job.MarkingParamsMap.MergeFromWithRemap(MPsMap, out var keyMapping);
            foreach (var vectorBlock in addedVectorBlocks) // update all vector block marking param keys after merge
                vectorBlock.MarkingParamsKey = keyMapping[vectorBlock.MarkingParamsKey];

            Part part = new Part();
            part.GeometryInfo = new Part.Types.GeometryInfo()
            {
                BuildHeightInMm = position.Z
            };
            job.PartsMap.Add(0, part);

            _cacheState = CacheState.CompleteJobCached;

            void processMovementCmd(MovementCommand movementCmd)
            {
                if (movementCmd.zPosition != null && movementCmd.zPosition != position.Z)
                {
                    NewWorkPlane();
                }
                switch (movementCmd)
                {
                    case LinearInterpolationCmd linearCmd:
                        UpdateLineSequence(linearCmd);
                        break;
                    case CircularInterpolationCmd circularCmd:
                        UpdateArc(circularCmd);
                        break;
                }

                UpdatePosition(movementCmd);

                void UpdateLineSequence(LinearInterpolationCmd linearCmd)
                {
                    UpdateSpeed(linearCmd.isOperation, linearCmd.feedRate);

                    if (_currentVB.LineSequence3D == null)
                    {
                        _currentVB.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                    }
                    _currentVB.LineSequence3D.Points.Add(linearCmd.xPosition ?? position.X);
                    _currentVB.LineSequence3D.Points.Add(linearCmd.yPosition ?? position.Y);
                    _currentVB.LineSequence3D.Points.Add(linearCmd.zPosition ?? position.Z);
                }

                void UpdateArc(CircularInterpolationCmd circularCmd)
                {
                    Vector3 targetPosition = new Vector3(circularCmd.xPosition ?? position.X, circularCmd.yPosition ?? position.Y, circularCmd.zPosition ?? position.Z);
                    Vector3 center = new Vector3(position.X + circularCmd.xCenterRel ?? 0, position.Y + circularCmd.yCenterRel ?? 0, position.Z);

                    Vector3 vectorCP = position - center; // Vector from center to start position
                    Vector3 vectorCT = targetPosition - center; // Vector from center to target position

                    float dotProduct = Vector3.Dot(Vector3.Normalize(vectorCP), Vector3.Normalize(vectorCT));
                    float angleAbs = (float)Math.Acos(dotProduct) * (180.0f / (float)Math.PI);
                    angle = (circularCmd.isClockwise ? angleAbs : -angleAbs);

                    if (angle != _currentVB.Arcs3D.Angle && _currentVB.Arcs3D != null)
                    {
                        if (_currentVB.Arcs3D.Angle != 0 && !VBlocked)
                        {
                            NewVectorBlock();
                        }
                    }

                    UpdateSpeed(true, circularCmd.feedRate); // Update Speed in between to complete all checks for new VBs before adding centers. Update speed after angle to not write a possible new speed to the old VB

                    if (_currentVB.Arcs3D == null)
                    {
                        _currentVB.Arcs3D = new VectorBlock.Types.Arcs3D
                        {
                            Angle = angle,

                            StartDx = position.X,
                            StartDy = position.Y,
                            StartDz = position.Z
                        };
                    }
                    _currentVB.Arcs3D.Centers.Add(position.X + circularCmd.xCenterRel ?? position.X);
                    _currentVB.Arcs3D.Centers.Add(position.Y + circularCmd.yCenterRel ?? position.Y);
                    _currentVB.Arcs3D.Centers.Add(position.Z);
                }

                void UpdateSpeed(bool isOperation, float? newSpeed)
                {
                    if(isOperation)
                    {
                        if (currentMP.LaserSpeedInMmPerS != 0 && newSpeed != null && currentMP.LaserSpeedInMmPerS != newSpeed)
                        {
                            if (!VBlocked)
                            {
                                NewVectorBlock();
                            }
                            currentMP.LaserSpeedInMmPerS = (float)newSpeed;
                        }
                    }
                    else
                    {
                        if (currentMP.JumpSpeedInMmS != 0 && newSpeed != null && currentMP.JumpSpeedInMmS != newSpeed)
                        {
                            if (!VBlocked)
                            {
                                NewVectorBlock();
                            }
                            currentMP.JumpSpeedInMmS = (float)newSpeed;
                        }
                    }   
                }
            }

            void processPauseCmd(PauseCommand pauseCmd)
            {
                _currentVB.ExposurePause = new VectorBlock.Types.ExposurePause();
                _currentVB.ExposurePause.PauseInUs = (ulong)pauseCmd.duration * 1000;
            }

            void processToolChandeCmd(ToolChangeCommand toolChangeCmd)
            {

            }

            void processMonitoringCmd(MonitoringCommand monitoringCmd)
            {

            }

            void processProgramLogicsCmd(ProgramLogicsCommand programLogicsCmd)
            {

            }

            void processMiscCmd(MiscCommand miscCmd)
            {

            }

            void UpdatePosition(MovementCommand movementCmd)
            {
                position = new Vector3(movementCmd.xPosition ?? position.X, movementCmd.yPosition ?? position.Y, movementCmd.zPosition ?? position.Z);
            }

            int NewMarkingParams()
            {
                if (cachedMP.TryGetValue(currentMP, out int key)) ;
                else
                {
                    key = MPKey++;
                    MPsMap.Add(key, currentMP);
                    cachedMP.Add(currentMP, key);
                }

                currentMP = new MarkingParams();

                return key;
            }

            void NewVectorBlock()
            {
                _currentVB.MarkingParamsKey = NewMarkingParams();
                _currentWP.VectorBlocks.Add(_currentVB);
                _currentWP.NumBlocks++;
                addedVectorBlocks.Add(_currentVB);

                _currentVB = new VectorBlock()
                {
                    MetaData = new VectorBlock.Types.VectorBlockMetaData
                    {
                        PartKey = 0
                    }
                };

                VBlocked = true;
            }

            void NewWorkPlane()
            {
                NewVectorBlock();
                _currentWP.ZPosInMm = position.Z;
                job.WorkPlanes.Add(_currentWP);
                job.NumWorkPlanes++;
                _currentWP = new WorkPlane
                {
                    WorkPlaneNumber = job.NumWorkPlanes
                };
            }
        }

        public override void UnloadJobFromMemory()
        {
            job = null;
            _cacheState = CacheState.NotCached;
        }
    }
}
