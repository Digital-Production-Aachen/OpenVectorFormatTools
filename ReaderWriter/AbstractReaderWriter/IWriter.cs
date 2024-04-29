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

using OpenVectorFormat;
using System;
using System.Threading.Tasks;

namespace OpenVectorFormat.AbstractReaderWriter
{
    public interface IWriter
    {
        /// <summary>
        /// Writes the given workPlane to the job file.
        /// More vector blocks can be appended to the workPlane last appended.
        /// </summary>
        /// <param name="workPlane">WorkPlane to add.</param>
        [Obsolete("Please use AppendWorkPlane")]
        Task AppendWorkPlaneAsync(WorkPlane workPlane);

        /// <summary>
        /// Writes the given workPlane to the job file.
        /// More vector blocks can be appended to the workPlane last appended.
        /// </summary>
        /// <param name="workPlane">WorkPlane to add.</param>
        void AppendWorkPlane(WorkPlane workPlane);

        /// <summary>
        /// Writes the VectorBlock to the workPlane last appended in the job.
        /// </summary>
        /// <param name="block">VectorBlock to write to file</param>
        [Obsolete("Please use AppendVectorBlock")]
        Task AppendVectorBlockAsync(VectorBlock block);

        /// <summary>
        /// Writes the VectorBlock to the workPlane last appended in the job.
        /// </summary>
        /// <param name="block">VectorBlock to write to file</param>
        void AppendVectorBlock(VectorBlock block);

        /// <summary>jobShell being written to. (just the shell without workPlanes).</summary>
        Job JobShell { get; }
    }
}
