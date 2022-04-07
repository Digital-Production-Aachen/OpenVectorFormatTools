/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2022 Digital-Production-Aachen

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

﻿namespace OpenVectorFormat.Plausibility
{
    /// <summary>Additional config for checker. Basic checks are always performend and cannot be disabled.</summary>
    public class CheckerConfig
    {
        /// <summary>Handling of errors if a check fails.</summary>
        public ErrorHandlingMode ErrorHandling { get; set; } = ErrorHandlingMode.THROWEXCEPTION;

        /// <summary>Checks if <see cref="WorkPlane.NumBlocks"/> is 0 for any workplane.</summary>
        public CheckAction CheckWorkPlanesNonEmpty { get; set; } = CheckAction.CHECKWARNING;

        /// <summary>Checks if all <see cref="VectorBlock"/>s contain vector data</summary>
        public CheckAction CheckVectorBlocksNonEmpty { get; set; } = CheckAction.CHECKWARNING;

        /// <summary>Checks if polylines are closed.</summary>
        public CheckAction CheckLineSequencesClosed { get; set; } = CheckAction.CHECKWARNING;

        /// <summary>Checks if all <see cref="VectorBlock.MarkingParamsKey"/> exist in <see cref="Job.MarkingParamsMap"/>.</summary>
        public CheckAction CheckMarkingParamsKeys { get; set; } = CheckAction.CHECKWARNING;

        /// <summary>Checks if all <see cref="VectorBlock.Types.VectorBlockMetaData.PartKey"/> exist in <see cref="Job.PartsMap"/>.</summary>
        public CheckAction CheckPartKeys { get; set; } = CheckAction.CHECKWARNING;

        /// <summary>Checks if all <see cref="VectorBlock.Types.VectorBlockMetaData.PatchKey"/> exist in <see cref="WorkPlane.Types.WorkPlaneMetaData.PatchesMap"/>.</summary>
        public CheckAction CheckPatchKeys { get; set; } = CheckAction.CHECKWARNING;

        public CheckAction CheckContourIndex { get; set; } = CheckAction.CHECKWARNING;
    }
}
