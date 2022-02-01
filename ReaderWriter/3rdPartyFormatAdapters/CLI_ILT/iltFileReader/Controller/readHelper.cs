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

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenVectorFormat.ILTFileReader.Controller
{
    class readHelper
    {

        public static UInt16 readUint16(StreamReader reader, ref long index) {
            byte[] bytes = new byte[2];
            bytes[0] = readByte(reader, ref index);
            bytes[1] = readByte(reader, ref index);
            return BitConverter.ToUInt16(bytes,0);
        }

        public static int readInt(StreamReader reader, ref long index)
        {
            byte[] bytes = new byte[4];
            bytes[0] = readByte(reader, ref index);
            bytes[1] = readByte(reader, ref index);
            bytes[2] = readByte(reader, ref index);
            bytes[3] = readByte(reader, ref index);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static float readReal(StreamReader reader, ref long index) {

            byte[] bytes = new byte[4];
            bytes[0] = readByte(reader, ref index);
            bytes[1] = readByte(reader, ref index);
            bytes[2] = readByte(reader, ref index);
            bytes[3] = readByte(reader, ref index);
            return BitConverter.ToSingle(bytes, 0); //Good Vartype name for computer scientists
        }

        public static float[] readFloatArray(BinaryReader reader, long loc, int length) {
            List<float> ret = new List<float>();

            if (reader.BaseStream.Position != loc)     //resync our filereader in case we've jumped vectorcoords or stuff, we don't want to have in ram
                reader.BaseStream.Seek(loc, SeekOrigin.Begin);

            while (reader.BaseStream.Position < length + loc) //save the location to prevent endless loop, because offset get's incremented, while loc shouldn't
            {
                ret.Add(reader.ReadSingle()); //Good Vartype name for computer scientists
            }
            return ret.ToArray();
        }

        public static float[] readFloatArrayFromUshorts(BinaryReader reader, long loc, int length)
        {
            List<float> ret = new List<float>();
            if (reader.BaseStream.Position != loc)     //resync our filereader in case we've jumped vectorcoords or stuff, we don't want to have in ram
                reader.BaseStream.Seek(loc, SeekOrigin.Begin);

            while (reader.BaseStream.Position < length + loc)
            {
                ret.Add(Convert.ToSingle(reader.ReadUInt16())); //Good Vartype name for computer scientists
            }
            return ret.ToArray();
        }

        private static byte readByte(StreamReader reader, ref long index) {
            byte beyti;
            char[] charr = new char[1];
            if(reader.BaseStream.Position != index)     //resync our filereader in case we've jumped vectorcoords or stuff, we don't want to have in ram
                reader.BaseStream.Seek(index, SeekOrigin.Begin);
            beyti = (byte)reader.BaseStream.ReadByte(); //ReadByte returns int but! byte casted to int or -1 if EndOfStream/-File
            //reader.Read(charr, Convert.ToInt32(index), 1);
            //reader.BaseStream = new BufferedStream(reader.BaseStream);
            //reader = new BinaryReader(new BufferedStream(reader.BaseStream));
            //beyti = (byte)reader.Read();
            index++;
            return beyti;
            //return (byte)charr[0];
        }
    }
}
