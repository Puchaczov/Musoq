using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class RecursiveCteBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CancellationToken _cancellationToken;
    private RecursiveCteBenchmarkFixture _fixture = null!;
    private CompiledQuery _query = null!;

    [Params(
        RecursiveCteBenchmarkScenario.Chain,
        RecursiveCteBenchmarkScenario.Tree,
        RecursiveCteBenchmarkScenario.Diamond,
        RecursiveCteBenchmarkScenario.Cycle,
        RecursiveCteBenchmarkScenario.DuplicateHeavyKeyed,
        RecursiveCteBenchmarkScenario.WideRows,
        RecursiveCteBenchmarkScenario.InvariantSnapshot,
        RecursiveCteBenchmarkScenario.IndexedEdges,
        RecursiveCteBenchmarkScenario.CorrelatedApply,
        RecursiveCteBenchmarkScenario.EmptyAnchor)]
    public RecursiveCteBenchmarkScenario Scenario { get; set; }

    [Params(1_024)]
    public int Scale { get; set; }

    [Params(ParallelizationMode.None, ParallelizationMode.Full)]
    public ParallelizationMode ExecutionMode { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _cancellationToken = CancellationToken.None;
        _fixture = RecursiveCteBenchmarkFixture.Create(Scenario, Scale);
        _query = InstanceCreator.CompileForExecution(
            _fixture.Query,
            $"{nameof(RecursiveCteBenchmark)}_{Scenario}_{Scale}_{ExecutionMode}",
            new RecursiveCteBenchmarkSchemaProvider(_fixture.Edges),
            _loggerResolver,
            RecursiveCteBenchmarkOptions.Create(_fixture, ExecutionMode));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _query.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Table HandwrittenSemiNaive() => RecursiveCteHandwrittenBaseline.Execute(_fixture, _cancellationToken);

    [Benchmark]
    public Table MusoqGenerated() => _query.Run(_cancellationToken);
}
