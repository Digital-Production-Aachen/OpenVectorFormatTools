# Unit Tests for OpenVectorFormatTools

This folder contains unit tests based on the netCore test framework.

They can be run from VisualStudios Test Explorer or through dotnet test on the command line.

## Testing the GRPC wrapper
The unit tests testing the FileReaderWriterFactoryGRPCWrapper require the GRPC wrapper to be running. This means that the project needs to be build, an instance of the GRPC wrapper needs to be started, and only then the tests may execute.

### Command Line
For the (linux) command line, a suitable workflow is:

```
dotnet build ReaderWriter/UnitTests/UnitTests.csproj --framework netcoreapp2.1
dotnet run --framework netcoreapp2.1 --project FileReaderWriterFactoryGRPCWrapper background &
grpcpid="$!"
dotnet test --no-build --framework netcoreapp2.1
kill $grpcpid
```

### Visual Studio
When running in Visual

- Build the UnitTest project (or just do a build of the complete solution)
- Execute the FileReaderWriterFactoryGRPCWrapper.exe (found e.g. in ReaderWriter\FileReaderWriterFactoryGRPCWrapper\bin\Debug\net46)
- Run the tests from the test explorer
