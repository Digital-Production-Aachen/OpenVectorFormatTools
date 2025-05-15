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
using System.Globalization;

namespace OpenVectorFormat.Plausibility.Exceptions
{
    public class IncoherentNumberOfVectorBlocksException : OVFPlausibilityCheckerException
    {
        public int WorkPlane_NumVectorBlocks { get; }
        public int WorkPlane_VectorBlocks_Length { get; }

        public IncoherentNumberOfVectorBlocksException(int numVectorBlocks, int VectorBlocksListLength) : base(string.Format(CultureInfo.InvariantCulture.NumberFormat, "Incoherent number of VectorBlocks! WorkPlane.NumVectorBlocks is {0}, but length of WorkPlane.VectorBlocks is {1}", numVectorBlocks, VectorBlocksListLength))
        {
            WorkPlane_NumVectorBlocks = numVectorBlocks;
            WorkPlane_VectorBlocks_Length = VectorBlocksListLength;
        }

        public IncoherentNumberOfVectorBlocksException(int numVectorBlocks, int VectorBlocksListLength, Exception innerException) : base(string.Format(CultureInfo.InvariantCulture.NumberFormat, "Incoherent number of VectorBlocks! WorkPlane.NumVectorBlocks is {0}, but length of WorkPlane.VectorBlocks is {1}", numVectorBlocks, VectorBlocksListLength), innerException)
        {
            WorkPlane_NumVectorBlocks = numVectorBlocks;
            WorkPlane_VectorBlocks_Length = VectorBlocksListLength;
        }
    }
}
