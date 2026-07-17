using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateRangeIndex(
    ExecutionVariable Index,
    ExecutionVariable Candidate,
    ExecutionExpression Candidates,
    ExecutionExpression CandidateKey,
    ExecutionTypeRef KeyType,
    BinaryOpKind ComparisonKind,
    IReadOnlyList<ExecutionAsOfEqualityKey>? PartitionKeys = null,
    ExecutionTypeRef? PartitionKeyType = null) : ExecutionNode
{
    internal ExecutionCreateRangeIndex(
        ExecutionVariable index,
        ExecutionVariable candidate,
        ExecutionExpression candidates,
        ExecutionExpression candidateKey,
        Type keyType,
        BinaryOpKind comparisonKind,
        IReadOnlyList<ExecutionAsOfEqualityKey>? partitionKeys = null,
        Type? partitionKeyType = null)
        : this(
            index,
            candidate,
            candidates,
            candidateKey,
            ExecutionTypeRef.FromClr(keyType),
            comparisonKind,
            partitionKeys,
            partitionKeyType == null ? null : ExecutionTypeRef.FromClr(partitionKeyType))
    {
    }
}
