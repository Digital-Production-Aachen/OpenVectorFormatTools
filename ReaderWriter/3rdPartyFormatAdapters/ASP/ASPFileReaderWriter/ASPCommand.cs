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



/// Parsing of ASPCommands. Courtesy of LasPC Project

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenVectorFormat.ASPFileReaderWriter
{
    public sealed class ParamLength : Attribute
    {
        public readonly int length;

        public ParamLength(int l)
        {
            length = l;
        }
    }

    /// <summary>
    /// Cached parameter length of each command for quick access. This class uses reflection to parse the CommandStatus enum for custom parameters. 
    /// </summary>
    public static class ParamLengthCached
    {
        public static readonly IDictionary<CommandType, int> ParamLengthDict;
        static ParamLengthCached()
        {
            ParamLengthDict = new Dictionary<CommandType, int>();
            foreach (string e in Enum.GetNames(typeof(CommandType)))
            {
                System.Reflection.MemberInfo[] memInfo = typeof(CommandType).GetMember(e);
                int length = ((ParamLength)ParamLength.GetCustomAttribute(memInfo[0], typeof(ParamLength), false)).length;
                ParamLengthDict.Add(new KeyValuePair<CommandType, int>((CommandType)Enum.Parse(typeof(CommandType), e), length));
            }
        }
    }

    /// <summary>
    /// CommandType for Command
    /// </summary>
    public enum CommandType
    {
        [ParamLength(3)]
        JP,
        [ParamLength(3)]
        GO,
        [ParamLength(1)]
        LP,
        [ParamLength(1)]
        VG,
        [ParamLength(1)]
        VJ,
        [ParamLength(3)]
        PX,
        [ParamLength(4)]
        PL,
        [ParamLength(2)]
        AO,
        [ParamLength(3)]
        MG,
        [ParamLength(4)]
        TG,
        [ParamLength(4)]
        TJ,
        [ParamLength(0)]
        ST,
        [ParamLength(1)]
        DO,
        [ParamLength(3)]
        BS,
        [ParamLength(3)]
        BC,
        [ParamLength(1)]
        LD,
        [ParamLength(3)]
        WE,
        [ParamLength(1)]
        WP,
        [ParamLength(0)]
        NO,
        [ParamLength(1)]
        SE,
        [ParamLength(1)]
        LS,
        [ParamLength(1)]
        ON,
        [ParamLength(3)]
        TR,
        [ParamLength(1)]
        VC,
        [ParamLength(4)]
        GP,
        [ParamLength(5)]
        TP,
        [ParamLength(2)]
        WM,
        [ParamLength(2)]
        OP,
        [ParamLength(2)]
        SP,
        [ParamLength(2)]
        SB,
        [ParamLength(4)]
        PA,
        [ParamLength(1)]
        MO,
        [ParamLength(1)]
        LI,
        [ParamLength(1)]
        DE,
        [ParamLength(3)]
        DS,
        [ParamLength(2)]
        DL,
        [ParamLength(4)]
        IE,
        [ParamLength(3)]
        EA,
        [ParamLength(3)]
        ER,
    }

    /// <summary>
    /// An asp command in object representation.
    /// </summary>
    public class Command
    {
        public CommandType Type;
        public double[] Parameters;

        public string GetString()
        {
            return Type.ToString() + string.Join(",", Parameters.Select(x => x.ToString("0.####")));

        }

        /// <summary>
        /// Trys to parse a string representation of a command into an instance of Command. Therefore the CommandType as well as the parameters are checked.
        /// If parsing fails, null is returned!
        /// </summary>
        /// <param name="serializedCmd">A String representation of a Command.</param>
        /// <returns>An instance of Command or null.</returns>
        public static Command TryParse(string serializedCmd)
        {
            if (Enum.TryParse<CommandType>(serializedCmd.Substring(0, 2), out CommandType cmdType))
            {
                string[] cmdParameters = serializedCmd.Substring(2).Split(',');

                //var memInfo = typeof(CommandType).GetMember(t.ToString());
                //var length = ((ParamLength)ParamLength.GetCustomAttribute(memInfo[0], typeof(ParamLength), false)).length;

                //Test for parameter length also in constructor, but costs performance to raise event
                //Cached Data -> 5x faster
                int length = ParamLengthCached.ParamLengthDict[cmdType];

                if (cmdParameters.Length != length && !(cmdParameters.Length == 1 && cmdParameters[0].Length == 0))
                {
                    return null;
                }

                if (cmdParameters.Length == 1 && cmdParameters[0].Length == 0)
                {
                    return new Command(cmdType, new double[] { });
                }
                else
                {
                    //var vals = ss.Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    double[] parameterValues = new double[cmdParameters.Length];
                    for (int i = 0; i < cmdParameters.Length; i++)
                    {
                        if (double.TryParse(cmdParameters[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                        {
                            parameterValues[i] = value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    return new Command(cmdType, parameterValues);
                }

            }
            else { return null; }
        }

        public Command(CommandType type, params double[] values)
        {
            int length = ParamLengthCached.ParamLengthDict[type];
            if (length == values.Count())
            {
                Type = type;
                Parameters = values;
            }
            else
            {
                throw new ArgumentException("Number of parameters does not match", "CommandType=" + type.ToString());
            }
        }

        public bool IsMovementCommand()
        {
            return Type == CommandType.GO || Type == CommandType.JP || Type == CommandType.TJ || Type == CommandType.TG || Type == CommandType.GP || Type == CommandType.TP;
        }
    }

    /// <summary>
    /// A List of asp command objects.
    /// </summary>
    public class CommandList : List<Command>
    {
        /// <summary>
        /// Parses the string representation of asp commands into an internal list. The generic typ of this list is of typ Command then.
        /// Commands in the string representation must be seperated by line breaks. Illegal commands shall be ignored.
        /// </summary>
        /// <param name="serializedCmds">The string representation of various asp commands seperated by line breaks.</param>
        public void Parse(string serializedCmds)
        {
            Clear();
            string[] serializedCmdsArray = serializedCmds.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (string s in serializedCmdsArray)
            {
                if (s.StartsWith("//") || s.StartsWith("#") || s.Trim() == string.Empty)
                {
                    continue;
                }
                var cmd = Command.TryParse(s);
                if (cmd != null)
                {
                    Add(cmd);
                }
                else
                {
                    throw new FormatException("Error parsing asp command " + s);
                }
            }
        }
    }

}
