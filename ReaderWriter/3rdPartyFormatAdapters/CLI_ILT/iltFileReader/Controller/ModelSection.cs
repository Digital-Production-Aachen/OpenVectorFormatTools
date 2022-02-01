/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2022 Digital-Production-Aachen

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


        private void ParseFileName()
        {
            string fileName = Path.GetFileName(this.cliFileName);
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
                    throw new FormatException("I don't know the Type / PartArea");
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
                    throw new FormatException("I don't know the SubType / VectorClass");
            }

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
