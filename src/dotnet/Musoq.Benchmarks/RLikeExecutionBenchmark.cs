using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>Measures compiled SQL with row-dependent RLIKE patterns.</summary>
[MemoryDiagnoser]
[ShortRunJob]
public class RLikeExecutionBenchmark : BenchmarkBase, IDisposable
{
    internal const string Query =
        "select p.Email from #A.Entities() p where p.Email rlike p.FirstName";

    private CompiledQuery _query = null!;

    [Params(1, 2, 64, 512, 4096)]
    public int PatternCardinality { get; set; }

    [Params(
        RLikeBenchmarkScenario.Literal,
        RLikeBenchmarkScenario.AnchoredLiteral,
        RLikeBenchmarkScenario.Complex,
        RLikeBenchmarkScenario.Unicode)]
    public RLikeBenchmarkScenario Scenario { get; set; }

    internal int RowCount { get; set; } = 10_000;

    public string ResultHash { get; private set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        var rows = RLikeBenchmarkData.CreateRows(RowCount, PatternCardinality, 32, Scenario);
        _query = Compile(rows);
        using var result = _query.Run();
        ValidateResult(result);
        ResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
    }

    [Benchmark]
    public Table DynamicRLike_Run() => _query.Run();

    internal static QueryInspectionResult InspectQuery()
    {
        var rows = RLikeBenchmarkData.CreateRows(16, 2, 32, RLikeBenchmarkScenario.Complex);
        return CompileForInspection(rows, Query, "RLikeDynamicInspection");
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _query?.Dispose();
    }

    private CompiledQuery Compile(ProfileEntity[] rows) => CreateForProfilesWithOptions(
        Query,
        new Dictionary<string, IEnumerable<ProfileEntity>> { ["#A"] = rows },
        new CompilationOptions(ParallelizationMode.None));

    private void ValidateResult(Table result)
    {
        if (result.Count != RowCount)
            throw new InvalidOperationException($"Expected {RowCount} rows, received {result.Count}.");
    }

    internal static QueryInspectionResult CompileForInspection(
        ProfileEntity[] rows,
        string query,
        string assemblyPrefix)
    {
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>> { ["#A"] = rows };
        var provider = new Schema.GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(
            BenchmarkSourceChunks.FromRows(sources, BenchmarkChunkShape.Chunk4096),
            ProfileEntity.KNameToIndexMap,
            ProfileEntity.KIndexToObjectAccessMap);
        return InstanceCreator.CompileForInspection(
            query,
            $"{assemblyPrefix}_{Guid.NewGuid():N}",
            provider,
            new BenchmarkLoggerResolver(),
            BenchmarkCompilationOptions.Materialized(new CompilationOptions(ParallelizationMode.None)));
    }
}
