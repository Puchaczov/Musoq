using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Diagnostics;

public sealed record QueryProfileSnapshot(
    TimeSpan TotalElapsed,
    IReadOnlyList<SourceProfileSnapshot> Sources,
    IReadOnlyList<OperatorProfileSnapshot> Operators)
{
    public string? QueryId { get; init; }

    public static QueryProfileSnapshot Empty { get; } = new(
        TimeSpan.Zero,
        Array.Empty<SourceProfileSnapshot>(),
        Array.Empty<OperatorProfileSnapshot>());
}
