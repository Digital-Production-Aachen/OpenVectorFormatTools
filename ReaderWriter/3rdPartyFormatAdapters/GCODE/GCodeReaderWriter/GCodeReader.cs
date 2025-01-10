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
using OpenVectorFormat;
using System;
using System.IO;
using System.Collections.Generic;
using OpenVectorFormat.Utils;
using System.Linq;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Globalization;
using Google.Protobuf.Collections;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using OVFDefinition;

namespace OpenVectorFormat.GCodeReaderWriter
{
    public class GCodeReader : FileReader
    {
        private WorkPlane currentWP;
        private VectorBlock currentVB;
        private IFileReaderWriterProgress progress;
        private CacheState _cacheState = CacheState.NotCached;
        private string filename;
        private bool fileLoadingFinished;

        public new static List<String> SupportedFileFormats { get; } = new List<string>() { ".gcode", ".gco" };

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
            if (_cacheState == CacheState.CompleteJobCached)
            {
                return CompleteJob;
            }
            else if (File.Exists(filename))
            {
                ParseGCodeFile();
                return CompleteJob;
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
                return CompleteJob.WorkPlanes[i_workPlane].VectorBlocks[i_vectorblock];
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
                return CompleteJob.WorkPlanes[i_workPlane];
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            if (CompleteJob.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane " + i_workPlane.ToString() + " out of range for jobfile with " + CompleteJob.NumWorkPlanes.ToString() + " workPlanes!");
            }

            if (_cacheState == CacheState.CompleteJobCached)
            {
                return CompleteJob.WorkPlanes[i_workPlane].CloneWithoutVectorData();
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        public override void OpenJob(string filename, IFileReaderWriterProgress progress = null)
        {
            this.progress = progress;
            this.filename = filename;
            _cacheState = CacheState.NotCached;
            if (!SupportedFileFormats.Contains(Path.GetExtension(filename)))
            {
                throw new ArgumentException(Path.GetExtension(filename) + " is not supported by GCodeReader. Supported formats are: " + string.Join(";", SupportedFileFormats));
            }
            CompleteJob = new Job
            {
                JobMetaData = new Job.Types.JobMetaData
                {
                    JobCreationTime = 0,
                    JobName = Path.GetFileNameWithoutExtension(filename)
                }
            };

            this.filename = filename;

            ParseGCodeFile();
        }

        public void ParseGCodeFile()
        {
            MapField<int, MarkingParams> MPsMap = new MapField<int, MarkingParams>();
            Dictionary<MarkingParams, int> cachedMP = new Dictionary<MarkingParams, int>();

            MarkingParams currentMP = new MarkingParams();
            Vector3 position = new Vector3(0, 0, 0);
            float angle = 0;
            GCodeCommandList gCodeCommands = new GCodeCommandList(File.ReadAllLines(filename));
            GCodeCommand firstGCodeCommand = gCodeCommands[0];
            GCodeCommand prevGCodeCommand = firstGCodeCommand;

            bool VBlocked = false;
            int MPKey = 0;

            currentWP = new WorkPlane
            {
                WorkPlaneNumber = 0
            };
            CompleteJob.NumWorkPlanes = 0;
            currentWP.Repeats = 0;

            currentVB = new VectorBlock
            {
                MarkingParamsKey = 0
            };

            switch (firstGCodeCommand)
            {
                case LinearInterpolationCmd linearCmd:
                    if (linearCmd.isOperation)
                    {
                        currentMP.LaserSpeedInMmPerS = (float)linearCmd.feedRate;
                    }
                    else
                    {
                        currentMP.JumpSpeedInMmS = (float)linearCmd.feedRate;
                    }

                    currentVB.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                    currentVB.LineSequence3D.Points.Add(linearCmd.xPosition ?? 0);
                    currentVB.LineSequence3D.Points.Add(linearCmd.yPosition ?? 0);
                    currentVB.LineSequence3D.Points.Add(linearCmd.zPosition ?? 0);

                    updatePosition(linearCmd);

                    MPsMap.Add(0, currentMP.Clone());

                    break;

                case CircularInterpolationCmd circularCmd:
                    currentMP.LaserSpeedInMmPerS = (float)circularCmd.feedRate;

                    currentVB.Arcs3D = new VectorBlock.Types.Arcs3D();

                    currentVB.Arcs3D.StartDx = 0;
                    currentVB.Arcs3D.StartDy = 0;
                    currentVB.Arcs3D.StartDz = 0;

                    Vector3 targetPosition = new Vector3(circularCmd.xPosition ?? 0, circularCmd.yPosition ?? 0, circularCmd.zPosition ?? 0);
                    Vector3 center = new Vector3(position.X + circularCmd.xCenterRel ?? 0, position.Y + circularCmd.yCenterRel ?? 0, position.Z);

                    currentVB.Arcs3D.Centers.Add(position.X + circularCmd.xCenterRel ?? 0);
                    currentVB.Arcs3D.Centers.Add(position.Y + circularCmd.yCenterRel ?? 0);
                    currentVB.Arcs3D.Centers.Add(position.Z);

                    updatePosition(circularCmd);

                    MPsMap.Add(0, currentMP.Clone());

                    break;

                case PauseCommand pauseCmd:
                    currentVB.ExposurePause.PauseInUs = (ulong)pauseCmd.duration * 1000;

                    break;

                case MiscCommand miscCmd:
                    break;

            }

            foreach (GCodeCommand currentGCodeCommand in gCodeCommands.Skip(1))
            {
                VBlocked = false;

                if (currentGCodeCommand is MovementCommand movementCmd)
                {
                    if (movementCmd.zPosition != null && movementCmd.zPosition != position.Z)
                    {
                        NewWorkPlane();
                    }
                    if (movementCmd is LinearInterpolationCmd linearCmd)
                    {
                        if (linearCmd.isOperation)
                        {
                            if (linearCmd.feedRate != null && currentMP.LaserSpeedInMmPerS != (float)linearCmd.feedRate)
                            {
                                if (currentMP.LaserSpeedInMmPerS != 0 && !VBlocked)
                                {
                                    NewVectorBlock();
                                }
                                currentMP.LaserSpeedInMmPerS = (float)linearCmd.feedRate;
                            }
                        }
                        else
                        {
                            if (linearCmd.feedRate != null && currentMP.JumpSpeedInMmS != (float)linearCmd.feedRate)
                            {
                                if (currentMP.JumpSpeedInMmS != 0 && !VBlocked)
                                {
                                    NewVectorBlock();
                                }
                                currentMP.JumpSpeedInMmS = (float)linearCmd.feedRate;
                            }
                        }

                        if (currentVB.LineSequence3D == null)
                        {
                            currentVB.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                        }
                        currentVB.LineSequence3D.Points.Add(linearCmd.xPosition ?? position.X);
                        currentVB.LineSequence3D.Points.Add(linearCmd.yPosition ?? position.Y);
                        currentVB.LineSequence3D.Points.Add(linearCmd.zPosition ?? position.Z);
                    }
                    else if (movementCmd is CircularInterpolationCmd circularCmd)
                    {
                        Vector3 targetPosition = new Vector3(circularCmd.xPosition ?? 0, circularCmd.yPosition ?? 0, circularCmd.zPosition ?? 0);
                        Vector3 center = new Vector3(position.X + circularCmd.xCenterRel ?? 0, position.Y + circularCmd.yCenterRel ?? 0, position.Z);
                        if (circularCmd.isClockwise)
                        {
                            angle = (float)Math.Atan2((double)center.Y - position.Y, (double)center.X - position.X);
                        }
                        else
                        {
                            angle = (float)-Math.Atan2((double)center.Y - position.Y, (double)center.X - position.X);
                        }

                        if (angle != currentVB.Arcs3D.Angle)
                        {
                            if (currentVB.Arcs3D.Angle != 0 && !VBlocked)
                            {
                                NewVectorBlock();
                            }
                            
                        }

                        if (circularCmd.feedRate != null && currentMP.LaserSpeedInMmPerS != (float)circularCmd.feedRate)
                        {
                            if (currentMP.LaserSpeedInMmPerS != 0 && VBlocked)
                            {
                                NewVectorBlock();
                            }
                            currentMP.LaserSpeedInMmPerS = (float)circularCmd.feedRate;
                        }
                        if (currentVB.Arcs3D == null)
                        {
                            currentVB.Arcs3D = new VectorBlock.Types.Arcs3D();

                            currentVB.Arcs3D.Angle = angle;

                            currentVB.Arcs3D.StartDx = position.X;
                            currentVB.Arcs3D.StartDy = position.Y;
                            currentVB.Arcs3D.StartDz = position.Z;

                            currentVB.Arcs3D.Centers.Add(position.X + circularCmd.xCenterRel ?? position.X);
                            currentVB.Arcs3D.Centers.Add(position.Y + circularCmd.yCenterRel ?? position.Y);
                            currentVB.Arcs3D.Centers.Add(position.Z);
                        }
                        else
                        {
                            currentVB.Arcs3D.Centers.Add(position.X + circularCmd.xCenterRel ?? position.X);
                            currentVB.Arcs3D.Centers.Add(position.Y + circularCmd.yCenterRel ?? position.Y);
                            currentVB.Arcs3D.Centers.Add(position.Z);
                        }
                    }

                    updatePosition(movementCmd);
                    prevGCodeCommand = currentGCodeCommand;
                }
                else if(currentGCodeCommand is PauseCommand pauseCmd)
                {
                    currentVB.ExposurePause = new VectorBlock.Types.ExposurePause();
                    currentVB.ExposurePause.PauseInUs = (ulong)pauseCmd.duration * 1000;
                }
                else if(currentGCodeCommand is ToolChangeCommand toolChangeCmd)
                {
                    // processToolchangeCmd(toolChangeCmd);
                }
                else if(currentGCodeCommand is MonitoringCommand monitoringCmd)
                {
                    // processMonitoringCmd(monitoringCmd);
                }
                else if (currentGCodeCommand is ProgramLogicsCommand programLogicsCmd)
                {
                    // processProgramLogicsCmd(programLogicsCmd);
                }
                else if (currentGCodeCommand is MiscCommand miscCmd)
                {
                    // processMiscCmd(miscCmd);
                }
                CompleteJob.MarkingParamsMap.MergeFrom(MPsMap);
            }

            _cacheState = CacheState.CompleteJobCached;

            void updatePosition(MovementCommand movementCmd)
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
                currentVB.MarkingParamsKey = NewMarkingParams();
                currentWP.VectorBlocks.Add(currentVB);
                currentWP.NumBlocks++;

                currentVB = new VectorBlock();

                VBlocked = true;
            }

            void NewWorkPlane()
            {
                NewVectorBlock();
                currentWP.ZPosInMm = position.Z;
                CompleteJob.WorkPlanes.Add(currentWP);
                CompleteJob.NumWorkPlanes++;
                currentWP = new WorkPlane
                {
                    WorkPlaneNumber = CompleteJob.NumWorkPlanes
                };
            }
        }

        public override void UnloadJobFromMemory()
        {
            CompleteJob = null;
            _cacheState = CacheState.NotCached;
        }
    }
}
