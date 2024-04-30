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

///////////////////////////////////////////////////////////
//  ILayer.cs
//  Implementation of the Interface ILayer
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
    /// A layer is the volume between two parallel slices, and is defined by its thickness, a set of contours and (optionally) hatches.
    /// Layers may be empty if parameter set is used for only few vectors.
    /// </summary>
	public interface ILayer  {

		float Height{
			get;
		}

		IList<IVectorBlock> VectorBlocks{
			get;
		}
	}//end ILayer

}//end namespace OpenVectorFormat.ILTFileReader
