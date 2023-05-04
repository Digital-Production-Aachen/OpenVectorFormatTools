/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2022 Digital-Production-Aachen

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
using System.Collections.Generic;
//using System.Numerics;

namespace OpenVectorFormat.Streaming
{
    /// <summary>
    /// common data structure to add FileReaders to streaming based merging
    /// </summary>
    public class FileReaderToMerge
    {
        /// <summary>
        /// File Reader object to merge
        /// </summary>
        public FileReader fr;
        /// <summary>
        /// override all vector blocks structure type meta data with SUPPORT when true
        /// </summary>
        public bool markAsSupport;
        /// <summary>
        /// translation to apply to all vectors in x plane
        /// </summary>
        public float translationX;

        /// <summary>
        /// translation to apply to all vectors in y plane
        /// </summary>
        public float translationY;

        /// <summary>
        /// number of layers this file reader needs to be shifted
        /// to reach the correct z-Height
        /// </summary>
        public int layerOffset { get; internal set; }
        /// <summary>
        /// minimum (starting) z-height of this file readers layers
        /// </summary>
        public float zMin { get; internal set; }
        /// <summary>
        /// index shift to apply to vector blocks of this file reader to match the merged partsMap
        /// </summary>
        public int partKeyIndexShift { get; internal set; }
        /// <summary>
        /// Mapping of param keys of this file readers vector blocks (key in dict) to the param key in the merged FileReader.
        /// </summary>
        public Dictionary<int, int> paramKeyMapping { get; internal set; }
    }
}
