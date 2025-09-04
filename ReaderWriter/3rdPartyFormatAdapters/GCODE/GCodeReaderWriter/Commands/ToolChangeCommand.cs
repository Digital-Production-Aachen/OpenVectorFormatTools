using System;
using System.Collections.Generic;
using System.Text;

namespace GCodeReaderWriter.Commands
{
    public class ToolChangeCommand : GCodeCommand
    {
        public ToolParams toolParams;

        public ToolChangeCommand(PrepCode prepCode, int codeNumber, ToolParams toolParams, string comment = null) : base(prepCode, codeNumber, null, comment)
        {
            this.toolParams = toolParams;
        }

        public ToolChangeCommand(PrepCode prepCode, int codeNumber, Dictionary<char, float> commandParams = null, string comment = null) : base(prepCode, codeNumber, commandParams, comment)
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
