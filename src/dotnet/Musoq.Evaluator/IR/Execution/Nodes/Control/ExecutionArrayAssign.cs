namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionArrayAssign(
    ExecutionVariable Array,
    ExecutionExpression Index,
    ExecutionExpression Value,
    ExecutionTypeRef ElementType) : ExecutionNode
{
    internal ExecutionArrayAssign(
        ExecutionVariable array,
        ExecutionExpression index,
        ExecutionExpression value,
        Type elementType)
        : this(array, index, value, ExecutionClrBindingFactory.FromClr(elementType))
    {
    }
}
