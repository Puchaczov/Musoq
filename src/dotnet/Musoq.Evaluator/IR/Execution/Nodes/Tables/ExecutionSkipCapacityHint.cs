namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSkipCapacityHint(
    ExecutionVariable Collection,
    int Count) : ExecutionCapacityHint;
