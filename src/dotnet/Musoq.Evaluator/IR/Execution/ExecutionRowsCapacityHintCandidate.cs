namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionRowsCapacityHintCandidate(
    ExecutionVariable Target,
    ExecutionExpression Rows) : ExecutionCapacityHint;
