using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Diagnostics;

public sealed record SourceProfileSnapshot(
    string Name,
    long RowsRead,
    TimeSpan? FirstRowLatency,
    TimeSpan? LastRowTime,
    TimeSpan MoveNextWaitTime,
    TimeSpan ConsumerGapTime,
    int ExceptionCount,
    string? ExceptionType,
    string? ExceptionMessage,
    SourceProfileDiagnosis Diagnosis)
{
    public long RowsProduced { get; init; }

    public long BytesRead { get; init; }

    public IReadOnlyDictionary<string, long> Metrics { get; init; } = new Dictionary<string, long>(StringComparer.Ordinal);

    public IReadOnlyList<SourceOperationProfileSnapshot> Operations { get; init; } =
        Array.Empty<SourceOperationProfileSnapshot>();

    public bool IsTimingEstimated { get; init; }

    public long TimedMoveNextCalls { get; init; }

    public long UntimedMoveNextCalls { get; init; }
}
