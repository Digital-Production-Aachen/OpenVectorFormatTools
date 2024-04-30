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

using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenVectorFormat.Streaming
{
    /// <summary>
    /// Wraps a FileReader and applies parameters to the FileReader interface calls
    /// using the internal parameterSetEngine. Can also merge other file readers and tag FileReaders as support.
    /// Merging capabilities of the build processor shall be used for merging File Readers that contain vectors for one part
    /// (e.g. external support files, multiple cli files to "tag" parameters).
    /// To merge multiple parts into a build job without changing parameters, use OVFStreamingMerger instead.
    /// </summary>
    public class BuildProcessor : AbstOVFStreamingMerger
    {
        private ParameterSetEngine _parameterSetEngine;
        private const int defaultPartKey = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterSetEngine">parameters to apply</param>
        /// <param name="slicableMesh">FileReader to wrap</param>
        /// <param name="ovfPart">part meta data to overwrite into JobShell.partsMap and vectorBlocks.metaData.partKey</param>
        /// <param name="markAsSupport">tags all vector blocks of the slicableMesh as support if true</param>
        /// <param name="translation">translation to apply to all vectors in the slicableMesh</param>
        public BuildProcessor(ParameterSetEngine parameterSetEngine, FileReader slicableMesh, OpenVectorFormat.Part ovfPart, bool markAsSupport = false, float translationX = 0, float translationY = 0, float rotationInRad = 0)
            :this(parameterSetEngine, new FileReaderToMerge() 
            { fr = slicableMesh, markAsSupport = markAsSupport, translationX = translationX, translationY = translationY, rotationInRad = rotationInRad}
            , ovfPart)
        {}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterSetEngine"></param>
        /// <param name="slicableMesh"></param>
        /// <param name="ovfPart">part meta data to overwrite into JobShell.partsMap and vectorBlocks.metaData.partKey</param>
        public BuildProcessor(ParameterSetEngine parameterSetEngine, FileReaderToMerge slicableMesh, OpenVectorFormat.Part ovfPart) : base(slicableMesh)
        {
            _parameterSetEngine = parameterSetEngine;
            mergedJobShell.PartsMap.Clear();
            mergedJobShell.PartsMap.Add(defaultPartKey, ovfPart);
            slicableMesh.partKeyIndexShift = defaultPartKey;
            _parameterSetEngine.ReplaceParamsMapInJobShell(mergedJobShell);
        }

        /// <summary>
        /// Adds another file reader to merge while streaming layers.
        /// Supports only merging of same layer thicknesses
        /// </summary>
        /// <param name="fileReaderToMerge"></param>
        public new void AddFileReaderToMerge(FileReaderToMerge fileReaderToMerge)
        {
            base.AddFileReaderToMerge(fileReaderToMerge);
            fileReaderToMerge.partKeyIndexShift = defaultPartKey;
        }

        public override WorkPlane GetWorkPlane(int i_workPlane)
        {
            var workPlane = base.GetWorkPlane(i_workPlane);
            _parameterSetEngine.ApplyParametersTo(workPlane);
            return workPlane;
        }

        protected override void PostProcessVectorBlock(VectorBlock vectorBlock)
        {
            //override part keys of all vector blocks
            vectorBlock.MetaData.PartKey = defaultPartKey;
        }
    }
}
