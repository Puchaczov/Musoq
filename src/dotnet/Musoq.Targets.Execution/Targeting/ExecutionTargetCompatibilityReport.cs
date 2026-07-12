using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution;

internal sealed record ExecutionTargetCompatibilityReport
{
    public ExecutionTargetCompatibilityReport(IReadOnlyList<ExecutionTargetRequirement>? requirements)
    {
        Requirements = Freeze(requirements);
    }

    public IReadOnlyList<ExecutionTargetRequirement> Requirements { get; }

    public bool HasRequirements => Requirements.Count > 0;

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}
