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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat.Plausibility.Exceptions;

namespace OpenVectorFormat.Plausibility.UnitTests
{
    [TestClass]
    public class PlausibilityCheckerUnitTest
    {
        [TestMethod]
        public async System.Threading.Tasks.Task TestAll()
        {
            Job origTestJob = SetupSquareTest(10, 10);

            CheckerConfig config = new CheckerConfig
            {
                CheckLineSequencesClosed = CheckAction.CHECKERROR,
                CheckMarkingParamsKeys = CheckAction.CHECKERROR,
                CheckPartKeys = CheckAction.CHECKERROR,
                CheckPatchKeys = CheckAction.CHECKERROR,
                CheckVectorBlocksNonEmpty = CheckAction.CHECKERROR,
                CheckWorkPlanesNonEmpty = CheckAction.CHECKERROR,

                ErrorHandling = ErrorHandlingMode.THROWEXCEPTION
            };

            CheckerResult checkResult = await PlausibilityChecker.CheckJob(origTestJob, config);
            Assert.AreEqual(OverallResult.ALLSUCCEDED, checkResult.Result);
            Assert.AreEqual(0, checkResult.Errors.Count);
            Assert.AreEqual(0, checkResult.Warnings.Count);

            Job modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].NumBlocks = 15;
            await Assert.ThrowsExceptionAsync<IncoherentNumberOfVectorBlocksException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.NumWorkPlanes = 0;
            await Assert.ThrowsExceptionAsync<IncoherentNumberOfWorkPlanesException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].WorkPlaneNumber = 10;
            await Assert.ThrowsExceptionAsync<IncoherentWorkPlaneNumberingException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].VectorBlocks[0].LineSequence.Points[0] = 0;
            await Assert.ThrowsExceptionAsync<LineSequenceNotClosedException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].VectorBlocks[0].MarkingParamsKey = 10;
            await Assert.ThrowsExceptionAsync<MarkingParamsKeyNotFoundException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].VectorBlocks[0].MetaData.PartKey = 10;
            await Assert.ThrowsExceptionAsync<PartKeyNotFoundException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].VectorBlocks[0].MetaData.PatchKey = 10;
            await Assert.ThrowsExceptionAsync<PatchKeyNotFoundException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].VectorBlocks[0].LineSequence.Points.Clear();
            await Assert.ThrowsExceptionAsync<VectorBlockEmptyException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].VectorBlocks[0].ClearVectorData();
            await Assert.ThrowsExceptionAsync<VectorBlockEmptyException>(async () => await PlausibilityChecker.CheckJob(modJob, config));

            modJob = origTestJob.Clone();
            modJob.WorkPlanes[0].NumBlocks = 0;
            await Assert.ThrowsExceptionAsync<WorkPlaneEmptyException>(async () => await PlausibilityChecker.CheckJob(modJob, config));
        }

        private static Job SetupSquareTest(float x_length, float y_length)
        {
            int numWorkPlanes = 1;
            int numVBperWP = 1;
            Job job = new Job
            {
                NumWorkPlanes = numWorkPlanes
            };

            int markingParamsKey = 2;
            int partKey = 3;
            int patchKey = 4;

            job.MarkingParamsMap.Add(markingParamsKey, new MarkingParams());
            job.PartsMap.Add(partKey, new Part());

            for (int i = 0; i < numWorkPlanes; i++)
            {
                WorkPlane workPlane = new WorkPlane
                {
                    NumBlocks = numVBperWP,
                    WorkPlaneNumber = i,
                    MetaData = new WorkPlane.Types.WorkPlaneMetaData(),
                };
                workPlane.MetaData.PatchesMap.Add(patchKey, new WorkPlane.Types.Patch());
                job.WorkPlanes.Add(workPlane);

                for (int j = 0; j < numVBperWP; j++)
                {
                    VectorBlock block = new VectorBlock
                    {
                        MetaData = new VectorBlock.Types.VectorBlockMetaData(),
                        LineSequence = new VectorBlock.Types.LineSequence(),
                        MarkingParamsKey = markingParamsKey,
                    };

                    block.MetaData.PartKey = partKey;
                    block.MetaData.PatchKey = patchKey;

                    // lower left
                    block.LineSequence.Points.Add(-x_length / 2);
                    block.LineSequence.Points.Add(-y_length / 2);

                    // upper left
                    block.LineSequence.Points.Add(-x_length / 2);
                    block.LineSequence.Points.Add(y_length / 2);

                    // upper right
                    block.LineSequence.Points.Add(x_length / 2);
                    block.LineSequence.Points.Add(y_length / 2);

                    // lower right
                    block.LineSequence.Points.Add(x_length / 2);
                    block.LineSequence.Points.Add(-y_length / 2);

                    // back to lower left
                    block.LineSequence.Points.Add(-x_length / 2);
                    block.LineSequence.Points.Add(-y_length / 2);

                    job.WorkPlanes[i].VectorBlocks.Add(block);
                }
            }
            return job;
        }
    }
}
