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

///////////////////////////////////////////////////////////
//  IModelSection.cs
//  Implementation of the Interface IModelSection
//  Generated by Enterprise Architect
//  Created on:      08-Mai-2018 17:55:36
//  Original author: Dirks
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;



using OpenVectorFormat.ILTFileReader;
namespace OpenVectorFormat.ILTFileReader
{
    public interface IModelSection : ICLIFile
    {

        IModelSectionParams Parameters
        {
            get;
        }

        VectorClass SubType
        {
            get;
        }

        PartArea Type
        {
            get;
        }

        string ModelsectionName
        {
            get;
        }
    }//end IModelSection

}//end namespace OpenVectorFormat.ILTFileReader
