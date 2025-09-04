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
    public class MiscCommand : GCodeCommand
    {
        public MiscCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, commandParams, comment)
        {
            miscParams = commandParams;
        }
        public MiscCommand(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
        {
            miscParams = commandParams;
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
