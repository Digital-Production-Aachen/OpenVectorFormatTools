/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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

﻿using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenVectorFormat.ILTFileReader.Model
{
    class Point2D : IPoint2D
    {

        public Point2D(float x, float y) {
            this.X = x;
            this.Y = y;
        }

        public float X
        {
            get;
            private set;

        }

        public float Y
        {
            get;
            private set;
        }

        public override bool Equals(object obj)
        {
            var d = obj as Point2D;
            return d != null &&
                   X == d.X &&
                   Y == d.Y;
        }
    }
}
