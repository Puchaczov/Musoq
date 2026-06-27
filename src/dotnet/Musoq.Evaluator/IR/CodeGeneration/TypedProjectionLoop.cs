using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal sealed record TypedProjectionLoop(
    ExecutionExpression SourceRows,
    ExecutionVariable Source,
    ExecutionExpression? Predicate,
    ExecutionAppendRow AppendRow,
    bool CanUseParallel,
    int Threshold,
    int MaxDegreeOfParallelism,
    ExecutionParallelFilterProjectLoop? OptionalProjectorLoop = null);
