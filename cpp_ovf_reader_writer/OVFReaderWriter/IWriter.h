#pragma once
#include "open_vector_format.pb.h"

using namespace open_vector_format;

namespace open_vector_format
{
    class IWriter
    {
    public:
        /// <summary>
        /// Writes the given workPlane to the job file.
        /// More vector blocks can be appended to the workPlane last appended.
        /// </summary>
        /// <param name="workPlane">WorkPlane to add.</param>
        virtual void append_work_plane_async(WorkPlane workPlane) = 0;

        /// <summary>
        /// Writes the VectorBlock to the workPlane last appended in the job.
        /// </summary>
        /// <param name="block">VectorBlock to write to file</param>
        virtual void append_vector_block_async(VectorBlock block) = 0;

        /// <summary>jobShell being written to. (just the shell without workPlanes).</summary>
        virtual void get_job_shell(Job** job_shell) = 0;
    };
}