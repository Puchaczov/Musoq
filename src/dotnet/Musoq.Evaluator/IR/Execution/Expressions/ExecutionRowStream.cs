namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRowStream(
    ExecutionVariable Variable,
    ExecutionRowStreamKind Kind,
    ExecutionRowStreamRowsAccess RowsAccess = ExecutionRowStreamRowsAccess.Direct
) : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(object)))
{
    public bool HasBaseRowElements { get; init; }
    internal bool HasDirectScalarElements { get; init; }
    internal bool HasRecordBatches { get; init; }
    internal bool IsValueEnumerable { get; init; }
}
