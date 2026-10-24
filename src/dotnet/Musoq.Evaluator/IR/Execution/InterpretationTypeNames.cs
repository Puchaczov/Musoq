namespace Musoq.Evaluator.IR.Execution;

internal static class InterpretationTypeNames
{
    // Union names identify semantic shapes; their values use their selected concrete type.
    internal static string? Executable(string? name) => name?.Contains(".TextSwitch_", StringComparison.Ordinal) == true ? null : name;
}
