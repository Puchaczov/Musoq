using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
/// Measures dynamic LIKE input lengths without multiplying the high-cardinality matrix.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DynamicLikeInputLengthBenchmark : BenchmarkBase, IDisposable
{
    private CompiledQuery _query = null!;

    [Params(4, 64, 4096)]
    public int InputLength { get; set; }

    [Params(
        DynamicLikeBenchmarkScenario.Ascii,
        DynamicLikeBenchmarkScenario.Unicode,
        DynamicLikeBenchmarkScenario.Wildcard)]
    public DynamicLikeBenchmarkScenario Scenario { get; set; }

    internal int RowCount { get; set; } = 1_024;

    public string ResultHash { get; private set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        var rows = DynamicLikeBenchmarkData.CreateRows(
            RowCount,
            patternCardinality: 64,
            InputLength,
            Scenario);
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            ["#A"] = rows
        };
        _query = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like p.FirstName",
            sources,
            new CompilationOptions(ParallelizationMode.None));

        using var result = _query.Run();
        if (result.Count != RowCount)
            throw new InvalidOperationException($"Expected {RowCount} rows, received {result.Count}.");
        ResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
    }

    [Benchmark]
    public Table DynamicLike_ByInputLength() => _query.Run();

    [GlobalCleanup]
    public void Dispose()
    {
        _query?.Dispose();
    }
}
