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

﻿using OpenVectorFormat.ILTFileReader.Controller;
using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenVectorFormat.ILTFileReader.Model
{
    public class Hatches : VectorBlock, ILTFileReader.IHatches
    {
        private bool isASCII = false;
        private float[] coordinates;

        public Hatches(long offsetInFile, int id, int n, bool isLong, BinaryReader reader)
        {
            OffsetInFile = offsetInFile;
            Id = id;
            N = n;
            IsLong = isLong;
            this.reader = reader;
        }

        public Hatches(int id, int n, float[] coordinates)
        {
            Id = id;
            N = n;
            isASCII = true;
            this.coordinates = coordinates;
        }


        override public Span<float> Coordinates
        {
            get
            {
                if (isASCII)
                    return coordinates;
                else if (IsLong)
                    return readHelper.readFloatArray(reader, OffsetInFile, N * 4 * 4);
                else
                    return readHelper.readFloatArrayFromUshorts(reader, OffsetInFile, N * 4 * 2);
            }

        }

        public IList<ILTFileReader.IHatch> hatches
        {
            get {
                IList<IHatch> ret = new List<IHatch>();
                for (int i = 0; i < this.Coordinates.Length; i+=4) // Jumping for at this post because we assume in the array there are following x1start,y1start,x1end,y1end,...
                {
                    ret.Add(new Hatch(new Point2D(Coordinates[i], Coordinates[i + 1]), new Point2D(Coordinates[i+2], Coordinates[i + 3])));
                }
                return ret;
            }
        }

    }
}
