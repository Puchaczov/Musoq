using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

public readonly record struct ProjectionOutput(int Id, string Name);
public readonly record struct GroupOutput(int Id, decimal? Total, long Present);
public readonly record struct WindowOutput(int Id, decimal? Total, decimal? First);

[MemoryDiagnoser]
public abstract class TypedOutputBenchmark<T>
{
    private CompiledTypedQuery<T> _query = null!;
    private readonly Consumer _consumer = new();
    public string ScenarioId => BenchmarkRegistrations.TypedScenario<T>();
    public IEnumerable<string> Sizes => BenchmarkSizes.Labels;
    [ParamsSource(nameof(Sizes))]
    public string Size { get; set; } = "Small";

    [GlobalSetup]
    public void Setup()
    {
        var scenario = ScenarioCatalog.Get(ScenarioId);
        var count = BenchmarkSizes.Count(Size);
        MeasuredProcess.Record(scenario, count);
        _query = InstanceCreator.CompileForTypedExecution<T>(scenario.Sql, "typed", FixtureCatalog.Prepare(scenario, count).CreateSchemaProvider(), BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, count));
    }

    [Benchmark]
    public long Execute()
    {
        long count = 0;
        foreach (var row in _query.Run())
        {
            _consumer.Consume(row);
            count++;
        }
        return count;
    }
}

public class TypedProjectionBenchmark : TypedOutputBenchmark<ProjectionOutput>
{
}
public class TypedGroupBenchmark : TypedOutputBenchmark<GroupOutput>
{
}
public class TypedWindowBenchmark : TypedOutputBenchmark<WindowOutput>
{
}
