# OVF File Structure
To enable selective streaming of various elements of a job from the file, the Job protobuf message cannot simply be seriallized and dumped to a file. Instead, the job is seriallized in parts and the position of the parts is indicated by look-up-tables (LUTs).

The LUTs themselves are protobuf messages, the .proto file can be found at [protobuf/ovf_lut.proto](OVFReaderWriter/protobuf/ovf_lut.proto).

## Structure overview

| Length (bytes) | Description | Values |
|---|---|---|
| 4      | Magic number | [0x4c, 0x56, 0x46, 0x21] |
| 8     | Position of Job LUT in File (Int64, LittleEndian)      |   |
| var | VectorBlock 0 of WorkPlane 0      |     |
| var | VectorBlock 1 of WorkPlane 0      |     |
| var | VectorBlock 2 of WorkPlane 0      |     |
| ... | ...      | ...     |
| var | WorkPlaneShell of WorkPlane 0 (WorkPlane message with empty vector-blocks array) | |
| var | WorkPlane LUT for WorkPlane 0 | |
| var | VectorBlock 0 of WorkPlane 1      |     |
| var | VectorBlock 1 of WorkPlane 1      |     |
| ... | ...      | ...     |
| var | WorkPlaneShell of WorkPlane 1 (WorkPlane message with empty VectorBlocks array) | |
| var | WorkPlane LUT for WorkPlane 1 | |
| ... | ...      | ...     |
| var | JobShell (Job message with empty WorkPlane array) ||
| var | JobLUT || 

## JobLUT
The JobLUT is positioned at the very end of the file. The starting position of the LUT is indicated by the Int64 at the begining of the file, right behind the magic number (see table above).

The JobLUT contains 
- the starting position of the JobShell
- an array of starting positions for each WorkPlaneLUT

After reading the JobLUT, the JobShell should be read. Furthermore, with the starting positions of the WorkPlaneLUTs, any WorkPlane or VectorBlock can be located in the file in arbitrary order.

## WorkPlaneLUTs
The WorkPlaneLUT is for a WorkPlane what the JobLUT is for the complete job. It is positioned at the very end of the WorkPlane, and the starting position of the WorkPlaneLUT can be found in the corresponding array provided by the JobLUT.

The WorkPlaneLUT contains
- the starting position of the WorkPlaneShell
- an array of starting positions for each VectorBlock in the WorkPlane

## How to read a job
- Read the first 4 bytes and verify that they are correct (magic number). This is just a first consistency check to make sure that it is indeed an OpenVectorFormat file.
- Read another 8 bits as Int64, LittleEndian for the JobLUT position
- Read the JobLUT by parsing a JobLUT protobuf message from the JobLUT position in the file
- Read the JobShell by parsing a Job protobuf message from the JobShell position provided by the JobLUT

### Read WorkPlanes and VectorBlocks
- Find the WorkPlaneLUT position from the WorkPlaneLUTs-position array in the JobLUT
- Read the WorkPlaneLUT by parsing a WorkPlaneLUT protobuf message from the found position in the file
- Read the WorkPlaneShell by parsing a WorkPlane protobuf message from the WorkPlaneShell position provided by the WorkPlaneLUT
- Read the VectorBlocks as needed by parsing a VectorBlock protobuf message from the position provided by the VectorBlocksPosition array in the WorkPlaneLUT
