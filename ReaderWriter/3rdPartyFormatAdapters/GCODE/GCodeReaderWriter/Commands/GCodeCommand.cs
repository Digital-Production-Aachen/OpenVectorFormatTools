/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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

namespace GCodeReaderWriter.Commands
{
    // Possible preparatory function codes
    public enum PrepCode
    {
        G,
        M,
        T,
        Comment
    }

    // Represents a basic GCode with a preparatory function code and a code number
    public readonly struct GCode
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

    public class ToolParams
    {
        // Number of the currently equipped tool
        int toolNumber;
    }

    public abstract class GCodeCommand
    {
        // The GCode of the command
        public readonly GCode gCode;

        public readonly string comment;

        // Dictionary of all unassignable parameters in a GCode line
        public Dictionary<char, float> miscParams;

        // List of all recorded parameters in a GCode line
        public readonly List<char> recordedParams;

        // Dicionary mapping parameter characters to their respective class variables
        protected static Dictionary<char, Action<float>> parameterMap;

        public GCodeCommand(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null)
        {
            miscParams = new Dictionary<char, float>();
            recordedParams = new List<char>();
            parameterMap = new Dictionary<char, Action<float>>();
            this.gCode = gCode;
            this.comment = comment;
        }

        public GCodeCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null)
        {
            miscParams = new Dictionary<char, float>();
            recordedParams = new List<char>();
            parameterMap = new Dictionary<char, Action<float>>();
            gCode = new GCode(prepCode, codeNumber);
            this.comment = comment;
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
            miscParams = commandParams;
        }

        public override string ToString()
        {
           return gCode.ToString();
        }
    }    

    public class GCodeConverter
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

        public GCodeConverter()
        {
            // Set culture info, so that floats are parsed correctly
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        public GCodeCommand ParseLine(string serializedCmdLine)
        {
            string[] commentSplit = serializedCmdLine.Split(';');
            string commandString = commentSplit[0].Trim();
            string commentString = commentSplit.Length > 1 ? commentSplit[1].Trim() : null;

            if (string.IsNullOrEmpty(commandString))
            {
                if (string.IsNullOrEmpty(commentString))
                {
                    return null;
                }
                return Activator.CreateInstance(typeof(MiscCommand), new object[] { PrepCode.Comment, 0, null, commentString }) as GCodeCommand;
            }

            string[] commandArr = commandString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            char commandChar;

            commandChar = char.ToUpper(commandArr[0][0]);

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

            if (_gCodeTranslations.TryGetValue(codeNumber, out Type gCodeClassType))
            {
                return Activator.CreateInstance(gCodeClassType, new object[] { prepCode, codeNumber, commandParams, commentString }) as GCodeCommand;
            }

            return Activator.CreateInstance(typeof(MiscCommand), new object[] { prepCode, codeNumber, commandParams, commentString }) as GCodeCommand;
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
            // Set culture info for correct float are parsing
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
                    GCodeCommand parseCommand = ParseLine(line);
                    if (parseCommand != null)
                    {
                        Add(parseCommand);
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }
        }

        public GCodeCommand ParseLine(string serializedCmdLine)
        {
            string[] commentSplit = serializedCmdLine.Split(';');
            string commandString = commentSplit[0].Trim();
            string commentString = commentSplit.Length > 1 ? commentSplit[1].Trim() : null;

            if (string.IsNullOrEmpty(commandString))
            {
                if (string.IsNullOrEmpty(commentString))
                {
                    return null;
                }
                return Activator.CreateInstance(typeof(MiscCommand), new object[] { PrepCode.Comment, 0, null, commentString }) as GCodeCommand;
            }

            string[] commandArr = commandString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            char commandChar;

            commandChar = char.ToUpper(commandArr[0][0]);

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

            if (_gCodeTranslations.TryGetValue(codeNumber, out Type gCodeClassType))
            {
                return Activator.CreateInstance(gCodeClassType, new object[] { prepCode, codeNumber, commandParams, commentString }) as GCodeCommand;
            }

            return Activator.CreateInstance(typeof(MiscCommand), new object[] { prepCode, codeNumber, commandParams, commentString }) as GCodeCommand;
        }
    }
}
