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

﻿using OpenVectorFormat.ILTFileReader;
using OpenVectorFormat.ILTFileReader.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenVectorFormat.ILTFileReader.Controller
{
    class ModelSection : CliFileAccess, IModelSection
    {
        private string cliFileName;
        public int ID { get; set; }


        public ModelSection(string cliFileName, IModelSectionParams mParams)
            : base()
        {
            this.cliFileName = cliFileName;
            this.Parameters = mParams;
            this.ParseFileName();
        }


        private void ParseFileName()
        {
            string fileName = Path.GetFileName(this.cliFileName);
            if (MatchMagics(fileName)) { Console.WriteLine("Magics"); return; }
            if (Match3dXpert(fileName)) { Console.WriteLine("3dXpert"); return; }
            if (MatchNetfabb(fileName)) { Console.WriteLine("Netfabb"); return; }
            throw new Exception($"could not match ilt filename: {fileName}");
        }

        private bool MatchMagics(string fileName)
        {
            var result = Regex.Match(fileName, @"(?<name>([\w-[_]]+)_\d+)_(?<part_area>..?)_(?<vector_class>..+)\.cli$");
            if (!result.Success) return false;
            ModelsectionName = result.Groups["name"].Value;
            switch (result.Groups["part_area"].Value)
            {
                case "st":
                    Type = PartArea.st;
                    break;
                case "k":
                    Type = PartArea.k;
                    break;
                case "s1":
                    Type = PartArea.s1;
                    break;
                case "s2":
                    Type = PartArea.s2;
                    break;
                default:
                    Type = PartArea.s2;
                    break;
            }
            switch (result.Groups["vector_class"].Value)
            {
                case "vs":
                    SubType = VectorClass.vs;
                    break;
                case "vk":
                    SubType = VectorClass.vk;
                    break;
                case "us":
                    SubType = VectorClass.us;
                    break;
                case "uk":
                    SubType = VectorClass.uk;
                    break;
                case "kv":
                    SubType = VectorClass.kv;
                    break;
                case "sx":
                    SubType = VectorClass.sx;
                    break;
                case "kvu":
                    SubType = VectorClass.kvu;
                    break;
                case "skin":
                    SubType = VectorClass.skin;
                    break;
                default:
                    SubType = VectorClass.vs;
                    break;
            }
            return true;
        }

        private bool Match3dXpert(string fileName)
        {
            var result = Regex.Match(fileName, @"(?<name>([\w-[_]]+)_\d+)_(?<build_style>[\w-[_]]+)_.\S+_(?<contour>\w*)(?<type>[a-z]{2})\.cli");
            if (!result.Success) return false;
            ModelsectionName = result.Groups["name"].Value;
            switch (result.Groups["contour"].Value)
            {
                case "":    // assumption: no c means hatches (?)
                    Type = PartArea.k;
                    switch (result.Groups["type"].Value)
                    {
                        // dn->Downskin
                        // up->Upskin
                        // md->Middle area(Infill)
                        case "up":
                            SubType = VectorClass.vs;   // this should be upskin instead
                            break;
                        case "md":
                            SubType = VectorClass.vs;
                            break;
                        case "dn":
                            SubType = VectorClass.us;
                            break;
                    }
                    break;
                // c1, c2, c3 -> Contour 1 to 3
                case "c1":
                case "c2":
                case "c3":
                default:
                    Type = PartArea.s2;
                    switch (result.Groups["type"].Value)
                    {
                        case "up":
                            SubType = VectorClass.vk;   // this should be upskin instead
                            break;
                        case "md":
                            SubType = VectorClass.vk;
                            break;
                        case "dn":
                            SubType = VectorClass.uk;
                            break;
                    }
                    break;
            }
            return true;
        }

        private bool MatchNetfabb(string fileName)
        {
            var result = Regex.Match(fileName, @"(?<name>\S+)\s*(?<support>\(\S+\))?\s*(?<filling>\(\S+\))?\.cli$");
            if (!result.Success) return false;
            ModelsectionName = result.Groups["name"].Value;
            if (result.Groups["filling"].Value == "(filling)")
            {
                SubType = VectorClass.vs;
            }
            else
            {
                SubType = VectorClass.vk;
            }
            switch (result.Groups["support"].Value)
            {
                case "(support)":
                    Type = PartArea.st;
                    SubType = VectorClass.sx;
                    break;
                case "(solidsupport)":
                    Type = PartArea.st;
                    break;
                default:
                    Type = PartArea.s2;
                    break;
            }
            return true;
        }


        public IModelSectionParams Parameters
        {
            get;
            set;
        }

        public VectorClass SubType
        {
            get;
            set;
        }

        public PartArea Type
        {
            get;
            set;
        }

        public string ModelsectionName
        {
            get;
            set;
        }
    }
}
