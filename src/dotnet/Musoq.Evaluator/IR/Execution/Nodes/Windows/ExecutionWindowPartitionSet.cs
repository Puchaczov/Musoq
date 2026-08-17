namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowPartitionSet(
    ExecutionVariable Variable,
    bool ShouldCreate,
    bool SortInPlace = false);
