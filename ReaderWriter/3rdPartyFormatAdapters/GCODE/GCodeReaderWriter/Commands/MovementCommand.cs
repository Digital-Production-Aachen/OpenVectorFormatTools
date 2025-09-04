using System;
using System.Collections.Generic;
using System.Text;

namespace GCodeReaderWriter.Commands
{
    public abstract class MovementCommand : GCodeCommand
    {
        // Target position in mm, feedrate in mm/min and acceleration in mm/min^2 of movement commands
        public float? xPosition;
        public float? yPosition;
        public float? zPosition;
        public float? feedRate;
        public float? acceleration;

        public MovementCommand(PrepCode prepCode, int codeNumber, float? xPosition, float? yPosition, float? zPosition, float? feedRate, float? acceleration, Dictionary<char, float> miscParams = null, string comment = null)
            : base(prepCode, codeNumber, null, comment)
        {
            this.miscParams = miscParams;
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.zPosition = zPosition;
            this.feedRate = feedRate;
            this.acceleration = acceleration;
        }

        public MovementCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, commandParams, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public MovementCommand(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public MovementCommand(Dictionary<char, float> commandParams = null, string comment = null) : base(PrepCode.G, 1, commandParams, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        private new void InitParameterMap()
        {
            parameterMap.Add('X', (x) => xPosition = x);
            parameterMap.Add('Y', (y) => yPosition = y);
            parameterMap.Add('Z', (z) => zPosition = z);
            parameterMap.Add('F', (f) => feedRate = f);
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
        public bool isOperation;

        public LinearInterpolationCmd(PrepCode prepCode, int codeNumber, bool isOperation, float? xPosition, float? yPosition, float? zPosition = null, float? feedRate = null, float? acceleration = null, Dictionary<char, float> miscParams = null, string comment = null)
            : base(prepCode, codeNumber, xPosition, yPosition, zPosition, feedRate, acceleration, miscParams, comment)
        {
            this.isOperation = isOperation;
        }

        public LinearInterpolationCmd(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, commandParams, comment)
        {
            CheckOperation();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public LinearInterpolationCmd(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
        {
            CheckOperation();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        private void CheckOperation()
        {
            if (gCode.codeNumber == 0 || gCode.codeNumber == 1)
            {
                isOperation = gCode.codeNumber == 1;
            }
            else
            {
                throw new ArgumentException($"Invalid code number for linear interpolation: {gCode.codeNumber} in line '{this}'");
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class CircularInterpolationCmd : MovementCommand
    {
        // Center of the circle relative to the start position in mm
        public float? xCenterRel;
        public float? yCenterRel;

        // Direction of the circular interpolation
        public readonly bool isClockwise;

        public CircularInterpolationCmd(PrepCode prepCode, int codeNumer, bool isClockwise, float? xPosition, float? yPosition, float? xCenterRel, float? yCenterRel, float? feedRate, float? acceleration, Dictionary<char, float> miscParams = null, string comment = null)
            : base(prepCode, codeNumer, xPosition, yPosition, null, feedRate, acceleration, miscParams, comment)
        {
            this.isClockwise = isClockwise;
            this.xCenterRel = xCenterRel;
            this.yCenterRel = yCenterRel;
        }

        public CircularInterpolationCmd(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, commandParams, comment)
        {
            isClockwise = CheckDirection();
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public CircularInterpolationCmd(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
        {
            isClockwise = CheckDirection();
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        private new void InitParameterMap()
        {
            parameterMap.Add('I', (i) => xCenterRel = i);
            parameterMap.Add('J', (j) => yCenterRel = j);
        }

        private bool CheckDirection()
        {
            if (gCode.codeNumber == 2 || gCode.codeNumber == 3)
            {
                return gCode.codeNumber == 2;
            }
            else
            {
                throw new ArgumentException($"Invalid code number for circular interpolation: {gCode.codeNumber} in line '{this}'");
            }
        }

        public override string ToString()
        {
            return base.ToString() + (xCenterRel != null ? $" I{xCenterRel}" : "") + (yCenterRel != null ? $" I{yCenterRel}" : "");
        }
    }
}
