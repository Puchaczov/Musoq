namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionSkipCapacityHintCandidate(
    ExecutionVariable Target,
    ExecutionVariable Collection,
    int Count) : ExecutionCapacityHint;
