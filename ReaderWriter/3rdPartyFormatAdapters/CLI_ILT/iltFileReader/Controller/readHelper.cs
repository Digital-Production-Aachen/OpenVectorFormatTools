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

﻿using System;
using System.Collections.Generic;
using System.IO;

namespace OpenVectorFormat.ILTFileReader.Controller
{
    class readHelper
    {
        public static float[] readFloatArray(BinaryReader reader, long loc, int length) {
            float[] ret = new float[length];
            int i = 0;
            lock (reader.BaseStream)
            {
                if (reader.BaseStream.Position != loc)     //resync our filereader in case we've jumped vectorcoords or stuff, we don't want to have in ram
                    reader.BaseStream.Seek(loc, SeekOrigin.Begin);

                while (reader.BaseStream.Position < length + loc) //save the location to prevent endless loop, because offset get's incremented, while loc shouldn't
                {
                    ret[i++] = (reader.ReadSingle()); //Good Vartype name for computer scientists
                }
            }
            return ret;
        }

        public static float[] readFloatArrayFromUshorts(BinaryReader reader, long loc, int length)
        {
            float[] ret = new float[length];
            int i = 0;
            lock (reader.BaseStream)
            {
                if (reader.BaseStream.Position != loc)     //resync our filereader in case we've jumped vectorcoords or stuff, we don't want to have in ram
                    reader.BaseStream.Seek(loc, SeekOrigin.Begin);

                while (reader.BaseStream.Position < length + loc)
                {
                    ret[i++] = (Convert.ToSingle(reader.ReadUInt16())); //Good Vartype name for computer scientists
                }
            }
            return ret;
        }
    }
}
