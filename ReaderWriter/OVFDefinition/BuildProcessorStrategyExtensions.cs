/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2024 Digital-Production-Aachen

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

---- Copyright End ----
*/

﻿using System;
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
