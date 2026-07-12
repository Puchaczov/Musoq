namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionVariable(
    string Name,
    ExecutionTypeRef Type,
    string? GeneratedRowTypeName = null)
{
    internal ExecutionVariable(string name, Type type, string? generatedRowTypeName = null)
        : this(name, ExecutionTypeRef.FromClr(type), generatedRowTypeName)
    {
    }
}
