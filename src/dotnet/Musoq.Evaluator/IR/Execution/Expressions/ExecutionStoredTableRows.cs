namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionStoredTableRows(
    int TableIndex,
    GeneratedRowShape? GeneratedRowShape = null) : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(object)));
