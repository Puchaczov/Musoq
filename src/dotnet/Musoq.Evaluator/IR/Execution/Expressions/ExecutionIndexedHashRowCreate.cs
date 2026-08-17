namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIndexedHashRowCreate(
    ExecutionVariable Row,
    ExecutionVariable Index,
    ExecutionTypeRef ReturnType,
    string? GeneratedRowTypeName = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionIndexedHashRowCreate(
        ExecutionVariable row,
        ExecutionVariable index,
        Type returnType,
        string? generatedRowTypeName = null)
        : this(row, index, ExecutionClrBindingFactory.FromClr(returnType), generatedRowTypeName)
    {
    }
}
