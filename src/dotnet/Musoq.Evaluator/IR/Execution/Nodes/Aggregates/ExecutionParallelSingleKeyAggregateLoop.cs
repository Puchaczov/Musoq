using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionParallelSingleKeyAggregateLoop(
    ExecutionSourceLoop SequentialLoop,
    ExecutionVariable Source,
    ExecutionExpression SourceRows,
    ExecutionExpression Key,
    string KeyName,
    Type KeyType,
    ExecutionVariable RootGroup,
    ExecutionVariable GroupsToFinalize,
    ExecutionVariable Group,
    ExecutionBlock AggregateBody,
    AggregateGroupShape GroupShape,
    int Threshold,
    int CardinalitySampleSize,
    int MaxDistinctSample,
    int MaxDegreeOfParallelism) : ExecutionNode;
