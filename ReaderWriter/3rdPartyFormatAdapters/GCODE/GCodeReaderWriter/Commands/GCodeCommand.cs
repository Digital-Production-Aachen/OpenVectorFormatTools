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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GCodeReaderWriter.Commands
{
    public enum PrepCode
    {
        G,
        M,
        T,
        Comment
    }

    public readonly struct GCode
    {
        public readonly PrepCode preparatoryFunctionCode;
        public readonly int codeNumber;

        public GCode(PrepCode preparatoryFunctionCode, int codeNumber)
        {
            this.preparatoryFunctionCode = preparatoryFunctionCode;
            this.codeNumber = codeNumber;
        }

        public override string ToString() => $"{preparatoryFunctionCode}{codeNumber}";
    }

    public class ToolParams
    {
        int toolNumber;
    }

    // Base class for all G/M/T code commands. Also used for unrecognized commands.
    // Inherit from this class or it's child classes to extend the parseable commands and/or parameters for your GCode-flavor.
    public abstract class GCodeCommand
    {
        public readonly GCode gCode;
        public readonly string comment;

        public Dictionary<char, float> miscParams;

        public readonly List<char> recordedParams;

        protected Dictionary<char, Action<float>> parameterMap;

        public GCodeCommand(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null)
        {
            miscParams = new Dictionary<char, float>();
            recordedParams = new List<char>();
            parameterMap = new Dictionary<char, Action<float>>();
            this.gCode = gCode;
            this.comment = comment;
        }

        public GCodeCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null)
            : this(new GCode(prepCode, codeNumber), commandParams, comment) { }

        protected void ParseParams(Dictionary<char, float> commandParams)
        {
            if (commandParams == null) return;

            foreach (var kv in commandParams.ToList())
            {
                if (parameterMap.TryGetValue(kv.Key, out var setter))
                {
                    setter(kv.Value);
                    recordedParams.Add(kv.Key);
                    commandParams.Remove(kv.Key);
                }
            }

            miscParams = commandParams;
        }

        protected string BuildStringSuffix()
        {
            return string.Join(" ", miscParams.Keys.Select(k => $"{k}{miscParams[k]}")) + (comment != null ? $" ; {comment}" : ""); ;
        }

        protected virtual string BuildStringFromParams()
        {
            return string.Join(" ", recordedParams.Select(k => $"{k}{miscParams[k]}"));
        }

        public override string ToString() => gCode.ToString() + BuildStringFromParams() + BuildStringSuffix();

    }

    public class GCodeConverter
    {
        private static readonly Dictionary<int, Func<PrepCode, int, Dictionary<char, float>, string, GCodeCommand>>
            _gFactories = new Dictionary<int, Func<PrepCode, int, Dictionary<char, float>, string, GCodeCommand>>
            {
                { 0,  (p, n, prm, c) => new LinearInterpolationCmd(p, n, prm, c) },
                { 1,  (p, n, prm, c) => new LinearInterpolationCmd(p, n, prm, c) },
                { 2,  (p, n, prm, c) => new CircularInterpolationCmd(p, n, prm, c) },
                { 3,  (p, n, prm, c) => new CircularInterpolationCmd(p, n, prm, c) },
                { 4,  (p, n, prm, c) => new PauseCommand(p, n, prm, c) },
                { 90, (p, n, prm, c) => new PositioningToggleCommand(p, n, prm, c) },
                { 91, (p, n, prm, c) => new PositioningToggleCommand(p, n, prm, c) },
            };

        private static readonly Dictionary<int, Func<PrepCode, int, Dictionary<char, float>, string, GCodeCommand>>
            _mFactories = new Dictionary<int, Func<PrepCode, int, Dictionary<char, float>, string, GCodeCommand>>();

        private static readonly Dictionary<int, Func<PrepCode, int, Dictionary<char, float>, string, GCodeCommand>>
            _tFactories = new Dictionary<int, Func<PrepCode, int, Dictionary<char, float>, string, GCodeCommand>>();

        public GCodeCommand ParseLine(string serializedCmdLine)
        {
            if (serializedCmdLine == null) return null;

            string[] commentSplit = serializedCmdLine.Split(new[] { ';' }, 2);
            string commandString = commentSplit[0].Trim();
            string commentString = commentSplit.Length > 1 ? commentSplit[1].Trim() : null;

            if (string.IsNullOrEmpty(commandString))
            {
                return string.IsNullOrEmpty(commentString)
                    ? null
                    : new MiscCommand(PrepCode.Comment, 0, null, commentString);
            }

            string[] tokens = commandString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            char prepChar = char.ToUpperInvariant(tokens[0][0]);

            if (!Enum.TryParse(prepChar.ToString(), out PrepCode prepCode))
                throw new ArgumentException($"Invalid preparatory function code: {prepChar} in line '{serializedCmdLine}'");

            string codeNumberStr = tokens[0].Substring(1);
            if (!int.TryParse(codeNumberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int codeNumber))
                throw new ArgumentException($"Invalid number format: {codeNumberStr} in line '{serializedCmdLine}'");

            var commandParams = new Dictionary<char, float>();
            foreach (var word in tokens.Skip(1))
            {
                if (word.Length == 0) continue;
                char paramChar = char.ToUpperInvariant(word[0]);

                if (word.Length == 1)
                {
                    commandParams[paramChar] = 0f;
                }
                else if (float.TryParse(word.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    commandParams[paramChar] = value;
                }
                else
                {
                    throw new ArgumentException(
                        $"Invalid command parameter format: {word} in line '{serializedCmdLine}'. " +
                        "Command parameters must be of format <char><float>.");
                }
            }

            Dictionary<int, Func<PrepCode, int, Dictionary<char, float>, string, GCodeCommand>> table;
            switch (prepCode)
            {
                case PrepCode.G: table = _gFactories; break;
                case PrepCode.M: table = _mFactories; break;
                case PrepCode.T: table = _tFactories; break;
                default: table = null; break;
            }

            if (table != null && table.TryGetValue(codeNumber, out var factory))
                return factory(prepCode, codeNumber, commandParams, commentString);

            return new MiscCommand(prepCode, codeNumber, commandParams, commentString);
        }
    }
}
