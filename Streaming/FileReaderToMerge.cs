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
