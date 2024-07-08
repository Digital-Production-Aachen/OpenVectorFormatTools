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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenVectorFormat.ILTFileReader.Controller
{
    class ModelSection : CliFileAccess, IModelSection
    {
        private string cliFileName;


        public ModelSection(string cliFileName, IModelSectionParams mParams)
            : base()
        {
            this.cliFileName = cliFileName;
            this.Parameters = mParams;
            this.ParseFileName();
        }


        private bool MatchMagics(string fileName)
        {
            var result = Regex.Match(fileName, @"(?<name>[a-z]+_[0-9]+)_(?<part_area>..?)_(?<vector_class>..+)\.cli$");
            if (!result.Success) return false;
            ModelsectionName = result.Groups["name"].Value;
            switch (result.Groups["part_area"].Value)
            {
                case "st":
                    this.Type = PartArea.st;
                    break;
                case "k":
                    this.Type = PartArea.k;
                    break;
                case "s1":
                    this.Type = PartArea.s1;
                    break;
                case "s2":
                    this.Type = PartArea.s2;
                    break;
                default:
                    this.Type = PartArea.s2;
                    break;
            }
            switch (result.Groups["vector_class"].Value)
            {
                case "vs":
                    this.SubType = VectorClass.vs;
                    break;
                case "vk":
                    this.SubType = VectorClass.vk;
                    break;
                case "us":
                    this.SubType = VectorClass.us;
                    break;
                case "uk":
                    this.SubType = VectorClass.uk;
                    break;
                case "kv":
                    this.SubType = VectorClass.kv;
                    break;
                case "sx":
                    this.SubType = VectorClass.sx;
                    break;
                case "kvu":
                    this.SubType = VectorClass.kvu;
                    break;
                case "skin":
                    this.SubType = VectorClass.skin;
                    break;
                default:
                    this.SubType = VectorClass.vs;
                    break;
            }
            return true;
        }

        private bool MatchNetfabb(string fileName)
        {
            //throw new NotImplementedException();
            var result = Regex.Match(fileName, @"(?<name>\S+)\s*(?<support>\(\S+\))?\s*(?<filling>\(\S+\))?\.cli$");
            if (!result.Success) return false;
            ModelsectionName = result.Groups["name"].Value;
            switch (result.Groups["support"].Value)
            {
                case "support":
                    break;
                case "solidsupport":
                    break;
                default:
                    break;
            }
            return true;
        }


        private void ParseFileName()
        {
            // ([a-z]+_[0-9]+)_(..?)_(..+)\.cli$
            string fileName = Path.GetFileName(this.cliFileName);
            if (MatchMagics(fileName)) return;
            if (MatchNetfabb(fileName)) return;
            throw new Exception();
#if false
            ModelsectionName = Regex.Match(fileName, @"([a-z]+_[0-9]+)_.+_.+\.cli$").Groups[1].Value;
            String partArea = Regex.Match(fileName, @"_(..?)_(..+)\.cli$").Groups[1].Value;
            switch (partArea)
            {
                case "st":
                    this.Type = PartArea.st;
                    break;
                case "k":
                    this.Type = PartArea.k;
                    break;
                case "s1":
                    this.Type = PartArea.s1;
                    break;
                case "s2":
                    this.Type = PartArea.s2;
                    break;
                default:
                    this.Type = PartArea.s2;
                    break;
            }

            String vectorClass = Regex.Match(fileName, @"_(..?)_(..+)\.cli$").Groups[2].Value;
            switch (vectorClass)
            {
                case "vs":
                    this.SubType = VectorClass.vs;
                    break;
                case "vk":
                    this.SubType = VectorClass.vk;
                    break;
                case "us":
                    this.SubType = VectorClass.us;
                    break;
                case "uk":
                    this.SubType = VectorClass.uk;
                    break;
                case "kv":
                    this.SubType = VectorClass.kv;
                    break;
                case "sx":
                    this.SubType = VectorClass.sx;
                    break;
                case "kvu":
                    this.SubType = VectorClass.kvu;
                    break;
                case "skin":
                    this.SubType = VectorClass.skin;
                    break;
                default:
                    this.SubType = VectorClass.vs;
                    break;
            }
#endif

            // netfabb regex: (\S+)\s*(\(\S+\))?\s*(\(\S+\))?\.cli
            // part name, support type, filling type

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
