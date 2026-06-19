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
    public abstract class MovementCommand : GCodeCommand
    {
        public float? xPosition;
        public float? yPosition;
        public float? zPosition;
        public float? feedRate;
        public float? acceleration;

        public MovementCommand(PrepCode prepCode, int codeNumber,
                               float? xPosition, float? yPosition, float? zPosition,
                               float? feedRate, float? acceleration,
                               Dictionary<char, float> miscParams = null, string comment = null)
            : base(prepCode, codeNumber, null, comment)
        {
            this.miscParams = miscParams ?? new Dictionary<char, float>();
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.zPosition = zPosition;
            this.feedRate = feedRate;
            this.acceleration = acceleration;
        }

        public MovementCommand(PrepCode prepCode, int codeNumber,
                               Dictionary<char, float> commandParams = null, string comment = null)
            : base(prepCode, codeNumber, commandParams, comment)
        {
            InitParameterMap();
            ParseParams(commandParams);
        }

        public MovementCommand(GCode gCode,
                               Dictionary<char, float> commandParams = null, string comment = null)
            : base(gCode, commandParams, comment)
        {
            InitParameterMap();
            ParseParams(commandParams);
        }

        private void InitParameterMap()
        {
            parameterMap.Add('X', x => xPosition = x);
            parameterMap.Add('Y', y => yPosition = y);
            parameterMap.Add('Z', z => zPosition = z);
            parameterMap.Add('F', f => feedRate = f);
        }

        public override string ToString()
            => base.ToString()
             + (feedRate.HasValue ? $" F{feedRate}" : "")
             + (xPosition.HasValue ? $" X{xPosition}" : "")
             + (yPosition.HasValue ? $" Y{yPosition}" : "")
             + (zPosition.HasValue ? $" Z{zPosition}" : "");
    }

    public class LinearInterpolationCmd : MovementCommand
    {
        public bool isOperation;

        public LinearInterpolationCmd(PrepCode prepCode, int codeNumber, bool isOperation,
                                      float? xPosition, float? yPosition, float? zPosition = null,
                                      float? feedRate = null, float? acceleration = null,
                                      Dictionary<char, float> miscParams = null, string comment = null)
            : base(prepCode, codeNumber, xPosition, yPosition, zPosition, feedRate, acceleration, miscParams, comment)
        {
            this.isOperation = isOperation;
        }

        public LinearInterpolationCmd(PrepCode prepCode, int codeNumber,
                                      Dictionary<char, float> commandParams = null, string comment = null)
            : base(prepCode, codeNumber, commandParams, comment)
        {
            CheckOperation();
        }

        public LinearInterpolationCmd(GCode gCode,
                                      Dictionary<char, float> commandParams = null, string comment = null)
            : base(gCode, commandParams, comment)
        {
            CheckOperation();
        }

        private void CheckOperation()
        {
            if (gCode.codeNumber == 0 || gCode.codeNumber == 1)
                isOperation = gCode.codeNumber == 1;
            else
                throw new ArgumentException(
                    $"Invalid code number for linear interpolation: {gCode.codeNumber} in line '{this}'");
        }
    }

    public class CircularInterpolationCmd : MovementCommand
    {
        public float? xCenterRel;   // I
        public float? yCenterRel;   // J
        public readonly bool isClockwise;

        public CircularInterpolationCmd(PrepCode prepCode, int codeNumber, bool isClockwise,
                                        float? xPosition, float? yPosition,
                                        float? xCenterRel, float? yCenterRel,
                                        float? feedRate, float? acceleration,
                                        Dictionary<char, float> miscParams = null, string comment = null)
            : base(prepCode, codeNumber, xPosition, yPosition, null, feedRate, acceleration, miscParams, comment)
        {
            this.isClockwise = isClockwise;
            this.xCenterRel = xCenterRel;
            this.yCenterRel = yCenterRel;
        }

        public CircularInterpolationCmd(PrepCode prepCode, int codeNumber,
                                        Dictionary<char, float> commandParams = null, string comment = null)
            : base(prepCode, codeNumber, commandParams, comment)
        {
            isClockwise = CheckDirection();
            InitParameterMap();
            ParseParams(miscParams);
        }

        public CircularInterpolationCmd(GCode gCode,
                                        Dictionary<char, float> commandParams = null, string comment = null)
            : base(gCode, commandParams, comment)
        {
            isClockwise = CheckDirection();
            InitParameterMap();
            ParseParams(miscParams);
        }

        private void InitParameterMap()
        {
            parameterMap.Add('I', i => xCenterRel = i);
            parameterMap.Add('J', j => yCenterRel = j);
        }

        private bool CheckDirection()
        {
            if (gCode.codeNumber == 2 || gCode.codeNumber == 3)
                return gCode.codeNumber == 2;
            throw new ArgumentException(
                $"Invalid code number for circular interpolation: {gCode.codeNumber} in line '{this}'");
        }

        public override string ToString()
            => base.ToString()
             + (xCenterRel.HasValue ? $" I{xCenterRel}" : "")
             + (yCenterRel.HasValue ? $" J{yCenterRel}" : "");
    }
}
