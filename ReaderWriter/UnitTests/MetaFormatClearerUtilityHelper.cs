using OpenVectorFormat;
using OpenVectorFormat.ReaderWriter.UnitTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    static public class MetaFormatClearerUtilityHelper
    {

        internal static void HandleJobCompare(ref Job originalJob, ref Job convertedJob, string originalExtension, string targetExtension)
        {
            #region Debug
            if (targetExtension == ".cli")
            {
                for (int i = 0; i < originalJob.PartsMap.Count; i++)
                {
                    var partOriginal = originalJob.PartsMap.Values.ToList()[i];
                    var partConverted = convertedJob.PartsMap.Values.ToList()[i];
                    if (partOriginal.GeometryInfo != null)
                    {
                        if (partConverted.GeometryInfo == null) partConverted.GeometryInfo = partOriginal.GeometryInfo.Clone();
                        //partConverted.GeometryInfo.BuildHeightInMm = partOriginal.GeometryInfo.BuildHeightInMm;
                        partConverted.ParentPartName = partOriginal.ParentPartName;
                        partConverted.Material = partOriginal.Material;
                    }
                }
                convertedJob.MarkingParamsMap.Clear();
                foreach (var key in originalJob.MarkingParamsMap.Keys)
                {
                    convertedJob.MarkingParamsMap.Add(key, originalJob.MarkingParamsMap[key]);
                }
                //convertedJob.JobMetaData.Version = originalJob.JobMetaData.Version;
                //convertedJob.JobMetaData.Bounds = originalJob.JobMetaData.Bounds;
                convertedJob.JobMetaData = originalJob.JobMetaData?.Clone();

                for (int i = 0; i < Math.Min(originalJob.WorkPlanes.Count, convertedJob.WorkPlanes.Count); i++)
                {
                    //if (originalJob.WorkPlanes[i].MetaData != null)
                    //{
                    //    convertedJob.WorkPlanes[i].MetaData = originalJob.WorkPlanes[i].MetaData?.Clone();
                    //}
                    var wp1 = originalJob.WorkPlanes[i];
                    var wp2 = convertedJob.WorkPlanes[i];

                    for (int j = 0; j < Math.Min(originalJob.WorkPlanes[i].VectorBlocks.Count, convertedJob.WorkPlanes[i].VectorBlocks.Count); j++)
                    {
                        var vb1 = originalJob.WorkPlanes[i].VectorBlocks[j];
                        var vb2 = convertedJob.WorkPlanes[i].VectorBlocks[j];

                        vb2.MetaData = vb1.MetaData?.Clone();
                        vb1.MarkingParamsKey = 0;
                        vb1.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Part;
                        vb1.LpbfMetadata.SkinCoreStrategyArea = VectorBlock.Types.LPBFMetadata.Types.SkinCoreStrategyArea.OuterHull;

                        vb2.LpbfMetadata = vb1.LpbfMetadata?.Clone();
                    }
                }

                //Delete 3D Data from asp
                if (originalExtension == ".asp")
                {
                    for (int i = 0; i < originalJob.WorkPlanes.Count; i++)
                    {
                        var removeOutOfList = new List<VectorBlock>();
                        for (int j = 0; j < originalJob.WorkPlanes[i].VectorBlocks.Count; j++)
                        {
                            var vb = originalJob.WorkPlanes[i].VectorBlocks[j];
                            if (vb.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence3D ||
                                vb.VectorDataCase == VectorBlock.VectorDataOneofCase.PointSequence3D ||
                                vb.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches3D)
                            {
                                removeOutOfList.Add(vb);
                            }
                        }

                        foreach (var vb in removeOutOfList)
                        {
                            originalJob.WorkPlanes[i].VectorBlocks.Remove(vb);
                        }
                        originalJob.WorkPlanes[i].NumBlocks = originalJob.WorkPlanes[i].VectorBlocks.Count;
                    }
                }
            }
            #endregion

            if (targetExtension == ".asp")
            {
                // ASP has no concept of workplanes, so only single-workplane jobs can be restored properly.
                // After conversion, all workplanes are merged into one for ASP.
                if (originalJob.WorkPlanes.Count <= 1)
                    convertedJob = ASPHelperUtils.HandleJobCompareWithASPTarget(originalJob, convertedJob);
            }

            if (targetExtension != originalExtension)
            {
                // all formats except ovf are unable to store meta data
                var job = originalJob;
                if (targetExtension == ".ovf")
                    job = convertedJob;

                originalJob.JobMetaData.Bounds = null;
                convertedJob.JobMetaData.Bounds = null;

                job.JobParameters = null;
                foreach (var workplane in job.WorkPlanes)
                {
                    workplane.MetaData = null;
                }
            }
        }
    }
}
