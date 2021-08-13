#pragma once
#include "open_vector_format.pb.h"

using namespace open_vector_format;

namespace open_vector_format
{
    class IReader
    {
    public:
        /// <summary>Retrieves workPlane point data on demand, delegating ownership of the data to the caller. The complete WorkPlane needs to be cached into memory for this operation.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <returns>Requested WorkPlane with all associated VectorBlocks.</returns>
        virtual void get_work_plane_async(int i_workPlane, WorkPlane** workplane) = 0;

        /// <summary>Retrieves WorkPlaneShell with all the meta-data, without the actual vectorblocks, delegating ownership of the data to the caller.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <returns>Requested WorkPlane with all associated VectorBlocks</returns>
        virtual void get_work_plane_shell(int i_workPlane, WorkPlane** workplane_shell) = 0;

        /// <summary>Retrieves vector block point data on demand, delegating ownership of the data to the caller.</summary>
        /// <param name="i_workPlane">index of workPlane</param>
        /// <param name="i_vectorblock">index of vectorblock</param>
        /// <returns>Requested VectorBlock</returns>
        virtual void get_vector_block_async(int i_workPlane, int i_vectorblock, VectorBlock** vectorblock) = 0;

        virtual ~IReader() {}
    };
}