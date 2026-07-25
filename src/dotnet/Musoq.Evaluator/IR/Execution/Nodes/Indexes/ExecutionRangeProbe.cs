using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRangeProbe : ExecutionNode
{
    public ExecutionRangeProbe(
        ExecutionVariable Match,
        ExecutionVariable Index,
        ExecutionExpression ProbeKey,
        ExecutionTypeRef KeyType,
        ExecutionBlock Body,
        ExecutionBlock? NoMatchBody = null,
        ExecutionVariable? MatchFound = null,
        IReadOnlyList<ExecutionAsOfEqualityKey>? PartitionKeys = null,
        ExecutionTypeRef? PartitionKeyType = null)
    {
        this.Match = Match;
        this.Index = Index;
        this.ProbeKey = ProbeKey;
        this.KeyType = KeyType;
        this.Body = Body;
        this.NoMatchBody = NoMatchBody;
        this.MatchFound = MatchFound;
        this.PartitionKeys = PartitionKeys == null ? null : ExecutionIrCollections.Freeze(PartitionKeys);
        this.PartitionKeyType = PartitionKeyType;
    }

    public ExecutionVariable Match { get; init; }
    public ExecutionVariable Index { get; init; }
    public ExecutionExpression ProbeKey { get; init; }
    public ExecutionTypeRef KeyType { get; init; }
    public ExecutionBlock Body { get; init; }
    public ExecutionBlock? NoMatchBody { get; init; }
    public ExecutionVariable? MatchFound { get; init; }
    public IReadOnlyList<ExecutionAsOfEqualityKey>? PartitionKeys { get; init; }
    public ExecutionTypeRef? PartitionKeyType { get; init; }

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
            ExecutionClrBindingFactory.FromClr(keyType),
            body,
            noMatchBody,
            matchFound,
            partitionKeys,
            partitionKeyType == null ? null : ExecutionClrBindingFactory.FromClr(partitionKeyType))
    {
    }
}
