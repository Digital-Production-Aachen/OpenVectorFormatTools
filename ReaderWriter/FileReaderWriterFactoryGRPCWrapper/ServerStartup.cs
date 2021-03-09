/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2021 Digital-Production-Aachen

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

---- Copyright End ----
*/



using System;
using System.IO;
using System.Threading;
using Grpc.Core;
using GrpcReaderWriterInterface;
using Newtonsoft.Json;

namespace OpenVectorFormat.FileHandlerFactoryGRPCWrapper
{

    class ServerStartup
    {
        /// <summary>
        /// Just initialization of server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {


            string path = System.Reflection.Assembly.GetEntryAssembly().Location;
            path = Path.GetDirectoryName(path);
            string configFile = path + "/grpc_server_config.json";
            using (StreamReader sr = new StreamReader(configFile))
            {
                JsonTextReader reader = new JsonTextReader(sr);
                while (reader.Read())
                {
                    if (reader.Value == null) { continue; }

                    string value = reader.Value.ToString().Trim().ToUpperInvariant();

                    switch (value)
                    {
                        case "IP":
                            reader.Read();
                            Config.IP = (string)reader.Value;
                            break;
                        case "PORT":
                            reader.Read();
                            Config.Port = (long)reader.Value;
                            break;
                        case "AUTOMATEDCACHINGTHRESHOLDBYTES":
                            reader.Read();
                            Config.AutomatedCachingThresholdBytes = (long)reader.Value;
                            break;
                    }
                }
            }

            if (Config.IP == string.Empty | Config.Port == -1 | Config.AutomatedCachingThresholdBytes == -1)
            {
                throw new InvalidDataException("Did not get all config values.");
            }

            Server server = new Server
            {
                Services = { VectorFileHandler.BindService(new GRPCWrapperFunctionsImplementation()) },
                Ports = { new ServerPort(Config.IP, (int)Config.Port, ServerCredentials.Insecure) }
            };
            server.Start();
            String[] arguments = Environment.GetCommandLineArgs();

            // check if CommandLineArg "background" was passed - use infinite timeout instead of reading a key. needed for CI testing.
            bool background = false;
            if (arguments.Length > 1 && arguments[1] == "background")
            {
                background = true;
                Console.WriteLine("running in background mode");
                Thread.Sleep(Timeout.Infinite);
            }
            if (!background)
            {
                Console.WriteLine("VectorFileHandler server listening on {0}:{1} ", Config.IP, Config.Port);
                Console.WriteLine("Press any key to stop the server...");
                Console.ReadKey();
            }
            server.ShutdownAsync().Wait();
        }
    }
}
