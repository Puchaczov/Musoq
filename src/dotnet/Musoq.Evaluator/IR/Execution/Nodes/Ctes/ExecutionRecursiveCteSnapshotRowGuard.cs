namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRecursiveCteSnapshotRowGuard(
    string Name,
    ExecutionVariable Counter,
    int MaxRows) : ExecutionNode;
