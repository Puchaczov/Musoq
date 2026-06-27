namespace Musoq.Benchmarks;

public sealed class OptimizationBenchmarkEntity
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string GroupKey { get; init; } = string.Empty;

    public int JoinKey { get; init; }

    public int Score { get; init; }

    public int Value { get; init; }

    public DateTime CreatedAt { get; init; }

    public string Payload { get; init; } = string.Empty;
}
