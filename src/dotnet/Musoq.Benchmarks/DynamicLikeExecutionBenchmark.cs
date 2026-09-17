using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
/// Measures row-dependent LIKE patterns through compiled Musoq SQL. Rows sharing a
/// pattern are contiguous so later execution-local caches have deterministic reuse.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DynamicLikeExecutionBenchmark : BenchmarkBase, IDisposable
{
    private const int BaselineMatcherCacheCapacity = 512;
    internal const string Query =
        "select p.Email from #A.Entities() p where p.Email like p.FirstName";

    private CompiledQuery _query = null!;

    [Params(1, 2, 64, 512, 4096)]
    public int PatternCardinality { get; set; }

    [Params(
        DynamicLikeBenchmarkScenario.Ascii,
        DynamicLikeBenchmarkScenario.Unicode,
        DynamicLikeBenchmarkScenario.Wildcard)]
    public DynamicLikeBenchmarkScenario Scenario { get; set; }

    internal int RowCount { get; set; } = 10_000;

    public string ResultHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the steady-state construction count implied by the R11 process-wide matcher
    /// cache. The setup run warms cardinalities that fit; larger sequential sets churn once
    /// per distinct pattern on every measured query.
    /// </summary>
    public int BaselineMatcherConstructions =>
        PatternCardinality <= BaselineMatcherCacheCapacity ? 0 : PatternCardinality;

    [GlobalSetup]
    public void Setup()
    {
        var rows = DynamicLikeBenchmarkData.CreateRows(
            RowCount,
            PatternCardinality,
            inputLength: 32,
            Scenario);
        _query = Compile(rows);

        using var result = _query.Run();
        if (result.Count != RowCount)
            throw new InvalidOperationException($"Expected {RowCount} rows, received {result.Count}.");
        ResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
    }

    [Benchmark]
    public Table DynamicLike_Run() => _query.Run();

    [GlobalCleanup]
    public void Dispose()
    {
        _query?.Dispose();
    }

    internal static QueryInspectionResult InspectQuery()
    {
        var rows = DynamicLikeBenchmarkData.CreateRows(
            rowCount: 16,
            patternCardinality: 2,
            inputLength: 32,
            DynamicLikeBenchmarkScenario.Ascii);
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            ["#A"] = rows
        };
        var provider = new GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(
            BenchmarkSourceChunks.FromRows(sources, BenchmarkChunkShape.Chunk4096),
            ProfileEntity.KNameToIndexMap,
            ProfileEntity.KIndexToObjectAccessMap);
        return InstanceCreator.CompileForInspection(
            Query,
            $"DynamicLikeInspection_{Guid.NewGuid():N}",
            provider,
            new BenchmarkLoggerResolver(),
            BenchmarkCompilationOptions.Materialized(new CompilationOptions(ParallelizationMode.None)));
    }

    private CompiledQuery Compile(ProfileEntity[] rows)
    {
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            ["#A"] = rows
        };
        return CreateForProfilesWithOptions(
            Query,
            sources,
            new CompilationOptions(ParallelizationMode.None));
    }
}
