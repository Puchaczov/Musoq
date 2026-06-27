using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class SourcePlanningV2Benchmark
{
    private static readonly CompilationOptions Options = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(ParallelizationMode.None));

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();

    private CompiledQuery _projectionOff = null!;
    private CompiledQuery _projectionOn = null!;
    private CompiledQuery _projectionRequiredPayloadOff = null!;
    private CompiledQuery _projectionRequiredPayloadOn = null!;
    private CompiledQuery _predicateOff = null!;
    private CompiledQuery _predicateOn = null!;

    [Params(10_000, 100_000)]
    public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = OptimizationBenchmarkRows.CreateSingleSource(RowsCount);

        _projectionOff = Compile(ProjectionQuery, rows, OptimizationBenchmarkPlanningMode.RejectProjection);
        _projectionOn = Compile(ProjectionQuery, rows, OptimizationBenchmarkPlanningMode.AcceptProjection);
        _projectionRequiredPayloadOff = Compile(ProjectionRequiredPayloadQuery, rows, OptimizationBenchmarkPlanningMode.RejectProjection);
        _projectionRequiredPayloadOn = Compile(ProjectionRequiredPayloadQuery, rows, OptimizationBenchmarkPlanningMode.AcceptProjection);
        _predicateOff = Compile(PredicateQuery, rows, OptimizationBenchmarkPlanningMode.RejectAll);
        _predicateOn = Compile(PredicateQuery, rows, OptimizationBenchmarkPlanningMode.AcceptPredicate);
    }

    [Benchmark(Baseline = true)]
    public Table Projection_SourcePlanningOff()
    {
        return _projectionOff.Run();
    }

    [Benchmark]
    public Table Projection_SourcePlanningOn()
    {
        return _projectionOn.Run();
    }

    [Benchmark]
    public Table ProjectionRequiredPayload_SourcePlanningOff()
    {
        return _projectionRequiredPayloadOff.Run();
    }

    [Benchmark]
    public Table ProjectionRequiredPayload_SourcePlanningOn()
    {
        return _projectionRequiredPayloadOn.Run();
    }

    [Benchmark]
    public Table Predicate_SourcePlanningOff()
    {
        return _predicateOff.Run();
    }

    [Benchmark]
    public Table Predicate_SourcePlanningOn()
    {
        return _predicateOn.Run();
    }

    private CompiledQuery Compile(
        string query,
        IReadOnlyDictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> rowsBySchema,
        OptimizationBenchmarkPlanningMode mode)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new OptimizationBenchmarkSchemaProvider(rowsBySchema, mode),
            _loggerResolver,
            Options);
    }

    private const string ProjectionQuery =
        "select b.Id, b.Name, b.Score from #bench.items() b";

    private const string ProjectionRequiredPayloadQuery =
        "select b.Id, b.Payload from #bench.items() b";

    private const string PredicateQuery =
        "select b.Id, b.Category, b.Score from #bench.items() b where b.Category = 'hot-a' and b.Score + 1 > 0";
}
