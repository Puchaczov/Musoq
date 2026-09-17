using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
/// Tracks compilation cost for a dynamic column pattern and correlated APPLY.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DynamicLikeCompilationBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private ISchemaProvider _applyProvider = null!;
    private ISchemaProvider _dynamicProvider = null!;

    [GlobalSetup]
    public void Setup()
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
        _dynamicProvider = new GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(
            BenchmarkSourceChunks.FromRows(sources, BenchmarkChunkShape.Chunk4096),
            ProfileEntity.KNameToIndexMap,
            ProfileEntity.KIndexToObjectAccessMap);

        var recorder = new DynamicLikeApplyRecorder();
        _applyProvider = new DynamicLikeApplySchemaProvider(
            DynamicLikeApplyBenchmark.CreateRows(4, 2, recorder),
            recorder);
    }

    [Benchmark(Baseline = true)]
    public int DynamicLike_Compilation() => Compile(
        DynamicLikeExecutionBenchmark.Query,
        _dynamicProvider,
        "DynamicLikeCompilation");

    [Benchmark]
    public int DynamicLikeApply_Compilation() => Compile(
        DynamicLikeApplyBenchmark.InnerPatternQuery,
        _applyProvider,
        "DynamicLikeApplyCompilation");

    private int Compile(string query, ISchemaProvider provider, string assemblyPrefix)
    {
        var inspection = InstanceCreator.CompileForInspection(
            query,
            $"{assemblyPrefix}_{Guid.NewGuid():N}",
            provider,
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized(new CompilationOptions(ParallelizationMode.None)));
        return inspection.GeneratedCSharpCode.Length;
    }
}
