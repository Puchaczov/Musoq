namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed record RecursiveCteInvariantInput(
    string Name,
    GeneratedRowShape RowShape,
    ExecutionExpression Rows,
    ExecutionVariable? Hash = null);
