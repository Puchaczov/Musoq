namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIndexedHashRowIndexRead(
    ExecutionVariable IndexedRow) : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(int)));
