/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2024 Digital-Production-Aachen

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

﻿using Google.Protobuf;
using OpenVectorFormat;
using OpenVectorFormat.OVFReaderWriter;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace OVFReaderWriter
{
    public class OVFStatistics
    {
        public static int CountVectorBlocks(OVFFileReader reader)
        {
            int totalCount = 0;
            for (int i = 0; i < reader.JobShell.NumWorkPlanes; i++)
            {
                var wp = reader.GetWorkPlaneShell(i);
                totalCount += wp.NumBlocks;
            }
            return totalCount;
        }

        public static Dictionary<VectorBlock.VectorDataOneofCase, int> CountVectorBlocksByType(OVFFileReader reader)
        {
            var counts = new Dictionary<VectorBlock.VectorDataOneofCase, int>();
            foreach (VectorBlock.VectorDataOneofCase key in Enum.GetValues(typeof(VectorBlock.VectorDataOneofCase)))
                counts.Add(key, 0);

            for (int i = 0; i < reader.JobShell.NumWorkPlanes; i++)
            {
                var wp = reader.GetWorkPlane(i);
                foreach (var vb in wp.VectorBlocks)
                {
                    counts[vb.VectorDataCase]++;
                }
            }
            return counts;
        }

        public static string GetJobShellInfoAsJSON(OVFFileReader reader, bool includeWorkPlaneShells = false)
        {
            var formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation());
            var job = reader.JobShell;
            if (includeWorkPlaneShells)
            {
                for (int i = 0; i < job.NumWorkPlanes; i++)
                {
                    job.WorkPlanes.Add(reader.GetWorkPlaneShell(i));
                } 
            }
            return formatter.Format(job);
        }

        public static string CreateSummary(OVFFileReader reader)
        {
            var shell = reader.JobShell;
            StringBuilder sb = new StringBuilder();
            int indent = 0;

            Line($"# Report for: {shell.JobMetaData.JobName}");
            EmptyLine();

            Line("## Job Info");
            Attribute("Metadata", shell.JobMetaData.ToString());
            Attribute("Bounds 2D", shell.Bounds2D());
            Attribute("Num Workplanes", shell.NumWorkPlanes);
            Attribute("Num Vectorblocks", OVFStatistics.CountVectorBlocks(reader));
            Indent();
            foreach (var kvp in OVFStatistics.CountVectorBlocksByType(reader))
            {
                if (kvp.Value > 0)
                    Attribute(kvp.Key.ToString(), kvp.Value.ToString());
            }
            Unindent();
            EmptyLine();

            Line($"## Marking Params Map ({shell.MarkingParamsMap.Count})");
            foreach (var kvp in shell.MarkingParamsMap)
            {
                Attribute(kvp.Key.ToString(), kvp.Value.ToString());
            }
            EmptyLine();

            Line($"## Parts Map ({shell.PartsMap.Count})");
            foreach (var kvp in shell.PartsMap)
            {
                Attribute(kvp.Key.ToString(), kvp.Value.ToString());
            }
            EmptyLine();

            return sb.ToString();

            #region formatting functions
            void Attribute(string name, object attr, int pad = -10)
            {
                sb.AppendFormat(new string(' ', indent) + $"{{0, {pad}}}", "- " + name + ": " + attr.ToString());
                sb.Append('\n');
            }

            void Line(string text)
            {
                sb.AppendFormat(new string(' ', indent) + "{0, -10}", text);
                sb.Append('\n');
            }

            void EmptyLine()
            {
                sb.AppendLine();
            }

            void Indent()
            {
                indent += 1;
            }

            void Unindent()
            {
                indent -= 1;
            }
            #endregion
        }
    }
}
