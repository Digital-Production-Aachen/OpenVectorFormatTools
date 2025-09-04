using System;
using System.Collections.Generic;
using System.Text;

namespace GCodeReaderWriter.Commands
{
    public class PauseCommand : GCodeCommand
    {
        // Duration of the pause in ms
        public float? duration;

        public PauseCommand(PrepCode prepCode, int codeNumber, float? duration, string comment = null) : base(prepCode, codeNumber, null, comment)
        {
            this.duration = duration;
        }

        public PauseCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null)
            : base(prepCode, codeNumber, commandParams, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public PauseCommand(GCode gCode, Dictionary<char, float> commandParams = null, string comment = null) : base(gCode, commandParams, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }

        public PauseCommand(Dictionary<char, float> commandParams = null, string comment = null) : base(PrepCode.G, 4, commandParams, comment)
        {
            InitParameterMap();
            if (commandParams != null)
            {
                ParseParams(commandParams);
            }
        }
        private new void InitParameterMap()
        {
            parameterMap.Add('P', (p) => duration = p);
            parameterMap.Add('F', (f) => duration = f);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
