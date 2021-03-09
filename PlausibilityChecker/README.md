# OpenVectorFormat PlausibilityChecker

This tool checks various aspects of OpenVectorFormat Job to ensure there are no consistency errors.

Currently only available as a C# libary.

## Features
* Comprehensive check for consistency and plausibility of a OpenVectorFormat job
* Highly configurable
  * Optional checks can be enabled / disabled at will
  * Classification of a check failing as Warning or Error can be adjusted
  * Error Handling can be adjusted: A check failing to Error can either immediately trigger an exception, or all checks can be completed to get a comprehensive report of all warnings and errors in the end.

Various check functions available:
* `CheckJob(job)`: Expects a complete job with all geometry data, and performs all of the following checks
* `CheckWorkPlanes(job)`. Checks for
  * Correct number of WorkPlanes (`job.NumWorkPlanes == job.WorkPlanes.Count`).
  * Correct WorkPlaneNumber in each Workplane (`job.WorkPlanes[i].WorkPlaneNumber == i`).
  * (optional: CheckWorkPlanesNonEmpty): Checks if `WorkPlane.NumBlocks > 0`.
* `CheckNumberOfVectorBlocks(workPlane)`: Checks if `worlPlane.NumBlocks == worlPlane.VectorBlocks.Count`
* `CheckVectorBlock(vectorBlock, optional: job, optional: workPlane)`. Checks for
  * (optional: CheckVectorBlocksNonEmpty): Checks if the VectorBlock contains VectorData.
  * (optional: CheckLineSequencesClosed): for VectorData Types `LineSequence` and `LineSequence3D`, the first and last point need to be equal.
  * (optional: CheckMarkingParamsKeys): Checks if key set in `VectorBlock.MarkingParamsKey` is present in `Job.MarkingParamsMap`.
  * (optional: CheckPartKeys): Checks if key set in `VectorBlock.VectorBlockMetaData.PartKey` is present in `Job.PartsMap`. 
  * (optional: CheckPatchKeys): Checks if key set in `VectorBlock.VectorBlockMetaData.PatchKey` is present in `WorkPlane.WorkPlaneMetaData.PatchesMap`.

## How to use?

```c#
using OpenVectorFormat.Plausibility

// ... fill test job / read from file

// Configure which option tests to execute, how to handle failing of a check, and how to handle errors
CheckerConfig config = new CheckerConfig
{
    CheckLineSequencesClosed = CheckAction.CHECKERROR,
    CheckMarkingParamsKeys = CheckAction.CHECKWARNING,
    CheckPartKeys = CheckAction.DONTCHECK,
    CheckPatchKeys = CheckAction.CHECKERROR,
    CheckVectorBlocksNonEmpty = CheckAction.CHECKWARNING,
    CheckWorkPlanesNonEmpty = CheckAction.DONTCHECK,

    ErrorHandling = ErrorHandlingMode.LOGANDCONTINUE
};

// execute a test function
CheckerResult checkResult = await PlausibilityChecker.CheckJob(testJob, config);

// evaluate the results
if (checkResult.ALLSUCCEDED) 
{
    // everything is good, carry on
}
else if (checkResult.Warnings.Count > 0)
{
    // there are warnings, handle them however you like
}
else if (checkResult.Errors.Count > 0)
{
    // there are warnings, handle them however you like
}
```

