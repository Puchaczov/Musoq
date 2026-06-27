using System;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed class SourcePlanningEntity
{
    public int Id { get; init; }

    public string? Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public int Score { get; init; }

    public DateTime CreatedAt { get; init; }

    public int JoinKey { get; init; }

    public string ExpensivePayload { get; init; } = string.Empty;
}
