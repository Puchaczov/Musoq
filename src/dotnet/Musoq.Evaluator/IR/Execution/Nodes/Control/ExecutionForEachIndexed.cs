namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionForEachIndexed(
    ExecutionVariable Item,
    ExecutionVariable Index,
    ExecutionVariable Source,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionBlock Body
) : ExecutionNode
{
    internal ExecutionVariable? WinnerRanks { get; init; }
    internal ExecutionVariable? WinnerPositions { get; init; }
}
