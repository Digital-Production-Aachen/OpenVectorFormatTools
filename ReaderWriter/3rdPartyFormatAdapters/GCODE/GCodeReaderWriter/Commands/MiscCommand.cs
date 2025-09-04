using System;
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
