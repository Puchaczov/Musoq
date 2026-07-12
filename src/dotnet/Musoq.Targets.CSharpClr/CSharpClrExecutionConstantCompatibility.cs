namespace Musoq.Targets.CSharpClr;

internal static class CSharpClrExecutionConstantCompatibility
{
    internal static object? RequireClrValue(this ExecutionConstantValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ToClrValue();
    }
}
