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

using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenVectorFormat.OVFReaderWriter
{
    /// <summary>
    /// This class support adding vector blocks to random heights in any order
    /// and writing the result to OVF sorted by layer height.
    /// The data is cached in memory until the file is written.
    /// </summary>
    public class OVFRandomWriter
    {
        private const int scale = 1 << 20;
        Dictionary<int, WorkPlane> workplanes = new Dictionary<int, WorkPlane>();
        Job jobshell;

        public OVFRandomWriter(string jobName = "job")
        {
            jobshell = new Job()
            {
                JobMetaData = new Job.Types.JobMetaData()
                {
                    JobName = "Debug_Job",
                    Author = Environment.UserName,
                    JobCreationTime = System.DateTimeOffset.Now.ToUnixTimeSeconds()
                },
                JobParameters = new JobParameters(),
            };
        }

        public int AddPart(OpenVectorFormat.Part part)
        {
            int key = jobshell.PartsMap.Any() ? jobshell.PartsMap.Keys.Max() + 1 : 0;
            jobshell.PartsMap.Add(key, part);
            return key;
        }

        public void AddVectorBlock(float height, VectorBlock block)
        {
            int key = (int)Math.Round(height * scale);
            var found = workplanes.TryGetValue(key, out var workplane);
            if (!found)
            {
                workplane = new WorkPlane() { ZPosInMm = height };
                workplanes.Add(key, workplane);
            }
            workplane.VectorBlocks.Add(block);
        }

        public void AddOrAppend(WorkPlane workPlane)
        {
            int key = (int)Math.Round(workPlane.ZPosInMm * scale);
            var found = workplanes.TryGetValue(key, out var existingWP);
            if (!found)
            {
                workplanes.Add(key, workPlane);
            }
            else
            {
                existingWP.VectorBlocks.AddRange(workPlane.VectorBlocks);
            }
        }

        public void Write(string targetFile, IFileReaderWriterProgress progress = null)
        {
            jobshell.NumWorkPlanes = workplanes.Count;
            using (var writer = new OpenVectorFormat.OVFReaderWriter.OVFFileWriter())
            {
                writer.StartWritePartial(jobshell, targetFile, progress);
                foreach (var wp in workplanes.OrderBy(x => x.Key))
                {
                    wp.Value.NumBlocks = wp.Value.VectorBlocks.Count;
                    writer.AppendWorkPlane(wp.Value);
                }
            }
        }
    }
}
