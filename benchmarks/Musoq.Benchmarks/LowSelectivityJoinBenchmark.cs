using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class LowSelectivityJoinBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(2000, 5000)] public int RowsCount { get; set; }

    [Params(true, false)] public bool UseSortMergeJoin { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var offset = (int)(RowsCount * 0.95);

        var entitiesA = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Name{i}",
            Population = i
        }).ToList();

        var entitiesB = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Name{i}",
            Population = i
        }).ToList();

        var schemaProvider = new LowSelectivitySchemaProvider(entitiesA, entitiesB);

        var script = $@"
                select 
                    1
                from #test.A() a
                inner join #test.B() b on a.Population > b.Population + {offset}";

        _query = InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            new CompilationOptions(useSortMergeJoin: UseSortMergeJoin)
        );
    }

    [Benchmark]
    public void RunQuery()
    {
        _query.Run();
    }
}
