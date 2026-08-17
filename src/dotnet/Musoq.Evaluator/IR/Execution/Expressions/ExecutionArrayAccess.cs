namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionArrayAccess(
    ExecutionExpression Array,
    ExecutionExpression Index,
    ExecutionTypeRef ElementType,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionArrayAccess(
        ExecutionExpression array,
        ExecutionExpression index,
        Type elementType,
        Type returnType)
        : this(array, index, ExecutionClrBindingFactory.FromClr(elementType), ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
