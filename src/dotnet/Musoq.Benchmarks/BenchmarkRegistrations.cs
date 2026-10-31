namespace Musoq.Benchmarks;

public sealed record BenchmarkRegistration(Type Type, string Phase, string ResultApi, string? ScenarioId = null,
    Type? OutputType = null);

public static class BenchmarkRegistrations
{
    public static IReadOnlyList<BenchmarkRegistration> All { get; } = Array.AsReadOnly(new BenchmarkRegistration[]
    {
        new(typeof(QueryExecutionBenchmark), "execution", "Table"),
        new(typeof(ColdCompilationBenchmark), "compilation-cold", "Table"),
        new(typeof(WarmCompilationBenchmark), "compilation-warm", "Table"),
        new(typeof(ArtifactLoadBenchmark), "artifact-load", "Table"),
        new(typeof(TypedProjectionBenchmark), "execution", "Typed", "B01_Scan", typeof(ProjectionOutput)),
        new(typeof(TypedGroupBenchmark), "execution", "Typed", "B14_Group", typeof(GroupOutput)),
        new(typeof(TypedWindowBenchmark), "execution", "Typed", "B16_RowsFrame", typeof(WindowOutput))
    });

    public static BenchmarkRegistration For(Type type) => All.Single(item => item.Type == type);
    public static string TypedScenario<T>() => All.Single(item => item.OutputType == typeof(T)).ScenarioId!;
}
