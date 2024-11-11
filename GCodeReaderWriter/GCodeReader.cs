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

namespace OpenVectorFormat.GCodeReaderWriter
{
    public class GCodeReader : FileReader
    {
        private WorkPlane workPlane;
        private VectorBlock currentVectorBlock;
        private IFileReaderWriterProgress progress;
        private CacheState _cacheState = CacheState.NotCached;
        private Job job;
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
                    ProtoUtils.CopyWithExclude(CompleteJob, jobShell, new List<int> { Job .WorkPlanesFieldNumber });
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
            if ( CompleteJob.NumWorkPlanes < i_workPlane )
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
            fileLoadingFinished = false;
            _cacheState = CacheState.NotCached;
            job = new Job();
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
        }

        private void ParseGCodeFile()
        {
            workPlane = new WorkPlane
            {
                WorkPlaneNumber = 0,
                ZPosInMm = 0
            };
            CompleteJob.NumWorkPlanes = 1;
            workPlane.Repeats = 0;

            MarkingParams _currentMarkingParams = new MarkingParams();

            currentVectorBlock = new VectorBlock
            {
                MarkingParamsKey = 0
            };

            string[] commandLines = File.ReadAllLines(filename);

            GCodeState gCodeState = new GCodeState(commandLines[0]);
            foreach (string commandLine in commandLines.Skip(1))
            {
                bool[] objectUpdates = gCodeState.Update(commandLine);
                bool workPlaneChanged = objectUpdates[0], markingParamsChanged = objectUpdates[1], vectorBlockChanged = objectUpdates[2];

                if (markingParamsChanged)
                {
                    GCodeCommand gCodeCommand = gCodeState.gCodeCommand;
                    //check if markingParams are already in the map
                    foreach (var markingParams in CompleteJob.MarkingParamsMap.Values)
                    {
                        object[] relevantParams = new object[] { markingParams.LaserSpeedInMmPerS, markingParams.JumpSpeedInMmS, };
                        if (gCodeCommand is LinearInterpolationCmd linearCmd &&linearCmd.isOperation)
                        {
                            if(markingParams.LaserSpeedInMmPerS == linearCmd.feedRate)
                            {
                                markingParamsChanged = false;
                            }
                        }
                        foreach (var markingParam in markingParams.GetType().GetProperties())
                        {
                            switch (markingParam.Name)
                            {
                                case "LaserSpeedInMmPerS":
                                    break;

                            }
                        }
                    }
                    
                }
                if (workPlaneChanged)
                {
                    workPlane = new WorkPlane
                    {
                        WorkPlaneNumber = workPlane.WorkPlaneNumber + 1,
                        ZPosInMm = gCodeState.position.Z
                    };
                    NewVectorBlock();
                    vectorBlockChanged = false;
                }
                if (vectorBlockChanged)
                {
                    NewVectorBlock();
                }
                // Add coordinates to the current vector block, clculate angles and centers from positions of arcs

            }
            _cacheState = CacheState.CompleteJobCached;

            void NewVectorBlock()
            {
                // if there is no geometry in the block, do not create new one.
                if (currentVectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.None)
                {
                    return;
                }

                int paramMapKey = currentVectorBlock.MarkingParamsKey;

                if (CompleteJob.MarkingParamsMap.Count == 0)
                {
                    CompleteJob.MarkingParamsMap.Add(paramMapKey, _currentMarkingParams.Clone());
                }
                else if (!_currentMarkingParams.Equals(CompleteJob.MarkingParamsMap[paramMapKey]))
                {
                    CompleteJob.MarkingParamsMap.Add(++paramMapKey, _currentMarkingParams.Clone());
                    currentVectorBlock.MarkingParamsKey = paramMapKey;
                }

                workPlane.VectorBlocks.Add(currentVectorBlock);
                workPlane.NumBlocks++;

                currentVectorBlock = new VectorBlock
                {
                    MarkingParamsKey = paramMapKey
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
