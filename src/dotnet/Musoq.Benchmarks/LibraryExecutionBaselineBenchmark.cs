using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class LibraryExecutionBaselineBenchmark
{
    private const int RowCount = 10_000;
    private const int PartitionCount = 16;

    private const string GroupedQuery = """
        select Partition, Sum(Amount) as Total from #readings.items() group by Partition
        """;
    private const string RunningQuery = """
        select Position, Sum(Amount) over (
            partition by Partition order by Position
            rows between unbounded preceding and current row) as Total
        from #readings.items()
        """;
    private const string BoundedQuery = """
        select Position, Sum(Amount) over (
            partition by Partition order by Position
            rows between 1 preceding and current row) as Total
        from #readings.items()
        """;

    private IReadOnlyList<Reading>[] _chunks = null!;
    private MusoqSourceRows _source = null!;
    private ICompiledTypedQuery<GroupTotal> _grouped = null!;
    private ICompiledTypedQuery<WindowTotal> _running = null!;
    private ICompiledTypedQuery<WindowTotal> _bounded = null!;

    [GlobalSetup]
    public void Setup()
    {
        _chunks = [Enumerable.Range(0, RowCount)
            .Select(static position => new Reading(position, position % PartitionCount, 1m + position % 7))
            .ToArray()];
        _source = MusoqApi.Source("#readings", "items", _chunks);
        _grouped = Compile<GroupTotal>(GroupedQuery);
        _running = Compile<WindowTotal>(RunningQuery);
        _bounded = Compile<WindowTotal>(BoundedQuery);
        ValidateResults();
    }

    [Benchmark]
    public decimal GroupedSum()
    {
        var checksum = 0m;
        foreach (var row in _grouped.Run(CancellationToken.None, _source))
            checksum += row.Total.GetValueOrDefault();

        return checksum;
    }

    [Benchmark]
    public decimal RunningSum()
    {
        var checksum = 0m;
        foreach (var row in _running.Run(CancellationToken.None, _source))
            checksum += row.Total!.Value;

        return checksum;
    }

    [Benchmark]
    public decimal BoundedSum()
    {
        var checksum = 0m;
        foreach (var row in _bounded.Run(CancellationToken.None, _source))
            checksum += row.Total!.Value;

        return checksum;
    }

    [Benchmark]
    public decimal CompileAndConsumeGroupedSum_Cold()
    {
        var identity = Guid.NewGuid().ToString("N");
        var coldQuery = GroupedQuery.Replace(" group by",
            $" where '{identity}' = '{identity}' group by", StringComparison.Ordinal);
        var query = Compile<GroupTotal>(coldQuery);
        var checksum = 0m;
        foreach (var row in query.Run(CancellationToken.None, _source))
            checksum += row.Total.GetValueOrDefault();

        return checksum;
    }

    private ICompiledTypedQuery<T> Compile<T>(string query)
        => MusoqApi.Query(query)
            .Source<Reading>("#readings", "items")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None))
            .Compile<T>();

    private void ValidateResults()
    {
        var grouped = 0m;
        var running = 0m;
        var bounded = 0m;
        var totals = new decimal[PartitionCount];
        var previous = new decimal[PartitionCount];
        foreach (var row in _chunks[0])
        {
            grouped += row.Amount;
            totals[row.Partition] += row.Amount;
            running += totals[row.Partition];
            bounded += previous[row.Partition] + row.Amount;
            previous[row.Partition] = row.Amount;
        }

        if (GroupedSum() != grouped || RunningSum() != running || BoundedSum() != bounded)
            throw new InvalidOperationException("Library execution benchmark results differ from the independent reference.");
    }

    public sealed record Reading(int Position, int Partition, decimal Amount);
    public readonly record struct GroupTotal(int Partition, decimal? Total);
    public readonly record struct WindowTotal(int Position, decimal? Total);
}
