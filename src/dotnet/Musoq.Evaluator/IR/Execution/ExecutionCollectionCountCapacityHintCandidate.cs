namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCollectionCountCapacityHintCandidate(
    ExecutionVariable Target,
    ExecutionVariable Collection) : ExecutionCapacityHint;
