using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionParallelFilterProjectLoop(
    ExecutionVariable Source,
    ExecutionExpression SourceRows,
    ExecutionExpression? Predicate,
    ExecutionAppendRow AppendRow,
    ExecutionBlock ProjectionBody,
    int Threshold,
    int MaxDegreeOfParallelism) : ExecutionNode;
