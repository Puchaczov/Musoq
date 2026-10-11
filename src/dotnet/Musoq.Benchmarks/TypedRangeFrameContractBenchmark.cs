using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TypedRangeFrameContractBenchmark
{
    public const int RowCount = 4096;
    private ICompiledTypedQuery<OutputRow> _query = null!;
    private MusoqSourceRows _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        var input = Enumerable.Range(0, RowCount).Select(index => new InputRow(index, index / 4, 1m + index % 7)).ToArray();
        _source = MusoqApi.Source("#input", "rows", BenchmarkSourceChunks.Create(input));
        _query = MusoqApi.Query("select Position, Count(Position) over (order by Key range between 2 preceding and 1 following) as Present, " +
            "Sum(Amount) over (order by Key range between 2 preceding and 1 following) as Total from #input.rows() order by Position")
            .Source<InputRow>("#input", "rows")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None)).Compile<OutputRow>();
        var actual = _query.Run(CancellationToken.None, _source).ToArray();
        if (actual.Length != RowCount) throw new InvalidOperationException("Range benchmark row count differs.");
        for (var index = 0; index < RowCount; index++)
        {
            var first = Math.Max(0, input[index].Key - 2) * 4;
            var end = Math.Min(RowCount, (input[index].Key + 2) * 4);
            decimal total = 0;
            for (var frame = first; frame < end; frame++) total += input[frame].Amount;
            if (actual[index].Position != index || actual[index].Present != end - first || actual[index].Total != total)
                throw new InvalidOperationException("Range benchmark differs from its independent frame reference.");
        }
    }

    [Benchmark]
    public decimal CompleteOffsetRangeConsumption()
    {
        decimal checksum = 0;
        foreach (var row in _query.Run(CancellationToken.None, _source))
            checksum += row.Position + row.Present + row.Total.GetValueOrDefault();
        return checksum;
    }

    public sealed record InputRow(int Position, int Key, decimal Amount);
    public readonly record struct OutputRow(int Position, long Present, decimal? Total);
}
