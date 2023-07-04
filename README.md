# OpenVectorFormat Tools
This repository provides basic tooling to make working with the protobuf-based [OpenVectorFormat](https://github.com/Digital-Production-Aachen/OpenVectorFormat) easier.

## How to get
* From nuget: [Digital-Production-Aachen on nuget](https://www.nuget.org/profiles/Digital-Production-Aachen)
* To integrate the source code into your C#-project directly, clone the repository and add the corresponding projects to your solution.
* For use with programmin languages other then C#, refer to [`FileReaderWriterFactoryGRPCWrapper`](ReaderWriter/FileReaderWriterFactoryGRPCWrapper).

## Available tools
Currently, the following tools are available:

### SIMD Accelerated Extension Methods for Translation, Rotation and Bounds
The OVF definition project offers LINQ-style extension methods for vector block, workplane and job objects.
The extensions implement the common vector block operations translate, rotate and calculating an axis algined bounding box.
They also offer direct memory access of the underlying float arrays of vector blocks utilizing memory marshalling to Span<float> : vectorBlock.RawCoordinates().AsSpan().
Translate, Rotate and Bounds uses SIMD intrinsics on supported hardware with software fallbacks. Benchmarks on AVX256 capable hardware show tenfold performance compared to simple loops.

### Readers / Writers
A set of reader / writer libraries providing a simple interface to manage OpenVectorFormat-Jobs in your application.
For further information, see here: [Reader / Writer Libraries](ReaderWriter)

### Streaming Capabilites to Merge, Translate, Rotate and Apply Parameters to OVF data
The BuildProcessor and StreamingMerger classes of the OVF Streaming project are used to merge sliced OVF data of single parts into full build job files.
The BuildProcessor accepts multiple OVF data sources and applies appropriate parameters to each vector block based on the block meta data provided by the slicer (e.g. upskin, downskin, contour, volume etc.).
It can also mark all blocks of an OVF source as support. Build Processors are initialized with an OVF ParameterSetEngine object that defines valid parameter sets and fallbacks for missing parameters.
BuildProcessor and StreamingMerger also support translating and rotating vector data to instantiate parts in a build job, using the SIMD accelerated extansions.
Streaming capable means that all operations are lazily executed only when workplanes and vector blocks are written, e.g. to an OVFFileWriter. This vastly reduced the in-memory footprint of build jobs.
All Streaming capable classes inherit from the abstract FileReader class and can be nested this way. A typical setup is using one BuildProcessor per part to merge part and supports and apply parameters,
and then merging multiple build processors to a job using StreamingMerger for instance-wise translations and rotations.

### Plausibility / Consistency Checker (C# only currently)
 A tool to check basic consistency of a job, e.g. to check that the number of layers is consistent throughout the job. For further information, see here: [Plausibility Checker](PlausibilityChecker)

### Missing tools?
Feel free to open an issue regarding any tools you would like to see - or start hacking and open a pull request for it to be integrated into this repository!

Also, chances are we might already have some prototype tools internally that are not yet ready for publication - contact us and join in with the development & testing!

## Licenses, Legal
This license for OpenVectorFormatTools can be found [here](LICENSE).

Licenses of libraries used / distributed with OpenVectorFormatTools are listed [here](Licenses_of_used_libraries.md).
