using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.FileReaderWriterFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenVectorFormat.Streaming
{
    /// <summary>
    /// This class support adding vector blocks to random heights in any order
    /// and writing the result to OVF sorted by layer height.
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
            if (progress == null)
                progress = new FileReaderWriterProgress();
            jobshell.NumWorkPlanes = workplanes.Count;
            using (var writer = new OpenVectorFormat.OVFReaderWriter.OVFFileWriter())
            {
                writer.StartWritePartial(jobshell, targetFile, progress);
                foreach (var wp in workplanes.OrderBy(x => x.Key))
                {
                    wp.Value.NumBlocks = wp.Value.VectorBlocks.Count;
                    writer.AppendWorkPlaneAsync(wp.Value).GetAwaiter().GetResult();
                }
            }
        }
    }
}
