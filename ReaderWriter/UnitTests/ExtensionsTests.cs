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

﻿using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void TestTranslateAndBounds()
        {
            Random rng = new Random();
            for (int i = 0; i < 100; i++)
            {
                var vb = GenerateRandomVectorBlock();

                vb.Translate(rng.NextSingle() * )
                Assert.IsTrue(clone.Equals(vb));
            }
        }

        [TestMethod]
        public void TestCloneVectorBlockWithoutVectorData()
        {
            for(int i = 0; i < 100; i++)
            {
                var vb = GenerateRandomVectorBlock();
                var clone = vb.CloneWithoutVectorData();
                vb.ClearVectorData();
                Assert.IsTrue(clone.Equals(vb));
            }
        }

        [TestMethod]
        public void TestCloneWorkPlaneWithoutVectorData()
        {
            for (int i = 0; i < 100; i++)
            {
                var wp = GenerateRandomWorkPlane();
                var clone = wp.CloneWithoutVectorData();
                wp.NumBlocks = 0;
                wp.VectorBlocks.Clear();
                if(wp.MetaData?.Bounds != null) wp.MetaData.Bounds = null;
                Assert.IsTrue(clone.Equals(wp));
            }
        }

        private VectorBlock GenerateRandomVectorBlock()
        {
            VectorBlock block = new VectorBlock();
            FillWithRandomDataByReflection(block);
            return block;
        }

        private WorkPlane GenerateRandomWorkPlane()
        {
            WorkPlane wp = new WorkPlane();
            FillWithRandomDataByReflection(wp);
            wp.NumBlocks = wp.VectorBlocks.Count;
            return wp;
        }

        private void FillWithRandomDataByReflection(IMessage protoMessage)
        {
            var rng = new Random();
            foreach (var fieldOneof in protoMessage.Descriptor.Fields.InFieldNumberOrder().GroupBy(f=> f.ContainingOneof == null ? f.Name : f.ContainingOneof.Name))
            {
                FieldDescriptor field;
                var fields = fieldOneof.ToArray();
                if(fields.Length == 1)
                {
                    field = fields[0];
                }
                else
                {
                    field = fields[rng.Next(fields.Length - 1)];
                }
                if (field.IsMap)
                {
                    var count = rng.Next(10);
                    IDictionary targetMap = (IDictionary)field.Accessor.GetValue(protoMessage);
                    var keyValuePairMessage = field.MessageType.Fields.InFieldNumberOrder();
                    for (int i = 0; i < count; i++)
                    {
                        var key = RNGFieldContent(keyValuePairMessage[0], rng);
                        var value = RNGFieldContent(keyValuePairMessage[1], rng);
                        targetMap.Add(key, value);
                    }
                }
                else if (field.IsRepeated)
                {
                    var count = rng.Next(100);
                    IList targetList = (IList)field.Accessor.GetValue(protoMessage);
                    for (int i = 0; i < count; i++)
                    {
                        var value = RNGFieldContent(field, rng);
                        targetList.Add(value);
                    }
                }
                else
                {
                    var value = RNGFieldContent(field, rng);
                    field.Accessor.SetValue(protoMessage, value);
                }
            }
        }

        private object RNGFieldContent(FieldDescriptor field, Random rng)
        {
            object value;
            switch (field.FieldType)
            {
                case FieldType.Double:
                    value = rng.NextDouble();
                    break;
                case FieldType.Float:
                    value = rng.NextSingle();
                    break;
                case FieldType.Int64:
                case FieldType.Fixed64:
                case FieldType.SInt64:
                case FieldType.SFixed64:
                    value = rng.NextInt64();
                    break;
                case FieldType.UInt64:
                    value = (ulong)rng.NextInt64();
                    break;
                case FieldType.Int32:
                case FieldType.Fixed32:
                case FieldType.SInt32:
                case FieldType.SFixed32:
                    value = rng.Next();
                    break;
                case FieldType.UInt32:
                    value = (uint)rng.Next();
                    break;
                case FieldType.Bool:
                    value = rng.Next(10) < 5;
                    break;
                case FieldType.String:
                    value = rng.Next().ToString();
                    break;
                case FieldType.Enum:
                    value = rng.Next(field.EnumType.Values.Count - 1);
                    break;
                case FieldType.Message:
                    value = Activator.CreateInstance(field.MessageType.ClrType);
                    FillWithRandomDataByReflection(value as IMessage);
                    break;
                default: throw new NotImplementedException();
            }
            return value;
        }
    }
}
