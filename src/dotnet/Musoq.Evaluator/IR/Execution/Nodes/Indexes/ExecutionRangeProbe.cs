using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRangeProbe(
    ExecutionVariable Match,
    ExecutionVariable Index,
    ExecutionExpression ProbeKey,
    ExecutionTypeRef KeyType,
    ExecutionBlock Body,
    ExecutionBlock? NoMatchBody = null,
    ExecutionVariable? MatchFound = null,
    IReadOnlyList<ExecutionAsOfEqualityKey>? PartitionKeys = null,
    ExecutionTypeRef? PartitionKeyType = null) : ExecutionNode
{
    internal ExecutionRangeProbe(
        ExecutionVariable match,
        ExecutionVariable index,
        ExecutionExpression probeKey,
        Type keyType,
        ExecutionBlock body,
        ExecutionBlock? noMatchBody = null,
        ExecutionVariable? matchFound = null,
        IReadOnlyList<ExecutionAsOfEqualityKey>? partitionKeys = null,
        Type? partitionKeyType = null)
        : this(
            match,
            index,
            probeKey,
            ExecutionTypeRef.FromClr(keyType),
            body,
            noMatchBody,
            matchFound,
            partitionKeys,
            partitionKeyType == null ? null : ExecutionTypeRef.FromClr(partitionKeyType))
    {
    }
}
