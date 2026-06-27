using System;

namespace Musoq.Evaluator.Diagnostics;

public sealed record OperatorProfileSnapshot(
    string Id,
    string Name,
    long InputRows,
    long OutputRows,
    TimeSpan ElapsedTime,
    bool HasActualStats = true,
    int ExceptionCount = 0,
    string? ExceptionType = null,
    string? ExceptionMessage = null)
{
    public bool HasElapsedTime { get; init; } = HasActualStats;
}
