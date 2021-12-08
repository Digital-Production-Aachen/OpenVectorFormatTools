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

using System.IO;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace OpenVectorFormat.FileHandlerFactoryGRPCWrapper
{
    class Program
    {

        public static void Main(string[] args)
        {
            ParseConfig();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureKestrel(options =>
            {
                IPAddress.TryParse(Config.IP, out IPAddress ip);
                options.Listen(ip, Config.Port);
            });
            webBuilder.UseStartup<Startup>();
        });

        private static void ParseConfig()
        {
            string path = System.Reflection.Assembly.GetEntryAssembly().Location;
            path = Path.GetDirectoryName(path);
            string configFile = path + "/grpc_server_config.json";

            var json = JsonSerializer.Deserialize<object>(File.ReadAllText(configFile)) as JsonElement?;
            Config.IP = json?.GetProperty("ip").ToString();
            Config.Port = int.Parse(json?.GetProperty("port").ToString());
            Config.AutomatedCachingThresholdBytes = long.Parse(json?.GetProperty("AutomatedCachingThresholdBytes").ToString());

            if (Config.IP == string.Empty | Config.Port == -1 | Config.AutomatedCachingThresholdBytes == -1)
            {
                throw new InvalidDataException("Did not get all config values.");
            }
        }
    }
}
