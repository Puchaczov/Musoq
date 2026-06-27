using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class ProfilingLifecycleOverheadBenchmark
{
    public enum ProfilingLifecycleScenario
    {
        Simple,
        HashJoin
    }

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private readonly CompilationOptions _options = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(
            parallelizationMode: ParallelizationMode.Full,
            useCteParallelization: true));

    private ISchemaProvider _schemaProvider = null!;
    private string _script = string.Empty;
    private int _assemblyNameCounter;
    private QueryProfileSnapshot? _lastProfile;
    private string? _lastDiagnosticsText;

    [Params(1_000)]
    public int RowsCount { get; set; }

    [Params(BenchmarkChunkShape.Chunk512, BenchmarkChunkShape.Chunk4096, BenchmarkChunkShape.SingleGiant)]
    public BenchmarkChunkShape ChunkShape { get; set; }

    [Params(ProfilingLifecycleScenario.Simple, ProfilingLifecycleScenario.HashJoin)]
    public ProfilingLifecycleScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _script = Scenario switch
        {
            ProfilingLifecycleScenario.Simple =>
                "select Id, Name, City, Population from #A.entities() where Population > 50000",
            ProfilingLifecycleScenario.HashJoin =>
                "select a.Id, a.Name, b.Score from #A.entities() a inner join #B.entities() b on a.Id = b.Id",
            _ => throw new ArgumentOutOfRangeException()
        };
        _schemaProvider = CreateSchemaProvider(RowsCount, ChunkShape);
    }

    [Benchmark(Baseline = true)]
    public Table Disabled_CompileAndRun()
    {
        return InstanceCreator
            .CompileForExecution(_script, CreateAssemblyName(), _schemaProvider, _loggerResolver, _options)
            .Run();
    }

    [Benchmark]
    public Table Profile_CompileAndRun()
    {
        var result = InstanceCreator.Profile(
            _script,
            CreateAssemblyName(),
            _schemaProvider,
            _loggerResolver,
            _options);

        _lastProfile = result.Profile;
        _lastDiagnosticsText = result.ProfileText;
        return result.Result;
    }

    [Benchmark]
    public Table ExplainAnalyze_CompileAndRun()
    {
        var result = InstanceCreator.ExplainAnalyze(
            _script,
            CreateAssemblyName(),
            _schemaProvider,
            _loggerResolver,
            _options);

        _lastProfile = result.Profile;
        _lastDiagnosticsText = result.ExplainAnalyzeText;
        return result.Result;
    }

    private string CreateAssemblyName()
    {
        var next = Interlocked.Increment(ref _assemblyNameCounter);
        return $"{nameof(ProfilingLifecycleOverheadBenchmark)}_{Scenario}_{RowsCount}_{ChunkShape}_{next}";
    }

    private static ISchemaProvider CreateSchemaProvider(int rowsCount, BenchmarkChunkShape chunkShape)
    {
        var leftRows = CreateRows(rowsCount, "left");
        var rightRows = CreateRows(rowsCount, "right");

        return new GenericSchemaProvider<ProfilingOverheadEntity, ProfilingOverheadTable>(
            BenchmarkSourceChunks.FromRows(new Dictionary<string, IEnumerable<ProfilingOverheadEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = leftRows,
                ["#A"] = leftRows,
                ["B"] = rightRows,
                ["#B"] = rightRows
            }, chunkShape),
            ProfilingOverheadEntity.NameToIndexMap,
            ProfilingOverheadEntity.IndexToObjectAccessMap);
    }

    private static ProfilingOverheadEntity[] CreateRows(int count, string prefix)
    {
        return Enumerable.Range(0, count)
            .Select(index => new ProfilingOverheadEntity
            {
                Id = index,
                Name = $"{prefix}_{index}",
                City = $"City_{index % 257}",
                Category = $"Category_{index % 17}",
                Population = 10_000 + (index * 97 % 1_000_000),
                Score = index * 31 % 10_000
            })
            .ToArray();
    }
}
