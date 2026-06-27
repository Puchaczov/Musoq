namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionTakeCapacityHintCandidate(
    ExecutionVariable Target,
    ExecutionVariable Collection,
    int Count) : ExecutionCapacityHint;
