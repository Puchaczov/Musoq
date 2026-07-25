namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRecursiveCteAppend(
    string Name,
    ExecutionVariable Result,
    ExecutionVariable Frontier,
    ExecutionVariable? Seen,
    int[] IdentityFieldIndexes,
    int MaxRows,
    ExecutionAppendRow AppendRow) : ExecutionNode;
