/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2021 Digital-Production-Aachen

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
using System.Threading.Tasks;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;

namespace OpenVectorFormat.OVFReaderWriter
{
    [Obsolete("MemoryReader is obsolete, please use unified MemoryReaderWriter instead.")]
    class MemoryReader : IReader, IDisposable
    {
        /// <summary>Gets reference to complete job, including WorkPlanes.</summary>
        public Job CompleteJob { get; private set; } = null;

        /// <inheritdoc/>
        public Job JobShell => jobShell;

        private Job jobShell = null;

        /// <summary>
        /// Sets up Memory Reader.
        /// </summary>
        /// <param name="job">Job to read from.</param>
        public void InitializeReader(Job job)
        {
            CompleteJob = job ?? throw new ArgumentNullException(nameof(job));
            jobShell = new Job();
            ProtoUtils.CopyWithExclude(job, jobShell, new List<int> { Job.WorkPlanesFieldNumber });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<VectorBlock> GetVectorBlockAsync(int i_workPlane, int i_vectorblock)
        {
            if (CompleteJob == null)
            {
                throw new InvalidOperationException("Initialize memory reader with 'OpenJob' before querying data!");
            }

            return await Task.Run(() => CompleteJob.WorkPlanes[i_workPlane].VectorBlocks[i_vectorblock]);
        }

        /// <inheritdoc/>
        public async Task<WorkPlane> GetWorkPlaneAsync(int i_workPlane)
        {
            if (CompleteJob == null)
            {
                throw new InvalidOperationException("Initialize memory reader with 'OpenJob' before querying data!");
            }

            return await Task.Run(() => CompleteJob.WorkPlanes[i_workPlane]);
        }

        /// <inheritdoc/>
        public WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            if (CompleteJob == null)
            {
                throw new InvalidOperationException("Initialize memory reader with 'OpenJob' before querying data!");
            }

            WorkPlane wpShell = new WorkPlane();
            ProtoUtils.CopyWithExclude(CompleteJob.WorkPlanes[i_workPlane], wpShell, new List<int> { WorkPlane.VectorBlocksFieldNumber });
            return wpShell;
        }
    }
}
