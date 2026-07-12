namespace Musoq.Evaluator.IR.Execution;

internal sealed record CapturedLocal(string Name, ExecutionTypeRef Type, string? GeneratedRowTypeName = null)
{
    internal CapturedLocal(string name, Type type, string? generatedRowTypeName = null)
        : this(name, ExecutionTypeRef.FromClr(type), generatedRowTypeName)
    {
    }
}
