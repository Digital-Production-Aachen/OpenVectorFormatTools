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

﻿using System;
using System.Collections.Generic;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    static class ASPHelperUtils
    {
        /// <summary>
        /// ASP does not support 2D data. Everything gets converted to 3D when writing. Thus, comparisions with 2D data will fail.
        /// ASP has no way to store Metadata, thus metadata is lost when writing.
        /// This function converts the VectorData in the original job to 3D and copies the metadata from the original to the target job, so that comparision of the jobs is possible.
        /// </summary>
        internal static Job HandleJobCompareWithASPTarget(Job originalJob, Job convertedJob)
        {
            // Console.WriteLine("ASP Compare Handler start");
            List<VectorBlock.VectorDataOneofCase> cases_2d = new List<VectorBlock.VectorDataOneofCase>() { VectorBlock.VectorDataOneofCase.Hatches, VectorBlock.VectorDataOneofCase.LineSequence, VectorBlock.VectorDataOneofCase.PointSequence };
            for (int i_workPlane = 0; i_workPlane < originalJob.NumWorkPlanes; i_workPlane++)
            {
                for (int i_vb = 0; i_vb < originalJob.WorkPlanes[i_workPlane].NumBlocks; i_vb++)
                {
                    // Console.WriteLine("workplane " + i_workPlane.ToString());
                    // Console.WriteLine("block" + i_vb.ToString());
 
                    if (cases_2d.Contains(originalJob.WorkPlanes[i_workPlane].VectorBlocks[i_vb].VectorDataCase))
                    {
                        VectorBlock oldVB = originalJob.WorkPlanes[i_workPlane].VectorBlocks[i_vb];
                        VectorBlock newVB = new VectorBlock();
                        newVB.LaserIndex = oldVB.LaserIndex;
                        newVB.LpbfMetadata = oldVB.LpbfMetadata?.Clone();
                        newVB.MarkingParamsKey = oldVB.MarkingParamsKey;
                        newVB.MetaData = oldVB.MetaData?.Clone();
                        newVB.MicroStructuringMetadata = oldVB.MicroStructuringMetadata?.Clone();
                        newVB.PolishingMetadata = oldVB.PolishingMetadata?.Clone();
                        newVB.Repeats = oldVB.Repeats;

                        switch (originalJob.WorkPlanes[i_workPlane].VectorBlocks[i_vb].VectorDataCase)
                        {
                            case VectorBlock.VectorDataOneofCase.Hatches:
                                newVB.Hatches3D = new VectorBlock.Types.Hatches3D();
                                for (int i = 0; i < oldVB.Hatches.Points.Count; i++)
                                {
                                    newVB.Hatches3D.Points.Add(oldVB.Hatches.Points[i]);
                                    if ((i + 1) % 2 == 0)
                                    {
                                        newVB.Hatches3D.Points.Add(0);
                                    }
                                }
                                break;
                            case VectorBlock.VectorDataOneofCase.LineSequence:
                                newVB.LineSequence3D = new VectorBlock.Types.LineSequence3D();
                                for (int i = 0; i < oldVB.LineSequence.Points.Count; i++)
                                {
                                    newVB.LineSequence3D.Points.Add(oldVB.LineSequence.Points[i]);
                                    if ((i + 1) % 2 == 0)
                                    {
                                        newVB.LineSequence3D.Points.Add(0);
                                    }
                                }
                                break;
                            case VectorBlock.VectorDataOneofCase.PointSequence:
                                newVB.PointSequence3D = new VectorBlock.Types.PointSequence3D();
                                for (int i = 0; i < oldVB.PointSequence.Points.Count; i++)
                                {
                                    newVB.PointSequence3D.Points.Add(oldVB.PointSequence.Points[i]);
                                    if ((i + 1) % 2 == 0)
                                    {
                                        newVB.PointSequence3D.Points.Add(0);
                                    }
                                }
                                break;
                            default:
                                throw new Exception("Unkown error!");
                        }
                        convertedJob.WorkPlanes[i_workPlane].VectorBlocks[i_vb] = newVB;
                    }
                }
            }
            convertedJob.JobMetaData = originalJob.JobMetaData;
            // Console.WriteLine("ASP Compare Handler end");
            return convertedJob;
        }
    }
}
