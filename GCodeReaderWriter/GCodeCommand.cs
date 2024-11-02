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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using OpenVectorFormat.Utils;

namespace OpenVectorFormat.GCodeReaderWriter
{
    public enum PrepCode
    {
        G,
        M,
        T
    }


    public struct GCode
    {
        public readonly PrepCode preparatoryFunctionCode;
        public readonly int codeNumber;

        public GCode(PrepCode preparatoryFunctionCode, int codeNumber)
        {
            this.preparatoryFunctionCode = preparatoryFunctionCode;
            this.codeNumber = codeNumber;
        }

        public override string ToString()
        {
            return $"{preparatoryFunctionCode}{codeNumber}";
        }
    }

    /*
    public struct GCodeParameter
    {
        char parameter;
        double value;
    }
    */
    class ToolParams
    {
        int toolNumber;
    }

    internal class GCodeCommand
    {
        /*
        private Dictionary<int, VectorBlock.VectorDataOneofCase> _gCodeTranslations = new Dictionary<int, VectorBlock.VectorDataOneofCase>
        {
            {1, VectorBlock.VectorDataOneofCase.LineSequence},
        };
        */
        public readonly GCode gCode;
        private static int parameterType;

        //public readonly GCodeCommand LayerChange = new GCodeCommand(PrepCode.T, 7);

        public GCodeCommand()
        {
        }

        public GCodeCommand(GCode gCode)
        {
            this.gCode = gCode;
        }

        public GCodeCommand(PrepCode prepCode, int codeNumber)
        {
            this.gCode = new GCode(prepCode, codeNumber);
        }

        public GCodeCommand(int codeNumber)
        {
            this.gCode = new GCode(PrepCode.G, codeNumber);
        }

        public override string ToString()
        {
            return gCode.ToString();
        }
    }

    internal abstract class MovementCommand : GCodeCommand
    {
        public readonly Vector2 position;
        public readonly ToolParams toolParams;
        public readonly float? feedRate;
        public readonly float? zHeight;

        public MovementCommand(Vector2 position, ToolParams toolParams, PrepCode prepCode, int codeNumber, float? feedRate = null, float ? zHeight = null)
            : base(prepCode, codeNumber)
        {
            this.position = position;
            this.feedRate = feedRate;
            this.toolParams = toolParams;
            this.zHeight = zHeight;
        }

        public MovementCommand(Vector2 position, ToolParams toolParams, float? feedRate = null, float ? zHeight = null)
            : base()
        {
            this.position = position;
            this.feedRate = feedRate;
            this.toolParams = toolParams;
            this.zHeight = zHeight;
        }

        public override string ToString()
        {
            return base.ToString() + $" F{feedRate} X{position.X} Y{position.Y}"
                + (zHeight != null ? $" Z{zHeight}" : "");
        }
    }

    internal class LinearInterpolationCmd : MovementCommand
    {
        public readonly bool isOperation;
        public LinearInterpolationCmd(bool isOperation, Vector2 position, ToolParams toolParams, PrepCode prepCode, int codeNumber, float? feedRate = null, float? zHeight = null)
            : base(position, toolParams, prepCode, codeNumber, feedRate, zHeight)
        {
            this.isOperation = isOperation;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class CircularInterpolationCmd : MovementCommand
    {
        public readonly Vector2 centerRel;
        public readonly float angle;

        public CircularInterpolationCmd(Vector2 startPos, Vector2 targetPos, Vector2 centerRel, ToolParams toolParams, PrepCode prepCode, int codeNumber, float? feedRate = null, float? zHeight = null)
            : base(startPos, toolParams, prepCode, codeNumber, feedRate, zHeight)
        {
            this.centerRel = centerRel;
            this.angle = (float)Math.Atan2(position.Y - this.centerRel.Y, position.X - this.centerRel.X) - (float)Math.Atan2(startPos.Y - this.centerRel.Y, startPos.X - this.centerRel.X);
        }

        public CircularInterpolationCmd(Vector2 startPos, Vector2 targetPos, float radius, ToolParams toolParams, PrepCode prepCode, int codeNumber, float? feedRate = null, float? zHeight = null) 
            : base(startPos, toolParams, prepCode, codeNumber, feedRate, zHeight)
        {
            float pointDistance = (float)Math.Sqrt(Math.Pow(targetPos.X - startPos.X, 2) + Math.Pow(targetPos.Y - startPos.Y, 2));
            float centerRelX = (float)(startPos.X + radius * Math.Cos(Math.Asin(radius / pointDistance)));
            float centerRelY = (float)(startPos.Y + radius * Math.Sin(Math.Asin(radius / pointDistance)));
            this.centerRel = new Vector2(centerRelX, centerRelY);
            this.angle = (float)Math.Atan2(targetPos.Y - this.centerRel.Y, targetPos.X - this.centerRel.X) - (float)Math.Atan2(startPos.Y - this.centerRel.Y, startPos.X - this.centerRel.X);
        }

        public override string ToString()
        {
            return base.ToString() + $"I{centerRel.X} J{centerRel.Y}";
        }
    }

    internal class PauseCommand : GCodeCommand
    {
        public readonly float duration;
        public readonly bool isEstimated;

        public PauseCommand(float duration) : base(PrepCode.G, 4)
        {
            this.duration = duration;
            this.isEstimated = false;
        }

        public PauseCommand(float duration, bool isEstimated, PrepCode prepCode, int codeNumber)
            : base(prepCode, codeNumber)
        {
            this.duration = duration;
            this.isEstimated = isEstimated;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class ToolChangeCommand : GCodeCommand
    {
        public readonly ToolParams toolParams;

        public ToolChangeCommand(ToolParams toolParams) : base()
        {
            this.toolParams = toolParams;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class MonitoringCommand : GCodeCommand
    {
        public MonitoringCommand() : base()
        {
        }
        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class ProgramLogicsCommand : GCodeCommand
    {
        public ProgramLogicsCommand() : base()
        {
        }
        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class BlockEndCmd : ProgramLogicsCommand
    {
        public BlockEndCmd() : base()
        {
        }
        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class ProgramEndCmd : ProgramLogicsCommand
    {
        public ProgramEndCmd() : base()
        {
        }
        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class MiscCommand : GCodeCommand
    {
        public readonly Dictionary<string, float> cmdParams;
        public MiscCommand(PrepCode prepCode, int codeNumber) : base(prepCode, codeNumber)
        {
        }

        public MiscCommand(PrepCode prepCode, int codeNumber, Dictionary<string, float> cmdParams) :base(prepCode, codeNumber)
        {
            this.cmdParams = cmdParams;
        }
        public override string ToString()
        {
            string outString = base.ToString();
            for (int i = 0; i < cmdParams.Count; i++)
            {
               outString += $" {cmdParams.Keys.ElementAt(i)}{cmdParams.Values.ElementAt(i)}";
            }
            return outString;
        }
    }

    internal class GCodeCommandList : List<GCodeCommand>
    {
        //Hier "lookahead" implementieren. Bspw. eine Klasse CommandBlock einführen, die quasi einem VectorBlock ähnlich ist. Den CommmandBlock dann über Lookahed füllen
        // CommandBLock enthält dann eine Liste von GCodeCommands und deren Gemeinsamkeiten, also feedRate, Accel... bzw. deren Unterscheide, also z.B. positions
        public void Parse(string serializedCmds)
        {  
            ToolParams toolParams = new ToolParams();
            Vector2 lastPosition = new Vector2(0, 0);

            string[] commandLines = serializedCmds.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string commandLine in commandLines)
            {
                lastPosition = ParseLine(commandLine, lastPosition);
            }
        }

        public Vector2 ParseLine(string commandLine, Vector2 lastPosition)
        {
            commandLine = commandLine.Split(';')[0].Trim();
            string commandString = Regex.Replace(commandLine.Split(' ')[0], @"[A-Z]0(\d)", "$1"); // kann weg

            char commandChar = commandString[0];
            if (!Enum.TryParse(commandChar.ToString(), out PrepCode prepCode))
                throw new ArgumentException($"Invalid preparatory function code: {commandChar}");

            string commandNumber = commandString.Substring(1);
            if(!int.TryParse(commandNumber, out int codeNumber))
                throw new ArgumentException($"Invalid number format: {commandNumber}");

            GCode gCode = new GCode(prepCode, codeNumber);
            
            Dictionary<string, float> cmdParams = new Dictionary<string, float>();

            foreach (string cmdParam in commandLine.Split(' ').Skip(1))
            {
                cmdParams.Add(cmdParam.Substring(0, 1), float.Parse(cmdParam.Substring(1)));
            }

            GCodeCommand commandInstance = null;

            switch (gCode.preparatoryFunctionCode)
            {
                case PrepCode.G:
                    switch (gCode.codeNumber)
                    {
                        case 0:
                            commandInstance = new LinearInterpolationCmd(
                                false,
                                new Vector2(cmdParams["X"], cmdParams["Y"]),
                                new ToolParams(),
                                gCode.preparatoryFunctionCode, gCode.codeNumber,
                                cmdParams["F"]);
                            break;
                        case 1:
                            commandInstance = new LinearInterpolationCmd(
                                true,
                                new Vector2(cmdParams["X"], cmdParams["Y"]),
                                new ToolParams(),
                                gCode.preparatoryFunctionCode, gCode.codeNumber,
                                cmdParams["F"]);
                            break;
                        case 2:
                            if (cmdParams.ContainsKey("R"))
                            {
                                commandInstance = new CircularInterpolationCmd(
                                    lastPosition,
                                    new Vector2(cmdParams["X"], cmdParams["Y"]),
                                    new Vector2(cmdParams["R"], cmdParams["R"]),
                                    new ToolParams(),
                                    gCode.preparatoryFunctionCode, gCode.codeNumber,
                                    cmdParams["F"]);
                            }
                            else
                            {
                                commandInstance = new CircularInterpolationCmd(
                                    lastPosition,
                                    new Vector2(cmdParams["X"], cmdParams["Y"]),
                                    new Vector2(cmdParams["I"], cmdParams["J"]),
                                    new ToolParams(),
                                    gCode.preparatoryFunctionCode, gCode.codeNumber,
                                    cmdParams["F"]);
                            }
                            break;
                        case 3:
                            if (cmdParams.ContainsKey("R"))
                            {
                                commandInstance = new CircularInterpolationCmd(
                                    lastPosition,
                                    new Vector2(cmdParams["X"], cmdParams["Y"]),
                                    new Vector2(cmdParams["R"], cmdParams["R"]),
                                    new ToolParams(),
                                    gCode.preparatoryFunctionCode, gCode.codeNumber,
                                    cmdParams["F"]);
                            }
                            else
                            {
                                commandInstance = new CircularInterpolationCmd(
                                    lastPosition,
                                    new Vector2(cmdParams["X"], cmdParams["Y"]),
                                    new Vector2(cmdParams["I"], cmdParams["J"]),
                                    new ToolParams(),
                                    gCode.preparatoryFunctionCode, gCode.codeNumber,
                                    cmdParams["F"]);
                            }
                            break;
                        case 4:
                            commandInstance = new PauseCommand(
                                cmdParams["F"]);
                            break;
                        default:
                            commandInstance = new MiscCommand(gCode.preparatoryFunctionCode, gCode.codeNumber, cmdParams);
                            break;
                    }
                    break;
                case PrepCode.M:
                    switch(gCode.codeNumber)
                    {
                        default:
                            commandInstance = new MiscCommand(gCode.preparatoryFunctionCode, gCode.codeNumber, cmdParams);
                            break;
                    }
                    break;
                case PrepCode.T:
                    switch(gCode.codeNumber)
                    {
                        default:
                            commandInstance = new ToolChangeCommand(new ToolParams());
                            break;
                    }
                    break;
            }
            if (commandInstance != null)
            {
                Add(commandInstance);
                return new Vector2(cmdParams["X"], cmdParams["Y"]);
            }
            return lastPosition;
        }
    }
}
