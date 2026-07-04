using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static StreamingSlice? TryCreateStreamingSlice(
        string resultTableName,
        IReadOnlyList<PostOperation> postOperations,
        bool isDistinct,
        TableProjection? finalProjection,
        IReadOnlyList<ProjectedField> projectedFields,
        out IReadOnlyList<PostOperation> remainingPostOperations)
    {
        return PostOperationPlanner.Default.TryCreateStreamingSlice(
            resultTableName,
            postOperations,
            isDistinct,
            finalProjection,
            projectedFields,
            out remainingPostOperations);
    }

    private static ExecutionCapacityHint? CreateStreamingSliceCapacityCandidate(
        ExecutionVariable target,
        StreamingSlice? streamingSlice)
    {
        return PostOperationPlanner.Default.CreateStreamingSliceCapacityCandidate(target, streamingSlice);
    }

    private static IEnumerable<ExecutionNode> CreateStreamingSliceCounterDeclarations(StreamingSlice? streamingSlice)
    {
        return PostOperationPlanner.Default.CreateStreamingSliceCounterDeclarations(streamingSlice);
    }
}
