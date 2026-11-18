using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class QueryExecutionBenchmark
{
    private CompiledQuery _query = null!;
    private readonly Consumer _consumer = new();
    public virtual IEnumerable<string> Scenarios => ScenarioCatalog.All.Select(scenario => scenario.Id);

    [ParamsSource(nameof(Scenarios))]
    public string ScenarioId { get; set; } = "B01_Scan";
    public IEnumerable<string> Sizes => BenchmarkSizes.Labels;

    [ParamsSource(nameof(Sizes))]
    public string Size { get; set; } = "Small";

    [GlobalSetup]
    public void Setup()
    {
        var scenario = ScenarioCatalog.Get(ScenarioId);
        var count = BenchmarkSizes.Count(Size);
        MeasuredProcess.Record(scenario, count);
        _query = InstanceCreator.CompileForExecution(
            scenario.Sql,
            "benchmark",
            FixtureCatalog.Prepare(scenario, count).CreateSchemaProvider(),
            BenchmarkLogger.Instance,
            ExecutionSettings.Create(scenario, count)
        );
        if (scenario.MembershipIds is { } keys)
            _query.TypedParameters.SetCollection("ids", keys);
    }

    [Benchmark]
    public long Execute()
    {
        using var table = _query.Run();
        long count = 0;
        var visitor = new ValueConsumer(_consumer);
        foreach (var row in table)
        {
            row.VisitColumns(ref visitor);
            count++;
        }
        return count;
    }

    [GlobalCleanup]
    public void Cleanup() => _query.Dispose();

    private readonly struct ValueConsumer(Consumer consumer) : IColumnValueVisitor
    {
        public void Visit<T>(int columnIndex, in T value) => consumer.Consume(value);
    }
}

public class VectorizationBenchmark : QueryExecutionBenchmark
{
    public override IEnumerable<string> Scenarios => VectorizationScenarios.All.Select(scenario => scenario.Id);
}

public class ExecutionPlanBenchmark : QueryExecutionBenchmark
{
    public override IEnumerable<string> Scenarios => ExecutionPlanScenarios.All.Select(scenario => scenario.Id);
}

public sealed class BenchmarkLogger : ILoggerResolver
{
    public static BenchmarkLogger Instance { get; } = new();

    public ILogger ResolveLogger() => NullLogger.Instance;

    public ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
}
