using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Text;

namespace ILTFileReader.Model
{
    class ParameterChange : IParameterChange
    {
        private readonly CLILaserParameter parameter;
        private readonly double value;

        public ParameterChange(CLILaserParameter parameter, double value)
        {
            this.parameter = parameter;
            this.value = value;
        }

        public CLILaserParameter Parameter => parameter;

        public double Value => value;

        public float[] Coordinates => throw new NotImplementedException();

        public int Id => throw new NotImplementedException();

        public int N => throw new NotImplementedException();
    }
}
