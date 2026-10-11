using System.Collections.Frozen;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Names owned by executable entrypoint parameters and runtime context bindings.</summary>
internal static class ExecutionRuntimeIdentifierFacts
{
    private static readonly FrozenSet<string> Names = new[]
    {
        "token", "provider", "logger", "queryContext", "sourceExecutionPlans",
        "sourceRuntimeSettingsBySourceContextId", "queryProgressContext"
    }.ToFrozenSet(StringComparer.Ordinal);

    public static bool Contains(string name) => Names.Contains(name);
}
