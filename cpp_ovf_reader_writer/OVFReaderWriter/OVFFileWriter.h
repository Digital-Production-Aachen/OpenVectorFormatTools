#pragma once
#include <iostream>
#include <fstream>
#include <stdlib.h>
#include <algorithm>
#include <google/protobuf/util/delimited_message_util.h>
#include <google/protobuf/io/zero_copy_stream_impl.h>
#include <list>

#include "FileWriter.h"
#include "ovf_lut.pb.h"
#include "copy_with_exclude.h"
using namespace open_vector_format;
using namespace std;

namespace open_vector_format
{
    class OVFFileWriter : FileWriter
    {
    private:
        //IFileReaderWriterProgress progress;
        string _filename;
        ofstream _fs;
        Job _jobShell;
        
        // Index in the file header where the index on the workPlane LUT will be saved. Should(!) be 2, since this index follows directly after the Int16 indicating FileWriteOperation.PartialWrite.
        long long _LUTPositionIndex = 0;

        // workPlaneLUT containing the start indices of each workPlane in filestream
        JobLUT _jobLUT;

        // shell object of the current workPlane
        unique_ptr<WorkPlane> _currentWorkPlaneShell = NULL;

        FileWriteOperation _fileOperationInProgress = FileWriteOperation::None;

        void finish_work_plane()
        {
            if (_currentWorkPlaneShell != NULL)
            {
                if(!_fs.is_open())
                {
                    return;
                }
            
                _fs.seekp(0, ios::end); // move stream position to the end of the stream

                _jobLUT.add_workplanepositions((long long)_fs.tellp());
                
                long long _workPlaneLUTIndex = _fs.tellp();

                char dummy_position[8];
                _fs.write(dummy_position, sizeof(long long));

                WorkPlaneLUT wpLUT;

                for (int i_block = 0; i_block < _currentWorkPlaneShell->vector_blocks_size(); i_block++)
                {
                    wpLUT.add_vectorblockspositions((long long)_fs.tellp());
                    google::protobuf::util::SerializeDelimitedToOstream(_currentWorkPlaneShell->vector_blocks(i_block), &_fs);
                }

                _currentWorkPlaneShell->set_work_plane_number(_jobShell.num_work_planes());
                _currentWorkPlaneShell->set_num_blocks(_currentWorkPlaneShell->vector_blocks_size());
                unique_ptr<WorkPlane> newWorkPlaneShell = unique_ptr<WorkPlane>(new WorkPlane());
                copy_with_exclude(_currentWorkPlaneShell.get(), newWorkPlaneShell.get(), list<string> {"vector_blocks"});

                _currentWorkPlaneShell = move(newWorkPlaneShell);

                wpLUT.set_workplaneshellposition(_fs.tellp());
                google::protobuf::util::SerializeDelimitedToOstream(*_currentWorkPlaneShell, &_fs);

                long long lutPosition = _fs.tellp();

                google::protobuf::util::SerializeDelimitedToOstream(wpLUT, &_fs);
                _fs.seekp(_workPlaneLUTIndex);
                unique_ptr<char[]> lutPosBytes = convert_to_little_endian_char_array(lutPosition);
                _fs.write(lutPosBytes.get(), sizeof(long long));
                _fs.seekp(0, ios::end);

                _jobShell.set_num_work_planes(_jobShell.num_work_planes() + 1);
            
            }
        }

        unique_ptr<char[]> convert_to_little_endian_char_array(long long val)
        {
            int BUFFER_SIZE = 8;
            unique_ptr<char[]> buffer(new char[BUFFER_SIZE]);
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                buffer[i] = ((val >> (8 * i)) & 0XFF);
            }

            if (is_big_endian())
            {
                reverse(buffer.get(), buffer.get() + sizeof(long long));
            }
            return buffer;
        }

        bool is_big_endian(void)
		{
			union {
				uint32_t i;
				char c[4];
			} bint = { 0x01020304 };

			return bint.c[0] == 1;
		}

    public:
        void get_job_shell(Job** job_shell)
        {
            *job_shell = &_jobShell;
        }

        /// <inheritdoc/>
        FileWriteOperation get_file_operation_in_progress() { return _fileOperationInProgress; }

        list<string> get_supported_file_formats() { return list<string> { ".ovf" }; }

        /// <inheritdoc/>
        void start_write_partial(Job* jobShell, string filename)
        {
            _fileOperationInProgress = FileWriteOperation::PartialWrite;

            // remove workplanes if present.
            // create copy of jobshell, since the provided jobShell is a reference to the callers' object. Removing potentially present workplanes in the provdied jobShell also removes them for the caller.
            _jobShell = Job();
            copy_with_exclude(jobShell, &_jobShell, list<string> { "work_planes" });
            _jobShell.set_num_work_planes(0);

            //this.progress = progress;
            _filename = filename;

            // Create jobfile and write header
            _fs.open(_filename, ios::binary);

            char magic_number[4] = { 0x4c, 0x56, 0x46, 0x21 };
            _fs.write(magic_number, 4);
            _LUTPositionIndex = _fs.tellp();
                
            char lut_pos_index_placeholder[8];
            _fs.write(lut_pos_index_placeholder, sizeof(long long));

                // Initialize workPlane lookup table
            _jobLUT = JobLUT();
        }
        
        /// <inheritdoc/>
        void append_work_plane_async(WorkPlane workPlane)
        {
            if (_fileOperationInProgress != FileWriteOperation::PartialWrite)
            {
                throw runtime_error("Adding workPlanes is only possible after initializing a file opertaion with StartWriteAsync");
            }
            // finish the previous workPlane
            finish_work_plane();
                        
            // hold the workPlane in RAM until writing is done
            _currentWorkPlaneShell = unique_ptr<WorkPlane>(new WorkPlane(workPlane));
            
            
        }

        /// <inheritdoc/>
        void append_vector_block_async(VectorBlock block)
        {
            if (_fileOperationInProgress != FileWriteOperation::PartialWrite)
            {
                throw runtime_error("Adding a VectorBlock is only possible after initializing a file opertaion with StartWriteAsync");
            }

            if (_currentWorkPlaneShell == NULL)
            {
                throw runtime_error("No workPlane added yet! A workPlane must be added before adding VectorBlocks");
            }

            auto vb_pointer = _currentWorkPlaneShell->add_vector_blocks();
            vb_pointer->CopyFrom(block);
        }

        /// <inheritdoc/>
        void finish_file()
        {
            if (_fileOperationInProgress == FileWriteOperation::PartialWrite && _fs.is_open() && _fs.good())
            {
                // [WorkPlaneN][JobShell][LUT]
                // write last workPlane
                finish_work_plane();
                
                    // write shell
                    _jobLUT.set_jobshellposition(_fs.tellp());
                    google::protobuf::util::SerializeDelimitedToOstream(_jobShell, &_fs);
                    // write LUT
                    long long LUTPosition = _fs.tellp();
                    google::protobuf::util::SerializeDelimitedToOstream(_jobLUT, &_fs);
                    //update LUT position to finalize file and make it valid
                    _fs.seekp(_LUTPositionIndex, ios::beg);
                    unique_ptr<char[]> lutPosBytes = convert_to_little_endian_char_array(LUTPosition);
                    _fs.write(lutPosBytes.get(), sizeof(long long));
                    //progress.IsFinished = true;
            }
            if (_fs.is_open()) { _fs.close(); }
            _jobLUT = JobLUT();
            _fileOperationInProgress = FileWriteOperation::None;
        }

        /// <inheritdoc/>
        void simple_job_write_async(Job* job, string filename)
        {
            if (job->num_work_planes() != job->work_planes_size())
            {
                throw runtime_error("File incosistend! num_work_planes not equal to size(job.work_planes).");
            }
            
            start_write_partial(job, filename);

            // Add all workPlanes already contained in the job
            for (int i = 0; i < job->work_planes_size(); i++)
            {
                //progress.Update("workPlane " + workPlane.WorkPlaneNumber + "added", (int)((double)workPlane.WorkPlaneNumber / (double)job.WorkPlanes.Count * 100));
                append_work_plane_async(job->work_planes(i));
            }
            finish_file();
            //progress.IsFinished = true;

        }

        ~OVFFileWriter()
        {
            if (_fileOperationInProgress != FileWriteOperation::None)
            {
                finish_file();
            }
        }
    };
}