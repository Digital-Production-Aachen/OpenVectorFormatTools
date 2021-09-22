#pragma once
#include "IReader.h"
#include <stdlib.h>
#include <list>

using namespace std;
using namespace open_vector_format;

namespace open_vector_format
{
	enum CacheState
	{
		NotCached = 1,
		CompleteJobCached = 2,
		JobShellCached = 3
	};

	class FileReader : IReader
	{
	public:
		/// <summary>
		/// Asynchronously open the given file, calling the given interface for status updates.
		/// Depending on file header and file extension, the file is either completely loaded into memory, or only the JobShell is loaded.
		/// The Job / JobShell is placed at <see cref="JobShell"/>
		/// </summary>
		/// <param name="filename">name of the file to open</param>
		/// <param name="proress">status update interface to be called</param>
		virtual void open_job_async(string filename, Job** job) = 0;

		/// <summary>
		/// Retrieves the complete job with all workplane data.
		/// CAUTION: Job will be cached to memory completely regardless of its size, not advised for large jobs.
		/// Cached job will stay in memory, future calls to <see cref="GetWorkPlaneShell(int)"/>, <see cref="GetWorkPlane(int)"/> and <see cref="GetVectorBlock(int, int)"/> will be accelareted.
		/// <see cref="CacheState"/> will be set to CompleteJobCached.
		/// </summary>
		/// <returns>Complete job with all <see cref="WorkPlane"/>s and <see cref="VectorBlock"/>s.</returns>
		virtual void cache_job_to_memory_async() = 0;

		/// <summary>
		/// Unloads stored vector data from memory. If the data is queried again, it needs to be read from the disk again.
		/// <see cref="CacheState"/> will be set to NotCached.
		/// </summary>
		virtual void unload_job_from_memory() = 0;

		/// <summary>Gets the current caching state of the file.</summary>
		virtual CacheState get_cache_state() = 0;

		/// <summary>List of all file extensins supported by this reader (format ".xxx")</summary>
		virtual list<string> get_supported_file_formats() = 0;

		/// <summary>
		/// Determines up to which serialized size jobs get automatically cached into memory automatically when reading. Default is 64MB.
		/// BEWARE: size recommendation by protobuf author Kenton Varda (protobuf uses 32bit int for size)
		/// https://stackoverflow.com/questions/34128872/google-protobuf-maximum-size
		/// Messages bigger than 2GB cannot be serialized and not be transmitted as one block.
		/// </summary>
		long long automated_caching_threshold_bytes = 67108864;

		virtual void close_file()  = 0;

		virtual ~FileReader() {}
	};
}