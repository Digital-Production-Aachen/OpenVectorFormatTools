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

﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GCodeReaderWriter.Commands
{
    public abstract class ProgramLogicsCommand : GCodeCommand
    {
        public ProgramLogicsCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, commandParams, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public ProgramLogicsCommand(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
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

    public class PositioningToggleCommand : ProgramLogicsCommand
    {
        public readonly bool isAbsolute;

        public PositioningToggleCommand(PrepCode prepCode, int codeNumber, bool isAbsolute, string comment = null) : base(prepCode, codeNumber, null, comment)
        {
            this.isAbsolute = checkPositioning();
        }
        public PositioningToggleCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, commandParams, comment)
        {
            isAbsolute = checkPositioning();
        }

        public PositioningToggleCommand(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
        {
            isAbsolute = checkPositioning();
        }

        private bool checkPositioning()
        {
            if (gCode.codeNumber == 90 || gCode.codeNumber == 91)
            {
                return gCode.codeNumber == 90;
            }
            else
            {
                throw new ArgumentException($"Invalid code number for positioning toggle: {gCode.codeNumber} in line '{this}'");
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class BlockEndCmd : ProgramLogicsCommand
    {
        public BlockEndCmd(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, null, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public BlockEndCmd(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
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

    public class ProgramEndCmd : ProgramLogicsCommand
    {
        public ProgramEndCmd(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, commandParams, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public ProgramEndCmd(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
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
}
