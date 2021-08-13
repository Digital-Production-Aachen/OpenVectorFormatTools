#pragma once
#include "IWriter.h"
#include <stdlib.h>

using namespace std;
using namespace open_vector_format;


/// <summary>
/// Abstract class of a FileWriter for slice data (workPlaneed geometry files).
/// </summary>
namespace open_vector_format
{
    enum class FileWriteOperation
    {
        None = 1,
        Undefined = 2,
        CompleteWrite = 3,
        PartialWrite = 4        
    };

    class FileWriter : IWriter
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
        virtual void start_write_partial(Job* jobShell, string filename) = 0;

        /// <summary>
        /// Writes a complete <see cref="Job"/>. Needs to contain all <see cref="WorkPlane"/>s and <see cref="VectorBlock"/>s already.
        /// Partial writes can be done with the <see cref="StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress)"/> method.
        /// </summary>
        /// <param name="job">job to write</param>
        /// <param name="filename">Path and name for savefile</param>
        /// <param name="progress">status update interface to be called</param>
        virtual void simple_job_write_async(Job* job, string filename) = 0;

        /// <summary>Indicates if a fileOperation is running, i.e. if a filestream is open.</summary>
        virtual FileWriteOperation get_file_operation_in_progress() = 0;

        /// <summary>List of all file extensins supported by this writer (format ".xxx")</summary>
        virtual list<string> get_supported_file_formats() = 0;
    };
}