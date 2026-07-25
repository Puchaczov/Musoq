using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.PostOperations;

internal abstract record PostOperation;

internal sealed record PostOperationProjection(
    ProjectedField[] MaterializedFields,
    GeneratedRowShape WorkingShape,
    ExecutionVariable WorkingTable,
    IReadOnlyList<PostOperation> PostOperations,
    TableProjection? FinalProjection,
    IReadOnlyList<GeneratedRowShape> Shapes);

internal sealed record FinalJoinProjectionRewrite(
    PhysicalProjectNode Project,
    PhysicalFilterNode? Filter);

internal sealed record TableProjection(
    ExecutionVariable Table,
    GeneratedRowShape RowShape,
    IReadOnlyList<int> FieldIndexes);

internal sealed record StreamingSlice(
    int SkipCount,
    int? TakeCount,
    ExecutionVariable? SkipRemaining,
    ExecutionVariable? TakeRemaining);

internal sealed record SortOperation(
    OrderField[] Keys,
    IReadOnlyList<ProjectedField> ProjectedFields) : PostOperation
{
    public SortOperation(OrderField[] keys)
        : this(keys, [])
    {
    }
}

internal sealed record TopNOperation(
    int Count,
    OrderField[] Keys,
    IReadOnlyList<ProjectedField> ProjectedFields) : PostOperation
{
    public TopNOperation(int count, OrderField[] keys)
        : this(count, keys, [])
    {
    }
}

internal sealed record TopOffsetOperation(
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

internal sealed record SkipOperation(int Count) : PostOperation;

internal sealed record TakeOperation(int Count) : PostOperation;

internal sealed record SliceOperation(int SkipCount, int TakeCount) : PostOperation;
