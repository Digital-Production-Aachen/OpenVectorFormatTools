/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2023 Digital-Production-Aachen

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
//  ICLIFile.cs
//  Implementation of the Interface ICLIFile
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
    /// <summary>
    /// The Common Layer Interface (CLI) is a universal format for the input of geometry data to model fabrication systems based
    /// on layer manufacturing technologies (LMT). It is suitable for systems using layer-wise photo-curing of resin, sintering
    /// or binding of powder, cutting of sheet material, solidification of molten material, and any other systems which build 
    /// models on a layer-by-layer basis. CLI is intended as a simple, efficient and unambiguous format for data input to all
    /// LMT-based systems, based on a "2 1/2D" layer representation.It is independent of vendors or fabrication machines, and
    /// should require only a simple conversion to the vendor-specific internal data structure of the machine.The obligatory parts
    /// of the format are also application independent, while the USERDATA command allows user- or application-specific data to
    /// be defined in the header.This flexibility allows the format to be used for a wide range of applications, without loss of
    /// important information and without excluding data transfer between different applications.One specific application, medical
    /// scan data, is already accommodated with appropriate user data. Others can be added as they are defined.
    /// </summary>
	public interface ICLIFile  {

		IGeometry Geometry{
			get;
		}

		IHeader Header{
			get;
		}

		IList<IPart> Parts{
			get;
		}

    }//end ICLIFile

}//end namespace OpenVectorFormat.ILTFileReader
