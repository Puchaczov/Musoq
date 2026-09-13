using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
/// Measures the query-level cost of payload work for a path predicate. The baseline
/// arm retains runtime filtering after payload work; the enabled arm applies the typed
/// path match against candidate metadata before payload work.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class LikeCandidatePayloadBenchmark : IDisposable
{
    private static readonly CompilationOptions Options = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(ParallelizationMode.None));

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private readonly OptimizationBenchmarkRecorder _offRecorder = new();
    private readonly OptimizationBenchmarkRecorder _onRecorder = new();
    private CompiledQuery _candidateMetadataOff = null!;
    private CompiledQuery _candidateMetadataOn = null!;

    [Params(10_000, 100_000)]
    public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = OptimizationBenchmarkRows.CreateSingleSource(RowsCount);

        _candidateMetadataOff = Compile(
            rows,
            OptimizationBenchmarkPlanningMode.RejectProjection,
            _offRecorder);
        _candidateMetadataOn = Compile(
            rows,
            OptimizationBenchmarkPlanningMode.AcceptCandidateStringPredicate,
            _onRecorder);
    }

    public void ResetPayloadCounters()
    {
        _offRecorder.Reset();
        _onRecorder.Reset();
    }

    [Benchmark(Baseline = true)]
    public Table CandidatePayload_SourcePlanningOff() => _candidateMetadataOff.Run();

    [Benchmark]
    public Table CandidatePayload_SourcePlanningOn() => _candidateMetadataOn.Run();

    public int CandidateMetadataOffPayloadOpens => _offRecorder.PayloadOpens;

    public int CandidateMetadataOnPayloadOpens => _onRecorder.PayloadOpens;

    [GlobalCleanup]
    public void Dispose()
    {
        _candidateMetadataOff?.Dispose();
        _candidateMetadataOn?.Dispose();
    }

    private CompiledQuery Compile(
        IReadOnlyDictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> rows,
        OptimizationBenchmarkPlanningMode mode,
        OptimizationBenchmarkRecorder recorder)
    {
        return InstanceCreator.CompileForExecution(
            Query,
            Guid.NewGuid().ToString(),
            new OptimizationBenchmarkSchemaProvider(rows, mode, recorder),
            _loggerResolver,
            Options);
    }

    private const string Query =
        "select b.Id, b.Payload from #bench.items() b where b.Name like 'entity-000%'";
}
