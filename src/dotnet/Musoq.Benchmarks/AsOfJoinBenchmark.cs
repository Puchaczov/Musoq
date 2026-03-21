using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class AsOfJoinBenchmark
{
    public enum AsOfVariant
    {
        InequalityOnly,
        EqualityAndInequality
    }

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(1000, 5000, 10000)]
    public int RowsCount { get; set; }

    [Params(AsOfVariant.InequalityOnly, AsOfVariant.EqualityAndInequality)]
    public AsOfVariant Variant { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var script = Variant switch
        {
            AsOfVariant.InequalityOnly => @"
                select
                    a.Name,
                    b.Name
                from #test.A() a
                asof join #test.B() b on a.Population >= b.Population",

            AsOfVariant.EqualityAndInequality => @"
                select
                    a.Name,
                    b.Name
                from #test.A() a
                asof join #test.B() b on a.Name = b.Name and a.Population >= b.Population",

            _ => throw new ArgumentOutOfRangeException()
        };

        var entitiesA = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Group{i % 100}",
            Population = i * 2
        }).ToList();

        var entitiesB = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Group{i % 100}",
            Population = i * 2 + 1
        }).ToList();

        var schemaProvider = new LowSelectivitySchemaProvider(entitiesA, entitiesB);

        _query = InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);
    }

    [Benchmark]
    public void RunQuery()
    {
        _query.Run();
    }
}
