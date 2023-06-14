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



using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace OpenVectorFormat.Utils
{
    /// <summary>
    /// Class to hold general, usefull utilites for working with protobuf messages.
    /// </summary>
    public static class ProtoUtils
    {
        //this fieldInfo to access private fields of repeatedField<float> is only calculated once
        private static readonly FieldInfo repFieldPrivateArrayFieldInfo = typeof(RepeatedField<float>).GetField("array", BindingFlags.NonPublic | BindingFlags.Instance);

#if NETCOREAPP3_0_OR_GREATER
        private static readonly Func<RepeatedField<float>, float[]> repFieldArrayDynamicGetter = CreateGetter<RepeatedField<float>, float[]>(repFieldPrivateArrayFieldInfo);

        private static Func<S, T> CreateGetter<S, T>(FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod getterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(S) }, true);
            ILGenerator gen = getterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Func<S, T>)getterMethod.CreateDelegate(typeof(Func<S, T>));
        }
#endif

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

        /// <summary>
        /// Creates a span for the given repeated field's underlying private array.
        /// It is not safe to add or remove to and from the repeated field while using the Span,
        /// since this might allocate a new private array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repeatedField"></param>
        /// <returns></returns>
        public static Span<T> AsSpan<T>(this RepeatedField<T> repeatedField)
        {
            var arrayField = typeof(RepeatedField<T>).GetField("array", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateArray = (T[]) arrayField.GetValue(repeatedField);
            return privateArray.AsSpan().Slice(0, repeatedField.Count);
        }

        /// <summary>
        /// Creates a span for the given repeated field's underlying private array.
        /// It is not safe to add or remove to and from the repeated field while using the Span,
        /// since this might allocate a new private array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repeatedField"></param>
        /// <returns></returns>
        public static Span<float> AsSpan(this RepeatedField<float> repeatedField)
        {
#if NETCOREAPP3_0_OR_GREATER
            var privateArray = repFieldArrayDynamicGetter(repeatedField);
#else
            var privateArray = (float[]) repFieldPrivateArrayFieldInfo.GetValue(repeatedField);
#endif
            return privateArray.AsSpan().Slice(0, repeatedField.Count);
        }
    }
}