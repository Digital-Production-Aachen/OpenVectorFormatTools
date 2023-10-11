using System;
using System.Collections.Generic;
using System.Text;

namespace OVFDefinition
{
    public class Utils
    {
        public static bool ApproxEquals(float value1, float value2, float tolerance = 1e-6f)
        {
            return Math.Abs(value1 - value2) <= tolerance;
        }
    }
}
