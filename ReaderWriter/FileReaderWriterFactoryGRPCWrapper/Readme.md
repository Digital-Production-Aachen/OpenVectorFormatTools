# OpenVectorFormat: FileReaderWriterFactoryGRPCWrapper

This project provides an standalone console-application that wraps the functionality of the `FileReaderWriterFactory` and exposes it as an gRPC service and can easily be integrated in most progamming languages. The service definition can be found [here](../AbstractReaderWriter/grpc_reader_writer_interface.proto). For more information on gRPC, see here: [gRPC](https://grpc.io/).

To read / write a job all-at-once, call the `SimpleJobRead` or `SimpleJobWrite` procedures. If the job is very big and you do not want to keep it in memory all the time, use the `PartialJobRead` and `PartialJobWrite` procedures to open a stream and do the reading / writing in parts.

## How to build
At this time, there are no prebuild artefacts provided with each release.
To compile & run (skip the first step if you already have .net build environment set up on your machine):
* Setup `dotnet`  on your machine: [Download](https://dotnet.microsoft.com/download/dotnet) - be sure to pick a version >= 2.1, and use the SDK, not the runtime!
* Download / Clone this repository and enter the root folder
* Edit the network settings in `ReaderWriter\FileReaderWriterFactoryGRPCWrapper\grpc_server_config.json` to your needs.
* Open a command prompt in the root folder
* Run `dotnet run --configuration Release --framework netcoreapp2.1 --project .\ReaderWriter\FileReaderWriterFactoryGRPCWrapper\FileReaderWriterFactoryGRPCWrapper.csproj` - you can also use `net46` as framework if you have a legacy .net framework installed.

## How to use
A quick example on how to set up a connection to the gRPC service in C#:
```c#
using Grpc.Core;
using GrpcReaderWriterInterface;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenVectorFormat.GRPCWrapperDemo
{
    public class DemoGRPCWrapper
    {
        public void SupportedFormat()
        {
            string ip = "127.0.0.1";
            uint port = 50051;
            Channel channel = new Channel(ip + ":" + port.ToString(), ChannelCredentials.Insecure);

            VectorFileHandler.VectorFileHandlerClient client = new VectorFileHandler.VectorFileHandlerClient(channel);
            IsFormatSupportedReply reply = client.IsFormatSupported(new IsFormatSupportedRequest { FileExtension = ".ovf" });
            Console.WriteLine("Format supported for reading: " + reply.ReadSupport.ToString());
            Console.WriteLine("Format supported for writing: " + reply.WriteSupport.ToString());
            
            Console.WriteLine("All formats supported for reading: " + reply.AllReadSupportedFormats);
            Console.WriteLine("All formats supported for writing: " + reply.AllWriteSupportedFormats);

            channel.ShutdownAsync().Wait();
        }
    }
}
```

Further procedure calls & streaming are done in the unit test for this module: [TestGRPCWrapper.cs](UnitTests/TestGRPCWrapper.cs).
However, those examples are all in C#.

For details how to set up a gRPC connection in your preferred language, check out the quick start guide [here](https://grpc.io/docs/languages/).
            
