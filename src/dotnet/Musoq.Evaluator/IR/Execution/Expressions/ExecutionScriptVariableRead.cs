namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionScriptVariableRead(
    string Name,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    /// <summary>Concrete query-generated storage selected during lowering.</summary>
    public string? GeneratedTypeName { get; init; }

    internal ExecutionScriptVariableRead(string name, Type returnType)
        : this(name, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
