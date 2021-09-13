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



using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using OpenVectorFormat.AbstractReaderWriter;
using System.Text.RegularExpressions;

namespace OpenVectorFormat.FileReaderWriterFactory
{
    public class FileConverter
    {
        MarkingParams fallbackContouringParams;
        MarkingParams fallbackHatchingParams;
        MarkingParams fallbackSupportContouringParams;
        MarkingParams fallbackSupportHatchingParams;
        private string supportPostfix;
        private System.Text.RegularExpressions.Regex supportRegex;

        public FileConverter()
        {
            SupportPostfix = "_support";
        }

        public MarkingParams FallbackContouringParams { get => fallbackContouringParams; set => fallbackContouringParams = value; }
        public MarkingParams FallbackHatchingParams { get => fallbackHatchingParams; set => fallbackHatchingParams = value; }
        public MarkingParams FallbackSupportContouringParams { get => fallbackSupportContouringParams; set => fallbackSupportContouringParams = value; }
        public MarkingParams FallbackSupportHatchingParams { get => fallbackSupportHatchingParams; set => fallbackSupportHatchingParams = value; }
        //support postfix used to identify parts that are a support of another part
        public string SupportPostfix
        {
            get => supportPostfix;
            set
            {
                supportPostfix = value;
                supportRegex = new System.Text.RegularExpressions.Regex(@"\b(.*)" + supportPostfix + @"\b");
            }
        }

        /// <summary>
        /// Convert a given vector file (a supported reader must be available) into the target format.
        /// </summary>
        /// <param name="file">a file to load in a supported format</param>
        /// <param name="targetFile">the target file. extension decides which writer will be used</param>
        /// <param name="progress">progress interface to call for updates</param>
        public static async System.Threading.Tasks.Task ConvertAsync(FileInfo file, FileInfo targetFile, IFileReaderWriterProgress progress)
        {
            CheckExtensions(file.Extension, targetFile.Extension);
            using (var reader = FileReaderFactory.CreateNewReader(file.Extension))
            {
                await reader.OpenJobAsync(file.FullName, progress);
                using (var writer = FileWriterFactory.CreateNewWriter(targetFile.Extension))
                {
                    writer.StartWritePartial(reader.JobShell, targetFile.FullName, progress);

                    for (int i = 0; i < reader.JobShell.NumWorkPlanes; i++)
                    {
                        await writer.AppendWorkPlaneAsync(await reader.GetWorkPlaneAsync(i));
                    }
                }
            }
        }

        /// <summary>
        /// Convert a given vector file (a supported reader must be available) into the target format.
        /// Uses fallback values set to this file converter to add parameters to vectorblocks that lack parameters.
        /// If a support postfix is provided, part names (if available) will be matched to determine if vector blocks shall use support parameters.
        /// Creates a printable file from formats that don't support parameters, e.g. CLI.
        /// Adds Metadata for LPBF accordingly.
        /// </summary>
        /// <param name="file">a file to load in a supported format</param>
        /// <param name="targetFile">the target file. extension decides which writer will be used</param>
        /// <param name="progress">progress interface to call for updates</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task ConvertAsyncAddParams(FileInfo file, FileInfo targetFile, IFileReaderWriterProgress progress)
        {
            CheckExtensions(file.Extension, targetFile.Extension);
            using (var reader = FileReaderFactory.CreateNewReader(file.Extension))
            {
                await reader.OpenJobAsync(file.FullName, progress);

                #region prepare job shell
                //jobShell gets written only when file is finalized, so we can edit it while streaming layers
                var extendedJobShell = reader.JobShell.Clone();
                //mapping of support part keys to real part keys
                Dictionary<int, int> SupportPartsMapping = new Dictionary<int, int>();
                int maxParamKey = extendedJobShell.MarkingParamsMap.Count > 0 ? extendedJobShell.MarkingParamsMap.Keys.Max() : -1;
                int contourParamsKey = maxParamKey + 1;
                int hatchParamsKey = maxParamKey + 2;
                int contourSupportParamsKey = maxParamKey + 3;
                int hatchSupportParamsKey = maxParamKey + 4;
                extendedJobShell.MarkingParamsMap.Add(contourParamsKey, fallbackContouringParams);
                extendedJobShell.MarkingParamsMap.Add(hatchParamsKey, fallbackHatchingParams);
                extendedJobShell.MarkingParamsMap.Add(contourSupportParamsKey, fallbackSupportContouringParams);
                extendedJobShell.MarkingParamsMap.Add(hatchSupportParamsKey, fallbackSupportHatchingParams);
                foreach (var part in extendedJobShell.PartsMap)
                {
                    if (supportRegex != null)
                    {
                        var match = supportRegex.Match(part.Value.Name);
                        if (match.Success)
                        {
                            if (match.Groups.Count > 1)
                            {
                                var partNameForSupport = match.Groups[1].Value;
                                foreach (var realPart in extendedJobShell.PartsMap)
                                {
                                    if (realPart.Value.Name.Trim().Trim('"').Equals(partNameForSupport)){
                                        //found a match for support, add to mapping and remove extra support part
                                        SupportPartsMapping.Add(part.Key, realPart.Key);
                                    }
                                }
                            }
                        }
                    }
                }
                //remove the parts that are support of another part
                foreach(var support in SupportPartsMapping)
                {
                    extendedJobShell.PartsMap.Remove(support.Key);
                }
                #endregion

                #region convert vector blocks
                using (var writer = FileWriterFactory.CreateNewWriter(targetFile.Extension))
                {
                    writer.StartWritePartial(extendedJobShell, targetFile.FullName, progress);

                    for (int i = 0; i < reader.JobShell.NumWorkPlanes; i++)
                    {
                        var workplane = await reader.GetWorkPlaneAsync(i);
                        foreach (var VB in workplane.VectorBlocks)
                        {
                            if (VB.LpbfMetadata == null) VB.LpbfMetadata = new VectorBlock.Types.LPBFMetadata();
                            if (reader.JobShell.MarkingParamsMap == null || !reader.JobShell.MarkingParamsMap.ContainsKey(VB.MarkingParamsKey))
                            {
                                //vector block misses parameters, use fallback
                                if (SupportPartsMapping.TryGetValue(VB.MetaData.PartKey, out var realPartKey))
                                {
                                    //we have a support vectorBlock
                                    VB.MetaData.PartKey = realPartKey;
                                    VB.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Support;
                                    if (VB.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                                    {
                                        VB.MarkingParamsKey = contourSupportParamsKey;
                                        VB.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Contour;
                                    }
                                    else if(VB.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches)
                                    {
                                        VB.MarkingParamsKey = hatchSupportParamsKey;
                                        VB.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Volume;
                                    }
                                }
                                else
                                {
                                    //we have a part vectorBlock
                                    VB.LpbfMetadata.StructureType = VectorBlock.Types.StructureType.Part;
                                    if (VB.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                                    {
                                        VB.MarkingParamsKey = contourSupportParamsKey;
                                        VB.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Contour;
                                    }
                                    else if (VB.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches)
                                    {
                                        VB.MarkingParamsKey = hatchSupportParamsKey;
                                        VB.LpbfMetadata.PartArea = VectorBlock.Types.PartArea.Volume;
                                    }
                                }
                            }
                            await writer.AppendWorkPlaneAsync(workplane);
                        }
                    }
                }
                #endregion
            }
        }

        private static void CheckExtensions(string readerExtension, string writerExtension)
        {
            if (!FileReaderFactory.SupportedFileFormats.Contains(readerExtension, StringComparer.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("no reader available for extension " + readerExtension);
            }
            if (!FileWriterFactory.SupportedFileFormats.Contains(writerExtension, StringComparer.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("no writer available for target extension " + writerExtension);
            }
        }
    }
}
