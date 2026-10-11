using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class LibraryNativeResultCleanupBenchmark
{
    private const int RowCount = 10_000;
    private CompiledQuery _query = null!;
    private long _expected;

    [Params(false, true)]
    public bool MaterializedMixedSet { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var left = Enumerable.Range(0, RowCount).Select(index => new NonEquiEntity
            { Id = index, Name = "A", Population = index * 2 }).ToList();
        var right = Enumerable.Range(0, RowCount).Select(index => new NonEquiEntity
            { Id = index, Name = "B", Population = index * 2 + 1 }).ToList();
        var query = MaterializedMixedSet
            ? "with combined as (select a.Id as Id, a.Population as Value from #test.A() a " +
              "union all (Id, Value) select b.Id as Id, case when b.Id % 3 = 0 then null else b.Population end as Value " +
              "from #test.B() b) select Id, Value from combined order by Id"
            : "select a.Id as Id, a.Population as Value from #test.A() a order by Id";
        _query = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"),
            new LowSelectivitySchemaProvider(left, right), new BenchmarkLoggerResolver(),
            BenchmarkCompilationOptions.Materialized());
        _expected = MaterializedMixedSet ? RowCount * 2 : RowCount;
        for (var index = 0; index < RowCount; index++)
        {
            _expected += (long)index * 31 + index * 2;
            if (MaterializedMixedSet) _expected += (long)index * 31 + (index % 3 == 0 ? 0 : index * 2 + 1);
        }
        if (Execute() != _expected) throw new InvalidOperationException("Native result benchmark differs from its independent reference.");
    }

    [Benchmark]
    public long Execute()
    {
        using var table = _query.Run();
        var visitor = new ChecksumVisitor();
        foreach (var row in table) row.VisitColumns(ref visitor);
        return visitor.Total + table.Count;
    }

    [GlobalCleanup]
    public void Cleanup() => _query.Dispose();

    private struct ChecksumVisitor : IColumnValueVisitor
    {
        public long Total;
        public void Visit<T>(int columnIndex, in T value)
        {
            var copy = value;
            long number;
            if (typeof(T) == typeof(int)) number = System.Runtime.CompilerServices.Unsafe.As<T, int>(ref copy);
            else if (typeof(T) == typeof(int?)) number = System.Runtime.CompilerServices.Unsafe.As<T, int?>(ref copy).GetValueOrDefault();
            else throw new InvalidOperationException($"Unexpected column type {typeof(T)}.");
            Total += columnIndex == 0 ? number * 31 : number;
        }
    }
}
