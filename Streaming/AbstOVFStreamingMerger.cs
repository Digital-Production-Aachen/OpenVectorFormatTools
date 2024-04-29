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

using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OVFDefinition;

namespace OpenVectorFormat.Streaming
{
    /// <summary>
    /// </summary>
    public abstract class AbstOVFStreamingMerger : FileReader
    {
        //common job shell of the merged file readers
        protected Job mergedJobShell;
        //File Readers to merge (including main File Reader) and if they are to be tagged as support
        protected List<FileReaderToMerge> fileReaders = new List<FileReaderToMerge>();

        /// <summary>
        /// Thickness of layers as determined from file readers. A value of -1 means thickness
        /// is not known yet.
        /// </summary>
        private float layerThickness = -1;

        /// <summary>z height of the lowest layer</summary>
        private float zMin = float.MaxValue;

        public AbstOVFStreamingMerger(FileReaderToMerge fileReaderToMerge)
        {
            AddFileReaderToMerge(fileReaderToMerge);
        }

        public void AddFileReaderToMerge(FileReaderToMerge fileReaderToMerge)
        {
            float layerThicknessToMerge = GetLayerThickness(fileReaderToMerge);
            // if no file reader has been added, no restrictions
            if (fileReaders.Count == 0)
            {
                IntegrateFileReaderToMerge(fileReaderToMerge);
            }
            // if all file readers so far had one layer
            else if (layerThickness == -1)
            {
                // if new file reader also has one layer, require z pos to match
                if (layerThicknessToMerge == -1)
                {
                    if (OVFDefinition.Utils.ApproxEquals(fileReaderToMerge.fr.GetWorkPlaneShell(0).ZPosInMm, zMin))
                        IntegrateFileReaderToMerge(fileReaderToMerge);
                    else throw new NotSupportedException($"Cannot merge file readers with a single layer " +
                        $"at different z positions");
                }
                // else no restrictions
                else IntegrateFileReaderToMerge(fileReaderToMerge);
            }
            // if layer thickness is known, make sure the new layer thickness matches
            else
            {
                if (layerThicknessToMerge == -1 || OVFDefinition.Utils.ApproxEquals(layerThicknessToMerge, layerThickness))
                    IntegrateFileReaderToMerge(fileReaderToMerge);
                else throw new NotSupportedException($"cannot merge file readers with different layer thicknesses: " +
                    $"({layerThickness} mm vs. {layerThicknessToMerge} mm)");
            }
        }

        /// <summary>
        /// Adds a new FileReaderToMerge (assuming it is valid!), and recalculates
        /// layer offsets, min z height etc.
        /// </summary>
        /// <param name="fileReaderToMerge"></param>
        private void IntegrateFileReaderToMerge(FileReaderToMerge fileReaderToMerge)
        {
            fileReaderToMerge.zMin = fileReaderToMerge.fr.GetWorkPlaneShell(0).ZPosInMm;
            fileReaders.Add(fileReaderToMerge);
            zMin = Math.Min(zMin, fileReaderToMerge.zMin);
            if (mergedJobShell == null) mergedJobShell = fileReaderToMerge.fr.JobShell.Clone();
            mergedJobShell.NumWorkPlanes = fileReaders.Max(x => x.layerOffset + x.fr.JobShell.NumWorkPlanes);
            if (layerThickness == -1) layerThickness = GetLayerThickness(fileReaderToMerge);

            foreach (var fr in fileReaders)
            {
                fr.layerOffset = layerThickness != -1 ? (int)Math.Round((fr.zMin - zMin) / layerThickness) : 0;
            }
        }

        private float GetLayerThickness(FileReaderToMerge fileReaderToMerge)
        {
            // if a process strategy is provided, read layer thickness from there
            float? result = fileReaderToMerge.fr.JobShell?.PartsMap.FirstOrDefault().Value.ProcessStrategy?.LayerThicknessInMm;
            if (result.HasValue) return result.Value;
            // else try calculating thickness as the height difference between the first two layers
            else if (fileReaderToMerge.fr.JobShell != null && fileReaderToMerge.fr.JobShell.NumWorkPlanes >= 2)
            {
                return fileReaderToMerge.fr.GetWorkPlaneShell(1).ZPosInMm - fileReaderToMerge.fr.GetWorkPlaneShell(0).ZPosInMm;
            }
            else return -1;
        }

        public override CacheState CacheState => fileReaders[0].fr.CacheState;

        public override Job JobShell => mergedJobShell;

        public override Job CacheJobToMemory()
        {
            foreach(var reader in fileReaders)
            {
                reader.fr.CacheJobToMemory();
            }
            var job = mergedJobShell.Clone();
            job.AddAllWorkPlanesParallel(GetWorkPlane);
            return job;
        }

        public override void Dispose()
        {
           foreach(var reader in fileReaders)
            {
                reader.fr.Dispose();
            }
        }

        public override VectorBlock GetVectorBlock(int i_workPlane, int i_vectorblock)
        {
            return GetWorkPlane(i_workPlane).VectorBlocks[i_vectorblock];
        }

        public override WorkPlane GetWorkPlane(int i_workPlane)
        {
            var mergedWorkPlane = new WorkPlane();
            mergedWorkPlane.MetaData = new WorkPlane.Types.WorkPlaneMetaData();
            mergedWorkPlane.WorkPlaneNumber = i_workPlane;
            bool zPosSet = false;
            for (int i = 0; i < fileReaders.Count; i++)
            {
                var frToMerge = fileReaders[i];
                var indexWithOffset = i_workPlane - frToMerge.layerOffset;
                var patchKeyOffset = mergedWorkPlane.MetaData.PatchesMap.Any() ? mergedWorkPlane.MetaData.PatchesMap.Keys.Max() + 1 : 0;
                var contoursCount = mergedWorkPlane.MetaData.Contours.Count;
                if (indexWithOffset >= 0 && indexWithOffset < frToMerge.fr.JobShell.NumWorkPlanes)
                {
                    var workPlaneToMerge = frToMerge.fr.GetWorkPlane(indexWithOffset);

                    if (!zPosSet)
                    {
                        mergedWorkPlane.ZPosInMm = workPlaneToMerge.ZPosInMm;
                        zPosSet = true;
                    } // check if z pos matches
                    else if (!OVFDefinition.Utils.ApproxEquals(workPlaneToMerge.ZPosInMm, mergedWorkPlane.ZPosInMm))
                    {
                        throw new StreamingMergerException("z pos of work planes to merge does not match");
                    }

                    for (int j = 0; j < workPlaneToMerge.VectorBlocks.Count; j++)
                    {
                        
                        var block = workPlaneToMerge.VectorBlocks[j];
                        if (frToMerge.translationX != 0 || frToMerge.translationY != 0)
                        {
                            //create deep copies of the vector blocks coordinates
                            block = block.Clone();
                            block.Rotate(frToMerge.rotationInRad);
                            block.Translate(new System.Numerics.Vector2(frToMerge.translationX, frToMerge.translationY));
                        }
                        else
                        {
                            //create shallow copy only. No side effects as long as we don't change the coordinates
                            block = block.ShallowCopy();
                        }

                        if (frToMerge.markAsSupport)
                        {
                            block.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Support;
                        }

                        mergedWorkPlane.VectorBlocks.Add(block);
                        mergedWorkPlane.NumBlocks++;


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
                        if ((block.MetaData != null) && frToMerge.fr?.JobShell?.PartsMap != null && frToMerge.fr.JobShell.PartsMap.ContainsKey(block.MetaData.PartKey))
                        {
                            block.MetaData.PartKey += frToMerge.partKeyIndexShift;
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
                            if (frToMerge.translationX != 0 || frToMerge.translationY != 0)
                            {
                                //create deep copy of the patch and translate it
                                patch = patch.Clone();
                                var tempBlock = new VectorBlock();
                                tempBlock.LineSequence = patch.OuterContour;
                                tempBlock.Rotate(frToMerge.rotationInRad);
                                tempBlock.Translate(new System.Numerics.Vector2(frToMerge.translationX, frToMerge.translationY));
                            }
                            mergedWorkPlane.MetaData.PatchesMap.Add(kvp.Key + patchKeyOffset, patch);
                        }
                    }

                    if (workPlaneToMerge.MetaData?.Contours != null)
                        mergedWorkPlane.MetaData.Contours.Add(workPlaneToMerge.MetaData.Contours);
                }
            }
            return mergedWorkPlane;
        }

        /// <summary>
        /// customizable post processing method for derived classes
        /// </summary>
        /// <param name="vectorBlock"></param>
        protected abstract void PostProcessVectorBlock(VectorBlock vectorBlock);

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            var wp = GetWorkPlane(i_workPlane);
            return wp.CloneWithoutVectorData();
        }

        public override void OpenJob(string filename, IFileReaderWriterProgress progress)
        {
            throw new NotSupportedException("Add file readers to merge and open their files individually using AddFileReaderToMerge");
        }

        public override void UnloadJobFromMemory()
        {
            foreach (var reader in fileReaders)
            {
                reader.fr.UnloadJobFromMemory();
            }
        }
    }

    public class StreamingMergerException : Exception
    {
        public StreamingMergerException(string message) : base(message) { }
    }
}
