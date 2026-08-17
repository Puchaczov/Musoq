namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionBetween(
    ExecutionExpression Expression,
    ExecutionExpression Low,
    ExecutionExpression High,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionBetween(
        ExecutionExpression expression,
        ExecutionExpression low,
        ExecutionExpression high,
        Type returnType)
        : this(expression, low, high, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
