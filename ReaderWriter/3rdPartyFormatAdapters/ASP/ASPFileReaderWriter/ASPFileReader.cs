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



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;
using OpenVectorFormat;

namespace OpenVectorFormat.ASPFileReaderWriter
{
    public class ASPFileReader : FileReader
    {
        private WorkPlane _workPlane;
        private VectorBlock _currentVectorBlock;
        private CacheState _cacheState = CacheState.NotCached;
        private string _filename;

        /// <inheritdoc/>
        public new static List<string> SupportedFileFormats { get; } = new List<string>() { ".asp" };

        /// <inheritdoc/>
        public override CacheState CacheState => _cacheState;

        public Job CompleteJob { get; private set; }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override void OpenJob(string filename, IFileReaderWriterProgress progress)
        {
            if (!SupportedFileFormats.Contains(Path.GetExtension(filename)))
            {
                throw new Exception(Path.GetExtension(filename) + " is not supported by ASPFileReader. Supported formats are: " + string.Join(";", SupportedFileFormats));
            }
            CompleteJob = new Job();
            DateTime fileCreationTime = File.GetCreationTime(filename);
            CompleteJob.JobMetaData = new Job.Types.JobMetaData
            {
                JobCreationTime = 0,
                JobName = Path.GetFileNameWithoutExtension(filename)
            };

            _filename = filename;

            ParseASPFile();
        }

        /// <inheritdoc/>
        public override Job CacheJobToMemory()
        {
            if (_cacheState == CacheState.CompleteJobCached)
            {
                return CompleteJob;
            }
            else if (File.Exists(_filename))
            {
                ParseASPFile();
                return CompleteJob;
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        /// <inheritdoc/>
        public override void UnloadJobFromMemory()
        {
            CompleteJob = null;
            _cacheState = CacheState.NotCached;
        }

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            if (CompleteJob.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane " + i_workPlane.ToString() + " out of range for jobfile with " + CompleteJob.NumWorkPlanes.ToString() + " workPlanes!");
            }

            if (CacheState == CacheState.CompleteJobCached)
            {
                return CompleteJob.WorkPlanes[i_workPlane].CloneWithoutVectorData();
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override void Dispose()
        {
            UnloadJobFromMemory();
        }
        public override void CloseFile()
        {
            UnloadJobFromMemory();
        }

        private void ParseASPFile()
        {
            _workPlane = new WorkPlane
            {
                WorkPlaneNumber = 0
            };
            CompleteJob.NumWorkPlanes = 1;
            _workPlane.Repeats = 0;

            MarkingParams _currentMarkingParams = new MarkingParams();

            _currentVectorBlock = new VectorBlock
            {
                MarkingParamsKey = 0
            };

            CommandList cmdList = new CommandList();
            cmdList.Parse(File.ReadAllText(_filename));
            Queue<Command> cmdQueue = new Queue<Command>(cmdList);
            Command lastCmd = null;
            double[] lastPt = new double[3] { 0, 0, 0 };
            while (cmdQueue.Count > 0)
            {
                Command cmd = cmdQueue.Dequeue();
                switch (cmd.Type)
                {

                    case CommandType.LP:
                        {
                            NewVectorBlock();
                            _currentMarkingParams.LaserPowerInW = (float)cmd.Parameters[0];
                            break;
                        }
                    case CommandType.VG:
                        {
                            NewVectorBlock();
                            _currentMarkingParams.LaserSpeedInMmPerS = (float)cmd.Parameters[0];
                            break;
                        }
                    case CommandType.VJ:
                        {
                            NewVectorBlock();
                            _currentMarkingParams.JumpSpeedInMmS = (float)cmd.Parameters[0];
                            break;
                        }
                    case CommandType.DS:
                        {
                            NewVectorBlock();
                            _currentMarkingParams.JumpDelayInUs = (float)cmd.Parameters[0];
                            _currentMarkingParams.MarkDelayInUs = (float)cmd.Parameters[1];
                            _currentMarkingParams.PolygonDelayInUs = (float)cmd.Parameters[2];
                            break;
                        }
                    case CommandType.DL:
                        {
                            NewVectorBlock();
                            _currentMarkingParams.LaserOnDelayInUs = (float)cmd.Parameters[0];
                            _currentMarkingParams.LaserOffDelayInUs = (float)cmd.Parameters[1];
                            break;
                        }
                    case CommandType.PA:
                        {
                            NewVectorBlock();
                            _currentMarkingParams.TimeLagInUs = (float)cmd.Parameters[0];
                            _currentMarkingParams.LaserOnShiftInUs = (float)cmd.Parameters[1];
                            _currentMarkingParams.NPrevInUs = (float)cmd.Parameters[2];
                            _currentMarkingParams.NPostInUs = (float)cmd.Parameters[3];
                            break;
                        }
                    case CommandType.LI:
                        {
                            NewVectorBlock();
                            _currentMarkingParams.Limit = (float)cmd.Parameters[0];
                            break;
                        }
                    case CommandType.MO:
                        {
                            NewVectorBlock();
                            switch(cmd.Parameters[0])
                            {
                                case 0:
                                    {
                                        _currentMarkingParams.MarkingMode = MarkingParams.Types.MarkingMode.NoSky;
                                        break;
                                    }
                                case 1:
                                    {
                                        _currentMarkingParams.MarkingMode = MarkingParams.Types.MarkingMode.Sky1;
                                        break;
                                    }
                                case 2:
                                    {
                                        _currentMarkingParams.MarkingMode = MarkingParams.Types.MarkingMode.Sky2;
                                        break;
                                    }
                                case 3:
                                    {
                                        _currentMarkingParams.MarkingMode = MarkingParams.Types.MarkingMode.Sky3;
                                        break;
                                    }
                                default:
                                    {
                                        _currentMarkingParams.MarkingMode = MarkingParams.Types.MarkingMode.NoSky;
                                        break;
                                    }
                            }
                            break;
                        }
                    case CommandType.ON:
                        {
                            if (lastCmd == null || lastCmd.Type != CommandType.ON || _currentMarkingParams.PointExposureTimeInUs != (float)cmd.Parameters[0])
                            {
                                NewVectorBlock();
                                _currentMarkingParams.PointExposureTimeInUs = (float)cmd.Parameters[0];
                                _currentVectorBlock.PointSequence3D = new VectorBlock.Types.PointSequence3D();
                            }
                            _currentVectorBlock.PointSequence3D.Points.Add(Array.ConvertAll(lastPt, item => (float)item));
                            break;
                        }
                    case CommandType.JP:
                        {
                            if (cmd.Parameters == lastPt)
                            {
                                break;
                            }
                            NewVectorBlock();
                            lastPt = cmd.Parameters;
                            break;
                        }
                    case CommandType.GO:
                        {
                            bool insertJumpAtBeginning = false;
                            if (lastCmd == null || lastCmd.Type != CommandType.GO)
                            {
                                NewVectorBlock();

                                insertJumpAtBeginning = true;
                            }

                            // if next commnad is another GO, create a LineSequence and parse all following GO commands.
                            if (cmdQueue.Peek().Type == CommandType.GO)
                            {
                                if (insertJumpAtBeginning)
                                {
                                    _currentVectorBlock.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                                    _currentVectorBlock.LineSequence3D.Points.Add(Array.ConvertAll(lastPt, item => (float)item));
                                }

                                _currentVectorBlock.LineSequence3D.Points.Add(Array.ConvertAll(cmd.Parameters, item => (float)item));

                                while (cmdQueue.Count > 0 && cmdQueue.Peek().Type == CommandType.GO)
                                {
                                    cmd = cmdQueue.Dequeue();
                                    _currentVectorBlock.LineSequence3D.Points.Add(Array.ConvertAll(cmd.Parameters, item => (float)item));
                                }
                            }
                            else if (cmdQueue.Peek().Type != CommandType.GO)
                            {
                                if (insertJumpAtBeginning)
                                {
                                    _currentVectorBlock.Hatches3D = new VectorBlock.Types.Hatches3D();
                                    _currentVectorBlock.Hatches3D.Points.Add(Array.ConvertAll(lastPt, item => (float)item));
                                }

                                _currentVectorBlock.Hatches3D.Points.Add(Array.ConvertAll(cmd.Parameters, item => (float)item));

                                // see if there are more hatchlines coming and parse them.
                                while (
                                     cmdQueue.Count >= 2 && // another hatchline takes commands: JP & GO.
                                     cmdQueue.Peek().Type == CommandType.JP && cmdQueue.ElementAt<Command>(1).Type == CommandType.GO && // check that the next two commands are JP and GO
                                     (cmdQueue.Count == 2 || cmdQueue.ElementAt<Command>(2).Type != CommandType.GO)) // check if this is either the end of the job, or if the command after the next GO is another GO - in this case, the next object is not a hatchline, but a LineSequence.
                                {
                                    cmd = cmdQueue.Dequeue();
                                    _currentVectorBlock.Hatches3D.Points.Add(Array.ConvertAll(cmd.Parameters, item => (float)item));
                                    cmd = cmdQueue.Dequeue();
                                    _currentVectorBlock.Hatches3D.Points.Add(Array.ConvertAll(cmd.Parameters, item => (float)item));
                                }
                            }
                            lastPt = cmd.Parameters;
                            break;
                        }
                    default:
                        {
                            throw new InvalidOperationException("Command " + cmd.Type.ToString() + " is not supported in by parser!");
                        }
                }
                lastCmd = cmd;
            }
            NewVectorBlock();
            CompleteJob.WorkPlanes.Add(_workPlane);
            _cacheState = CacheState.CompleteJobCached;

            /// <summary>
            /// Creates new vector block and new <see cref="MarkingParams"/> cloned from the previous block.
            /// </summary>
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
    }
}
