namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionConstantCapacityHintCandidate(
    ExecutionVariable Target,
    int Capacity) : ExecutionCapacityHint;
