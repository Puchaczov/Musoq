namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIndexedHashRowRowRead(
    ExecutionVariable IndexedRow,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionIndexedHashRowRowRead(ExecutionVariable indexedRow, Type returnType)
        : this(indexedRow, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
