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



using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace OpenVectorFormat.Utils
{
    /// <summary>
    /// Class to hold general, usefull utilites for working with protobuf messages.
    /// </summary>
    public static class ProtoUtils
    {
        /// <summary>
        /// Creates a copy of a protobuf message with some fields excluded.
        /// </summary>
        /// <param name="source">Source message to be copied from.</param>
        /// <param name="target">Target message to copy to.</param>
        /// <param name="excludedFieldNumbers">Protobuf fieldnumbers of the fields to exclude while copying.</param>
        public static void CopyWithExclude(IMessage source, IMessage target, IList<int> excludedFieldNumbers = null)
        {
            if(excludedFieldNumbers == null)
            {
                excludedFieldNumbers = new List<int> {};
            }
            foreach (FieldDescriptor field in source.Descriptor.Fields.InFieldNumberOrder())
            {
                if (excludedFieldNumbers.Contains(field.FieldNumber))
                {
                    continue;
                }

                if (field.IsMap)
                {
                    IDictionary sourceMap = (IDictionary)field.Accessor.GetValue(source);
                    IDictionary targetMap = (IDictionary)field.Accessor.GetValue(target);
                    foreach (DictionaryEntry entry in sourceMap)
                    {
                        targetMap.Add(entry.Key, entry.Value);
                    }
                }
                else if (field.IsRepeated)
                {
                    IList sourceList = (IList)field.Accessor.GetValue(source);
                    IList targetList = (IList)field.Accessor.GetValue(target);
                    foreach (object element in sourceList)
                    {
                        targetList.Add(element);
                    }
                }
                else if(field.FieldType == FieldType.Message)
                {
                    //embedded message, will be fully cloned (since field numbers in excludedFieldNumbers are not unique across IMessages)
                    var sourceValue = field.Accessor.GetValue(source);
                    if (sourceValue != null) {
                        //get the copy constuctor declared by all protobuf messages, and clone the embedded message with it
                        var copyCtor = sourceValue.GetType().GetConstructor(new System.Type[] { sourceValue.GetType() });
                        var embeddedMessageClone = copyCtor.Invoke(new object[] { sourceValue });
                        field.Accessor.SetValue(target, embeddedMessageClone);
                    }
                }
                else
                {
                    field.Accessor.SetValue(target, field.Accessor.GetValue(source));
                }
            }
        }
    }
}