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

﻿using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenVectorFormat.OVFReaderWriter
{
    [Obsolete("MemoryReader is obsolete, please use unified MemoryReaderWriter instead.")]
    public class MemoryWriter : IWriter, IDisposable
    {
        /// <summary>Gets reference to complete job, including WorkPlanes.</summary>
        public Job CompleteJob { get; private set; } = null;

        /// <inheritdoc/>
        public Job JobShell
        {
            get
            {
                if (CompleteJob == null)
                {
                    throw new InvalidOperationException("Initialize memory reader with 'StartWritePartial' before querying data!");
                }
                Job jobShell = new Job();
                ProtoUtils.CopyWithExclude(CompleteJob, jobShell, new List<int> { Job.WorkPlanesFieldNumber });
                return jobShell;
            }
        }

        /// <summary>Initializes memory writer and generates new job to write to.</summary>
        public void InitializeWriter()
        {
            CompleteJob = new Job();
            CompleteJob.JobMetaData.Author = Environment.UserName;
            CompleteJob.JobMetaData.JobCreationTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        }

        /// <summary>Initializes memory writer to writes to provided job.</summary>
        /// <param name="jobShell">job object to write to.</param>
        public void InitializeWriter(Job jobShell)
        {
            CompleteJob = jobShell ?? throw new ArgumentNullException(nameof(jobShell));
        }

        public async Task AppendWorkPlaneAsync(WorkPlane workPlane)
        {
            workPlane.WorkPlaneNumber = CompleteJob.NumWorkPlanes;
            CompleteJob.WorkPlanes.Add(workPlane);
            CompleteJob.NumWorkPlanes++;
        }

        /// <inheritdoc/>
        public async Task AppendVectorBlockAsync(VectorBlock block)
        {
            if (CompleteJob == null)
            {
                throw new InvalidOperationException("Initialize memory reader with 'StartWritePartial' before querying data!");
            }
            else if (CompleteJob.NumWorkPlanes == 0)
            {
                throw new InvalidOperationException("Add a workplane before appending VectorBlocks!");
            }

            CompleteJob.WorkPlanes[CompleteJob.NumWorkPlanes - 1].VectorBlocks.Add(block);
            CompleteJob.WorkPlanes[CompleteJob.NumWorkPlanes - 1].NumBlocks++;
        }

        public void Dispose()
        {
        }
    }
}
