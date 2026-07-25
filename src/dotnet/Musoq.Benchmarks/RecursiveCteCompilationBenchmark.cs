using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class RecursiveCteCompilationBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private RecursiveCteBenchmarkFixture _fixture = null!;
    private RecursiveCteBenchmarkSchemaProvider _schemaProvider = null!;

    [Params(
        RecursiveCteBenchmarkScenario.Chain,
        RecursiveCteBenchmarkScenario.WideRows,
        RecursiveCteBenchmarkScenario.IndexedEdges)]
    public RecursiveCteBenchmarkScenario Scenario { get; set; }

    [Params(1_024)]
    public int Scale { get; set; }

    [Params(ParallelizationMode.None, ParallelizationMode.Full)]
    public ParallelizationMode ExecutionMode { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _fixture = RecursiveCteBenchmarkFixture.Create(Scenario, Scale);
        _schemaProvider = new RecursiveCteBenchmarkSchemaProvider(_fixture.Edges);
    }

    [Benchmark]
    public CompiledQuery Compile() => InstanceCreator.CompileForExecution(
        _fixture.Query,
        $"{nameof(RecursiveCteCompilationBenchmark)}_{Scenario}_{Scale}_{ExecutionMode}",
        _schemaProvider,
        _loggerResolver,
        RecursiveCteBenchmarkOptions.Create(_fixture, ExecutionMode));
}
