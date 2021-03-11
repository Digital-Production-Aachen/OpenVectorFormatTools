# OpenVectorFormat Tools
This repository provides basic tooling to make working with the protobuf-based [OpenVectorFormat](https://github.com/Digital-Production-Aachen/OpenVectorFormat) easier.

## How to get
* From nuget: [Digital-Production-Aachen on nuget](https://www.nuget.org/profiles/Digital-Production-Aachen)
* To integrate the source code into your C#-project directly, clone the repository and add the corresponding projects to your solution.
* For use with programmin languages other then C#, refer to [`FileReaderWriterFactoryGRPCWrapper`](ReaderWriter/FileReaderWriterFactoryGRPCWrapper).

## Available tools
Currently, the following tools are available:
### Readers / Writers
A set of reader / writer libraries providing a simple interface to manage OpenVectorFormat-Jobs in your application. For further information, see here: [Reader / Writer Libraries](ReaderWriter)

### Plausibility / Consistency Checker (C# only currently)
 A tool to check basic consistency of a job, e.g. to check that the number of layers is consistent throughout the job. For further information, see here: [Plausibility Checker](PlausibilityChecker)

### Missing tools?
Feel free to open an issue regarding any tools you would like to see - or start hacking and open a pull request for it to be integrated into this repository!

Also, chances are we might already have some prototype tools internally that are not yet ready for publication - contact us and join in with the development & testing!
