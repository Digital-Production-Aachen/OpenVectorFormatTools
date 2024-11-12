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


            List<MarkingParams> gCodeMarkingParams = new List<MarkingParams>();
            currentVectorBlock = new VectorBlock
            {
                MarkingParamsKey = 0
            };

            //GCodeCommandList gCodeCommands = new GCodeCommandList(File.ReadAllLines(filename));
            string[] gCodeCommands = File.ReadAllLines(filename);
            // Split list into segments of same Type. Then use parsing of gcode state and potentially split further.
            GCodeState currentGCodeState = new GCodeState(gCodeCommands[0]);

            foreach (string gCodeCommand in gCodeCommands)
            {
                currentGCodeState.Update(gCodeCommand);
            }

                
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

        public class GCodeState
        {
            /*
            internal class GCodeMarkingParams
            {
                internal float LaserSpeedInMmPerS;
                internal float JumpSpeedInMmS;
                internal float AccelerationInMmPerSSquared;
                internal string name;
                internal Dictionary<char, float> miscParams;

                public GCodeMarkingParams(float operationSpeed, float travelSpeed, float acceleration, string name)
                {
                    this.LaserSpeedInMmPerS = operationSpeed;
                    this.JumpSpeedInMmS = travelSpeed;
                    this.AccelerationInMmPerSSquared = acceleration;
                    this.name = name;
                }
            }
            */
            private readonly Dictionary<int, Type> _gCodeTranslations = new Dictionary<int, Type>
        {
            {0, typeof(LinearInterpolationCmd)},
            {1, typeof(LinearInterpolationCmd)},
            {2, typeof(CircularInterpolationCmd)},
            {3, typeof(CircularInterpolationCmd)},
            {4, typeof(PauseCommand)},
        };

            private Dictionary<int, Type> _mCodeTranslations = new Dictionary<int, Type>();

            private Dictionary<int, Type> _tCodeTranslations = new Dictionary<int, Type>();

            public GCodeCommand gCodeCommand;
            public Vector3 position { internal set; get; }
            public MarkingParams currentGCodeMarkingParams { internal set; get; }

            public GCodeState(string serializedCmdLine)
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                this.gCodeCommand = ParseToGCodeCommand(serializedCmdLine);
                if (this.gCodeCommand is MovementCommand movementCmd)
                {
                    position = new Vector3(movementCmd.xPosition ?? 0, movementCmd.yPosition ?? 0, movementCmd.zPosition ?? 0);
                }
                // potentially use 'this.Update(serializedCmdLine);' This though repeats the gCode assignment. Find better solution.
            }

            public GCodeCommand ParseToGCodeCommand(string serializedCmdLine)
            {
                string commandString = serializedCmdLine.Split(';')[0].Trim();
                string[] commandArr = commandString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                char commandChar = char.ToUpper(commandArr[0][0]);
                if (!Enum.TryParse(commandChar.ToString(), out PrepCode prepCode))
                    throw new ArgumentException($"Invalid preparatory function code: {commandChar} in line '{serializedCmdLine}'");

                string commandNumber = commandArr[0].Substring(1);
                if (!int.TryParse(commandNumber, out int codeNumber))
                    throw new ArgumentException($"Invalid number format: {commandNumber} in line '{serializedCmdLine}'");

                Dictionary<char, float> commandParams = new Dictionary<char, float>();
                Console.WriteLine(string.Join(Environment.NewLine, commandArr));

                foreach (var commandParam in commandArr.Skip(1))
                {
                    if (float.TryParse(commandParam.Substring(1), out float paramValue))
                    {
                        commandParams[commandParam[0]] = paramValue;
                    }
                    else if (commandParam.Length == 1)
                    {
                        commandParams[commandParam[0]] = 0;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid command parameter format: {commandParam} in line '{serializedCmdLine}'. Command parameters must be of format <char><float>");
                    }
                }
                Console.WriteLine(string.Join(Environment.NewLine, commandParams));
                if (_gCodeTranslations.TryGetValue(codeNumber, out Type gCodeClassType))
                {
                    return Activator.CreateInstance(gCodeClassType, new Object[] { prepCode, codeNumber, commandParams }) as GCodeCommand;
                }

                return Activator.CreateInstance(typeof(MiscCommand), new Object[] { prepCode, codeNumber, commandParams }) as GCodeCommand;
            }

            public void ParseMarkingParams(GCodeCommand gCodeCommand)
            {
                this.currentGCodeMarkingParams = new MarkingParams();
                if (gCodeCommand is MovementCommand movementCmd)
                {
                    this.currentGCodeMarkingParams.LaserSpeedInMmPerS = movementCmd.feedRate ?? 0;
                    this.currentGCodeMarkingParams.JumpSpeedInMmS = movementCmd.feedRate ?? 0;
                    //this.currentGCodeMarkingParams.AccelerationInMmPerSSquared = movementCmd.acceleration ?? 0;
                }
                else if (gCodeCommand is PauseCommand pauseCmd)
                {
                    this.currentGCodeMarkingParams.LaserSpeedInMmPerS = 0;
                    this.currentGCodeMarkingParams.JumpSpeedInMmS = 0;
                    //this.currentGCodeMarkingParams.AccelerationInMmPerSSquared = 0;
                }

                // Add logic so if a set is found where jumpspeed or work speed are same, the set is added to the list of marking params
            }

            public bool[] Update(string serializedCmdLine)
            {
                GCodeCommand previousGCodeCommand = this.gCodeCommand;
                GCodeCommand currentGCodeCommand = ParseToGCodeCommand(serializedCmdLine);

                bool vectorBlockChanged = previousGCodeCommand.GetType() != currentGCodeCommand.GetType();
                bool markingParamsChanged = UpdateParameters();
                bool positionChanged = updatePosition();

                bool UpdateParameters()
                {
                    bool parametersChanged = false;
                    foreach (var property in previousGCodeCommand.GetType().GetProperties())
                    {
                        var previousValue = property.GetValue(previousGCodeCommand);
                        var currentValue = property.GetValue(currentGCodeCommand);
                        var propName = property.Name;
                        Dictionary<string, object> currentGCodeMarkingParams = new Dictionary<string, object>();

                        if (!Equals(previousValue, currentValue))
                        {
                            property.SetValue(currentGCodeCommand, previousValue);
                            if (propName != "xPosition" && propName != "yPosition" && propName != "zPosition")
                            {
                                //gCodeMarkingParams.Add(propName, currentValue);
                                parametersChanged = true;
                            }
                        }
                    }

                    return parametersChanged;
                }

                bool updatePosition()
                {
                    bool newPosition = false;
                    if (currentGCodeCommand is MovementCommand movementCmd)
                    {
                        newPosition = (movementCmd.xPosition != null && movementCmd.xPosition != position.X) || (movementCmd.yPosition != null && movementCmd.yPosition != position.Y) || (movementCmd.zPosition != null && movementCmd.zPosition != position.Z);
                    }
                    return newPosition;
                }

                this.gCodeCommand = currentGCodeCommand;

                return new bool[] { positionChanged, markingParamsChanged, vectorBlockChanged };
            }
        }

        public override void UnloadJobFromMemory()
        {
            CompleteJob = null;
            _cacheState = CacheState.NotCached;
        }
    }
}
