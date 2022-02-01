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



///////////////////////////////////////////////////////////
//  FileReader.cs
//  Implementation of the Class FileReader
//  Generated by Enterprise Architect
//  Created on:      18-Jun-2018 16:55:24
//  Original author: Dirks
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Abstract class of a FileReader for slice data (workPlaneed geometry files).
/// 
/// this implementation is necessary because autogenerated protobuf classes should not be extended/overriden
/// </summary>
namespace OpenVectorFormat.AbstractReaderWriter
{
    public enum CacheState
    {
        NotCached = 1,
        CompleteJobCached = 2,
        JobShellCached = 3
    }

    public abstract class FileReader : IReader, IDisposable
    {
        /// <summary>
        /// Asynchronously open the given file, calling the given interface for status updates.
        /// Depending on file header and file extension, the file is either completely loaded into memory, or only the JobShell is loaded.
        /// The Job / JobShell is placed at <see cref="JobShell"/>
        /// </summary>
        /// <param name="filename">name of the file to open</param>
        /// <param name="progress">status update interface to be called</param>
        public abstract Task OpenJobAsync(string filename, IFileReaderWriterProgress progress);

        /// <summary>
        /// Retrieves the complete job with all workplane data.
        /// CAUTION: Job will be cached to memory completely regardless of its size, not advised for large jobs.
        /// Cached job will stay in memory, future calls to <see cref="GetWorkPlaneShell(int)"/>, <see cref="GetWorkPlane(int)"/> and <see cref="GetVectorBlock(int, int)"/> will be accelareted.
        /// <see cref="CacheState"/> will be set to CompleteJobCached.
        /// </summary>
        /// <returns>Complete job with all <see cref="WorkPlane"/>s and <see cref="VectorBlock"/>s.</returns>
        public abstract Task<OpenVectorFormat.Job> CacheJobToMemoryAsync();

        /// <summary>
        /// Unloads stored vector data from memory. If the data is queried again, it needs to be read from the disk again.
        /// <see cref="CacheState"/> will be set to NotCached.
        /// </summary>
        public abstract void UnloadJobFromMemory();

        /// <summary>Gets the current caching state of the file.</summary>
        public abstract CacheState CacheState
        {
            get;
        }

        /// <summary>List of all file extensins supported by this reader (format ".xxx")</summary>
        public static List<string> SupportedFileFormats { get; }

        /// <summary>
        /// Determines up to which serialized size jobs get automatically cached into memory automatically when reading. Default is 64MB.
        /// BEWARE: size recommendation by protobuf author Kenton Varda (protobuf uses 32bit int for size)
        /// https://stackoverflow.com/questions/34128872/google-protobuf-maximum-size
        /// Messages bigger than 2GB cannot be serialized and not be transmitted as one block.
        /// </summary>
        public Int64 AutomatedCachingThresholdBytes { get; set; } = 67108864;
        public abstract Job JobShell { get; }

        public virtual void CloseFile()
        {

        }

        /// <inheritdoc/>
        public abstract void Dispose();

        /// <inheritdoc/>
        public abstract Task<WorkPlane> GetWorkPlaneAsync(int i_workPlane);

        /// <inheritdoc/>
        public abstract WorkPlane GetWorkPlaneShell(int i_workPlane);

        /// <inheritdoc/>
        public abstract Task<VectorBlock> GetVectorBlockAsync(int i_workPlane, int i_vectorblock);
    }//end FileReader

}//end namespace VectorFileHandler