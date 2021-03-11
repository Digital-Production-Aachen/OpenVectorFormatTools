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

﻿using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OVFReaderWriter
{
    public class MemoryReaderWriter : IReader, IWriter
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

        /// <summary>
        /// Initializes ReaderWriter.
        /// </summary>
        /// <param name="job">Job to read from / write to. If null, a new job is created internaly.</param>
        public void InitializeReaderWriter(Job job = null)
        {
            if (job == null)
            {
                CompleteJob = new Job();
                CompleteJob.JobMetaData.Author = Environment.UserName;
                CompleteJob.JobMetaData.JobCreationTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            }
            else
            {
                CompleteJob = job;
            }
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
