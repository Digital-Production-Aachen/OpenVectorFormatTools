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

using Google.Protobuf.Collections;
using OpenVectorFormat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OVFDefinition
{
    public static class Utils
    {
        public static bool ApproxEquals(float value1, float value2, float tolerance = 1e-6f)
        {
            return Math.Abs(value1 - value2) <= tolerance;
        }

        public static void MergeFromWithRemap(this MapField<int, MarkingParams> map, MapField<int, MarkingParams> other, out Dictionary<int, int> keyMapping)
        {
            keyMapping = new Dictionary<int, int>();
            int maxParamsKey = map.Any() ? map.Keys.Max() + 1 : 0;
            foreach (var parameterToInsert in other)
            {
                bool found = false;
                foreach (var existingParameter in map)
                {
                    if (existingParameter.Value.Equals(parameterToInsert.Value))
                    {
                        found = true;
                        keyMapping.Add(parameterToInsert.Key, existingParameter.Key);
                        break;
                    }
                }
                if (!found)
                {
                    maxParamsKey++;
                    keyMapping.Add(parameterToInsert.Key, maxParamsKey);
                    map.Add(maxParamsKey, parameterToInsert.Value);
                }
            }
        }
    }
}
