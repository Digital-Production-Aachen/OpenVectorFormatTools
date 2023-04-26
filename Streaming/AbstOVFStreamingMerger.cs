using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenVectorFormat.Streaming
{
    /// <summary>
    /// Wraps a FileReader and applies parameters to the FileReader interface calls
    /// using the internal parameterSetEngine. Can also merge other file readers and tag FileReaders as support.
    /// Merging capabilities of the build processor shall be used for merging File Readers that contain vectors for one part
    /// (e.g. external support files, multiple cli files to "tag" parameters).
    /// To merge multiple parts into a build job, use 
    /// </summary>
    public abstract class AbstOVFStreamingMerger : FileReader
    {
        //common job shell of the merged file readers
        protected Job mergedJobShell;
        //File Readers to merge (including main File Reader) and if they are to be tagged as support
        protected List<FileReaderToMerge> fileReaders = new List<FileReaderToMerge>();

        /// <summary>
        /// Adds another file reader to merge while streaming layers.
        /// Supports only merging of same layer thicknesses
        /// </summary>
        /// <param name="fileReaderToMerge"></param>
        public void AddFileReaderToMerge(FileReaderToMerge fileReaderToMerge)
        {
            var processStrategy = mergedJobShell.PartsMap.FirstOrDefault().Value.ProcessStrategy;
            var processStrategyToMerge = fileReaderToMerge.fr.JobShell?.PartsMap.FirstOrDefault().Value.ProcessStrategy;
            if (processStrategy == null || processStrategyToMerge == null || processStrategyToMerge.LayerThicknessInMm != processStrategy.LayerThicknessInMm)
            {
                throw new NotSupportedException($"BuildProcessor only supports merging FileReader with same layer thickness");
                //and does so because it is too hard to correctly identify how to merge when streaming if layers have different heights
                //might be ok to do clean multiples, e.g. merging 20 µm and 40 µm
                //better do that without streaming, like in OVFFileMerger
            }
            else
            {
                //determine min z and offsets
                fileReaderToMerge.zMin = fileReaderToMerge.fr.GetWorkPlaneShell(0).ZPosInMm;
                var zMin = fileReaders.Min(x => x.zMin);
                foreach(var fileReader in fileReaders)
                {
                    fileReader.layerOffset = (int)Math.Round((fileReader.zMin - zMin) / processStrategy.LayerThicknessInMm);
                }
                mergedJobShell.NumWorkPlanes = fileReaders.Max(x => x.layerOffset + x.fr.JobShell.NumWorkPlanes);
                fileReaders.Add(fileReaderToMerge);
            }
        }

        public override CacheState CacheState => fileReaders[0].fr.CacheState;

        public override Job JobShell => mergedJobShell;

        public override async Task<Job> CacheJobToMemoryAsync()
        {
            foreach(var reader in fileReaders)
            {
                await reader.fr.CacheJobToMemoryAsync();
            }
            var job = mergedJobShell.Clone();
            for(int i = 0; i < job.NumWorkPlanes; i++)
            {
                job.WorkPlanes.Add(await GetWorkPlaneAsync(i));
            }
            return job;
        }

        public override void Dispose()
        {
           foreach(var reader in fileReaders)
            {
                reader.fr.Dispose();
            }
        }

        public override Task<VectorBlock> GetVectorBlockAsync(int i_workPlane, int i_vectorblock)
        {
            return Task.FromResult(GetWorkPlaneAsync(i_workPlane).Result.VectorBlocks[i_vectorblock]);
        }

        public override Task<WorkPlane> GetWorkPlaneAsync(int i_workPlane)
        {
            var workPlane = new WorkPlane();
            workPlane.MetaData = new WorkPlane.Types.WorkPlaneMetaData();
            workPlane.WorkPlaneNumber = i_workPlane;
            foreach (var fileReader in fileReaders)
            {
                var indexWithOffset = i_workPlane - fileReader.layerOffset;
                var patchKeyOffset = workPlane.MetaData.PatchesMap.Any() ? workPlane.MetaData.PatchesMap.Keys.Max() + 1 : 0;
                var contoursCount = workPlane.MetaData.Contours.Count;
                if (indexWithOffset >= 0 && indexWithOffset < fileReader.fr.JobShell.NumWorkPlanes)
                {
                    var workPlaneToMerge = fileReader.fr.GetWorkPlaneAsync(indexWithOffset).Result;

                    for(int i = 0; i < workPlaneToMerge.VectorBlocks.Count; i++)
                    {
                        
                        var block = workPlaneToMerge.VectorBlocks[i];
                        if (fileReader.translationX != 0 || fileReader.translationY != 0)
                        {
                            //create deep copies of the vector blocks coordinates
                            block = block.Clone();
                            block.Translate(fileReader.translationX, fileReader.translationY);
                        }
                        else
                        {
                            //create shallow copy only. No side effects as long as we don't change the coordinates
                            block = block.ShallowCopy();
                        }

                        if (fileReader.markAsSupport)
                        {
                            block.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Support;
                        }

                        workPlane.VectorBlocks.Add(block);
                        workPlane.NumBlocks++;

                        workPlane.ZPosInMm = workPlaneToMerge.ZPosInMm;

                        //check if patch is valid before applying index shift
                        if (workPlaneToMerge.MetaData?.PatchesMap != null && workPlaneToMerge.MetaData.PatchesMap.ContainsKey(block.MetaData.PatchKey))
                        {
                            block.MetaData.PatchKey += patchKeyOffset;
                        }
                        else
                        {
                            //explicitly mark as invalid
                            block.MetaData.PatchKey = -1;
                        }

                        //check if contour is valid before applying index shift
                        if((workPlaneToMerge.MetaData?.Contours != null) && workPlaneToMerge.MetaData.Contours.Count > block.MetaData.ContourIndex 
                            && block.MetaData.ContourIndex >= 0)
                        {
                            block.MetaData.ContourIndex += contoursCount;
                        }
                        else
                        {
                            //explicitly mark as invalid
                            block.MetaData.ContourIndex = -1;
                        }

                        //check if part key is valid before applying index shift
                        if ((block.MetaData != null) && fileReader.fr?.JobShell?.PartsMap != null && fileReader.fr.JobShell.PartsMap.ContainsKey(block.MetaData.PartKey))
                        {
                            block.MetaData.PartKey += fileReader.partKeyIndexShift;
                        }
                        else
                        {
                            //explicitly mark as invalid
                            block.MetaData.PartKey = -1;
                        }
                        PostProcessVectorBlock(block);
                    }

                    //merge meta data
                    if (workPlaneToMerge.MetaData?.PatchesMap != null)
                    {
                        foreach (var kvp in workPlaneToMerge.MetaData.PatchesMap)
                        {
                            var patch = kvp.Value;
                            if (fileReader.translationX != 0 || fileReader.translationY != 0)
                            {
                                //create deep copy of the patch and translate it
                                patch = patch.Clone();
                                var tempBlock = new VectorBlock();
                                tempBlock.LineSequence = patch.OuterContour;
                                tempBlock.Translate(fileReader.translationX, fileReader.translationY);
                            }
                            workPlane.MetaData.PatchesMap.Add(kvp.Key + patchKeyOffset, patch);
                        }
                    }

                    if (workPlaneToMerge.MetaData?.Contours != null)
                        workPlane.MetaData.Contours.Add(workPlaneToMerge.MetaData.Contours);
                }
            }
            return Task.FromResult(workPlane);
        }

        /// <summary>
        /// customizable post processing method for derived classes
        /// </summary>
        /// <param name="vectorBlock"></param>
        protected abstract void PostProcessVectorBlock(VectorBlock vectorBlock);

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            var wp = GetWorkPlaneAsync(i_workPlane);
            var wpShell = new WorkPlane();
            OpenVectorFormat.Utils.ProtoUtils.CopyWithExclude(wp.GetAwaiter().GetResult(), wpShell, new System.Collections.Generic.List<int> { WorkPlane.VectorBlocksFieldNumber });
            return wpShell;
        }

        public override Task OpenJobAsync(string filename, IFileReaderWriterProgress progress)
        {
            List<Task> tasks = new List<Task>();
            foreach (var reader in fileReaders)
            {
                tasks.Add(reader.fr.OpenJobAsync(filename, progress));
            }
            return Task.WhenAll(tasks);
        }

        public override void UnloadJobFromMemory()
        {
            foreach (var reader in fileReaders)
            {
                reader.fr.UnloadJobFromMemory();
            }
        }
    }
}
