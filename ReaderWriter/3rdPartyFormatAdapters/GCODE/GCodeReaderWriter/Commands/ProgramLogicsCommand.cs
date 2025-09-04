using System;
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
