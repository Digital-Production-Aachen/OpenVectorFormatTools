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

﻿using System;
using System.Drawing;

namespace OVFDefinition
{
    /// <summary>
    /// Converts between System.Drawing.Color and VectorBlockMetaData.display_color
    /// as specified by open_vector_format.proto.
    /// 
    /// The OVF display color is represented by an int32, interpreted as byte[4] with
    /// byte[0] = red, byte[1] = green, byte[2] = blue, byte[3] = alpha
    /// (assuming little-endian byte order).
    /// </summary>
    public class ColorConversions
    {
        public static uint ColorToUInt(Color color)
        {
            byte[] bytes = ColorToBytes(color);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static Color UIntToColor(uint ovfColor)
        {
            byte[] bytes = BitConverter.GetBytes(ovfColor);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BytesToColor(bytes);
        }

        public static int ColorToInt(Color color)
        {
            byte[] bytes = ColorToBytes(color);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static Color IntToColor(int ovfColor)
        {
            byte[] bytes = BitConverter.GetBytes(ovfColor);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BytesToColor(bytes);
        }

        private static byte[] ColorToBytes(Color color)
        {
            byte[] bytes = new byte[4];
            bytes[0] = color.R;
            bytes[1] = color.G;
            bytes[2] = color.B;
            bytes[3] = color.A;
            return bytes;
        }

        private static Color BytesToColor(byte[] bytes)
        {
            return Color.FromArgb(alpha: bytes[3], red: bytes[0], green: bytes[1], blue: bytes[2]);
        }

    }
}
