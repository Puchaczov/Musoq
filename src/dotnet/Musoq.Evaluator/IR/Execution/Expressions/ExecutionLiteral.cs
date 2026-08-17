namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionLiteral(
    ExecutionConstantValue Value,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionLiteral(object? value, Type returnType)
        : this(ExecutionConstantValue.FromClr(value, ExecutionClrBindingFactory.FromClr(returnType)), ExecutionClrBindingFactory.FromClr(returnType))
    {
    }

    internal ExecutionLiteral(object? value, ExecutionTypeRef returnType)
        : this(ExecutionConstantValue.FromClr(value, returnType), returnType)
    {
    }
}
