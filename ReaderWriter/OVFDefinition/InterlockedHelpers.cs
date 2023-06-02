using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OpenVectorFormat
{
    internal class InterlockedHelpers
    {
        internal static bool AssignIfNewValueSmaller(ref float target, float newValue)
        {
            float snapshot;
            bool stillLess;
            do
            {
                snapshot = target;
                stillLess = newValue < snapshot;
            } while (stillLess && Interlocked.CompareExchange(ref target, newValue, snapshot) != snapshot);

            return stillLess;
        }

        internal static bool AssignIfNewValueBigger(ref float target, float newValue)
        {
            float snapshot;
            bool stillMore;
            do
            {
                snapshot = target;
                stillMore = newValue > snapshot;
            } while (stillMore && Interlocked.CompareExchange(ref target, newValue, snapshot) != snapshot);

            return stillMore;
        }
    }
}
