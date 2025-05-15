/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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

namespace OpenVectorFormat.Plausibility.Exceptions
{
    public class IncoherentWorkPlaneNumberingException : OVFPlausibilityCheckerException
    {
        /// <summary>Index of workplane in WorkPlanes list.</summary>
        public int WorkPlaneIndex { get; }

        /// <summary>Value of WorkPlanes[workPlaneIndex].WorkPlanenNumber.</summary>
        public int WorkPlane_WorkPlaneNumber { get; }

        /// <summary>Exception thrown if workplane numbering is incoherent.</summary>
        /// <param name="workPlaneIndex">Index of workplane in WorkPlanes list</param>
        /// <param name="workPlaneNumber">Value of WorkPlanes[workPlaneIndex].WorkPlanenNumber.</param>
        public IncoherentWorkPlaneNumberingException(int workPlaneIndex, int workPlaneNumber) : base(string.Format("Incoherent Workplane number! WorkPlane.WorkPlaneNumber for Job.WorkPlanes[{0}] should be {0}, but is {1}", workPlaneIndex, workPlaneNumber))
        {
            WorkPlaneIndex = workPlaneIndex;
            WorkPlane_WorkPlaneNumber = workPlaneNumber;
        }
    }
}
