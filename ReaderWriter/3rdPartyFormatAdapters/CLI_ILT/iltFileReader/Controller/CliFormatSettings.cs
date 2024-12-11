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

namespace OpenVectorFormat.ILTFileReader
{
    public class CliFormatSettings
    {
        public static CliFormatSettings _instance;
        public static CliFormatSettings Instance { 
            get { 
                if (_instance == null)
                    _instance = new CliFormatSettings();
                return _instance; 
            } 
        }
        private CliFormatSettings() { }

        public enum BinaryWriteStyle
        {
            LONG,
            SHORT
        }

        public const ushort hatchStartLong = 132; //Hex 84
        public const ushort hatchStartShort = 131; //Hex 83

        public const ushort layerStartLong = 127; // Hex 7F
        public const ushort layerStartShort = 128; // Hex 80
        public const ushort polylineStartLong = 130; // Hex 82
        public const ushort polylineStartShort = 129; // Hex 81


        public BinaryWriteStyle HatchesStyle { get; set; } = BinaryWriteStyle.SHORT; //EOS => Short
        public BinaryWriteStyle LayerStyle { get; set; } = BinaryWriteStyle.LONG;
        public BinaryWriteStyle PolylineStyle { get; set; } = BinaryWriteStyle.LONG;

        public DataFormatType dataFormatType { get; set; } = DataFormatType.binary;

        public float Units { get; set; } = 1;
        bool convertUnits = false;

        public bool FormatForEOS
        {
            set
            {
                HatchesStyle = BinaryWriteStyle.SHORT;
                PolylineStyle = BinaryWriteStyle.LONG;
                LayerStyle = BinaryWriteStyle.LONG;
            }
            get
            {
                return HatchesStyle == BinaryWriteStyle.SHORT &&
                    PolylineStyle == BinaryWriteStyle.LONG &&
                    LayerStyle == BinaryWriteStyle.LONG;
            }
        }
    }
}