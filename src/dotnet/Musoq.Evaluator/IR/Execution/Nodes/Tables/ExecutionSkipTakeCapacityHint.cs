namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSkipTakeCapacityHint(
    ExecutionVariable Collection,
    int SkipCount,
    int TakeCount) : ExecutionCapacityHint;
