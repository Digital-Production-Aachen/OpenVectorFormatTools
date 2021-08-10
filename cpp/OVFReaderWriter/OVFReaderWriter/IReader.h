#pragma once
#include "open_vector_format.pb.h"

using namespace open_vector_format;

namespace open_vector_format
{
    class IReader
    {
    public:
        /// <summary>
        /// Gets the JobShell with all metadata, but without the actual workplane data (<see cref="Job.WorkPlanes"/> is empty).
        /// <see cref="OpenJobAsync(string, IFileReaderWriterProgress)"/> needs to be called before.
        /// </summary>
        virtual int get_job_shell(Job* job_shell);

        /// <summary>Retrieves workPlane point data on demand, delegating ownership of the data to the caller. The complete WorkPlane needs to be cached into memory for this operation.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <returns>Requested WorkPlane with all associated VectorBlocks.</returns>
        virtual int get_work_plane_async(int i_workPlane, WorkPlane* workplane);

        /// <summary>Retrieves WorkPlaneShell with all the meta-data, without the actual vectorblocks, delegating ownership of the data to the caller.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <returns>Requested WorkPlane with all associated VectorBlocks</returns>
        virtual int get_work_plane_shell(int i_workPlane, WorkPlane* workplane_shell);

        /// <summary>Retrieves vector block point data on demand, delegating ownership of the data to the caller.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <param name="i_vectorblock">index of vectorblock</param>
        /// <returns>Requested VectorBlock</returns>
        virtual int get_vector_block_async(int i_workPlane, int i_vectorblock, VectorBlock* vectorblock);

        //virtual ~IReader() {}
    };
}