# c++ reader / writer for OVF Files - WIP

Bazel is used as build tool.
To build the c++ library use `bazel build OVFReaderWriter:OVFFileReaderWriter`.
The `OVFReaderWriter_test.cpp` is not a unit test, but just used to build an executable to test whatever aspects you code in there. There is also no test framework around. To build, use `bazel build OVFReaderWriter:OVFReaderWriter_test`, and to run replace `build` with `run`.

Also included is a config for VS Code to automatically build & debug with dbg within VS Code.
