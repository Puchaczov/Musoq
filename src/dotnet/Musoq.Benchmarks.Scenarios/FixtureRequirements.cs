using Musoq.Schema.StructuralInputs;

namespace Musoq.Benchmarks.Scenarios;

public sealed record FixtureRequirements(bool ValidatePrimitiveTypes, int? RecursiveMaxIterations,
    StructuralInputLimits StructuralLimits);

public static class BenchmarkInputSizes
{
    public const int Small = 1_000;
    public const int Medium = 10_000;
    public const int Large = 100_000;
    public static IReadOnlyList<int> Standard { get; } = Array.AsReadOnly(new[] { Small, Medium, Large });
}
