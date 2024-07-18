using System;
using System.Collections.Generic;
using System.Text;


namespace OpenVectorFormat.ILTFileReader
{

    public enum CLILaserParameter { POWER, SPEED, FOCUS }

    public interface IParameterChange : IVectorBlock
    {
        CLILaserParameter Parameter { get; }
        double Value { get; }
    }
}
