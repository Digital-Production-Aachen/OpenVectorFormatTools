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
//  ILoaderProgress.cs
//  Implementation of the Interface ILoaderProgress
//  Generated by Enterprise Architect
//  Created on:      20-Jun-2018 16:45:57
//  Original author: Dirks
///////////////////////////////////////////////////////////

namespace OpenVectorFormat.AbstractReaderWriter
{
	public interface IFileReaderWriterProgress  {

		bool IsCancelled
        { 
			get;
            set;
		}

        bool IsFinished
        {
            get;
            set;
        }

		/// 
		/// <param name="message"></param>
		/// <param name="progressPerCent"></param>
		void Update(string message, int progressPerCent);
	}//end ILoaderProgress

}//end namespace VectorFileHandler