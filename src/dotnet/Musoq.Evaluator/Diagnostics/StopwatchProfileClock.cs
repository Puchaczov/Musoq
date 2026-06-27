using System;
using System.Diagnostics;

namespace Musoq.Evaluator.Diagnostics;

public sealed class StopwatchProfileClock : IProfileClock
{
    public static StopwatchProfileClock Instance { get; } = new();

    private StopwatchProfileClock()
    {
    }

    public long GetTimestamp() => Stopwatch.GetTimestamp();

    public TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp) =>
        Stopwatch.GetElapsedTime(startTimestamp, endTimestamp);
}
