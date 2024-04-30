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

﻿using System;
using System.Threading.Tasks;
using OpenVectorFormat.Plausibility.Exceptions;

namespace OpenVectorFormat.Plausibility
{
    public static class PlausibilityChecker
    {
        /// <summary>
        /// Performes plausibility test on the provided job with the provided config.
        /// </summary>
        /// <param name="job">Job to check. A complete job including VectorData in the VectorBlocks is expected.</param>
        /// <param name="config">Config to use.</param>
        /// <returns>Result of checks.</returns>
        public static async Task<CheckerResult> CheckJob(Job job, CheckerConfig config)
        {
            CheckerResult result = new CheckerResult();

            await Task.Run(() =>
            {
                result = UnifyResult(result, CheckWorkPlanes(job, config));

                for (int i = 0; i < job.NumWorkPlanes; i++)
                {
                    WorkPlane wp = job.WorkPlanes[i];
                    result = UnifyResult(result, CheckNumberOfVectorBlocks(wp, config));

                    for (int j = 0; j < wp.VectorBlocks.Count; j++)
                    {
                        result = UnifyResult(result, CheckVectorBlock(wp.VectorBlocks[j], config, job, wp, j));
                    }
                }
            }
            );

            result.Result = GetResultMode(result);
            return result;
        }

        /// <summary>
        /// Performs checks related to WorkPlane properties.
        /// Optional checks are only performed if enabled in the config.
        /// Checks are:
        /// * Correct number of WorkPlanes (job.NumWorkPlanes == job.WorkPlanes.Count).
        /// * Correct WorkPlaneNumber in each Workplane (job.WorkPlanes[i].WorkPlaneNumber == i).
        /// * (optional: CheckWorkPlanesNonEmpty): Checks if WorkPlane.NumBlocks > 0.
        /// </summary>
        /// <param name="job">Job object with all WorkPlaneShells loaded (VectorData is not required)</param>
        /// <param name="config">Configuration to enable / disable checks and control error handling behaviour.</param>
        /// <returns>Result of the check.</returns>
        public static CheckerResult CheckWorkPlanes(Job job, CheckerConfig config)
        {
            CheckerResult result = new CheckerResult();
            if (job.NumWorkPlanes != job.WorkPlanes.Count)
            {
                IncoherentNumberOfWorkPlanesException exception = new IncoherentNumberOfWorkPlanesException(job.NumWorkPlanes, job.WorkPlanes.Count);
                if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                else { throw new Exception("Unknown Error Handling Mode."); }
            }

            for (int i = 0; i < job.WorkPlanes.Count; i++)
            {
                if (job.WorkPlanes[i].WorkPlaneNumber != i)
                {
                    IncoherentWorkPlaneNumberingException exception = new IncoherentWorkPlaneNumberingException(i, job.WorkPlanes[i].WorkPlaneNumber);

                    if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                    else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                    else { throw new Exception("Unknown Error Handling Mode."); }
                }

                if (config.CheckWorkPlanesNonEmpty != CheckAction.DONTCHECK && job.WorkPlanes[i].NumBlocks < 1)
                {
                    WorkPlaneEmptyException exception = new WorkPlaneEmptyException(i);

                    if (config.CheckWorkPlanesNonEmpty == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckWorkPlanesNonEmpty == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }
            }

            result.Result = GetResultMode(result);
            return result;
        }

        /// <summary>
        /// Checks if worlPlane.NumBlocks == worlPlane.VectorBlocks.Count.
        /// CAUTION! For this check, all VectorBlocks need to be included in the WorkPlane, a WorkPlaneShell is not sufficent, this may cause memory problems for big jobs.
        /// </summary>
        /// <param name="worlPlane">Job object with all WorkPlaneShells loaded (VectorData is not required)</param>
        /// <param name="config">Configuration to control error handling behaviour.</param>
        /// <returns>Result of the check.</returns>
        public static CheckerResult CheckNumberOfVectorBlocks(WorkPlane worlPlane, CheckerConfig config)
        {
            CheckerResult result = new CheckerResult();
            if (worlPlane.NumBlocks != worlPlane.VectorBlocks.Count)
            {
                IncoherentNumberOfVectorBlocksException exception = new IncoherentNumberOfVectorBlocksException(worlPlane.NumBlocks, worlPlane.VectorBlocks.Count);
                if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                else { throw new Exception("Unknown Error Handling Mode."); }
            }
            return result;
        }

        /// <summary>
        /// Performs checks related to VectorBlock properties.
        /// Optional checks are only performed if enabled in the config.
        /// * (optional: CheckVectorBlocksNonEmpty): Checks if the VectorBlock contains VectorData.
        /// * (optional: CheckLineSequencesClosed): for VectorData Types <see cref="VectorBlock.Types.LineSequence"/> and <see cref="VectorBlock.Types.LineSequence3D"/>, the first and last point need to be equal (e.g. the first two points of the <see cref="VectorBlock.Types.LineSequence.Points"/> list need to match the last two).
        /// * (optional: CheckMarkingParamsKeys): Checks if key set in <see cref="VectorBlock.MarkingParamsKey"/> is present in <see cref="Job.MarkingParamsMap"/>. Requires jobShell to be provided.
        /// * (optional: CheckPartKeys): Checks if key set in <see cref="VectorBlock.Types.VectorBlockMetaData.PartKey"/> is present in <see cref="Job.PartsMap"/>. Requires jobShell to be provided.
        /// * (optional: CheckPatchKeys): Checks if key set in <see cref="VectorBlock.Types.VectorBlockMetaData.PatchKey"/> is present in <see cref="WorkPlane.Types.WorkPlaneMetaData.PatchesMap"/>. Requires workPlaneShell to be provided.
        /// </summary>
        /// <param name="vectorBlock"><see cref="VectorBlock"/> to be checked.</param>
        /// <param name="config">Configuration for check.</param>
        /// <param name="jobShell"><see cref="Job"/> containing the <see cref="VectorBlock"/>. Only required for the checks <see cref="CheckerConfig.CheckMarkingParamsKeys"/> or <see cref="CheckerConfig.CheckPartKeys"/>. <see cref="Job.WorkPlanes"/> may be empty (jobShell configuration).</param>
        /// <param name="workPlaneShell"><see cref="WorkPlane"/> containing the <see cref="VectorBlock"/>. Only required for the check <see cref="CheckerConfig.CheckPatchKeys"/>. <see cref="WorkPlane.VectorBlocks"/> may be empty (workPlaneShell configuration).</param>
        /// <param name="vectorBlockNumber">Number of the vectorBlock. Optional, will be included in exceptions if a check fails. Usefull to know in which <see cref="VectorBlock"/> the check failed when testing a lot of blocks.</param>
        /// <returns>Result of the check.</returns>
        public static CheckerResult CheckVectorBlock(VectorBlock vectorBlock, CheckerConfig config, Job jobShell = null, WorkPlane workPlaneShell = null, int vectorBlockNumber = 0)
        {
            CheckerResult result = new CheckerResult();

            void BlockIsEmpty()
            {
                VectorBlockEmptyException exception = new VectorBlockEmptyException(workPlaneShell.WorkPlaneNumber, vectorBlockNumber);

                if (config.CheckVectorBlocksNonEmpty == CheckAction.CHECKERROR)
                {
                    if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                    else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                    else { throw new Exception("Unknown Error Handling Mode."); }
                }
                else if (config.CheckVectorBlocksNonEmpty == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                else { throw new Exception("Unknown CheckAction Mode."); }
            }

            if (config.CheckVectorBlocksNonEmpty != CheckAction.DONTCHECK)
            {
                switch (vectorBlock.VectorDataCase)
                {
                    case VectorBlock.VectorDataOneofCase.None:
                        BlockIsEmpty();
                        break;
                    case VectorBlock.VectorDataOneofCase.Arcs:
                        if (vectorBlock.Arcs.Centers.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.Arcs3D:
                        if (vectorBlock.Arcs3D.Centers.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.Ellipses:
                        if (vectorBlock.Ellipses.EllipsesArcs.Centers.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.Hatches:
                        if (vectorBlock.Hatches.Points.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.Hatches3D:
                        if (vectorBlock.Hatches3D.Points.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.LineSequence:
                        if (vectorBlock.LineSequence.Points.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.LineSequence3D:
                        if (vectorBlock.LineSequence3D.Points.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.LineSequenceParaAdapt:
                        if (vectorBlock.LineSequenceParaAdapt.PointsWithParas.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.PointSequence:
                        if (vectorBlock.PointSequence.Points.Count < 1) { BlockIsEmpty(); }
                        break;
                    case VectorBlock.VectorDataOneofCase.PointSequence3D:
                        if (vectorBlock.PointSequence3D.Points.Count < 1) { BlockIsEmpty(); }
                        break;
                    default:
                        throw new Exception("Unknown VectorDataCase");
                }
            }

            void LineNotClosed(Tuple<float, float, float> startPoint, Tuple<float, float, float> endPoint)
            {
                LineSequenceNotClosedException exception = new LineSequenceNotClosedException(workPlaneShell.WorkPlaneNumber, vectorBlockNumber, startPoint, endPoint);

                if (config.CheckLineSequencesClosed == CheckAction.CHECKERROR)
                {
                    if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                    else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                    else { throw new Exception("Unknown Error Handling Mode."); }
                }
                else if (config.CheckLineSequencesClosed == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                else { throw new Exception("Unknown CheckAction Mode."); }
            }

            if (config.CheckLineSequencesClosed != CheckAction.DONTCHECK)
            {
                if (vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                {
                    int lastInd = vectorBlock.LineSequence.Points.Count - 1;
                    Tuple<float, float, float> start = new Tuple<float, float, float>(vectorBlock.LineSequence.Points[0], vectorBlock.LineSequence.Points[1], 0);
                    Tuple<float, float, float> end = new Tuple<float, float, float>(vectorBlock.LineSequence.Points[lastInd - 1], vectorBlock.LineSequence.Points[lastInd], 0);
                    if (start.Item1 != end.Item1 || start.Item2 != end.Item2) { LineNotClosed(start, end); }
                }
                else if (vectorBlock.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence3D)
                {
                    int lastInd = vectorBlock.LineSequence3D.Points.Count - 1;
                    Tuple<float, float, float> start = new Tuple<float, float, float>(vectorBlock.LineSequence3D.Points[0], vectorBlock.LineSequence3D.Points[1], vectorBlock.LineSequence3D.Points[2]);
                    Tuple<float, float, float> end = new Tuple<float, float, float>(vectorBlock.LineSequence3D.Points[lastInd - 2], vectorBlock.LineSequence3D.Points[lastInd - 1], vectorBlock.LineSequence3D.Points[lastInd]);
                    if (start.Item1 != end.Item1 || start.Item2 != end.Item2 || start.Item3 != end.Item3) { LineNotClosed(start, end); }
                }
            }

            if (config.CheckMarkingParamsKeys != CheckAction.DONTCHECK)
            {
                if (jobShell == null)
                {
                    OVFPlausibilityCheckerException exception = new OVFPlausibilityCheckerException("jobShell is required to perform the check 'CheckMarkingParamsKeys'");
                    if (config.CheckMarkingParamsKeys == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckMarkingParamsKeys == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }

                else if (!jobShell.MarkingParamsMap.ContainsKey(vectorBlock.MarkingParamsKey))
                {
                    MarkingParamsKeyNotFoundException exception = new MarkingParamsKeyNotFoundException(workPlaneShell.WorkPlaneNumber, vectorBlockNumber, vectorBlock.MarkingParamsKey);

                    if (config.CheckMarkingParamsKeys == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckMarkingParamsKeys == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }
            }

            if (config.CheckPartKeys != CheckAction.DONTCHECK)
            {
                if (jobShell == null)
                {
                    OVFPlausibilityCheckerException exception = new OVFPlausibilityCheckerException("jobShell is required to perform the check 'CheckPartKeys'");
                    if (config.CheckPartKeys == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckPartKeys == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }

                else if (!jobShell.PartsMap.ContainsKey(vectorBlock.MetaData.PartKey))
                {
                    PartKeyNotFoundException exception = new PartKeyNotFoundException(workPlaneShell.WorkPlaneNumber, vectorBlockNumber, vectorBlock.MetaData.PartKey);

                    if (config.CheckPartKeys == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckPartKeys == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }
            }

            if (config.CheckPatchKeys != CheckAction.DONTCHECK)
            {
                if (workPlaneShell == null)
                {
                    OVFPlausibilityCheckerException exception = new OVFPlausibilityCheckerException("workPlaneShell is required to perform the check 'CheckPatchKeys'");
                    
                    if (config.CheckPatchKeys == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckPatchKeys == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }

                }
                else if (workPlaneShell.MetaData == null)
                {
                    OVFPlausibilityCheckerException exception = new OVFPlausibilityCheckerException("workPlaneShell.MetaData is required to perform the check 'CheckPatchKeys'");
                    
                    if (config.CheckPatchKeys == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckPatchKeys == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }
                else if (!workPlaneShell.MetaData.PatchesMap.ContainsKey(vectorBlock.MetaData.PatchKey))
                {
                    PatchKeyNotFoundException exception = new PatchKeyNotFoundException(workPlaneShell.WorkPlaneNumber, vectorBlockNumber, vectorBlock.MetaData.PatchKey);

                    if (config.CheckPatchKeys == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckPatchKeys == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }
            }

            if (config.CheckContourIndex != CheckAction.DONTCHECK)
            {
                if (workPlaneShell == null)
                {
                    OVFPlausibilityCheckerException exception = new OVFPlausibilityCheckerException("workPlaneShell is required to perform the check 'CheckContourIndex'");

                    if (config.CheckContourIndex == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckContourIndex == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }

                }
                else if (workPlaneShell.MetaData == null)
                {
                    OVFPlausibilityCheckerException exception = new OVFPlausibilityCheckerException("workPlaneShell.MetaData is required to perform the check 'CheckContourIndex'");

                    if (config.CheckContourIndex == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckContourIndex == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }
                else if (workPlaneShell.MetaData.Contours == null ||
                    workPlaneShell.MetaData.Contours.Count <= vectorBlock.MetaData.ContourIndex)
                {
                    ContourIndexNotFoundException exception = new ContourIndexNotFoundException(workPlaneShell.WorkPlaneNumber, vectorBlockNumber, vectorBlock.MetaData.ContourIndex);

                    if (config.CheckContourIndex == CheckAction.CHECKERROR)
                    {
                        if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                        else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                        else { throw new Exception("Unknown Error Handling Mode."); }
                    }
                    else if (config.CheckContourIndex == CheckAction.CHECKWARNING) { result.Warnings.Add(exception); }
                    else { throw new Exception("Unknown CheckAction Mode."); }
                }
            }

            return result;
        }

        private static OverallResult GetResultMode(CheckerResult result)
        {
            if (result.Errors.Count > 0) { return OverallResult.ERRORS; }
            else if (result.Warnings.Count > 0) { return OverallResult.WARNINGS; }
            else { return OverallResult.ALLSUCCEDED; }
        }

        private static CheckerResult UnifyResult(CheckerResult first, CheckerResult second)
        {
            first.Warnings.AddRange(second.Warnings);
            first.Errors.AddRange(second.Errors);
            return first;
        }
    }
}
