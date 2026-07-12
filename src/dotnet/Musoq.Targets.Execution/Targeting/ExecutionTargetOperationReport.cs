using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution;

internal sealed record ExecutionOperationUsage
{
    public ExecutionOperationUsage(ExecutionOperationId operationId, int occurrenceCount)
    {
        if (occurrenceCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(occurrenceCount), "Operation occurrence count must be positive.");

        OperationId = operationId;
        OccurrenceCount = occurrenceCount;
    }

    public ExecutionOperationId OperationId { get; }

    public int OccurrenceCount { get; }
}

internal sealed record ExecutionTargetOperationReport
{
    public ExecutionTargetOperationReport(IEnumerable<ExecutionOperationUsage>? operations)
    {
        Operations = Array.AsReadOnly(
            (operations ?? [])
            .OrderBy(static usage => usage.OperationId.Value, StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<ExecutionOperationUsage> Operations { get; }

    public static ExecutionTargetOperationReport Empty { get; } = new([]);
}
