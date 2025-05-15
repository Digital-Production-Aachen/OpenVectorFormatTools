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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenVectorFormat;
using System.IO;
using Google.Protobuf;
using static OpenVectorFormat.Part.Types;
using static OpenVectorFormat.VectorBlock.Types;
using static OpenVectorFormat.BuildProcessorStrategy.Types;

namespace OpenVectorFormat.Streaming
{
    /// <summary>
    /// This class processes OVF jobs and applies parameters based on the part material and vector block meta data.
    /// </summary>
    public class ParameterSetEngine : ICloneable
    {
        private BuildProcessorStrategy buildProcessorStrategy;
        private Dictionary<int, MarkingParams> markingParamsMap;
        private Dictionary<LPBFMetadata, int> metaDataToParamsIndexMap;
        private int nextMarkingParamsIndex = 0;
        private OpenVectorFormat.Part partMetaData;

        public IReadOnlyList<ParameterSet> ParameterSets => buildProcessorStrategy.ParameterSets;
        public string BuildProcessorStrategyID { get => buildProcessorStrategy.BuildProcessorStrategyId; set => buildProcessorStrategy.BuildProcessorStrategyId = value; }
        public string Name { get => buildProcessorStrategy.Name; set => buildProcessorStrategy.Name = value; }
        public string MaterialID { get => buildProcessorStrategy.MaterialId; set => buildProcessorStrategy.MaterialId = value; }
        public string MaterialName { get => buildProcessorStrategy.MaterialName; set => buildProcessorStrategy.MaterialName = value; }

        /// <summary>
        /// process strategy used for the main (inSkin/infill) part areas
        /// </summary>
        public ProcessStrategy PartProcessStrategy { get => partMetaData.ProcessStrategy; }
        public ProcessStrategy UpSkinProcessStrategy { get => partMetaData.UpSkinProcessStrategy; }
        public ProcessStrategy DownSkinProcessStrategy { get => partMetaData.DownSkinProcessStrategy; }

        public OpenVectorFormat.Part PartMetaData => partMetaData;

        public IList<LPBFMetadata> exposureOrder { get => partMetaData.ExposureOrder; }

        public ParameterSetEngine(FileInfo saveFile) : this()
        {
            if (saveFile is null)
            {
                throw new ArgumentNullException(nameof(saveFile));
            }

            using (var file = new FileStream(saveFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (CodedInputStream inStream = new CodedInputStream(file, false))
                {
                    buildProcessorStrategy.MergeFrom(inStream);
                }
            }
            ValidateBuildProcessorStrategy();
        }

        public ParameterSetEngine(Stream serializedData) : this()
        {
            using (CodedInputStream inStream = new CodedInputStream(serializedData, true))
            {
                buildProcessorStrategy.MergeFrom(inStream);
            }
            ValidateBuildProcessorStrategy();
        }

        public ParameterSetEngine(BuildProcessorStrategy buildProcessorStrategy) : this()
        {
            this.buildProcessorStrategy = buildProcessorStrategy ?? throw new ArgumentNullException(nameof(buildProcessorStrategy));
            ValidateBuildProcessorStrategy();
        }

        private void ValidateBuildProcessorStrategy()
        {
            int numProcessStrategies = 0;
            //shallow copy the parameter sets and readd valid sets
            var parameterSetsTemp = new List<ParameterSet>();
            parameterSetsTemp.AddRange(buildProcessorStrategy.ParameterSets);
            buildProcessorStrategy.ParameterSets.Clear();
            
            foreach (var paramSet in parameterSetsTemp)
            {
                if (!ValidateParameters(paramSet, out string error))
                {
                    throw new ArgumentException(error);
                }
                if (paramSet.ProcessStrategy != null)
                {
                    numProcessStrategies++;
                }
                AddParameterSet(paramSet);
            }
            if (partMetaData.ProcessStrategy==null)
            {
                if (numProcessStrategies < 1)
                {
                    throw new ArgumentException("no process strategy defined");
                }
                else
                {
                    throw new ArgumentException("no process strategy for InSkin areas defined");
                }
            }
        }

        public ParameterSetEngine()
        {
            buildProcessorStrategy = new BuildProcessorStrategy();
            markingParamsMap = new Dictionary<int, MarkingParams>();
            metaDataToParamsIndexMap = new Dictionary<LPBFMetadata, int>();
            partMetaData = new OpenVectorFormat.Part();
        }

        /// <summary>
        /// Adds the given parameter set to the engine if it is valid and no parameter set for this Meta Data combination has already been set.
        /// Exposure order is defined by the adding order, parameter sets that get added first will get exposed first as well.
        /// </summary>
        /// <param name="parameterSet">parameterSet to add</param>
        /// <returns>bool that indicates if the parameter set was added</returns>
        public bool AddParameterSet(ParameterSet parameterSet)
        {
            bool valid = ValidateParameters(parameterSet, out string errorDescription);
            if (valid)
            {
                valid &= !metaDataToParamsIndexMap.ContainsKey(parameterSet.LpbfMetaData);
                if (valid)
                {
                    buildProcessorStrategy.ParameterSets.Add(parameterSet);
                    markingParamsMap.Add(nextMarkingParamsIndex, parameterSet.MarkingParams);
                    metaDataToParamsIndexMap.Add(parameterSet.LpbfMetaData, nextMarkingParamsIndex);
                    nextMarkingParamsIndex++;
                    partMetaData.ExposureOrder.Add(parameterSet.LpbfMetaData);
                    if (parameterSet.ProcessStrategy != null)
                    {
                        switch (parameterSet.LpbfMetaData.SkinType)
                        {
                            case LPBFMetadata.Types.SkinType.InSkin:
                                //use last added InSkin ProcessStrategy for Part (priority) or any InSkin processStrategy as fallback
                                if (parameterSet.LpbfMetaData.StructureType == StructureType.Part || partMetaData.ProcessStrategy == null)
                                {
                                    partMetaData.ProcessStrategy = parameterSet.ProcessStrategy;
                                }
                                break;
                            case LPBFMetadata.Types.SkinType.DownSkin:
                                //use last added DownSkin ProcessStrategy for Part (priority) or any DownSkin processStrategy as fallback
                                if (parameterSet.LpbfMetaData.StructureType == StructureType.Part || partMetaData.ProcessStrategy == null)
                                {
                                    partMetaData.DownSkinProcessStrategy = parameterSet.ProcessStrategy;
                                }
                                break;
                            case LPBFMetadata.Types.SkinType.UpSkin:
                                //use last added UpSkin ProcessStrategy for Part (priority) or any UpSkin processStrategy as fallback
                                if (parameterSet.LpbfMetaData.StructureType == StructureType.Part || partMetaData.ProcessStrategy == null)
                                {
                                    partMetaData.UpSkinProcessStrategy = parameterSet.ProcessStrategy;
                                }
                                break;
                        }
                    }
                }
            }
            return valid;
        }

        public void ApplyParametersTo(OpenVectorFormat.Job job)
        {
            if (job is null)
            {
                throw new ArgumentNullException(nameof(job));
            }
            ReplaceParamsMapInJobShell(job);
            foreach (var workPlane in job.WorkPlanes)
            {
                ApplyParametersTo(workPlane);
            }
        }

        public void ReplaceParamsMapInJobShell(OpenVectorFormat.Job job)
        {
            if (job is null)
            {
                throw new ArgumentNullException(nameof(job));
            }
            job.MarkingParamsMap.Clear();
            foreach (var param in this.markingParamsMap)
            {
                job.MarkingParamsMap.Add(param.Key, param.Value);
            }
            foreach(var part in job.PartsMap)
            {
                part.Value.Material = new Material() {Name = MaterialName };
                try
                {
                    part.Value.Material.Id = ulong.Parse(MaterialID);
                }catch(Exception e)
                {
                    //don't crash on invalid ids
                }
            }
        }

        public void ApplyParametersTo(WorkPlane workPlane)
        {
            if (workPlane is null)
            {
                throw new ArgumentNullException(nameof(workPlane));
            }
            foreach (var vectorBlock in workPlane.VectorBlocks)
            {
                ApplyParametersTo(vectorBlock);
            }
        }

        public void ApplyParametersTo(VectorBlock vectorBlock)
        {
            if (vectorBlock is null)
            {
                throw new ArgumentNullException(nameof(vectorBlock));
            }

            if (vectorBlock.LpbfMetadata is null)
            {
                throw new ArgumentNullException(nameof(vectorBlock.LpbfMetadata));
            }

            int paramIndex;
            if (metaDataToParamsIndexMap.TryGetValue(vectorBlock.LpbfMetadata, out paramIndex))
            {
                //direct match, we have a parameter set for exactly this block type, we are fine
            }
            else
            {
                //fallback 1, try using InSkin for other skin types
                var metaData = vectorBlock.LpbfMetadata.Clone();
                metaData.SkinType = LPBFMetadata.Types.SkinType.InSkin;
                if (!metaDataToParamsIndexMap.TryGetValue(metaData, out paramIndex))
                {
                    metaData = vectorBlock.LpbfMetadata.Clone();
                    //fallback 2, try using part params for support
                    metaData.StructureType = StructureType.Part;
                    if (!metaDataToParamsIndexMap.TryGetValue(metaData, out paramIndex))
                    {
                        //both fallback 1 and 2, try using inskin part params
                        metaData.SkinType = LPBFMetadata.Types.SkinType.InSkin;
                        if (!metaDataToParamsIndexMap.TryGetValue(metaData, out paramIndex))
                        {
                            //fallback 3, use contour for transition contour, no fallback for missing hatches params
                            if(metaData.PartArea == PartArea.TransitionContour)
                            {
                                metaData.PartArea = PartArea.Contour;
                            }
                            if (!metaDataToParamsIndexMap.TryGetValue(metaData, out paramIndex))
                            {
                                //this case should only happen if skin-core was used during slicing
                                //but no params were added for it. Skin-core requires special params and no fallback is possible
                                throw new ArgumentException("no parameter set or fallback set found for vector block of type " + vectorBlock.LpbfMetadata.ToString());
                            }
                        }
                    }
                }
            }
            vectorBlock.MarkingParamsKey = paramIndex;
        }

        public void Export(Stream output)
        {
            using (CodedOutputStream outStream = new CodedOutputStream(output, true))
            {
                buildProcessorStrategy.WriteTo(outStream);
                outStream.Flush();
            }
        }

        public void Export(FileInfo exportFile)
        {
            exportFile.Directory.Create();
            using (var file = new FileStream(exportFile.FullName, FileMode.Create, FileAccess.Write))
            {
                Export(file);
            }
        }

        /// <summary>
        /// Method that checks to minimum requirements for a parameter set to be valid. Only prevents program crash, does not check processability.
        /// </summary>
        /// <param name="parameterSet"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        public bool ValidateParameters(ParameterSet parameterSet, out string errorDescription)
        {
            if (parameterSet is null)
            {
                throw new ArgumentNullException(nameof(parameterSet));
            }
            //we only check the absolute minimum of requirements here, other fallbacks and defaults are handled by the respective components
            errorDescription = "no error";
            if (parameterSet.LpbfMetaData == null)
            {
                errorDescription = "LPBFMetaData is null";
                return false;
            }
            if (parameterSet.MarkingParams == null)
            {
                errorDescription = "MarkingParams is null";
                return false;
            }
            // a layer thickness <= 0 would result in an undefined number of layers
            if (parameterSet.ProcessStrategy != null && parameterSet.ProcessStrategy.LayerThicknessInMm <= 0)
            {
                errorDescription = "invalid value for LayerThicknessInMm: " + parameterSet.ProcessStrategy.LayerThicknessInMm;
                return false;
            }
            // a mark speed <= 0 would result in vectors that never finish
            if (parameterSet.MarkingParams.LaserSpeedInMmPerS <= 0)
            {
                errorDescription = "invalid value for LaserSpeedInMmPerS: " + parameterSet.ProcessStrategy.LayerThicknessInMm;
                return false;
            }
            return true;
        }

        public object Clone()
        {
            return new ParameterSetEngine(buildProcessorStrategy.Clone());
        }
    }
}
