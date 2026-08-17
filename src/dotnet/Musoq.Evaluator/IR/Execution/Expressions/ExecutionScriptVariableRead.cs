namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionScriptVariableRead(
    string Name,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionScriptVariableRead(string name, Type returnType)
        : this(name, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
