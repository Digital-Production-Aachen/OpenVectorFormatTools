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

            ParseGCodeFile(progress);
        }

        public void ParseGCodeFile(IFileReaderWriterProgress progress = null)
        {
            MapField<int, MarkingParams> MPsMap = new MapField<int, MarkingParams>();
            Dictionary<MarkingParams, int> cachedMP = new Dictionary<MarkingParams, int>();

            MarkingParams currentMP = new MarkingParams();
            List<VectorBlock> addedVectorBlocks = new List<VectorBlock>();

            Vector3 position = new Vector3(0, 0, 0);
            float angle = 0f;
            bool absolutePositioning = true;

            bool VBlocked = true;
            bool VBempty = true;
            int MPKey = 0;

            // Load command lines from file and transfer to list of GCodeCommand objects.
            GCodeCommandList gCodeCommands = new GCodeCommandList(File.ReadAllLines(_filename));

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
                },
                LpbfMetadata = new VectorBlock.Types.LPBFMetadata
                {
                    StructureType = VectorBlock.Types.StructureType.Part
                }
            };

            //int gCodeCommandCount = gCodeCommands.Count;
            int percentComplete = 0;

            using (StreamReader sr = new StreamReader(_filename))
            {
                string line;
                GCodeConverter gCodeConverter = new GCodeConverter();

                while ((line = sr.ReadLine()) != null)
                {
                    object command = gCodeConverter.ParseLine(line);

                    //commandChanges = commandStateTracker.UpdateState(gCodeCommands[i]);
                    switch (command)
                    {
                        // Process command according to the command-type
                        case MovementCommand movementCmd:
                            ProcessMovementCmd(movementCmd);
                            break;
                        case PauseCommand pauseCmd:
                            ProcessPauseCmd(pauseCmd);
                            break;
                        case ToolChangeCommand toolChangeCmd:
                            ProcessToolChangeCmd(toolChangeCmd);
                            break;
                        case MonitoringCommand monitoringCmd:
                            ProcessMonitoringCmd(monitoringCmd);
                            break;
                        case ProgramLogicsCommand programLogicsCmd:
                            ProcessProgramLogicsCmd(programLogicsCmd);
                            break;
                        case MiscCommand miscCmd:
                            ProcessMiscCmd(miscCmd);
                            break;
                    }

                    // Reset locking state for new vector blocks
                    VBlocked = false;

                    // Update progress
                    /*
                    if (percentComplete != (int)(i + 1) * 100 / gCodeCommandCount)
                    {
                        percentComplete = (i + 1) * 100 / gCodeCommandCount;
                        progress?.Update("Command " + i + " of " + gCodeCommandCount, percentComplete);
                    }
                    */
                }
            }

            // Save last work plane to job and merge marking params
            NewWorkPlane();
            //job.MarkingParamsMap.MergeFromWithRemap(MPsMap, out var keyMapping);
            job.MarkingParamsMap.MergeFrom(MPsMap);
            // Update all vector block marking param keys after merge
            //foreach (var vectorBlock in addedVectorBlocks)
            //    vectorBlock.MarkingParamsKey = keyMapping[vectorBlock.MarkingParamsKey];

            // Add part info to job
            Part part = new Part();
            part.GeometryInfo = new Part.Types.GeometryInfo()
            {
                BuildHeightInMm = Math.Round(Convert.ToDouble(position.Z), 2)
            };
            job.PartsMap.Add(0, part);

            _cacheState = CacheState.CompleteJobCached;

            void ProcessMovementCmd(MovementCommand movementCmd)
            {
                // Check if layer has changed
                if (movementCmd.zPosition != null && movementCmd.zPosition != position.Z && !VBempty)   //[0].LineSequence.Points.Count == 0 && _currentWP.VectorBlocks[0].Arcs.Centers.Count == 0)
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

                // Update current position after movement & indicate that the vectorblocks are not empty
                UpdatePosition(movementCmd);
                VBempty = false;

                void UpdateLineSequence(LinearInterpolationCmd linearCmd)
                {
                    // Update speed first to check if marking params changed and new vector block is needed
                    UpdateSpeed(linearCmd.isOperation, linearCmd.feedRate);

                    if (_currentVB.LineSequence == null)
                    {
                        _currentVB.LineSequence = new VectorBlock.Types.LineSequence();
                    }
                    _currentVB.LineSequence.Points.Add(absolutePositioning 
                        ? (linearCmd.xPosition ?? position.X) // Use absolute x-positioning
                        : (position.X + (linearCmd.xPosition ?? 0))); // Use relative xpositioning
                    _currentVB.LineSequence.Points.Add(absolutePositioning
                        ? (linearCmd.yPosition ?? position.Y) // Use absolute y-positioning
                        : (position.Y + (linearCmd.yPosition ?? 0))); // Use relative y-positioning
                }

                void UpdateArc(CircularInterpolationCmd circularCmd)
                {
                    // Create neccessary variables for conversion of G-Code arc definition to OVF arc definition
                    Vector2 targetPosition = new Vector2(absolutePositioning ? (circularCmd.xPosition ?? position.X) : (position.X + (circularCmd.xPosition ?? 0)),
                        absolutePositioning ? (circularCmd.yPosition ?? position.Y) : (position.Y + (circularCmd.yPosition ?? 0)));

                    Vector2 center = new Vector2(position.X + circularCmd.xCenterRel ?? 0, position.Y + circularCmd.yCenterRel ?? 0);

                    Vector2 vectorCP = new Vector2(position.X - center.X, position.Y - center.Y); // Vector from center to start position
                    Vector2 vectorCT = targetPosition - center; // Vector from center to target position

                    float dotProduct = Vector2.Dot(Vector2.Normalize(vectorCP), Vector2.Normalize(vectorCT));
                    float angleAbs = (float)Math.Acos(dotProduct) * (180.0f / (float)Math.PI);
                    angle = (circularCmd.isClockwise ? angleAbs : -angleAbs);

                    // Check if angle has changed, so marking params changed and new vector block is needed
                    if (angle != _currentVB.Arcs.Angle && _currentVB.Arcs != null)
                    {
                        if (_currentVB.Arcs.Angle != 0 && !VBlocked)
                        {
                            NewVectorBlock();
                        }
                    }

                    /* Update Speed inbetween to complete all checks for new vector blocks before adding centers. 
                       Update speed after angle to not write a new speed to an old vector block. */
                    UpdateSpeed(true, circularCmd.feedRate);

                    if (_currentVB.Arcs == null)
                    {
                        _currentVB.Arcs = new VectorBlock.Types.Arcs
                        {
                            Angle = angle,

                            StartDx = position.X,
                            StartDy = position.Y
                        };
                    }
                    _currentVB.Arcs.Centers.Add(position.X + circularCmd.xCenterRel ?? position.X);
                    _currentVB.Arcs.Centers.Add(position.Y + circularCmd.yCenterRel ?? position.Y);
                }

                void UpdateSpeed(bool isOperation, float? newSpeed)
                {
                    // Check if machine movement is travel or operation move and assign speed accordingly
                    if (isOperation)
                    {
                        if (newSpeed != null && currentMP.LaserSpeedInMmPerS != newSpeed)
                        {
                            if (currentMP.LaserSpeedInMmPerS != 0 && !VBlocked)
                            {
                                NewVectorBlock();
                            }
                            currentMP.LaserSpeedInMmPerS = (float)newSpeed;
                        }
                    }
                    else
                    {
                        if (newSpeed != null && currentMP.JumpSpeedInMmS != newSpeed)
                        {
                            if (currentMP.JumpSpeedInMmS != 0 && !VBlocked)
                            {
                                NewVectorBlock();
                            }
                            currentMP.JumpSpeedInMmS = (float)newSpeed;
                        }
                    }   
                }
            }

            void ProcessPauseCmd(PauseCommand pauseCmd)
            {
                // Save current vector block and create new vector block for pause
                NewVectorBlock();
                _currentVB.ExposurePause = new VectorBlock.Types.ExposurePause();
                _currentVB.ExposurePause.PauseInUs = (ulong)pauseCmd.duration * 1000;

                // Save pause vector block and create new vector block for next command
                NewVectorBlock();
            }

            void ProcessToolChangeCmd(ToolChangeCommand toolChangeCmd)
            {

            }

            void ProcessMonitoringCmd(MonitoringCommand monitoringCmd)
            {

            }

            void ProcessProgramLogicsCmd(ProgramLogicsCommand programLogicsCmd)
            {
                switch (programLogicsCmd)
                {
                    case PositioningToggleCommand toggleCmd:
                        TogglePositioning(toggleCmd);
                        break;
                }

                void TogglePositioning(PositioningToggleCommand toggleCmd)
                {
                    absolutePositioning = toggleCmd.isAbsolute;
                }
            }

            void ProcessMiscCmd(MiscCommand miscCmd)
            {

            }

            void UpdatePosition(MovementCommand movementCmd)
            {
                /* Update position with coordinates given in movement command.
                   If a coordinate is not given, keep the current corrdinate value. */
                position = new Vector3(movementCmd.xPosition ?? position.X, movementCmd.yPosition ?? position.Y, movementCmd.zPosition ?? position.Z);
            }

            int NewMarkingParams()
            {
                /* Try to get current marking params from cached params.
                   If not found, create new key and add to cache */
                if (cachedMP.TryGetValue(currentMP, out int key)) ;
                else
                {
                    key = MPKey++;
                    MPsMap.Add(key, currentMP);
                    cachedMP.Add(currentMP, key);
                }

                // Create new marking params for next vector block
                currentMP = new MarkingParams();

                return key;
            }

            void NewVectorBlock()
            {
                // Update current vector block with current marking params and add vector block to current work plane
                _currentVB.MarkingParamsKey = NewMarkingParams();
                _currentWP.VectorBlocks.Add(_currentVB);
                _currentWP.NumBlocks++;
                addedVectorBlocks.Add(_currentVB);

                // Create new vector block
                _currentVB = new VectorBlock()
                {
                    MetaData = new VectorBlock.Types.VectorBlockMetaData
                    {
                        PartKey = 0
                    },
                    LpbfMetadata = new VectorBlock.Types.LPBFMetadata
                    {
                        StructureType = VectorBlock.Types.StructureType.Part
                    }
                };

                // Lock vector block to prevent creating mulitple new vector blocks for one command
                VBlocked = true;
                VBempty = true;
            }

            void NewWorkPlane()
            {
                // Update current work plane with current vector block and z-position
                NewVectorBlock();
                _currentWP.ZPosInMm = position.Z;
                job.WorkPlanes.Add(_currentWP);
                job.NumWorkPlanes++;

                // Create new work plane
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
