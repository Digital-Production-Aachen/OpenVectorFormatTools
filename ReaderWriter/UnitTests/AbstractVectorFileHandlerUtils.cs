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

﻿using Google.Protobuf;
using Google.Protobuf.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    internal static class AbstractVectorFileHandlerUtils
    {
        public static List<string> NonEqualFieldsDebug(IMessage msg1, IMessage msg2)
        {
            var result = new List<string>();
            var fields2 = msg2.Descriptor.Fields.InDeclarationOrder().ToHashSet();
            foreach (var field in msg1.Descriptor.Fields.InDeclarationOrder())
            {
                if (fields2.TryGetValue(field, out var field2))
                {
                    var val1 = field.Accessor.GetValue(msg1);
                    var val2 = field.Accessor.GetValue(msg2);

                    //if (val1?.Equals(val2) == false)
                    //{
                    //    Debug.WriteLine($"{field.Name}: {val1} | {val2}");
                    //}

                    if (field.FieldType == FieldType.Message && val1 != null && val2 != null)
                    {
                        if (field.IsMap)
                        {
                            var dict1 = val1 as IDictionary;
                            var dict2 = val2 as IDictionary;

                            var count = Math.Min(dict1.Count, dict2.Count);
                            if (dict1.Count != dict2.Count) result.Add("Count not equal of lists: " + field.Name);

                            foreach (var item in dict1.Keys)
                            {
                                if (dict2.Contains(item))
                                    result.AddRange(NonEqualFieldsDebug((IMessage)dict1[item], (IMessage)dict2[item]));
                                else
                                    result.Add("Missing key in map: " + field.Name);
                            }

                        }
                        else if (field.IsRepeated)
                        {
                            var list1 = val1 as IList;
                            var list2 = val2 as IList;

                            var count = Math.Min(list1.Count, list2.Count);
                            if (list1.Count != list2.Count) result.Add("Count not equal of lists: " + field.Name);
                            for (int i = 0; i < count; i++)
                            {
                                result.AddRange(NonEqualFieldsDebug((IMessage)list1[i], (IMessage)list2[i]));
                            }
                        }
                        else
                        {
                            result.AddRange(NonEqualFieldsDebug((IMessage)val1, (IMessage)val2));
                        }
                    }
                    else if (val1?.Equals(val2) == false)
                    {
                        result.Add($"{field.Name}: {val1} | {val2}");
                    }
                }

            }
            return result;
        }
        public static void RoundDoubles(this IMessage msg, int digitsPrecision)
        {
            if (msg == null) return;
            foreach (var field in msg.Descriptor.Fields.InDeclarationOrder())
            {
                if (field.FieldType == FieldType.Message)
                {
                    RoundDoubles((IMessage)field.Accessor.GetValue(msg), digitsPrecision);
                }
                else if (field.FieldType == FieldType.Double)
                {
                    if (field.IsRepeated)
                    {
                        var list = (System.Collections.IList)field.Accessor.GetValue(msg);
                        for (int i = 0; i < list.Count; i++)
                        {
                            list[i] = Math.Round((double)list[i], digitsPrecision, MidpointRounding.AwayFromZero);
                        }
                    }
                    else
                    {
                        field.Accessor.SetValue(msg, Math.Round((double)field.Accessor.GetValue(msg), digitsPrecision, MidpointRounding.AwayFromZero));
                    }
                }
            }
        }
    }
}
