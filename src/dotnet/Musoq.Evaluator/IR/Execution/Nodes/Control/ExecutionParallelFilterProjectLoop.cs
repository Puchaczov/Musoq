using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionParallelFilterProjectLoop(
    ExecutionSourceLoop SerialLoop,
    ExecutionVariable Source,
    ExecutionExpression SourceRows,
    ExecutionExpression? Predicate,
    ExecutionAppendRow AppendRow,
    int Threshold,
    int MaxDegreeOfParallelism) : ExecutionNode;
