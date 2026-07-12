using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution;

internal sealed record ExecutionTargetReadinessReport
{
    public ExecutionTargetReadinessReport(
        IReadOnlyList<ExecutionTargetReadinessIssue>? issues,
        ExecutionSemanticsContract? semanticsContract = null)
    {
        Issues = Freeze(issues);
        SemanticsContract = semanticsContract ?? ExecutionSemanticsContract.Version1;
    }

    public IReadOnlyList<ExecutionTargetReadinessIssue> Issues { get; }

    public ExecutionSemanticsContract SemanticsContract { get; }

    public bool IsReady(ExecutionTargetRuntimeFamily runtimeFamily)
    {
        return Issues.All(issue => issue.RuntimeFamily != runtimeFamily);
    }

    public IReadOnlyList<ExecutionTargetReadinessIssue> IssuesFor(
        ExecutionTargetRuntimeFamily runtimeFamily)
    {
        return Issues
            .Where(issue => issue.RuntimeFamily == runtimeFamily)
            .ToArray();
    }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}
