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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenVectorFormat.ILTFileReader;
using OpenVectorFormat.Utils;
using System.IO;

namespace OpenVectorFormat.ILTFileReaderAdapter
{
    /// <summary>
    /// Adapter pattern class that converts the ILTFile structure found in ILTFileReader.IBuildJob into protobuf structure defined in FileReader/FileFormat.cs.
    /// This includes resorting vector information and parameters form the model sections.
    /// </summary>
    public class ILTFileReaderAdapter : FileReader
    {
        private bool _fileLoadingFinished;
        private bool vectorDataLoaded;
        private CacheState _cacheState = CacheState.NotCached; 
        private IFileAccess fileHandler;
        private IBuildJob buildJob;
        private ICLIFile cliFile;
        private Dictionary<IModelSection, int> ModelsectionMap;
        private Dictionary<string, int> ModelsectionIdMap;
        //private Dictionary<VectorBlock, IVectorBlock> vectorBlockDictionary;
        private Job job;
        private int workPlaneNumber = 0;
        private IFileReaderWriterProgress progress;
        private string filename;
        private string jobfilename;
        private static List<string> fileFormats = new List<string>() { ".ilt", ".cli" };
        /// <summary>
        /// minimum length of a jump between hatches to be considered a real hatch
        /// </summary>
        private readonly float minJumpLength = 0.0001f;

        public ILTFileReaderAdapter(IFileAccess fileAccess)
        {
            this.fileHandler = fileAccess ?? throw new ArgumentNullException(nameof(fileAccess));
            if (fileHandler is IBuildJob)
            {
                buildJob = (IBuildJob)fileHandler;
            }
            else if (fileHandler is ICLIFile)
            {
                cliFile = (ICLIFile)fileHandler;
            }
        }

        #region FileReader

        public override Job JobShell 
        {
            get
            {
                if (_cacheState == CacheState.CompleteJobCached)
                {
                    Job jobShell = new Job();
                    ProtoUtils.CopyWithExclude(job, jobShell, new List<int> { Job.WorkPlanesFieldNumber });
                    return jobShell;
                }
                else
                {
                    throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
                }
            }
        }

        public override CacheState CacheState => _cacheState;

        public override async Task OpenJobAsync(string filename, IFileReaderWriterProgress progress)
        {
            this.progress = progress;
            this.filename = filename;
            _fileLoadingFinished = false;
            _cacheState = CacheState.NotCached;
            ModelsectionMap = new Dictionary<IModelSection, int>();
            ModelsectionIdMap = new Dictionary<string, int>();
            job = new Job();
            await Task.Run(() =>
            {
                OpenFile();
            });
            return;
        }

        public async override Task<Job> CacheJobToMemoryAsync()
        {
            CheckFile();
            if (!vectorDataLoaded)
            {
                for (int i = 0; i < job.WorkPlanes.Count; i++)
                {
                    var workPlane = job.WorkPlanes[i];
                    for (int j = 0; j < workPlane.VectorBlocks.Count; j++)
                    {
                        LoadVectorDataIntoBlock(i, j, false);
                    }
                }
                vectorDataLoaded = true;
            }
            return job;
        }

        public async override Task<WorkPlane> GetWorkPlaneAsync(int i_workPlane)
        {
            if (vectorDataLoaded)
            {
                return job.WorkPlanes[i_workPlane];
            }
            else
            {
                WorkPlane fullWorkPlane = job.WorkPlanes[i_workPlane].Clone();//copy the shell
                for (int j = 0; j < fullWorkPlane.VectorBlocks.Count; j++)
                {
                    fullWorkPlane.VectorBlocks[j] = await GetVectorBlockAsync(i_workPlane, j);
                }
                return fullWorkPlane;//ownership passed
            }
        }

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            if (job.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane " + i_workPlane.ToString() + " out of range for jobfile with " + job.NumWorkPlanes.ToString() + " workPlanes!");
            }
            if (CacheState == CacheState.CompleteJobCached)
            {
                WorkPlane wpShell = new WorkPlane();
                ProtoUtils.CopyWithExclude(job.WorkPlanes[i_workPlane], wpShell, new List<int> { WorkPlane.VectorBlocksFieldNumber });
                return wpShell;
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
            }
        }

        public async override Task<VectorBlock> GetVectorBlockAsync(int i_workPlane, int i_vectorblock)
        {
            if (vectorDataLoaded)
            {
                return job.WorkPlanes[i_workPlane].VectorBlocks[i_vectorblock];
            }
            else
            {
                return LoadVectorDataIntoBlock(i_workPlane, i_vectorblock, true);//ownership passed
            }
        }

        #endregion // FileReader

        /// <summary>
        /// List of file format extensions supported by this file reader.
        /// </summary>
        public static new List<string> SupportedFileFormats => fileFormats;

        private void CheckFile()
        {
            if (this.job == null) throw new InvalidOperationException("No file opened. Please open a job file first");
        }

        private IVectorBlock GetILTBlock(VectorBlock block, int workPlaneNumber, int index, out float unit)
        {
            //derive model section number
            int i = 0;
            var section = buildJob.ModelSections[i];
            //while(section.Geometry.WorkPlanes[i])
            while (index >= section.Geometry.Layers[workPlaneNumber].VectorBlocks.Count)
            {
                index -= section.Geometry.Layers[workPlaneNumber].VectorBlocks.Count;
                index -= section.Geometry.Layers[workPlaneNumber].VectorBlocks.Count;
                i++;
                section = buildJob.ModelSections[i];
            }
            unit = section.Header.Units;
            return section.Geometry.Layers[workPlaneNumber].VectorBlocks[index];
        }

        private void OpenFile()
        {
            fileHandler.OpenFile(filename);
            jobfilename = Path.GetFileNameWithoutExtension(filename);
            if (buildJob != null)
            {
                ConvertILTStructure(progress);
            }
            else if (cliFile != null)
            {
                ConvertCLIStructure(progress);
            }
            else
            {
                Debug.Fail("illegal state, fileHandler not initialized");
            }
            _fileLoadingFinished = true;
            _cacheState = CacheState.CompleteJobCached;
        }

        public override void UnloadJobFromMemory()
        {
            CheckFile();
            if (vectorDataLoaded)
            {
                //delete vactor data
                for (int i = 0; i < job.WorkPlanes.Count; i++)
                {
                    for (int j = 0; j < job.WorkPlanes[i].VectorBlocks.Count; j++)
                    {
                        job.WorkPlanes[i].VectorBlocks[j].ClearVectorData();
                    }
                }
                vectorDataLoaded = false;
            }
        }
        /// <summary>
        /// Translates an IWorkPlane from a cliFile to a FilReader WorkPlane
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="cLIFile"></param>
        private WorkPlane CreateWorkPlane(ILayer layer, ICLIFile clIFile)
        {
            WorkPlane currentWorkPlane = new WorkPlane();
            currentWorkPlane.ZPosInMm = layer.Height * clIFile.Header.Units;
            currentWorkPlane.NumBlocks = 0;
            currentWorkPlane.WorkPlaneNumber = workPlaneNumber++;
            //WorkPlane recoatinig set to false, because we don't have this information
            currentWorkPlane.Repeats = 0;
            //add all workPlanes to the jobfile
            if (job.WorkPlanes.Count == 0 || currentWorkPlane.ZPosInMm > job.WorkPlanes[job.WorkPlanes.Count - 1].ZPosInMm)
            {
                //workPlane is in order
                job.WorkPlanes.Add(currentWorkPlane);
            }
            else
            {
                int insertIndex = 0;
                while (job.WorkPlanes[insertIndex].ZPosInMm < currentWorkPlane.ZPosInMm)
                {
                    insertIndex++;
                }
                job.WorkPlanes.Insert(insertIndex, currentWorkPlane);
            }
            return currentWorkPlane;
        }

        private void AddVectorBlocksToWorkPlane(WorkPlane buildJobWorkPlane, ILayer sectionLayer, IModelSection section)
        {
            Debug.Assert(Math.Abs(buildJobWorkPlane.ZPosInMm - sectionLayer.Height * section.Header.Units) < 0.00001);//check if same heigth
            buildJobWorkPlane.NumBlocks += sectionLayer.VectorBlocks.Count;
            foreach (IVectorBlock ILTvBlock in sectionLayer.VectorBlocks)
            {
                var newBlock = TranslateBlockData(ILTvBlock, section);
                //block might be split into hatches and polylines for fake hatches
                var newBlocks = ReadCoordinates(ILTvBlock, newBlock, section.Header.Units);

                vectorDataLoaded = true;
                job.PartsMap.TryGetValue(newBlock.MetaData.PartKey, out Part part);
                Debug.Assert(part != null);
                if (part.GeometryInfo.BuildHeightInMm < buildJobWorkPlane.ZPosInMm)
                {
                    part.GeometryInfo.BuildHeightInMm = buildJobWorkPlane.ZPosInMm;
                }
                buildJobWorkPlane.VectorBlocks.Add(newBlocks);
            }
        }

        /// <summary>
        /// Scans the ILT file and converts the structural data to a target
        /// </summary>
        private void ConvertILTStructure(IFileReaderWriterProgress progress)
        {
            // ilt file is handled
            SetJobData(buildJob);
            double sectionProgress = 0;
            foreach (IModelSection section in buildJob.ModelSections)
            {
                /*convert Modelsection to Part, ignore every "parts" in a Modelsection, since 
                they don't provide any information*/
                TranslateModelsectionToPart(section);
                for (int i = 0; i < section.Geometry.Layers.Count; i++)
                {
                    WorkPlane buildJobWorkPlane = null;
                    ILayer sectionWorkPlane = section.Geometry.Layers[i];
                    foreach (var workPlane in job.WorkPlanes)
                    {
                        if (Math.Abs(workPlane.ZPosInMm - sectionWorkPlane.Height) < 0.00001)
                        {
                            buildJobWorkPlane = workPlane;
                            break;
                        }
                    }
                    if (buildJobWorkPlane == null)
                    {
                        buildJobWorkPlane = CreateWorkPlane(sectionWorkPlane, section);
                    }

                    AddVectorBlocksToWorkPlane(buildJobWorkPlane, sectionWorkPlane, section);

                    if (i % 100 == 0)
                    {
                        progress.Update(section.ModelsectionName + " layer " + i + @"/" + section.Geometry.Layers.Count,
                            (int)((sectionProgress / buildJob.ModelSections.Count) * 100.0
                            + ((i / (double)section.Geometry.Layers.Count) * 100.0 / buildJob.ModelSections.Count)));
                    }

                }
                sectionProgress++;
            }

            var workPlaneNumber = 0;
            foreach (var workPlane in job.WorkPlanes)
            {
                workPlane.WorkPlaneNumber = workPlaneNumber++;//override workPlane numbers after sorting
                workPlane.NumBlocks = workPlane.VectorBlocks.Count;
            }
            job.NumWorkPlanes = job.WorkPlanes.Count;
        }

        private void ConvertCLIStructure(IFileReaderWriterProgress progress)
        {
            job.JobMetaData = TranslateMetaData(cliFile);
            job.JobMetaData.JobName = this.jobfilename;
            foreach (IPart part in cliFile.Parts)
            {
                TranslateCliPart(part);
            }
            for (int i = 0; i < cliFile.Geometry.Layers.Count; i++)
            {
                ILayer workPlane = cliFile.Geometry.Layers[i];
                var newWorkPlane = CreateWorkPlane(workPlane, cliFile);
                foreach (var block in workPlane.VectorBlocks)
                {
                    newWorkPlane.VectorBlocks.Add(TranslateBlockData(block, cliFile));
                }
                newWorkPlane.NumBlocks = workPlane.VectorBlocks.Count;
                if (i % 100 == 0)
                {
                    progress.Update("workPlane " + i + @"/" + cliFile.Geometry.Layers.Count,
                        (int)((i / (double)cliFile.Geometry.Layers.Count) * 100.0));
                }
            }
            job.NumWorkPlanes = job.WorkPlanes.Count;
        }

        /// <summary>
        /// Sets the MetaData inside a BuildJob
        /// Sets up the BuildParameters Map
        /// </summary>
        /// <param name="buildJob"></param>
        private void SetJobData(IBuildJob buildJob)
        {
            int i = 0;
            foreach (IModelSection section in buildJob.ModelSections)
            {
                job.MarkingParamsMap.Add(i, TranslateBuildParams(section.Parameters));
                ModelsectionMap.Add(section, i);
                i++;
            }
            //jobCreationTime is the same across all ModelSections
            job.JobMetaData = TranslateMetaData(buildJob.ModelSections[0]);
            job.JobMetaData.JobName = jobfilename;
        }

        private VectorBlock TranslateBlockData(IVectorBlock cliBlock, ICLIFile cliFile)
        {
            VectorBlock block = new VectorBlock();
            block.LpbfMetadata = new VectorBlock.Types.LPBFMetadata();
            block.MetaData = new VectorBlock.Types.VectorBlockMetaData();
            block.MetaData.PartKey = cliBlock.Id;
            block.LpbfMetadata.Reexposure = false;
            return block;
        }

        private VectorBlock TranslateBlockData(IVectorBlock iltBlock, IModelSection modelSection)
        {
            VectorBlock block = new VectorBlock();
            block.LpbfMetadata = new VectorBlock.Types.LPBFMetadata();
            block.MetaData = new VectorBlock.Types.VectorBlockMetaData();
            block.MetaData.PartKey = ModelsectionIdMap[modelSection.ModelsectionName];
            //this information gets lost
            block.LpbfMetadata.Reexposure = false;

            //SetJobData has to be called before using this
            block.MarkingParamsKey = ModelsectionMap[modelSection];
            Debug.Assert(job.MarkingParamsMap.ContainsKey(block.MarkingParamsKey));
            /* vs=Volumen Schraffur, vk=Volumen Kontur, u steht fuer unten-> down,
             us=Downskin Schraffur, uk=Downskin Kontur, kv=Kontur Versatz, sx = single vector, support */
            switch (modelSection.SubType)
            {
                case VectorClass.vs:
                    block.LpbfMetadata.SkinType = VectorBlock.Types.LPBFMetadata.Types.SkinType.InSkin;
                    block.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Volume;
                    break;
                case VectorClass.vk:
                    block.LpbfMetadata.SkinType = VectorBlock.Types.LPBFMetadata.Types.SkinType.InSkin;
                    block.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Contour;
                    break;
                case VectorClass.us:
                    block.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Volume;
                    block.LpbfMetadata.SkinType = VectorBlock.Types.LPBFMetadata.Types.SkinType.DownSkin;
                    break;
                case VectorClass.uk:
                    block.LpbfMetadata.SkinType = VectorBlock.Types.LPBFMetadata.Types.SkinType.DownSkin;
                    block.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Contour;
                    break;
                case VectorClass.kv:
                    block.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.TransitionContour;
                    block.LpbfMetadata.SkinType = VectorBlock.Types.LPBFMetadata.Types.SkinType.InSkin;
                    break;
                case VectorClass.kvu:
                    block.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Contour;
                    block.LpbfMetadata.SkinType = VectorBlock.Types.LPBFMetadata.Types.SkinType.InSkin;
                    break;
                case VectorClass.skin:
                    //different from downskin, no separate parameters for contour and volume, both in this file
                    block.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Volume;//hatches are more
                    block.LpbfMetadata.SkinType = VectorBlock.Types.LPBFMetadata.Types.SkinType.UpSkin;
                    break;
                case VectorClass.sx:
                    block.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Support;
                    //PartArea = Single Vector
                    break;
                default:
                    block.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Volume;
                    block.LpbfMetadata.SkinType = VectorBlock.Types.LPBFMetadata.Types.SkinType.InSkin;
                    break;
            }
            //st=Stützen, k=Kern, s1=Hülle1, s2=Hülle2
            PartArea partArea = modelSection.Type;
            switch (partArea)
            {
                case PartArea.st:
                    block.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Support;
                    block.LpbfMetadata.SkinCoreStrategyArea = VectorBlock.Types.LPBFMetadata.Types.SkinCoreStrategyArea.OuterHull;
                    break;
                case PartArea.k:
                    block.LpbfMetadata.SkinCoreStrategyArea = VectorBlock.Types.LPBFMetadata.Types.SkinCoreStrategyArea.Core;
                    block.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Part;
                    break;
                case PartArea.s1:
                    block.LpbfMetadata.SkinCoreStrategyArea = VectorBlock.Types.LPBFMetadata.Types.SkinCoreStrategyArea.OuterHull;
                    block.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Part;
                    break;
                case PartArea.s2:
                    block.LpbfMetadata.SkinCoreStrategyArea = VectorBlock.Types.LPBFMetadata.Types.SkinCoreStrategyArea.InbetweenHull;
                    block.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Part;
                    break;
                //PART is default, usually refering to a tesselated model, e.g. STL models.
                //OuterHull is default for using the small spot for the whole part.
                default:
                    throw new ArgumentException("Unknown PartArea");

            }
            return block;
        }

        /// <summary>
        /// Loads the vector data into block workPlane i block j (All data will be read from disk into RAM).
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        private VectorBlock LoadVectorDataIntoBlock(int i, int j, bool clone)
        {
            VectorBlock fullBlock;
            if (clone)
            {
                fullBlock = job.WorkPlanes[i].VectorBlocks[j].Clone();
            }
            else
            {
                fullBlock = job.WorkPlanes[i].VectorBlocks[j];
            }
            IVectorBlock readBlock = null;
            float unit = 0;
            if (cliFile != null)
            {
                readBlock = cliFile.Geometry.Layers[i].VectorBlocks[j];
                unit = cliFile.Header.Units;
            }
            else if (buildJob != null)
            {
                readBlock = GetILTBlock(fullBlock, i, j, out unit);
            }
            else
            {
                Debug.Fail("state error");
            }

            var blocks = ReadCoordinates(readBlock, fullBlock, unit);
            if (blocks.Count > 1)
            {
                for (int k = 1; k < blocks.Count; k++)
                {
                    job.WorkPlanes[i].VectorBlocks.Insert(j + k, blocks[k]);
                    job.WorkPlanes[i].NumBlocks++;
                }

            }

            return fullBlock;
        }

        /// <summary>
        /// Reads the coordinates from readBlock into block, converting with the unit found in the cli header.
        /// Implements a fake hatches detection: vector blocks with hatches will be searched for identical start and end coordinates
        /// and converted into a polyline. Additional hatches at the end of the "fake" polyline will be returned as a new VectroBlock.
        /// </summary>
        /// <param name="readBlock"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        private List<VectorBlock> ReadCoordinates(IVectorBlock readBlock, VectorBlock block, float unit)
        {
            List<VectorBlock> blocks = new List<VectorBlock>();
            var tempBlock = block.Clone();
            blocks.Add(block);
            if (readBlock is IPolyline)
            {
                block.LineSequence = new VectorBlock.Types.LineSequence();
                foreach (var coordinate in readBlock.Coordinates)
                {
                    block.LineSequence.Points.Add(coordinate * unit);
                }
            }
            else if (readBlock is IHatches)
            {
                VectorBlock currBlock = block;
                var buffer = readBlock.Coordinates;
                var coords = new float[buffer.Length];
                for (int i = 0; i < buffer.Length; i++)
                {
                    coords[i] = buffer[i] * unit;
                }
                //fake hatches detection, some (stupid) slicers put polylines into hatches with end point of the first hatch beeing start point of the second etc.
                bool fakeHatches = false;
                if (coords.Length > 8)
                {
                    var xDif = Math.Abs(coords[2] - coords[4]);
                    var yDif = Math.Abs(coords[3] - coords[5]);
                    if (xDif < minJumpLength && yDif < minJumpLength)
                    {
                        //fakeHatches = true;
                    }
                }

                if (fakeHatches)
                {
                    bool polylines = true;
                    currBlock.LineSequence = new VectorBlock.Types.LineSequence();
                    currBlock.LineSequence.Points.Add(coords[0]);
                    currBlock.LineSequence.Points.Add(coords[1]);
                    currBlock.LineSequence.Points.Add(coords[2]);
                    currBlock.LineSequence.Points.Add(coords[3]);
                    for (int k = 4; k < coords.Length; k += 4)
                    {
                        var xDif = Math.Abs(coords[k] - coords[k - 2]);
                        var yDif = Math.Abs(coords[k + 1] - coords[k - 1]);
                        if (xDif < minJumpLength && yDif < minJumpLength)
                        {
                            if (!polylines)
                            {
                                currBlock = tempBlock.Clone();
                                currBlock.LineSequence = new VectorBlock.Types.LineSequence();
                                blocks.Add(currBlock);
                                currBlock.LineSequence.Points.Add(coords[k]);
                                currBlock.LineSequence.Points.Add(coords[k + 1]);
                            }

                            //ignore start point of 2. "hatch", since it is the end point of the previous hatch
                            currBlock.LineSequence.Points.Add(coords[k + 2]);
                            currBlock.LineSequence.Points.Add(coords[k + 3]);

                            polylines = true;
                        }
                        else
                        {
                            if (polylines)
                            {
                                currBlock = tempBlock.Clone();
                                currBlock.Hatches = new VectorBlock.Types.Hatches();
                                blocks.Add(currBlock);
                            }
                            currBlock.Hatches.Points.Add(coords[k]);
                            currBlock.Hatches.Points.Add(coords[k + 1]);
                            currBlock.Hatches.Points.Add(coords[k + 2]);
                            currBlock.Hatches.Points.Add(coords[k + 3]);
                            polylines = false;
                        }
                    }
                }
                else
                {
                    block.Hatches = new VectorBlock.Types.Hatches();
                    block.Hatches.Points.AddRange(coords);
                }
            }
            else
            {
                throw new FormatException("Vectorblocktype unknown: not Hatches or Polyline: " + readBlock?.ToString());
            }
            return blocks;
        }

        //LaserIndex set to 1, since we only have 1 laser
        private MarkingParams TranslateBuildParams(IModelSectionParams iltParams)
        {
            return new MarkingParams
            {
                LaserFocusShiftInMm = (float)iltParams.FocusShift,
                LaserPowerInW = (float)iltParams.LaserPower,
                LaserSpeedInMmPerS = (float)iltParams.LaserSpeed,
                //PointDistance = (float)iltParams.PointDistance,
                PointExposureTimeInUs = (float)iltParams.ExposureTime,
            };
        }
        private Job.Types.JobMetaData TranslateMetaData(ICLIFile section)
        {
            Job.Types.JobMetaData metaData = new Job.Types.JobMetaData();
            if (section.Header.Date != 0)
            {
                //assuming that the date is after the year 2000, since there is no information about it
                int year = (section.Header.Date % 100) + 2000;
                int month = (section.Header.Date / 100) % 100;
                int day = section.Header.Date % 100;
                DateTime date = new DateTime(year, month, day);
                metaData.JobCreationTime = new DateTimeOffset(date).ToUnixTimeSeconds();
            }
            metaData.Version = (ulong)section.Header.Version;
            return metaData;
        }
        //Generate Parts from unique ModelSections, no BuildStrategy available
        private void TranslateModelsectionToPart(IModelSection modelSection)
        {
            if (modelSection.Parts.Count > 1)
            {
                Debug.WriteLine("You are now loosing Part information, since there is more than 1 part in a ModelSection");
            }
            Part part = new Part();
            part.Material = new Part.Types.Material();
            int partId = Int32.Parse(Regex.Match(modelSection.ModelsectionName, @"[0-9]+").Value);
            part.Name = modelSection.ModelsectionName;
            part.GeometryInfo = new Part.Types.GeometryInfo()
            {
                BuildHeightInMm = modelSection.Geometry.Layers[modelSection.Geometry.Layers.Count - 1].Height
            };

            if (!job.PartsMap.ContainsKey(partId))
            {
                job.PartsMap.Add(partId, part);
                ModelsectionIdMap.Add(modelSection.ModelsectionName, partId);
            }
        }

        private void TranslateCliPart(IPart cliPart)
        {
            Part part = new Part();
            part.Material = new Part.Types.Material();
            part.Name = cliPart.name;
            job.PartsMap.Add(cliPart.id, part);
        }


        public override void Dispose()
        {
            fileHandler?.CloseFile();
            _cacheState = CacheState.NotCached;
            job = null;
        }
        public override void CloseFile() { fileHandler?.CloseFile(); }
    }
}
