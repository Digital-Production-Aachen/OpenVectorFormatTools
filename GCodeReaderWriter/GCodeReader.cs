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
        private WorkPlane _workPlane;
        private VectorBlock _currentVectorBlock;
        private CacheState _cacheState = CacheState.NotCached;
        private string _filename;

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
            else if (File.Exists(_filename))
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

            _filename = filename;
        }

        private void ParseGCodeFile()
        {
            Dictionary<int, MarkingParams> _markingParamsMap = new Dictionary<int, MarkingParams>
            {
                // {LaserSpeedInMmPerSec, LinearInterpolationCommand.feedRate where isOperation = true}
                // {JumpSpeedInMmPerSec, LinearInterpolationCommand.feedRate where isOperation = false}
                // {FurtherMarkingParams, Acceleration}
                // {FurtherMarkingParams, ToolParams}
                // {FurtherMarkingParams, RemainingParameters}
                // {FurtherMarkingParams, RecordedParameters}
            };
        
            Dictionary<Type, Func<VectorBlock.VectorDataOneofCase>> vectorDataMap = new Dictionary<Type, Func<VectorBlock.VectorDataOneofCase>>
            {   
                //{typeof(LinearInterpolationCmd), () => new LinearInterpolationCmd()},
                //{typeof(CircularInterpolationCmd), () => new CircularInterpolationCmd()},
                //{typeof(PauseCommand), () => new PauseCommand()},
                //{typeof(MiscCommand), () => new MiscCommand()}
            };
            _workPlane = new WorkPlane
            {
                WorkPlaneNumber = 0,
                ZPosInMm = 0
            };
            CompleteJob.NumWorkPlanes = 1;
            _workPlane.Repeats = 0;

            MarkingParams _currentMarkingParams = new MarkingParams();

            _currentVectorBlock = new VectorBlock
            {
                MarkingParamsKey = 0
            };

            string[] commandLines = File.ReadAllLines(_filename);

            GCodeState gCodeState = new GCodeState(commandLines[0]);
            foreach (string commandLine in commandLines.Skip(1))
            {
                if (gCodeState.Update(commandLine))
                {
                    switch (gCodeState.gCodeCommand)
                    {
                        case LinearInterpolationCmd linearCmd:
                            break;

                        case CircularInterpolationCmd circularCmd:
                            break;

                        case PauseCommand pauseCmd:
                            break;

                        case MiscCommand miscCommand:
                            break;
                    }
                }
                else
                {
                    switch (gCodeState.gCodeCommand)
                    {
                        case LinearInterpolationCmd linearCmd:
                            _currentVectorBlock.LineSequence.Points.Add((float)linearCmd.xPosition);
                            _currentVectorBlock.LineSequence.Points.Add((float)linearCmd.yPosition);
                            break;

                        case CircularInterpolationCmd circularCmd:
                            _currentVectorBlock.Arcs.Centers.Add((float)circularCmd.xCenterRel);
                            _currentVectorBlock.Arcs.Centers.Add((float)circularCmd.yCenterRel);
                            break;

                        case PauseCommand pauseCmd:
                            _currentVectorBlock.ExposurePause.PauseInUs = (ulong)pauseCmd.duration*1000;
                            break;

                        case MiscCommand miscCommand:
                            //_currentMarkingParams._unknownFields
                            break;
                    }
                    
                }
                
            }
            _cacheState = CacheState.CompleteJobCached;

            void NewVectorBlock()
            {
                // if there is no geometry in the block, do not create new one.
                if (_currentVectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.None)
                {
                    return;
                }

                int paramMapKey = _currentVectorBlock.MarkingParamsKey;

                if (CompleteJob.MarkingParamsMap.Count == 0)
                {
                    CompleteJob.MarkingParamsMap.Add(paramMapKey, _currentMarkingParams.Clone());
                }
                else if (!_currentMarkingParams.Equals(CompleteJob.MarkingParamsMap[paramMapKey]))
                {
                    CompleteJob.MarkingParamsMap.Add(++paramMapKey, _currentMarkingParams.Clone());
                    _currentVectorBlock.MarkingParamsKey = paramMapKey;
                }

                _workPlane.VectorBlocks.Add(_currentVectorBlock);
                _workPlane.NumBlocks++;

                _currentVectorBlock = new VectorBlock
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
