/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2024 Digital-Production-Aachen

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
using System.Numerics;
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
    public class OVFStreamingMerger : AbstOVFStreamingMerger
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterSetEngine">parameters to apply</param>
        /// <param name="slicableMesh">FileReader to wrap</param>
        /// <param name="markAsSupport">tags all vector blocks of the slicableMesh as support if true</param>
        public OVFStreamingMerger(FileReader slicableMesh, bool markAsSupport = false, float translationX = 0, float translationY = 0)
            :this(new FileReaderToMerge() 
            { fr = slicableMesh, markAsSupport = markAsSupport, translationX = translationX, translationY = translationY })
        {}


        public OVFStreamingMerger(FileReaderToMerge slicableMesh) : base(slicableMesh) { }


        /// <summary>
        /// Adds another file reader to merge while streaming layers.
        /// Supports only merging of same layer thicknesses
        /// </summary>
        /// <param name="fileReaderToMerge"></param>
        public new void AddFileReaderToMerge(FileReaderToMerge fileReaderToMerge)
        {
            base.AddFileReaderToMerge(fileReaderToMerge);
            int maxPartKey = mergedJobShell.PartsMap.Any() ? mergedJobShell.PartsMap.Keys.Max() + 1 : 0;
            int maxParamsKey = mergedJobShell.MarkingParamsMap.Any() ? mergedJobShell.MarkingParamsMap.Keys.Max() + 1 : 0;
            fileReaderToMerge.partKeyIndexShift = maxPartKey;
            fileReaderToMerge.paramKeyMapping = new Dictionary<int, int>();
            foreach (var part in fileReaderToMerge.fr.JobShell.PartsMap)
            {
                //make part instance names unique
                var instancePart = part.Value.Clone();
                int counter = 0;
                string originalName = instancePart.Name;
                while (mergedJobShell.PartsMap.Any(x => x.Value.Name == instancePart.Name))
                {
                    counter++;
                    instancePart.Name = $"{originalName}{counter.ToString("D3")}";
                }
                mergedJobShell.PartsMap.Add(part.Key + maxPartKey, instancePart);
            }

            foreach (var parameter in fileReaderToMerge.fr.JobShell.MarkingParamsMap)
            {
                bool found = false;
                foreach(var mergedShellParam in mergedJobShell.MarkingParamsMap)
                {
                    if (mergedShellParam.Value.Equals(parameter.Value))
                    {
                        found = true;
                        fileReaderToMerge.paramKeyMapping.Add(parameter.Key, mergedShellParam.Key);
                        break;
                    }
                }
                if (!found)
                {
                    maxParamsKey++;
                    fileReaderToMerge.paramKeyMapping.Add(parameter.Key, maxParamsKey);
                    mergedJobShell.MarkingParamsMap.Add(maxParamsKey, parameter.Value);
                }
            }
        }

        protected override void PostProcessVectorBlock(VectorBlock vectorBlock)
        {
            //we do nothing here
        }
    }
}
