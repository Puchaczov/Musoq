namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionTakeCapacityHint(
    ExecutionVariable Collection,
    int Count) : ExecutionCapacityHint;
