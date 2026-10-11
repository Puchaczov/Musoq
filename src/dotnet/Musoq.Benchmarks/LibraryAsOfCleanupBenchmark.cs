using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class LibraryAsOfCleanupBenchmark
{
    private const int RowCount = 10_000;
    private CompiledQuery _query = null!;
    private long _expected;

    [Params(false, true)]
    public bool CompositeKeys { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var predicate = CompositeKeys
            ? "a.Name = b.Name and a.Id % 7 = b.Id % 7 and a.Population >= b.Population"
            : "a.Population >= b.Population";
        var left = Enumerable.Range(0, RowCount).Select(index => new NonEquiEntity
            { Id = index, Name = $"Group{index % 100}", Population = index * 2 }).ToList();
        var right = Enumerable.Range(0, RowCount).Select(index => new NonEquiEntity
            { Id = index, Name = $"Group{index % 100}", Population = index * 2 + 1 }).ToList();
        var query = $"select a.Id as LeftId, b.Id as RightId from #test.A() a asof join #test.B() b on {predicate}";
        _query = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new LowSelectivitySchemaProvider(left, right), new BenchmarkLoggerResolver(),
            BenchmarkCompilationOptions.Materialized());
        var distance = CompositeKeys ? 700 : 1;
        _expected = RowCount - distance;
        for (var index = distance; index < RowCount; index++)
            _expected += (long)index * 31 + index - distance;
        if (Execute() != _expected) throw new InvalidOperationException("ASOF result differs from the independent reference.");
    }

    [Benchmark]
    public long Execute()
    {
        using var table = _query.Run();
        long checksum = 0;
        foreach (var row in table) checksum += (long)row.GetValue<int>(0) * 31 + row.GetValue<int>(1);
        return checksum + table.Count;
    }

    [GlobalCleanup]
    public void Cleanup() => _query.Dispose();
}
