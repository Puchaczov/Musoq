using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>Tracks compilation cost for dynamic and correlated RLIKE queries.</summary>
[MemoryDiagnoser]
[ShortRunJob]
public class RLikeCompilationBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private ISchemaProvider _applyProvider = null!;
    private ISchemaProvider _dynamicProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows = RLikeBenchmarkData.CreateRows(16, 2, 32, RLikeBenchmarkScenario.Complex);
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>> { ["#A"] = rows };
        _dynamicProvider = new GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(
            BenchmarkSourceChunks.FromRows(sources, BenchmarkChunkShape.Chunk4096),
            ProfileEntity.KNameToIndexMap,
            ProfileEntity.KIndexToObjectAccessMap);

        var recorder = new DynamicLikeApplyRecorder();
        _applyProvider = new DynamicLikeApplySchemaProvider(
            RLikeApplyBenchmark.CreateRows(4, 2, recorder),
            recorder);
    }

    [Benchmark(Baseline = true)]
    public int DynamicRLike_Compilation() => Compile(
        RLikeExecutionBenchmark.Query,
        _dynamicProvider,
        "DynamicRLikeCompilation");

    [Benchmark]
    public int RLikeApply_Compilation() => Compile(
        RLikeApplyBenchmark.InnerPatternQuery,
        _applyProvider,
        "RLikeApplyCompilation");

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
