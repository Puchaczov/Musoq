namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRowStream(
    ExecutionVariable Variable,
    ExecutionRowStreamKind Kind,
    ExecutionRowStreamRowsAccess RowsAccess = ExecutionRowStreamRowsAccess.Direct)
    : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(object)));
