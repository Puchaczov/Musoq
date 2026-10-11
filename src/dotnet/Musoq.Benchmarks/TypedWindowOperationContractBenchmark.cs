using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TypedWindowOperationContractBenchmark
{
    private ICompiledTypedQuery<RankOutput> _ranking = null!;
    private ICompiledTypedQuery<ValueOutput> _offset = null!;
    private ICompiledTypedQuery<ValueOutput> _wideFrame = null!;
    private ICompiledTypedQuery<CountOutput> _count = null!;
    private ICompiledTypedQuery<ValueOutput> _extrema = null!;
    private ICompiledTypedQuery<ValueOutput> _peerSum = null!;
    private ICompiledTypedQuery<ValueOutput> _peerAverage = null!;
    private MusoqSourceRows _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, 10_000).Select(static index =>
            new Input(index, index % 16, index % 37, 1m + index % 7)).ToArray();
        _source = MusoqApi.Source("#input", "rows", BenchmarkSourceChunks.Create(rows));
        _ranking = Compile<RankOutput>("Rank() over (partition by Partition order by SortKey)");
        _offset = Compile<ValueOutput>("Lag(Value, 1, 0) over (partition by Partition order by Position)");
        _wideFrame = Compile<ValueOutput>("Sum(Value) over (partition by Partition order by Position rows between 64 preceding and current row)");

        _count = Compile<CountOutput>("ToInt64(Count(Value) over (partition by Partition))");
        _extrema = Compile<ValueOutput>("Min(Value) over (partition by Partition order by Position rows between 64 preceding and current row)");
        _peerSum = Compile<ValueOutput>("Sum(Value) over (partition by Partition order by Position)");
        _peerAverage = Compile<ValueOutput>("Avg(Value) over (partition by Partition order by Position)");
        var running = 0m;
        var average = 0m;
        var averageValues = new decimal[rows.Length];
        var minimum = 0m;
        var rank = 0L;
        var offset = 0m;
        var wide = 0m;
        foreach (var partition in rows.GroupBy(static row => row.Partition))
        {
            var ordered = partition.OrderBy(static row => row.Position).ToArray();
            var keys = partition.Select(static row => row.SortKey).Order().ToArray();
            var total = 0m;
            for (var index = 0; index < ordered.Length; index++)
            {
                total += ordered[index].Value;
                running += total;
                averageValues[ordered[index].Position] = total / (index + 1);
                average += decimal.Round(averageValues[ordered[index].Position], 12, MidpointRounding.ToEven);
                rank += Array.IndexOf(keys, ordered[index].SortKey) + 1L;
                if (index > 0)
                    offset += ordered[index - 1].Value;
                minimum += ordered.Skip(Math.Max(0, index - 64)).Take(Math.Min(65, index + 1))
                    .Min(static row => row.Value);
                for (var frame = Math.Max(0, index - 64); frame <= index; frame++)
                    wide += ordered[frame].Value;
            }
        }
        foreach (var row in _peerAverage.Run(CancellationToken.None, _source))
        {
            if (row.Value != averageValues[row.Position])
                throw new InvalidOperationException($"Average differs at {row.Position}: {row.Value}, expected {averageValues[row.Position]}.");
        }
        if (Ranking() != rank || Offset() != offset || WideFrame() != wide || Extrema() != minimum || WholeCount() != 6_250_000L || PeerSum() != running || PeerAverage() != average)
            throw new InvalidOperationException("Window operation benchmark differs from independent references.");
    }

    [Benchmark]
    public long Ranking()
    {
        var checksum = 0L;
        foreach (var row in _ranking.Run(CancellationToken.None, _source))
            checksum += row.Value;
        return checksum;
    }

    [Benchmark] public long WholeCount()
    {
        var checksum = 0L;
        foreach (var row in _count.Run(CancellationToken.None, _source))
            checksum += row.Value.GetValueOrDefault();
        return checksum;
    }

    [Benchmark]
    public decimal CompilePeerAverage_Cold()
    {
        var identity = Guid.NewGuid().ToString("N");
        var query = MusoqApi.Query($"select Position, Avg(Value) over (partition by Partition order by Position) as Value from #input.rows() where '{identity}' = '{identity}'")
            .Source<Input>("#input", "rows")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None)).Compile<ValueOutput>();
        return Consume(query);
    }

    [Benchmark] public decimal PeerSum() => Consume(_peerSum);
    [Benchmark] public decimal PeerAverage()
    {
        var checksum = 0m;
        foreach (var row in _peerAverage.Run(CancellationToken.None, _source))
            checksum += decimal.Round(row.Value.GetValueOrDefault(), 12, MidpointRounding.ToEven);
        return checksum;
    }
    [Benchmark] public decimal Extrema() => Consume(_extrema);
    [Benchmark] public decimal Offset() => Consume(_offset);
    [Benchmark] public decimal WideFrame() => Consume(_wideFrame);

    private decimal Consume(ICompiledTypedQuery<ValueOutput> query)
    {
        var checksum = 0m;
        foreach (var row in query.Run(CancellationToken.None, _source))
            checksum += row.Value.GetValueOrDefault();
        return checksum;
    }

    private static ICompiledTypedQuery<T> Compile<T>(string expression)
        => MusoqApi.Query($"select Position, {expression} as Value from #input.rows()")
            .Source<Input>("#input", "rows")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None)).Compile<T>();

    public sealed record Input(int Position, int Partition, int SortKey, decimal Value);
    public readonly record struct CountOutput(int Position, long? Value);
    public readonly record struct RankOutput(int Position, long Value);
    public readonly record struct ValueOutput(int Position, decimal? Value);
}
