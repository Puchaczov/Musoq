using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private abstract record PostOperation;

    private sealed record PostOperationProjection(
        ProjectedField[] MaterializedFields,
        GeneratedRowShape WorkingShape,
        ExecutionVariable WorkingTable,
        IReadOnlyList<PostOperation> PostOperations,
        TableProjection? FinalProjection,
        IReadOnlyList<GeneratedRowShape> Shapes);

    private sealed record FinalJoinProjectionRewrite(
        PhysicalProjectNode Project,
        PhysicalFilterNode? Filter);

    private sealed record TableProjection(
        ExecutionVariable Table,
        GeneratedRowShape RowShape,
        IReadOnlyList<int> FieldIndexes);

    private sealed record StreamingSlice(
        int SkipCount,
        int? TakeCount,
        ExecutionVariable? SkipRemaining,
        ExecutionVariable? TakeRemaining);

    private sealed record SortOperation(
        OrderField[] Keys,
        IReadOnlyList<ProjectedField> ProjectedFields) : PostOperation
    {
        public SortOperation(OrderField[] keys)
            : this(keys, [])
        {
        }
    }

    private sealed record TopNOperation(
        int Count,
        OrderField[] Keys,
        IReadOnlyList<ProjectedField> ProjectedFields) : PostOperation
    {
        public TopNOperation(int count, OrderField[] keys)
            : this(count, keys, [])
        {
        }
    }

    private sealed record TopOffsetOperation(
        int SkipCount,
        int TakeCount,
        OrderField[] Keys,
        IReadOnlyList<ProjectedField> ProjectedFields) : PostOperation
    {
        public TopOffsetOperation(int skipCount, int takeCount, OrderField[] keys)
            : this(skipCount, takeCount, keys, [])
        {
        }
    }

    private sealed record SkipOperation(int Count) : PostOperation;

    private sealed record TakeOperation(int Count) : PostOperation;

    private sealed record SliceOperation(int SkipCount, int TakeCount) : PostOperation;
}
