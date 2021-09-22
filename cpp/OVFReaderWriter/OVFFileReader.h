#pragma once
#include <iostream>
#include <fstream>
#include <stdlib.h>
#include <algorithm>
#include <google/protobuf/util/delimited_message_util.h>
#include <google/protobuf/io/zero_copy_stream_impl.h>
#include <list>

#include "FileReader.h"
#include "ovf_lut.pb.h"
#include "copy_with_exclude.h"

using namespace open_vector_format;
using namespace std;

namespace open_vector_format
{
	enum class FileReadOperation
	{
		None = 1,
		Undefined = 2,
		CompleteRead = 3,
		Streaming = 4
	};

	class OVFFileReader : FileReader
	{
	private:
		//IFileReaderWriterProgress progress;
		ifstream _fs;
		long long _streamlength;
		string _filename;

		// Look-up-table for workPlane positions in filestream.
		JobLUT _jobLUT;

		// List of Look-up-tables for vectorblock positions in the filestream for each workplane.
		unique_ptr<WorkPlaneLUT[]> _workPlaneLUTs;

		Job _jobShell;
		Job* _job;
		int _numberOfWorkPlanes = 0;
		int _numberOfCachedLayers = 0;

		CacheState _cacheState = NotCached;

		FileReadOperation _fileOperationInProgress = FileReadOperation::None;

		bool is_big_endian(void)
		{
			union {
				uint32_t i;
				char c[4];
			} bint = { 0x01020304 };

			return bint.c[0] == 1;
		}

		void _readJobShell(long long LUTindex)
		{
			// Read LUT
			ifstream fs(_filename, ios::binary);
			fs.seekg(LUTindex, ios::beg);
			google::protobuf::io::IstreamInputStream zc_stream(&fs);
			google::protobuf::util::ParseDelimitedFromZeroCopyStream(&_jobLUT, &zc_stream, NULL);
			fs.close();

			// Read job shell
			ifstream fs2(_filename, ios::binary);
			fs2.seekg(_jobLUT.jobshellposition(), ios::beg);
			google::protobuf::io::IstreamInputStream zc_stream2(&fs2);
			google::protobuf::util::ParseDelimitedFromZeroCopyStream(&_jobShell, &zc_stream2, NULL);
			fs2.close();

			// check for file corruption
			if (_jobShell.num_work_planes() != _jobLUT.workplanepositions_size())
			{
				throw runtime_error("OVF file is corrupted! job.NumWorkPlanes does not match jobLUT.Workplanepositions.length");
			}

			// check that stream is long enough for all workplanes
			for (int i = 0; i < _jobLUT.workplanepositions_size(); i++)
			{
				long long pos = _jobLUT.workplanepositions(i);
				if (pos > _streamlength)
				{
					throw runtime_error("Invalid workPlane position detected in file");
				}
			}
			//progress.IsFinished = true;
		}

		void _readWorkPlaneLUT(int i_workPlane)
		{
			long wpLUTIndexIndex = _jobLUT.workplanepositions(i_workPlane);
			ifstream fs(_filename, ios::binary);
			fs.seekg(wpLUTIndexIndex, ios::beg);

			char LUTIndexBuffer[sizeof(long long)];
			fs.read(LUTIndexBuffer, sizeof(long long));
			if (is_big_endian())
			{
				reverse(LUTIndexBuffer, LUTIndexBuffer + sizeof(long long));
			}
			long long wpLUTindex;
			memcpy(&wpLUTindex, LUTIndexBuffer, sizeof(wpLUTindex));

			fs.seekg(wpLUTindex, ios::beg);
			WorkPlaneLUT wpLUT;
			google::protobuf::io::IstreamInputStream zc_stream(&fs);
			google::protobuf::util::ParseDelimitedFromZeroCopyStream(&wpLUT, &zc_stream, NULL);

			for (int i = 0; i < wpLUT.vectorblockspositions_size(); i++)
			{
				long long pos = wpLUT.vectorblockspositions(i);
				if (pos > _streamlength)
				{
					throw runtime_error("Invalid vectorblock position detected in file");
				}
			}

			_workPlaneLUTs[i_workPlane] = wpLUT;
			fs.close();
		}

		void update_status()
		{
			//progress.Update("reading workPlane " + _numberOfCachedLayers, (int)(((double)(_numberOfCachedLayers * 100)) / _numberOfWorkPlanes));
		}


	public:
		/// <inheritdoc/>
		list<string> get_supported_file_formats() { return list<string> { ".ovf" }; }

		/// <inheritdoc/>
		CacheState get_cache_state() { return _cacheState; }

		/// <inheritdoc/>
		void open_job_async(string filename, Job** job)
		{
			if (_fileOperationInProgress != FileReadOperation::None)
			{
				throw runtime_error("Another FileLoadingOperation is currently running. Please wait until it is finished.");
			}

			_fileOperationInProgress = FileReadOperation::Undefined;

			_filename = filename;

			_job = *job;

			//this.progress = progress;
			ifstream _fs(_filename, ios::binary);
			if (!_fs.good())
			{
				throw runtime_error("File " + _filename + "does not exist!");	
			}

			_fs.seekg(0, ios::end);
			_streamlength = _fs.tellg();
			if (_streamlength < 12)
			{
				_fs.close();
				throw runtime_error("binary file is empty!");
			}
			else
			{
				_fs.seekg(0, ios::beg);


				char magic_number_buffer[4];
				char known_magic_number[4] = { 0x4c, 0x56, 0x46, 0x21 };
				_fs.read(magic_number_buffer, sizeof(magic_number_buffer));

				char LUTIndexBuffer[sizeof(long long)];
				_fs.read(LUTIndexBuffer, sizeof(long long));
				if (is_big_endian())
				{
					reverse(LUTIndexBuffer, LUTIndexBuffer + sizeof(long long));
				}
				long long jobLUTindex;
				memcpy(&jobLUTindex, LUTIndexBuffer, sizeof(jobLUTindex));

				// check that magic number is correct
				bool magic_number_correct = true;
				for (int i = 0; i < 4; i++)
				{
					if (magic_number_buffer[i] != known_magic_number[i])
					{
						magic_number_correct = false;
						break;
					}
				}

				if (!magic_number_correct || jobLUTindex >= _streamlength || jobLUTindex < -1)
				{
					_fs.close();
					throw runtime_error("binary file is not an OVF file or corrupted!");
				}
				else
				{
					if (jobLUTindex <= automated_caching_threshold_bytes)
					{
						_fileOperationInProgress = FileReadOperation::CompleteRead;
					}
					else
					{
						_fileOperationInProgress = FileReadOperation::Streaming;
					}
					
					_readJobShell(jobLUTindex);
					//progress.IsFinished = false;
					_workPlaneLUTs = unique_ptr<WorkPlaneLUT[]>(new WorkPlaneLUT[_jobShell.num_work_planes()]);
					for (int i_plane = 0; i_plane < _jobShell.num_work_planes(); i_plane++)
					{
						_readWorkPlaneLUT(i_plane);
					}
				}

				if (_fileOperationInProgress == FileReadOperation::CompleteRead)
				{
					cache_job_to_memory_async();
					_cacheState = CompleteJobCached;
				}
				else
				{
					_cacheState = JobShellCached;
				}
			}
		}



		/// <inheritdoc/>
		void cache_job_to_memory_async()
		{
			if (!(_cacheState == CompleteJobCached)) 
			{
				//progress.IsCancelled = false;
				//progress.IsFinished = false;
				_job->CopyFrom(_jobShell);
				_numberOfWorkPlanes = _job->num_work_planes();
				for (int i = 0; i < _numberOfWorkPlanes; i++)
				{
					auto wp_pointer = _job->add_work_planes();
					get_work_plane_async(i, &wp_pointer);
				}

				//progress.Update("reading workPlane " + k, 100);
				//progress.IsFinished = true;
				_cacheState = CompleteJobCached;
			}
		}

		/// <inheritdoc/>
		void get_work_plane_async(int i_workPlane, WorkPlane** workplane)
		{
			if (_jobShell.num_work_planes() < i_workPlane)
			{
				throw runtime_error("i_workPlane " + to_string(i_workPlane) + " out of range for jobfile with " + to_string(_jobShell.num_work_planes()) + " workPlanes!");
			}
			if (_cacheState == CompleteJobCached)
			{
				**workplane = const_cast<WorkPlane&>(_job->work_planes(i_workPlane));
			}
			else
			{
				get_work_plane_shell(i_workPlane, workplane);
				
				for (int i_block = 0; i_block < (*workplane)->num_blocks(); i_block++)
				{
					auto vb_pointer = (*workplane)->add_vector_blocks();
					get_vector_block_async(i_workPlane, i_block, &vb_pointer);
				}
				_numberOfCachedLayers++;
				update_status();
			}
		}
		

		void get_work_plane_shell(int i_workPlane, WorkPlane** workplane_shell)
		{
			if (_jobShell.num_work_planes() < i_workPlane)
			{
				throw runtime_error("i_workPlane " + to_string(i_workPlane) + " out of range for jobfile with " + to_string(_jobShell.num_work_planes()) + " workPlanes!");
			}

			if (false && _cacheState == CompleteJobCached)
			{
				WorkPlane wp = _job->work_planes(i_workPlane);
				copy_with_exclude(&wp, *workplane_shell, list<string> { "vector_blocks" });
			}
			else
			{
				long long start = _workPlaneLUTs[i_workPlane].workplaneshellposition();
				ifstream fs(_filename, ios::binary);
				fs.seekg(start, ios::beg);
				google::protobuf::io::IstreamInputStream zc_stream(&fs);
				google::protobuf::util::ParseDelimitedFromZeroCopyStream(*workplane_shell, &zc_stream, NULL);
				fs.close();
			}
		}

		/// <inheritdoc/>
		void get_vector_block_async(int i_workPlane, int i_vectorblock, VectorBlock** vector_block)
		{
			if (_jobShell.num_work_planes() < i_workPlane)
			{
				throw runtime_error("i_workPlane" + to_string(i_workPlane) + " out of range for job with " + to_string(_jobShell.num_work_planes()) + " workplanes!");
			}

			WorkPlaneLUT wpLut = _workPlaneLUTs[i_workPlane];
			if (wpLut.vectorblockspositions_size() < i_vectorblock)
			{
				throw runtime_error("i_vectorblock " + to_string(i_vectorblock) + " out of range for workPlane with " + to_string(wpLut.vectorblockspositions_size()) + " blocks!");
			}

			if (_cacheState == CompleteJobCached)
			{
				**vector_block = const_cast<VectorBlock&>(_job->work_planes(i_workPlane).vector_blocks(i_vectorblock));
			}
			else
			{
				long long start = _workPlaneLUTs[i_workPlane].vectorblockspositions(i_vectorblock);

				ifstream fs(_filename, ios::binary);
				fs.seekg(start, ios::beg);
				google::protobuf::io::IstreamInputStream zc_stream(&fs);
				google::protobuf::util::ParseDelimitedFromZeroCopyStream(*vector_block, &zc_stream, NULL);
				fs.close();
			}
		}

		/// <inheritdoc/>
		void unload_job_from_memory()
		{
			_cacheState = NotCached;
		}

		void close_file()
		{
			if (_fs.is_open())
			{
				_fs.close();
			}
		}

		~OVFFileReader()
		{
			if (_fs.is_open()) { _fs.close(); }
		}
	};
}
