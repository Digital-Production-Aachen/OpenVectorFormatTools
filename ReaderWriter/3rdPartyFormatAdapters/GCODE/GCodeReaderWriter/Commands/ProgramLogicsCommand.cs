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

namespace GCodeReaderWriter.Commands
{
    public abstract class ProgramLogicsCommand : GCodeCommand
    {
        public ProgramLogicsCommand(PrepCode prepCode, int codeNumber,
                                    Dictionary<char, float> commandParams = null, string comment = null)
            : base(prepCode, codeNumber, commandParams, comment)
        {
            ParseParams(commandParams);
        }

        public ProgramLogicsCommand(GCode gCode,
                                    Dictionary<char, float> commandParams = null, string comment = null)
            : base(gCode, commandParams, comment)
        {
            ParseParams(commandParams);
        }
    }

    public class PositioningToggleCommand : ProgramLogicsCommand
    {
        public readonly bool isAbsolute;

        public PositioningToggleCommand(PrepCode prepCode, int codeNumber,
                                        Dictionary<char, float> commandParams = null, string comment = null)
            : base(prepCode, codeNumber, commandParams, comment)
        {
            isAbsolute = CheckPositioning();
        }

        public PositioningToggleCommand(GCode gCode,
                                        Dictionary<char, float> commandParams = null, string comment = null)
            : base(gCode, commandParams, comment)
        {
            isAbsolute = CheckPositioning();
        }

        private bool CheckPositioning()
        {
            if (gCode.codeNumber == 90 || gCode.codeNumber == 91)
                return gCode.codeNumber == 90;
            throw new ArgumentException(
                $"Invalid code number for positioning toggle: {gCode.codeNumber} in line '{this}'");
        }
    }

    public class BlockEndCmd : ProgramLogicsCommand
    {
        public BlockEndCmd(PrepCode prepCode, int codeNumber,
                           Dictionary<char, float> commandParams = null, string comment = null)
            : base(prepCode, codeNumber, commandParams, comment) { }

        public BlockEndCmd(GCode gCode,
                           Dictionary<char, float> commandParams = null, string comment = null)
            : base(gCode, commandParams, comment) { }
    }

    public class ProgramEndCmd : ProgramLogicsCommand
    {
        public ProgramEndCmd(PrepCode prepCode, int codeNumber,
                             Dictionary<char, float> commandParams = null, string comment = null)
            : base(prepCode, codeNumber, commandParams, comment) { }

        public ProgramEndCmd(GCode gCode,
                             Dictionary<char, float> commandParams = null, string comment = null)
            : base(gCode, commandParams, comment) { }
    }
}
