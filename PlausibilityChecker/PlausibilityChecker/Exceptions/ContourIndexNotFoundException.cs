/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2023 Digital-Production-Aachen

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
using System.Globalization;

namespace OpenVectorFormat.Plausibility.Exceptions
{
    public class ContourIndexNotFoundException : OVFPlausibilityCheckerException
    {
        public int WorkPlaneNumber { get; }
        public int VectorBlockNumber { get; }
        public int ContourIndex { get; }

        public ContourIndexNotFoundException(int workPlaneNumber, int vectorBlockNumber, int contourIndex) : base(string.Format(CultureInfo.InvariantCulture.NumberFormat, "no valid contour at index {0} from VectorBlock {1} in WorkPlane {2} found in Workplane.meta_data.contours!", vectorBlockNumber, workPlaneNumber, contourIndex))
        {
            WorkPlaneNumber = workPlaneNumber;
            VectorBlockNumber = vectorBlockNumber;
            ContourIndex = contourIndex;
        }
    }
}
