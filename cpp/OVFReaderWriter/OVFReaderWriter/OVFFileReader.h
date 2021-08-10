#pragma once
#include <iostream>
#include <fstream>
#include <stdlib.h>
#include <boost/iostreams/device/mapped_file.hpp>
#include <algorithm>
#include <google/protobuf/util/delimited_message_util.h>
#include <google/protobuf/io/zero_copy_stream_impl.h>
#include <boost/iostreams/stream.hpp>

#include "FileReader.h"
#include "ovf_lut.pb.h"
using namespace open_vector_format;
using namespace std;
using namespace boost::iostreams;

namespace open_vector_format
{
	enum FileReadOperation
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
		boost::iostreams::mapped_file _mmf;

		// Look-up-table for workPlane positions in filestream.
		JobLUT _jobLUT;

		// List of Look-up-tables for vectorblock positions in the filestream for each workplane.
		WorkPlaneLUT* _workPlaneLUTs;

		Job _jobShell;
		Job _job;
		int _numberOfLayers = 0;
		int _numberOfCachedLayers = 0;


		CacheState _cacheState = NotCached;

		FileReadOperation _fileOperationInProgress = None;

		bool is_big_endian(void)
		{
			union {
				uint32_t i;
				char c[4];
			} bint = { 0x01020304 };

			return bint.c[0] == 1;
		}


	public:
		string last_error;

		int get_job_shell(Job* job_shell)
		{
			*job_shell = _jobShell;
			return 0;
		}

		/// <inheritdoc/>
		list<string> get_supported_file_formats() { return list<string> { ".ovf" }; }

		/// <inheritdoc/>
		CacheState get_cache_state() { return _cacheState; }

		/// <inheritdoc/>
		int OpenJobAsync(string filename)
		{
			auto _readWorkPlaneLUT = [&](int i_workPlane)
			{
				long wpLUTIndexIndex = _jobLUT.workplanepositions(i_workPlane);
				_fs.seekg(wpLUTIndexIndex, ios::beg);

				char LUTIndexBuffer[sizeof(long long)];
				_fs.read(LUTIndexBuffer, sizeof(long long));
				if (is_big_endian())
				{
					reverse(LUTIndexBuffer, LUTIndexBuffer + sizeof(long long));
				}
				long long wpLUTindex;
				memcpy(&wpLUTindex, LUTIndexBuffer, sizeof(wpLUTindex));

				_fs.seekg(wpLUTindex, ios::beg);
				WorkPlaneLUT wpLUT;
				google::protobuf::io::IstreamInputStream zc_stream(&_fs);
				bool fls = false;
				google::protobuf::util::ParseDelimitedFromZeroCopyStream(&wpLUT, &zc_stream, &fls);

				for (int i = 0; i < wpLUT.vectorblockspositions_size(); i++)
				{
					long long pos = wpLUT.vectorblockspositions(i);
					if (pos > _streamlength)
					{
						Dispose();
						last_error = "Invalid vectorblock position detected in file";
						return -4;
					}
				}

				_workPlaneLUTs[i_workPlane] = wpLUT;

				return 0;
			};

			auto _readJobShell = [&](long long LUTindex)
			{
				// Read LUT
				_fs.seekg(LUTindex, ios::beg);
				google::protobuf::io::IstreamInputStream zc_stream(&_fs);
				bool fls = false;
				google::protobuf::util::ParseDelimitedFromZeroCopyStream(&_jobLUT, &zc_stream, &fls);

				// Read job shell
				_fs.seekg(_jobLUT.jobshellposition(), ios::beg);
				google::protobuf::io::IstreamInputStream zc_stream2(&_fs);
				google::protobuf::util::ParseDelimitedFromZeroCopyStream(&_jobShell, &zc_stream2, &fls);

				// check for file corruption
				if (_jobShell.num_work_planes() != _jobLUT.workplanepositions_size())
				{
					last_error = "OVF file is corrupted";
					return -5;
				}

				for (int i = 0; i < _jobLUT.workplanepositions_size(); i++)
				{
					long long pos = _jobLUT.workplanepositions(i);
					if (pos > _streamlength)
					{
						Dispose();
						last_error = "Invalid workPlane position detected in file";
						return -6;
					}
				}
				//progress.IsFinished = true;
			};

			if (_fileOperationInProgress != None)
			{
				last_error = "Another FileLoadingOperation is currently running. Please wait until it is finished.";
				return -1;
			}

			_fileOperationInProgress = Undefined;

			//this.progress = progress;
			ifstream _fs(_filename, ios::binary);
			_fs.seekg(0, ios::end);
			_streamlength = _fs.tellg();
			if (_streamlength < 12)
			{
				_fs.close();
				last_error = "binary file is empty!";
				return -2;
			}
			else
			{
				_fs.seekg(0, ios::beg);


				char magic_number_buffer[4];
				char known_magic_number[4] = { 0x4c, 0x56, 0x46, 0x21 };
				_fs.read(magic_number_buffer, sizeof(magic_number_buffer));
				_fs.close();

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

				if (magic_number_correct || jobLUTindex >= _streamlength || jobLUTindex < -1)
				{
					_fs.close();
					last_error = "binary file is not an OVF file or corrupted!";
					return -3;
				}
				else
				{
					if (jobLUTindex <= automated_caching_threshold_bytes)
					{
						_fileOperationInProgress = CompleteRead;
					}
					else
					{
						_fileOperationInProgress = Streaming;
					}

					int ret = _readJobShell(jobLUTindex);
					if (ret != 0)
					{
						return ret;
					}
					//progress.IsFinished = false;
					_workPlaneLUTs = new WorkPlaneLUT[_jobShell.num_work_planes()];
					for (int i_plane = 0; i_plane < _jobShell.num_work_planes(); i_plane++)
					{
						ret = _readWorkPlaneLUT(i_plane);
						if (ret != 0)
						{
							return ret;
						}
					}
				}

				if (_fileOperationInProgress == CompleteRead)
				{
					cache_job_to_memory_async(nullptr);
					_cacheState = CompleteJobCached;
				}
				else
				{
					_cacheState = JobShellCached;
				}
			}




			_filename = filename;
			return 0;
		}

		/// <inheritdoc/>
		int cache_job_to_memory_async(Job* job)
		{
			if (_cacheState == CompleteJobCached)
			{
				*job = _job;
				return 0;
			}
			else
			{
				//progress.IsCancelled = false;
				//progress.IsFinished = false;

				_job.CopyFrom(_jobShell);
				_numberOfLayers = _job.num_work_planes();
				int ret;
				for (int i = 0; i < _numberOfLayers; i++)
				{
					WorkPlane* workplane;
					ret = get_work_plane_async(i, workplane);
					if (ret != 0)
					{
						return ret;
					}
					auto wp_pointer = _job.add_work_planes();
					wp_pointer = workplane;
				}

				//progress.Update("reading workPlane " + k, 100);
				//progress.IsFinished = true;
				_cacheState = CompleteJobCached;
				*job = _job;
				return 0;
			}
		}

		/// <inheritdoc/>
		int get_work_plane_async(int i_workPlane, WorkPlane* workplane)
		{
			if (_jobShell.num_work_planes() < i_workPlane)
			{
				last_error = "i_workPlane " + to_string(i_workPlane) + " out of range for jobfile with " + to_string(_jobShell.num_work_planes()) + " workPlanes!";
				return -7;
			}
			if (_cacheState == CompleteJobCached)
			{
				*workplane = _job.work_planes(i_workPlane);
				return 0;
			}
			else
			{
				WorkPlane wp;// = GetWorkPlaneShell(i_workPlane, stream);
				int ret = 0;
				ret = get_work_plane_shell(i_workPlane, &wp);
				if (ret != 0)
				{
					return ret;
				}

				for (int i_block = 0; i_block < wp.num_blocks(); i_block++)
				{
					VectorBlock* vb;
					ret = get_vector_block_async(i_workPlane, i_block, vb);
					auto vb_pointer = wp.add_vector_blocks();
					vb_pointer = vb;
				}
				_numberOfCachedLayers++;
				update_status();
				*workplane = wp;
				return 0;
			}
		}
		void update_status()
		{
			//progress.Update("reading workPlane " + _numberOfCachedLayers, (int)(((double)(_numberOfCachedLayers * 100)) / _numberOfLayers));
		}

		int get_work_plane_shell(int i_workPlane, WorkPlane* work_plane_shell)
		{
			if (_jobShell.num_work_planes() < i_workPlane)
			{
				last_error = "i_workPlane " + to_string(i_workPlane) + " out of range for jobfile with " + to_string(_jobShell.num_work_planes()) + " workPlanes!";
				return -8;
			}

			if (false && _cacheState == CompleteJobCached)
			{
				/* WorkPlane wpShell = new WorkPlane();
				ProtoUtils.CopyWithExclude(_job.WorkPlanes[i_workPlane], wpShell, new List<int>{ WorkPlane.VectorBlocksFieldNumber });
				return wpShell;
				*/
			}
			else
			{
				long long end;
				if (_numberOfLayers - i_workPlane > 1)
				{
					end = _workPlaneLUTs[i_workPlane + 1].vectorblockspositions(0) - 1;
				}
				else
				{
					end = _streamlength;
				}
				long long start = _workPlaneLUTs[i_workPlane].workplaneshellposition();

				boost::iostreams::mapped_file_params params;
				params.path = _filename;
				params.mode = ios_base::binary;
				params.length = end - start;
				params.offset = start;
				params.flags = boost::iostreams::mapped_file::readonly;

				boost::iostreams::mapped_file_source mfs;
				mfs.open(params);
				stream<mapped_file_source> str{ mfs };
				google::protobuf::io::IstreamInputStream zc_stream(&str);

				WorkPlaneLUT wpLUT = _workPlaneLUTs[i_workPlane];
				bool fls = false;
				google::protobuf::util::ParseDelimitedFromZeroCopyStream(work_plane_shell, &zc_stream, &fls);
				return 0;
			}
		}

		/*
		google::protobuf::io::IstreamInputStream CreateLocalStream(int i_workPlane)
		{
			long long end;
			if (_numberOfLayers - i_workPlane > 1)
			{
				end = _workPlaneLUTs[i_workPlane + 1].vectorblockspositions(0) - 1;
			}
			else
			{
				end = _streamlength;
			}
			long long start = _workPlaneLUTs[i_workPlane].vectorblockspositions(0);

			boost::iostreams::mapped_file_params params;
			params.path = _filename;
			params.mode = ios_base::binary;
			params.length = end - start;
			params.offset = start;
			params.flags = boost::iostreams::mapped_file::readonly;

			boost::iostreams::mapped_file_source mfs;
			mfs.open(params);
			stream<mapped_file_source> str{ mfs };
			google::protobuf::io::IstreamInputStream zc_stream(&str);
			return zc_stream;
		}
		*/

		long long GetStreamOffset(int i_workPlane)
		{
			return _workPlaneLUTs[i_workPlane].vectorblockspositions(0);
		}

		/// <inheritdoc/>
		int get_vector_block_async(int i_workPlane, int i_vectorblock, VectorBlock* vectorblock)
		{
			return  get_vector_block_async(i_workPlane, i_vectorblock, vectorblock);
		}

		int get_vector_block(int i_workPlane, int i_vectorblock, VectorBlock* vectorblock)
		{
			if (_jobShell.num_work_planes() < i_workPlane)
			{
				last_error = "i_workPlane" + to_string(i_workPlane) + " out of range for job with " + to_string(_jobShell.num_work_planes()) + " workplanes!";
				return -9;
			}

			WorkPlaneLUT wpLut = _workPlaneLUTs[i_workPlane];
			if (wpLut.vectorblockspositions_size() < i_vectorblock)
			{
				last_error = "i_vectorblock " + to_string(i_vectorblock) + " out of range for workPlane with " + to_string(wpLut.vectorblockspositions_size()) + " blocks!";
				return -10;
			}

			if (_cacheState == CompleteJobCached)
			{
				*vectorblock = _job.work_planes(i_workPlane).vector_blocks(i_vectorblock);
				return 0;
			}
			else
			{
				long long end;
				if (wpLut.vectorblockspositions_size() - i_vectorblock > 1) // not last vector block in workplane
				{
					end = _workPlaneLUTs[i_workPlane].vectorblockspositions(i_vectorblock + 1) - 1;
				}
				else if (_numberOfLayers - i_workPlane > 1) // not last workplane in job
				{
					end = _workPlaneLUTs[i_workPlane + 1].vectorblockspositions(0) - 1;
				}
				else // if it is the last vector block in the last workplane, set end of sub-stream to end of file.
				{
					end = _streamlength;
				}
				long long start = _workPlaneLUTs[i_workPlane].vectorblockspositions(0);

				boost::iostreams::mapped_file_params params;
				params.path = _filename;
				params.mode = ios_base::binary;
				params.length = end - start;
				params.offset = start;
				params.flags = boost::iostreams::mapped_file::readonly;

				boost::iostreams::mapped_file_source mfs;
				mfs.open(params);
				stream<mapped_file_source> str{ mfs };
				google::protobuf::io::IstreamInputStream zc_stream(&str);

				VectorBlock vb;
				bool fls = false;
				google::protobuf::util::ParseDelimitedFromZeroCopyStream(&vb, &zc_stream, &fls);

				*vectorblock = vb;
				return 0;
			}
		}

		/// <inheritdoc/>
		void UnloadJobFromMemory()
		{
			_cacheState = NotCached;
		}

		/// <inheritdoc/>
		void Dispose()
		{
			// TODO: Implement clean-up
		}

		void CloseFile()
		{
			if (_fs.is_open())
			{
				_fs.close();
			}
		}
	};
}