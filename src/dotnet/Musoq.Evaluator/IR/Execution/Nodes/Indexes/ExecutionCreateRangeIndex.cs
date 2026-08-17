using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateRangeIndex : ExecutionNode
{
    public ExecutionCreateRangeIndex(
        ExecutionVariable Index,
        ExecutionVariable Candidate,
        ExecutionExpression Candidates,
        ExecutionExpression CandidateKey,
        ExecutionTypeRef KeyType,
        BinaryOpKind ComparisonKind,
        IReadOnlyList<ExecutionAsOfEqualityKey>? PartitionKeys = null,
        ExecutionTypeRef? PartitionKeyType = null)
    {
        this.Index = Index;
        this.Candidate = Candidate;
        this.Candidates = Candidates;
        this.CandidateKey = CandidateKey;
        this.KeyType = KeyType;
        this.ComparisonKind = ComparisonKind;
        this.PartitionKeys = PartitionKeys == null ? null : ExecutionIrCollections.Freeze(PartitionKeys);
        this.PartitionKeyType = PartitionKeyType;
    }

    public ExecutionVariable Index { get; init; }
    public ExecutionVariable Candidate { get; init; }
    public ExecutionExpression Candidates { get; init; }
    public ExecutionExpression CandidateKey { get; init; }
    public ExecutionTypeRef KeyType { get; init; }
    public BinaryOpKind ComparisonKind { get; init; }
    public IReadOnlyList<ExecutionAsOfEqualityKey>? PartitionKeys { get; init; }
    public ExecutionTypeRef? PartitionKeyType { get; init; }

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
            ExecutionClrBindingFactory.FromClr(keyType),
            comparisonKind,
            partitionKeys,
            partitionKeyType == null ? null : ExecutionClrBindingFactory.FromClr(partitionKeyType))
    {
    }
}
