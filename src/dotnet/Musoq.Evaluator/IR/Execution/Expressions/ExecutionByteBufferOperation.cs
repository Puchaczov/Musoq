using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal enum ExecutionByteBufferOperationKind
{
    BorrowArray,
    Range,
    Snapshot,
    Concatenate,
    Materialize,
}

internal enum ExecutionByteBufferMaterialization
{
    OriginalCallable,
    PlannedOperations,
}

internal enum ExecutionByteBufferViewAccess
{
    Storage,
    LogicalLength,
}

/// <summary>A planner-selected buffer operation retaining its complete callable evaluation.</summary>
public sealed record ExecutionByteBufferOperation : ExecutionExpression
{
    internal ExecutionByteBufferOperation(ExecutionExpression original, ExecutionByteBufferViewAccess access)
        : base(
            ExecutionClrBindingFactory.FromClr(
                access == ExecutionByteBufferViewAccess.LogicalLength ? typeof(int?) : typeof(byte[])
            )
        )
    {
        if (!ByteBufferViewShape.IsView(original))
            throw new ArgumentException("View access requires an already captured private field.", nameof(original));
        Original = original;
        ViewAccess = access;
        Materialization = ExecutionByteBufferMaterialization.PlannedOperations;
    }

    internal ExecutionByteBufferOperation(ExecutionExpression original, ByteBufferValuePlan plan)
        : base(original.ReturnType)
    {
        Original = original;
        Operation = plan.Operation switch
        {
            ByteBufferOperationKind.BorrowArray => ExecutionByteBufferOperationKind.BorrowArray,
            ByteBufferOperationKind.Range => ExecutionByteBufferOperationKind.Range,
            ByteBufferOperationKind.Snapshot => ExecutionByteBufferOperationKind.Snapshot,
            ByteBufferOperationKind.Concatenate => ExecutionByteBufferOperationKind.Concatenate,
            ByteBufferOperationKind.Materialize => ExecutionByteBufferOperationKind.Materialize,
            _ => throw new ArgumentOutOfRangeException(nameof(plan)),
        };
        Materialization = plan.Materialization switch
        {
            ByteBufferMaterialization.OriginalCallable => ExecutionByteBufferMaterialization.OriginalCallable,
            ByteBufferMaterialization.PlannedOperations => ExecutionByteBufferMaterialization.PlannedOperations,
            _ => throw new ArgumentOutOfRangeException(nameof(plan)),
        };
        MaximumReadBytes = plan.Requirement.MaximumReadBytes;
        ValueId = plan.Facts.ValueId;
    }

    public ExecutionExpression Original { get; init; }
    internal ExecutionByteBufferProgram? Program { get; init; }
    internal ExecutionByteBufferViewAccess? ViewAccess { get; init; }
    public int? MaximumReadBytes { get; init; }
    internal int ValueId { get; init; }
    internal ExecutionByteBufferOperationKind Operation { get; init; }
    internal ExecutionByteBufferMaterialization Materialization { get; init; }
}
