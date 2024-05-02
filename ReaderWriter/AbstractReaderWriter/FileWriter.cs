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



using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Abstract class of a FileWriter for slice data (workPlaneed geometry files).
/// </summary>
namespace OpenVectorFormat.AbstractReaderWriter
{
    public enum FileWriteOperation
    {
        None = 1,
        Undefined = 2,
        CompleteWrite = 3,
        PartialWrite = 4        
    }
    public abstract class FileWriter : IWriter, IDisposable
    {
        /// <summary>
        /// Open file for writing a job with a workPlane based look-up table (LUT).
        /// Writes the job structured in a way that allows partial (per workPlane / vectorblock) reading.
        /// Writes the given jobShell with all workPlanes/vectorBlocks contained, but enables adding more workPlanes.
        /// Calls the given interface for status updates.
        /// </summary>
        /// <param name="jobShell">OVF Job Object to write to file</param>
        /// <param name="filename">Path and name for savefile</param>
        /// <param name="progress">status update interface to be called</param>
        public abstract void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress = null);

        /// <summary>
        /// Writes a complete <see cref="Job"/>. Needs to contain all <see cref="WorkPlane"/>s and <see cref="VectorBlock"/>s already.
        /// Partial writes can be done with the <see cref="StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress)"/> method.
        /// </summary>
        /// <param name="job">job to write</param>
        /// <param name="filename">Path and name for savefile</param>
        /// <param name="progress">status update interface to be called</param>
        [Obsolete("Please use SimpleJobWrite")]
        public virtual Task SimpleJobWriteAsync(Job job, string filename, IFileReaderWriterProgress progress)
        {
            SimpleJobWrite(job, filename, progress);
            return Task.CompletedTask;
        }

        public abstract void SimpleJobWrite(Job job, string filename, IFileReaderWriterProgress progress = null);

        /// <summary>Disposes the FileWriter, finishing the partial write (if initiated).</summary>
        public abstract void Dispose();

        /// <inheritdoc/>
        [Obsolete("Please use AppendWorkPlane")]
        public Task AppendWorkPlaneAsync(WorkPlane workPlane)
        {
            AppendWorkPlane(workPlane);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public abstract void AppendWorkPlane(WorkPlane workPlane);

        /// <inheritdoc/>
        [Obsolete("Please use AppendVectorBlock")]
        public Task AppendVectorBlockAsync(VectorBlock block)
        {
            AppendVectorBlock(block);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Writes the VectorBlock to the workPlane last appended in the job.
        /// </summary>
        /// <param name="block">VectorBlock to write to file</param>
        public abstract void AppendVectorBlock(VectorBlock block);

        /// <summary>Indicates if a fileOperation is running, i.e. if a filestream is open.</summary>
        public abstract FileWriteOperation FileOperationInProgress { get; }

        /// <summary>List of all file extensins supported by this writer (format ".xxx")</summary>
        public static List<string> SupportedFileFormats { get; }
        public abstract Job JobShell { get; }
    }//end FileWrtier

}//end namespace VectorFileHandler