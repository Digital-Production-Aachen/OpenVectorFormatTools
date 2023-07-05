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

﻿using System.Threading.Tasks;
using OpenVectorFormat;

namespace OpenVectorFormat.AbstractReaderWriter
{
    public interface IReader
    {
        /// <summary>
        /// Gets the JobShell with all metadata, but without the actual workplane data (<see cref="Job.WorkPlanes"/> is empty).
        /// <see cref="OpenJobAsync(string, IFileReaderWriterProgress)"/> needs to be called before.
        /// </summary>
        Job JobShell
        {
            get;
        }

        /// <summary>Retrieves workPlane point data on demand, delegating ownership of the data to the caller. The complete WorkPlane needs to be cached into memory for this operation.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <returns>Requested WorkPlane with all associated VectorBlocks.</returns>
        Task<WorkPlane> GetWorkPlaneAsync(int i_workPlane);

        /// <summary>Retrieves WorkPlaneShell with all the meta-data, without the actual vectorblocks, delegating ownership of the data to the caller.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <returns>Requested WorkPlane with all associated VectorBlocks</returns>
        WorkPlane GetWorkPlaneShell(int i_workPlane);

        /// <summary>Retrieves vector block point data on demand, delegating ownership of the data to the caller.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <param name="i_vectorblock">index of vectorblock</param>
        /// <returns>Requested VectorBlock</returns>
        Task<VectorBlock> GetVectorBlockAsync(int i_workPlane, int i_vectorblock);
    }
}
