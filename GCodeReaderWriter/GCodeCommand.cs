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
using System.Net.Http.Headers;
using OpenVectorFormat.GCodeReaderWriter;

namespace OpenVectorFormat.GCodeReaderWriter
{
    // Possible preparatory function codes
    public enum PrepCode
    {
        G,
        M,
        T
    }

    // Represents a basic GCode with a preparatory function code and a code number
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
        // Number of the currently equipped tool
        int toolNumber;
    }

    public class GCodeCommand
    {
        // The GCode of the command
        public readonly GCode gCode;

        // Dictionary of all unassignable parameters in a GCode line
        private protected Dictionary<char, float> miscParams;

        // List of all recorded parameters in a GCode line
        private protected List<char> recordedParams;

        // Dicionary mapping parameter characters to their respective class variables
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

        // Is used by child classes to initialize their parameter map
        protected static void InitParameterMap() 
        {
        }

        // Iterates through all given parameter characters and assigns the according values to the respective variables
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

            // If there are still unknown parameters left, they are added to the miscParams dictionary
            this.miscParams = commandParams;
        }

        public override string ToString()
        {
            /*
            string outString = gCode.ToString();
            foreach (var param in miscParams)
            {
                outString += $" {param.Key}{param.Value}";
            }
            return outString;
            */
           return this.gCode.ToString();
        }
    }

    public abstract class MovementCommand : GCodeCommand
    {
        // Target position in mm, feedrate in mm/min and acceleration in mm/min^2 of movement commands
        internal float? xPosition;
        internal float? yPosition;
        internal float? zPosition;
        internal float? feedRate;
        internal float? acceleration;

        public MovementCommand(PrepCode prepCode, int codeNumber, float? xPosition, float? yPosition, float? zPosition, float? feedRate, float? acceleration) : base(prepCode, codeNumber)
        {
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.zPosition = zPosition;
            this.feedRate = feedRate;
            this.acceleration = acceleration;
        }

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
        // Operational move or travel move
        internal bool isOperation;

        public LinearInterpolationCmd(PrepCode prepCode, int codeNumber, float? xPosition, float? yPosition, float? zPosition = null, float? feedRate = null, float? acceleration = null) : base(prepCode, codeNumber, xPosition, yPosition, zPosition, feedRate, acceleration)
        {
            CheckOperation();
        }

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
        // Center of the circle relative to the start position in mm
        internal float? xCenterRel;
        internal float? yCenterRel;

        // Direction of the circular interpolation
        internal bool isClockwise;

        public CircularInterpolationCmd(PrepCode prepCode, int codeNumer, float? xPosition, float? yPosition, float? xCenterRel, float? yCenterRel, float? feedRate, float? acceleration) : base(prepCode, codeNumer, xPosition, yPosition, null, feedRate, acceleration)
        {
            this.xCenterRel = xCenterRel;
            this.yCenterRel = yCenterRel;
        }

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

        public override string ToString()
        {
            return base.ToString() + (xCenterRel != null ? $" I{xCenterRel}" : "") + (yCenterRel != null ? $" I{yCenterRel}" : "");
        }
    }

    internal class PauseCommand : GCodeCommand
    {
        // Duration of the pause in ms
        internal float? duration;

        public PauseCommand(PrepCode prepCode, int codeNumber, float? duration) : base(prepCode, codeNumber)
        {
            this.duration = duration;
        }

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

        public ToolChangeCommand(PrepCode prepCode, int codeNumber, ToolParams toolParams) : base(prepCode, codeNumber)
        {
            this.toolParams = toolParams;
        }

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
        public MonitoringCommand(PrepCode prepCode, int codeNumber) : base(prepCode, codeNumber)
        {
        }
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
        public ProgramLogicsCommand(PrepCode prepCode, int codeNumber) : base(prepCode, codeNumber)
        {
        }
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
        public BlockEndCmd(PrepCode prepCode, int codeNumber) : base(prepCode, codeNumber)
        {
        }
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
        public ProgramEndCmd(PrepCode prepCode, int codeNumber) : base(prepCode, codeNumber)
        {
        }

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
        public MiscCommand(PrepCode prepCode, int codeNumber) : base(prepCode, codeNumber)
        {
        }

        public MiscCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> cmdParams = null) :base(prepCode, codeNumber)
        {
            this.miscParams = cmdParams;
        }
         public MiscCommand(GCode gCode, Dictionary<char, float> cmdParams = null) : base(gCode)
        {
            this.miscParams = cmdParams;
        }
        public override string ToString()
        {
            /*
            string outString = base.ToString();
            if (this.miscParams != null) 
            {
                for (int i = 0; i < this.miscParams.Count; i++)
                {
                    outString += $" {this.miscParams.Keys.ElementAt(i)}{this.miscParams.Values.ElementAt(i)}";
                }
            }
            return outString;
            */
            return base.ToString();
        }
    }
}

public class GCodeCommandList : List<GCodeCommand>
{
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

    public GCodeCommandList(string[] commandLines)
    {
        // Set culture info, so that floats are parsed correctly
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        // Try to parse the given command lines to GCodeCommand objects
        TryParse(commandLines);
    }

    public void TryParse(string[] commandLines)
    {
        foreach (string line in commandLines)
        {
            try
            {
                this.Add(ParseLine(line));
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    public GCodeCommand ParseLine(string serializedCmdLine)
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
}
