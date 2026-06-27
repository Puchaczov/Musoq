using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionContextSegment(
    ExecutionContextSegmentKind Kind,
    ExecutionExpression Value,
    int Count);
