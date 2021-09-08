// OVFReaderWriter_test.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include "OVFFileReader.h"
#include "OVFFileWriter.h"

using namespace open_vector_format;
using namespace std;

int main()
{
    
    
    Job* job = new Job();
    OVFFileReader reader;
    reader.open_job_async("testjob.ovf", &job);
    reader.cache_job_to_memory_async();
    
    list<string> exclude_fields { "work_planes" };
    Job* job2 = new Job();
    cout << "author before: " << job->job_meta_data().author() << endl;
    google::protobuf::Map<google::protobuf::int32,open_vector_format::MarkingParams> mpm = job->marking_params_map();
    cout << "mpm 12 before: " << mpm[12].jump_delay_in_us() << endl;
    cout << "mpm 21 before: " << mpm[21].jump_delay_in_us() << endl;
    cout << "nwp before: " << job2->num_work_planes() << endl;


    copy_with_exclude(job, job2, exclude_fields);    


    cout << "nwp after: " << job2->num_work_planes() << endl;
    cout << "author after: " << job2->job_meta_data().author() << endl;
    google::protobuf::Map<google::protobuf::int32,open_vector_format::MarkingParams> mpm2 = job2->marking_params_map();
    cout << "mpm 12 before: " << mpm2[12].jump_delay_in_us() << endl;
    cout << "mpm 21 before: " << mpm2[21].jump_delay_in_us() << endl;

    
    //auto vb = wp.vector_blocks(0);
    //cout << "point: " << job->work_planes(0).vector_blocks(0)._hatches().points(4) << endl;    
    
    OVFFileWriter writer;
    writer.simple_job_write_async(job, "testjob2.ovf");
    writer.finish_file();
    delete job;
}