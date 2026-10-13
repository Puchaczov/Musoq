namespace Musoq.Evaluator.IR.Execution;

public sealed record NestedClrPropertyAccess(string PropertyPath) : FieldAccessStrategy
{
    public bool IsNullSafe { get; init; }
}
