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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
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

    class ToolParams
    {
        int toolNumber;
    }

    public class GCodeState
    {
        private readonly Dictionary<int, Type> _gCodeTranslations = new Dictionary<int, Type>
        {
            {0, typeof(LinearInterpolationCmd)},
            {1, typeof(LinearInterpolationCmd)},
            {2, typeof(CircularInterpolationCmd)},
            {3, typeof(CircularInterpolationCmd)},
            {4, typeof(PauseCommand)},
        };

        internal class GCodeProperties
        {
            internal Type gCodeType;
            internal List<char> recordedParams;
            internal Dictionary<char, float> miscParams;
        }

        internal class GCodeMarkingParams
        {
            // Marking parameters
            internal float workSpeed = 0;
            internal float travelSpeed = 0;
            internal float acceleration = 0;

            // Pause parameters
            internal float pauseDuration = 0;

            // Other parameters
            internal ToolParams toolParams;

            Dictionary<string, float> miscParameters = new Dictionary<string, float>();

            internal GCodeMarkingParams()
            {

            }
        }

        internal class GCodePosition
        {
            internal Vector3 position;
            internal Vector2 centerRel;
            internal GCodePosition()
            {

            }
        }

        private Dictionary<int, Type> _mCodeTranslations = new Dictionary<int, Type>();

        private Dictionary<int, Type> _tCodeTranslations = new Dictionary<int, Type>();

        public GCodeCommand gCodeCommand;
        internal Vector3 position;

        public GCodeState(string serializedCmdLine)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            this.gCodeCommand = ParseToGCodeCommand(serializedCmdLine);
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

        public bool[] Update(string serializedCmdLine)
        {
            GCodeCommand previousGCodeCommand = this.gCodeCommand;
            GCodeCommand currentGCodeCommand = ParseToGCodeCommand(serializedCmdLine);

            bool workPlaneChanged = false;
            bool markingParamsChanged = false;
            bool vectorBlockChanged = false;

            if (previousGCodeCommand.GetType() != currentGCodeCommand.GetType())
            {
                vectorBlockChanged = true;
            }
            markingParamsChanged = UpdateParameters();
            workPlaneChanged = updatePosition();

            bool UpdateParameters()
            {
                bool parametersChanged = false;
                foreach (var property in previousGCodeCommand.GetType().GetProperties())
                {
                    var previousValue = property.GetValue(previousGCodeCommand);
                    var currentValue = property.GetValue(currentGCodeCommand);

                    if (!Equals(previousValue, currentValue))
                    {
                        property.SetValue(currentGCodeCommand, previousValue);
                        parametersChanged = (property.Name != "xPosition" && property.Name != "yPosition" && property.Name != "zPosition");
                    }
                }
                
                return parametersChanged;
            }

            bool updatePosition()
            {
                bool zChange = false;
                if (currentGCodeCommand is MovementCommand movementCmd)
                {
                    try
                    {
                        if (movementCmd.zPosition != position.Z)
                        {
                            zChange = true;
                        }
                        position = new Vector3((float)movementCmd.xPosition, (float)movementCmd.yPosition, (float)movementCmd.zPosition);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException($"Unable to update position in line '{serializedCmdLine}'. Creating a Vector object from command was not possible.");
                    }
                }
                return zChange;
            }

            this.gCodeCommand = currentGCodeCommand;

            return new bool [] { workPlaneChanged, markingParamsChanged, vectorBlockChanged};
        }
    }

    public class GCodeCommand
    {
        public readonly GCode gCode;

        private protected Dictionary<char, float> miscParams;
        private protected List<char> recordedParams;
        protected static Dictionary<char, Action<float>> parameterMap;

        public GCodeCommand(GCode gCode, Dictionary<char, float> commandParams = null)
        {
            miscParams = new Dictionary<char, float>();
            recordedParams = new List<char>();
            parameterMap = new Dictionary<char, Action<float>>();
            this.gCode = gCode;
        }

        public GCodeCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null)
        {
            miscParams = new Dictionary<char, float>();
            recordedParams = new List<char>();
            parameterMap = new Dictionary<char, Action<float>>();
            this.gCode = new GCode(prepCode, codeNumber);
        }

        public GCodeCommand(GCode gCode)
        {
            this.gCode = gCode;
            miscParams = new Dictionary<char, float>();
            recordedParams = new List<char>();
            parameterMap = new Dictionary<char, Action<float>>();
        }

        public GCodeCommand(PrepCode prepCode, int codeNumber)
        {
            this.gCode = new GCode(prepCode, codeNumber);
            miscParams = new Dictionary<char, float>();
            recordedParams = new List<char>();
            parameterMap = new Dictionary<char, Action<float>>();
        }

        protected static void InitMaps()
        {

        }

        protected static void InitParameterMap() 
        {
        }

        protected void ParseParams(Dictionary<char, float> commandParams)
        {
            foreach (var commandParam in commandParams)
            {
                if (parameterMap.ContainsKey(commandParam.Key))
                {
                    parameterMap[commandParam.Key](commandParam.Value);
                    recordedParams.Add(commandParam.Key);
                    commandParams.Remove(commandParam.Key);
                }
            }

            this.miscParams = commandParams;
        }

        public override string ToString()
        {
            string outString = gCode.ToString();
            foreach (var param in miscParams)
            {
                outString += $" {param.Key}{param.Value}";
            }
            return outString;
        }
    }

    public abstract class MovementCommand : GCodeCommand
    {
        internal float? xPosition;
        internal float? yPosition;
        internal float? zPosition;
        internal float? feedRate;
        internal float? acceleration;

        public MovementCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null) : base(prepCode, codeNumber)
        {
            InitParameterMap();
            if (commandParams != null) 
            {
                ParseParams(commandParams);
            }
        }

        public MovementCommand(GCode gCode, Dictionary<char, float> commandParams = null) : base(gCode)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public MovementCommand(Dictionary<char, float> commandParams = null) : base(PrepCode.G, 1)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }


        private new void InitParameterMap()
        {
            parameterMap.Add('X', (float x) => xPosition = x);
            parameterMap.Add('Y', (float y) => yPosition = y);
            parameterMap.Add('Z', (float z) => zPosition = z);
            parameterMap.Add('F', (float f) => feedRate = f);
        }

        public override string ToString()
        {
            return base.ToString() + (feedRate != null ? $" F{feedRate}" : "") + (xPosition != null ? $" X{xPosition}" : "") + (yPosition != null ? $" Y{yPosition}" : "")
                + (zPosition != null ? $" Z{zPosition}" : "");
        }
    }

    public class LinearInterpolationCmd : MovementCommand
    {
        internal bool isOperation;

        public LinearInterpolationCmd(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null) : base(prepCode, codeNumber)
        {
            CheckOperation();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public LinearInterpolationCmd(GCode gCode, Dictionary<char, float> commandParams = null) : base(gCode)
        {
            CheckOperation();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        private void CheckOperation()
        {
            if (this.gCode.codeNumber == 0 || this.gCode.codeNumber == 1)
            {
                this.isOperation = this.gCode.codeNumber == 1;
            }
            else
            {
                throw new ArgumentException($"Invalid code number for linear interpolation: {this.gCode.codeNumber} in line '{this.ToString()}'");
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class CircularInterpolationCmd : MovementCommand
    {
        internal float? xCenterRel;
        internal float? yCenterRel;

        internal bool isClockwise;

        public CircularInterpolationCmd(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null) : base(prepCode, codeNumber)
        {
            CheckDirection();
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public CircularInterpolationCmd(GCode gCode, Dictionary<char, float> commandParams = null) : base(gCode)
        {
            CheckDirection();
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        private new void InitParameterMap()
        {
            parameterMap.Add('I', (float i) => xCenterRel = i);
            parameterMap.Add('J', (float j) => yCenterRel = j);
        }

        private void CheckDirection()
        {
            if (this.gCode.codeNumber == 2 || this.gCode.codeNumber == 3)
            {
                this.isClockwise = this.gCode.codeNumber == 2;
            }
            else
            {
                throw new ArgumentException($"Invalid code number for circular interpolation: {this.gCode.codeNumber} in line '{this.ToString()}'");
            }
        }

        // Move angle calculation below to gcode state object or to transition point of OVF-Parameters
        // this.angle = (float) Math.Atan2((double)(yPosition - yCenterRel),(double) (xPosition - xCenterRel)) - (float) Math.Atan2((double)(yStartPos - yCenterRel), (double) (xStartPos - xCenterRel));


        /*
        public CircularInterpolationCmd(Vector2 startPos, Vector2 targetPos, float radius, ToolParams toolParams, PrepCode prepCode, int codeNumber, float? feedRate = null, float? zHeight = null) 
            : base(startPos, toolParams, prepCode, codeNumber, feedRate, zHeight)
        {
            float pointDistance = (float)Math.Sqrt(Math.Pow(targetPos.X - startPos.X, 2) + Math.Pow(targetPos.Y - startPos.Y, 2));
            float centerRelX = (float)(startPos.X + radius * Math.Cos(Math.Asin(radius / pointDistance)));
            float centerRelY = (float)(startPos.Y + radius * Math.Sin(Math.Asin(radius / pointDistance)));
            this.centerRel = new Vector2(centerRelX, centerRelY);
            this.angle = (float)Math.Atan2(targetPos.Y - this.centerRel.Y, targetPos.X - this.centerRel.X) - (float)Math.Atan2(startPos.Y - this.centerRel.Y, startPos.X - this.centerRel.X);
        }
        */
        public override string ToString()
        {
            return base.ToString() + (xCenterRel != null ? $" I{xCenterRel}" : "") + (yCenterRel != null ? $" I{yCenterRel}" : "");
        }
    }

    internal class PauseCommand : GCodeCommand
    {
        internal float? duration;

        public PauseCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null)
            : base(prepCode, codeNumber)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public PauseCommand(GCode gCode, Dictionary<char, float> commandParams = null) : base(gCode)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public PauseCommand(Dictionary<char, float> commandParams = null) : base(PrepCode.G, 4)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class ToolChangeCommand : GCodeCommand
    {
        public readonly ToolParams toolParams;

        public ToolChangeCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null) : base(prepCode, codeNumber)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class MonitoringCommand : GCodeCommand
    {
        public MonitoringCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null) : base(prepCode, codeNumber)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class ProgramLogicsCommand : GCodeCommand
    {
        public ProgramLogicsCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null) : base(prepCode, codeNumber)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public ProgramLogicsCommand(GCode gCode, Dictionary<char, float> commandParams = null) : base(gCode)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class BlockEndCmd : ProgramLogicsCommand
    {
        public BlockEndCmd(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null) : base(prepCode, codeNumber)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public BlockEndCmd(GCode gCode, Dictionary<char, float> commandParams = null) : base(gCode)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }


        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class ProgramEndCmd : ProgramLogicsCommand
    {
        public ProgramEndCmd(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null) : base(prepCode, codeNumber)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public ProgramEndCmd(GCode gCode, Dictionary<char, float> commandParams = null) : base(gCode)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    internal class MiscCommand : GCodeCommand
    {
        public readonly Dictionary<string, float> cmdParams;

        public MiscCommand(PrepCode prepCode, int codeNumber, Dictionary<string, float> cmdParams = null) :base(prepCode, codeNumber)
        {
            this.cmdParams = cmdParams;
        }
         public MiscCommand(GCode gCode, Dictionary<string, float> cmdParams = null) : base(gCode)
        {
            this.cmdParams = cmdParams;
        }
        public override string ToString()
        {
            string outString = base.ToString();
            if (cmdParams != null) {
                for (int i = 0; i < cmdParams.Count; i++)
                {
                    outString += $" {cmdParams.Keys.ElementAt(i)}{cmdParams.Values.ElementAt(i)}";
                }
            }
            return outString;
        }
    }
}
