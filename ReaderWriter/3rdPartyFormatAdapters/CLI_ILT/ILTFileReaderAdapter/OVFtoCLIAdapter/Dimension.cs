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
using OpenVectorFormat.ILTFileReader;


namespace ILTFileReader.OVFToCLIAdapter
{
    public class Dimension : IDimension
    {
        public Dimension(float zmin, float zmax, float xmin, float xmax, float ymin, float ymax)
        {
            Z1 = zmin;
            Z2 = zmax;
            X1 = xmin;
            X2 = xmax;
            Y1 = ymin;
            Y2 = ymax;
        }
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float Y1 { get; set; }
        public float Y2 { get; set; }
        public float Z1 { get; set; }
        public float Z2 { get; set; }
    }
}
