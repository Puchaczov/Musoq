using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmark to measure Table.Add performance under parallel execution.
///     Tests the impact of lock-free vs lock-based collection access.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class TableLockBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _parallelQuery = null!;
    private CompiledQuery _sequentialQuery = null!;

    [Params(10_000, 100_000)] public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var testData = Enumerable.Range(0, RowsCount).Select(i => new TableTestEntity
        {
            Id = i,
            Name = $"Name{i}",
            Value = i * 10,
            Category = $"Category{i % 10}"
        }).ToList();

        var schemaProvider = new TableTestSchemaProvider(testData);


        _sequentialQuery = InstanceCreator.CompileForExecution(
            @"select Id, Name, Value, Category, HeavyComputation(Value) from #test.entities() where Value > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            new CompilationOptions(ParallelizationMode.None));


        _parallelQuery = InstanceCreator.CompileForExecution(
            @"select Id, Name, Value, Category, HeavyComputation(Value) from #test.entities() where Value > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            new CompilationOptions(ParallelizationMode.Full));
    }

    [Benchmark(Baseline = true)]
    public void Sequential_TableAdd()
    {
        _sequentialQuery.Run();
    }

    [Benchmark]
    public void Parallel_TableAdd()
    {
        _parallelQuery.Run();
    }
}
