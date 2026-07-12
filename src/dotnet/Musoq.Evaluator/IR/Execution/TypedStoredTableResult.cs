namespace Musoq.Evaluator.IR.Execution;

internal sealed record TypedStoredTableResult(
    int TableIndex,
    GeneratedRowShape RowShape);
