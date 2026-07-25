using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionParallelSingleKeyAggregateLoop(
    ExecutionVariable Source,
    ExecutionExpression SourceRows,
    ExecutionExpression Key,
    string KeyName,
    ExecutionTypeRef KeyType,
    ExecutionVariable RootGroup,
    ExecutionVariable GroupsToFinalize,
    ExecutionVariable Group,
    ExecutionBlock AggregateBody,
    AggregateGroupShape GroupShape,
    int Threshold,
    int CardinalitySampleSize,
    int MaxDistinctSample,
    int MaxDegreeOfParallelism) : ExecutionNode
{
    internal ExecutionParallelSingleKeyAggregateLoop(
        ExecutionVariable source,
        ExecutionExpression sourceRows,
        ExecutionExpression key,
        string keyName,
        Type keyType,
        ExecutionVariable rootGroup,
        ExecutionVariable groupsToFinalize,
        ExecutionVariable group,
        ExecutionBlock aggregateBody,
        AggregateGroupShape groupShape,
        int threshold,
        int cardinalitySampleSize,
        int maxDistinctSample,
        int maxDegreeOfParallelism)
        : this(
            source,
            sourceRows,
            key,
            keyName,
            ExecutionClrBindingFactory.FromClr(keyType),
            rootGroup,
            groupsToFinalize,
            group,
            aggregateBody,
            groupShape,
            threshold,
            cardinalitySampleSize,
            maxDistinctSample,
            maxDegreeOfParallelism)
    {
    }
}
