using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TypedNumericWindowContractBenchmark
{
    private ICompiledTypedQuery<OutputRow> _running = null!;
    private ICompiledTypedQuery<OutputRow> _bounded = null!;
    private MusoqSourceRows _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, 10_000).Select(static index => new InputRow(index, index % 16, 1m + index % 7)).ToArray();
        _source = MusoqApi.Source("#input", "rows", BenchmarkSourceChunks.Create(rows));
        _running = Compile("unbounded preceding");
        _bounded = Compile("1 preceding");
        var totals = new decimal[16];
        var previous = new decimal[16];
        var running = 0m;
        var bounded = 0m;
        foreach (var row in rows)
        {
            totals[row.Partition] += row.Value;
            running += totals[row.Partition];
            bounded += previous[row.Partition] + row.Value;
            previous[row.Partition] = row.Value;
        }
        if (RunningSum() != running || BoundedSum() != bounded)
            throw new InvalidOperationException("Window contract benchmark differs from the independent reference.");
    }

    [Benchmark] public decimal RunningSum() => Consume(_running);
    [Benchmark] public decimal BoundedSum() => Consume(_bounded);

    private ICompiledTypedQuery<OutputRow> Compile(string start)
        => MusoqApi.Query($"select Position, Sum(Value) over (partition by Partition order by Position rows between {start} and current row) as Total from #input.rows()")
            .Source<InputRow>("#input", "rows")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None)).Compile<OutputRow>();

    private decimal Consume(ICompiledTypedQuery<OutputRow> compiled)
    {
        var checksum = 0m;
        foreach (var row in compiled.Run(CancellationToken.None, _source))
            checksum += row.Total.GetValueOrDefault();
        return checksum;
    }

    public sealed record InputRow(int Position, int Partition, decimal Value);
    public readonly record struct OutputRow(int Position, decimal? Total);
}
