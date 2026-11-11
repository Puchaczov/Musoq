using System.Collections.Generic;
using System.Collections.Immutable;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal enum ByteBufferOperationKind
{
    BorrowArray,
    Range,
    Snapshot,
    Concatenate,
    Materialize
}

internal enum ByteBufferOwnership
{
    External,
    Borrowed,
    Owned
}

internal enum ByteBufferNullState
{
    Unknown,
    NonNull,
    Null
}

internal enum ByteBufferMaterialization
{
    OriginalCallable,
    PlannedOperations
}

internal enum ByteBufferProvenance
{
    OriginalArray,
    OpaqueProducer,
    Snapshot,
    Concatenation
}

internal readonly record struct ByteBufferLogicalLength(int ValueId, int? Constant);

internal sealed record ByteBufferValueFacts(
    int ValueId,
    ByteBufferOwnership Ownership,
    ByteBufferNullState NullState,
    ScalarEvaluationRegion Region,
    int MaterializationPoint)
{
    internal ByteBufferProvenance Provenance { get; init; }
    internal ByteBufferLogicalLength LogicalLength { get; init; }
}

internal sealed record ByteBufferValuePlan
{
    internal ByteBufferValuePlan(ByteBufferOperationKind operation, ByteBufferValueFacts facts,
        IEnumerable<IrExpression> arguments, ByteBufferRequirement requirement)
    {
        Operation = operation;
        Facts = facts;
        Arguments = arguments.ToImmutableArray();
        Requirement = requirement;
    }

    internal ByteBufferOperationKind Operation { get; init; }
    internal ByteBufferValueFacts Facts { get; init; }
    internal ImmutableArray<IrExpression> Arguments { get; init; }
    internal ImmutableArray<ByteBufferValuePlan?> ArgumentPlans { get; init; } = [];
    internal ByteBufferRequirement Requirement { get; init; }
    internal ByteBufferMaterialization Materialization { get; init; } = ByteBufferMaterialization.OriginalCallable;
    internal ByteBufferProgram? Program { get; init; }
}
