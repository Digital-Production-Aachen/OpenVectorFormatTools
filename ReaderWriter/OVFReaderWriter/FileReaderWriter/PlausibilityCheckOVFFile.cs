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

using System;
using System.IO;
using System.Threading.Tasks;
using OpenVectorFormat.Plausibility;
using OpenVectorFormat.Plausibility.Exceptions;

namespace OpenVectorFormat.OVFReaderWriter
{
    public static class PlausibilityCheckOVFFile
    {

        /// <summary>
        /// Perform a plausability check on an ovf file.
        /// </summary>
        /// <param name="filepath">Path to ovf file.</param>
        /// <param name="config"><see cref="Plausibility"/> config to use.</param>
        /// <param name="CheckNumberOfVectorBlocks">Additional setting: check if worlPlane.NumBlocks == worlPlane.VectorBlocks.Count. Requires the complete workplane to be loaded into memory. Do not use for very large workplanes.</param>
        /// <returns>Result of the checks.</returns>
        public static async Task<CheckerResult> CheckJobFile(string filepath, CheckerConfig config, CheckAction CheckNumberOfVectorBlocks = CheckAction.CHECKERROR)
        {
            if (!File.Exists(filepath)) { throw new FileNotFoundException(string.Format("File '{0}' for PlausibilityChecker not found!", filepath), filepath); }
            if (Path.GetExtension(filepath) != ".ovf") { throw new NotSupportedException("Unsupported file format. Only OpenVectorFormat files (*.ovf) are supported"); }
            OVFFileReader reader = new OVFFileReader();

            reader.OpenJob(filepath, new FileReaderWriterProgressDummy());

            CheckerResult result = new CheckerResult();

            Job jobShell = reader.JobShell;
            for (int i = 0; i < jobShell.NumWorkPlanes; i++)
            {
                try
                {
                    jobShell.WorkPlanes.Add(reader.GetWorkPlaneShell(i));
                }
                catch (IndexOutOfRangeException ex)
                {
                    IncoherentNumberOfWorkPlanesException exception = new IncoherentNumberOfWorkPlanesException(jobShell.NumWorkPlanes, i, ex);
                    if (config.ErrorHandling == ErrorHandlingMode.THROWEXCEPTION) { throw exception; }
                    else if (config.ErrorHandling == ErrorHandlingMode.LOGANDCONTINUE) { result.Errors.Add(exception); }
                }
            }

            result = UnifyResult(result, PlausibilityChecker.CheckWorkPlanes(jobShell, config));

            if (CheckNumberOfVectorBlocks != CheckAction.DONTCHECK)
            {
                for (int i = 0; i < jobShell.NumWorkPlanes; i++)
                {
                    WorkPlane wp = reader.GetWorkPlane(i);
                    result = UnifyResult(result, PlausibilityChecker.CheckNumberOfVectorBlocks(wp, config));

                    for (int j = 0; j < wp.VectorBlocks.Count; j++)
                    {
                        result = UnifyResult(result, PlausibilityChecker.CheckVectorBlock(wp.VectorBlocks[j], config, jobShell, wp, j));
                    }
                }
            }
            else
            {
                for (int i = 0; i < jobShell.NumWorkPlanes; i++)
                {
                    WorkPlane wp = reader.GetWorkPlaneShell(i);

                    for (int j = 0; j < wp.VectorBlocks.Count; j++)
                    {
                        result = UnifyResult(result, PlausibilityChecker.CheckVectorBlock(reader.GetVectorBlock(i, j), config, jobShell, wp, j));
                    }
                }
            }

            result.Result = GetResultMode(result);
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
