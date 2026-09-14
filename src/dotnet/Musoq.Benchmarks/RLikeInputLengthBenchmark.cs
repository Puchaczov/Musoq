using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>Measures RLIKE matching as input length grows.</summary>
[MemoryDiagnoser]
[ShortRunJob]
public class RLikeInputLengthBenchmark : BenchmarkBase, IDisposable
{
    private CompiledQuery _query = null!;

    [Params(4, 64, 4096)]
    public int InputLength { get; set; }

    [Params(RLikeBenchmarkScenario.Literal, RLikeBenchmarkScenario.Complex)]
    public RLikeBenchmarkScenario Scenario { get; set; }

    internal int RowCount { get; set; } = 10_000;

    public string ResultHash { get; private set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        var rows = RLikeBenchmarkData.CreateRows(RowCount, 2, InputLength, Scenario);
        _query = CreateForProfilesWithOptions(
            RLikeExecutionBenchmark.Query,
            new Dictionary<string, IEnumerable<ProfileEntity>> { ["#A"] = rows },
            new CompilationOptions(ParallelizationMode.None));
        using var result = _query.Run();
        if (result.Count != RowCount)
            throw new InvalidOperationException($"Expected {RowCount} rows, received {result.Count}.");
        ResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
    }

    [Benchmark]
    public Table RLike_ByInputLength() => _query.Run();

    [GlobalCleanup]
    public void Dispose()
    {
        _query?.Dispose();
    }
}
