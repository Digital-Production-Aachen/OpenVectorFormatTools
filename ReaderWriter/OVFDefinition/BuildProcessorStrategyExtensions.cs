using System;
using System.Collections.Generic;
using System.Text;

namespace OVFDefinition
{
    public static class BuildProcessorStrategyExtensions
    {
        /// <summary>
        /// Calculate the theoretical build up rate of the given parameter set [mm³].
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static double TheoreticalBuildUpRateInMM3perS(this OpenVectorFormat.BuildProcessorStrategy.Types.ParameterSet parameters)
        {
            var hatchDist = parameters?.ProcessStrategy?.HatchDistanceInMm ?? 0;
            var exposureSpeed = parameters?.MarkingParams?.LaserSpeedInMmPerS ?? 0;
            var layerThickness = parameters?.ProcessStrategy?.LayerThicknessInMm ?? 0;
            return hatchDist * exposureSpeed * layerThickness;
        }
    }
}
