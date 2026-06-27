namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionSkipTakeCapacityHintCandidate(
    ExecutionVariable Target,
    ExecutionVariable Collection,
    int SkipCount,
    int TakeCount) : ExecutionCapacityHint;
