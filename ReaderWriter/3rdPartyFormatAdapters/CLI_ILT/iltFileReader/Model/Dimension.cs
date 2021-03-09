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

﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenVectorFormat.ILTFileReader.Model
{
    class Dimension:ILTFileReader.IDimension
    {

        public Dimension(float x1, float y1, float z1, float x2, float y2, float z2) {
            this.X1 = x1;
            this.Y1 = y1;
            this.Z1 = z1;
            this.X2 = x2;
            this.Y2 = y2;
            this.Z2 = z2;
        }

        public float X1
        {
            get;
            private set;
        }

        public float X2
        {
            get;
            private set;
        }

        public float Y1
        {
            get;
            private set;
        }

        public float Y2
        {
            get;
            private set;
        }

        public float Z1
        {
            get;
            private set;
        }

        public float Z2
        {
            get;
            private set;
        }
    }
}
