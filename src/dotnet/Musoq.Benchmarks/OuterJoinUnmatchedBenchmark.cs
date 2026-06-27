using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class OuterJoinUnmatchedBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(1000, 5000, 10000)]
    public int RowsCount { get; set; }

    [Params(0.25, 0.50, 0.75)]
    public double UnmatchedRatio { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var matchedCount = (int)(RowsCount * (1 - UnmatchedRatio));

        var entitiesA = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Name{i}",
            Population = i
        }).ToList();

        var entitiesB = Enumerable.Range(0, matchedCount).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Country{i}",
            Population = i * 10
        }).ToList();

        var schemaProvider = new LowSelectivitySchemaProvider(entitiesA, entitiesB);

        var script = @"
            select
                a.Name,
                a.Population,
                b.Name,
                b.Population
            from #test.A() a
            left outer join #test.B() b on a.Id = b.Id";

        _query = InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized());
    }

    [Benchmark]
    public Table RunQuery()
    {
        return _query.Run();
    }
}
