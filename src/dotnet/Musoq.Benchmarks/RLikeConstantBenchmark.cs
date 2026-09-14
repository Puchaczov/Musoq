using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>Measures constant RLIKE patterns through compiled Musoq SQL.</summary>
[MemoryDiagnoser]
[ShortRunJob]
public class RLikeConstantBenchmark : BenchmarkBase, IDisposable
{
    private CompiledQuery _query = null!;

    [Params(
        RLikeBenchmarkScenario.Literal,
        RLikeBenchmarkScenario.AnchoredLiteral,
        RLikeBenchmarkScenario.Complex,
        RLikeBenchmarkScenario.Unicode)]
    public RLikeBenchmarkScenario Scenario { get; set; }

    internal int RowCount { get; set; } = 100_000;

    public string ResultHash { get; private set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        var rows = RLikeBenchmarkData.CreateConstantRows(RowCount, Scenario);
        var pattern = RLikeBenchmarkData.ConstantPattern(Scenario).Replace("'", "''", StringComparison.Ordinal);
        var query = $"select p.Email from #A.Entities() p where p.Email rlike r'{pattern}'";
        _query = CreateForProfilesWithOptions(
            query,
            new Dictionary<string, IEnumerable<ProfileEntity>> { ["#A"] = rows },
            new CompilationOptions(ParallelizationMode.None));

        using var result = _query.Run();
        if (result.Count != RowCount)
            throw new InvalidOperationException($"Expected {RowCount} rows, received {result.Count}.");
        ResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
    }

    [Benchmark]
    public Table ConstantRLike_Run() => _query.Run();

    [GlobalCleanup]
    public void Dispose()
    {
        _query?.Dispose();
    }
}
