namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateResultRef(
    string Identifier,
    string? DisplayName,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionAggregateResultRef(string identifier, string? displayName, Type returnType)
        : this(identifier, displayName, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
