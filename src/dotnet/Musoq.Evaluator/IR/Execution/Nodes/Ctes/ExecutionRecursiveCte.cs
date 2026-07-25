namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRecursiveCte(
    string Name,
    int TableIndex,
    ExecutionVariable Result,
    ExecutionVariable CurrentFrontier,
    ExecutionVariable NextFrontier,
    ExecutionVariable SnapshotRows,
    ExecutionVariable? Seen,
    ExecutionRecursiveCteIdentityMode IdentityMode,
    int[] IdentityFieldIndexes,
    GeneratedRowShape RowShape,
    ExecutionBlock Anchor,
    ExecutionBlock InvariantSetup,
    ExecutionBlock RecursiveMember,
    int MaxIterations,
    int MaxRows,
    int MaxSnapshotRows) : ExecutionNode;
