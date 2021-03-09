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
//  IHeader.cs
//  Implementation of the Interface IHeader
//  Generated by Enterprise Architect
//  Created on:      08-Mai-2018 17:55:36
//  Original author: Dirks
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;



using OpenVectorFormat.ILTFileReader;
namespace OpenVectorFormat.ILTFileReader {
	public interface IHeader  {
        /// <summary>
        /// Mandatory: binary or ASCII
        /// </summary>
		DataFormatType DataFormat
        {
			get;
		}
        /// <summary>
        /// Optional: file was built on date. d will be interpreted in the sequence DDMMYY.
        /// </summary>
		int Date
        {
			get;
		}
        /// <summary>
        /// Optional: Describes the dimensions of the outline box which completely contains the part in absolute coordinates (in mm)
        /// with respect to the origin. The conditions x1 &lt x2 , y1 &lt y2 and z1 &lt z2 must be satisfied.
        /// </summary>
        IDimension Dimension
        {
			get;
		}
        /// <summary>
        ///  Optional: number of layers inside the file
        /// </summary>
		int NumLayers
        {
			get;
		}
        /// <summary>
        /// Mandatory: Units indicates the units of the coordinates in mm, e.g. Units = 0.001 means that each unit of the file is 0.001 mm -> units are �m.
        /// </summary>
		float Units
        {
			get;
		}
        /// <summary>
        /// Optional: the USERDATA command allows user- or application-specific data to be defined in the header.
        /// </summary>
		IUserData UserData
        {
			get;
		}
        /// <summary>
        /// Mandatory: The version number.
        /// </summary>
		int Version
        {
			get;
		}
	}//end IHeader

}//end namespace OpenVectorFormat.ILTFileReader
