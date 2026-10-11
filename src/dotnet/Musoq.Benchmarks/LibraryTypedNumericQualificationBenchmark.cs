using System.Numerics;
using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

/// <summary>Measures full typed result consumption; BenchmarkDotNet generates derived execution runners.</summary>
[GenericTypeArguments(typeof(byte))]
[GenericTypeArguments(typeof(sbyte))]
[GenericTypeArguments(typeof(short))]
[GenericTypeArguments(typeof(ushort))]
[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(uint))]
[GenericTypeArguments(typeof(long))]
[GenericTypeArguments(typeof(ulong))]
[GenericTypeArguments(typeof(decimal))]
[GenericTypeArguments(typeof(float))]
[GenericTypeArguments(typeof(double))]
[MemoryDiagnoser]
public class LibraryTypedNumericQualificationBenchmark<T> where T : struct, INumber<T>
{
    private ICompiledTypedQuery<Result> _query = null!;
    private MusoqSourceRows _source = null!;
    private decimal _expected;

    [Params(1000, 10000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, RowCount).Select(static index => new Input(index % 16,
            index % 7 == 0 ? null : T.CreateChecked(index < 16 ? index : 0))).ToArray();
        _source = MusoqApi.Source("#rows", "items", new IReadOnlyList<Input>[] { rows });
        const string sql = "select Partition, Sum(Value)::decimal as Total, Min(Value)::decimal as Minimum, " +
            "Max(Value)::decimal as Maximum, Count(Value) as Present from #rows.items() group by Partition";
        _query = MusoqApi.Query(sql).Source<Input>("#rows", "items")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None))
            .Compile<Result>();
        var expected = rows.GroupBy(static row => row.Partition).ToDictionary(static group => group.Key,
            static group => Reference(group.Key, group));
        _expected = expected.Values.Sum(static row => row.Total.GetValueOrDefault() +
            row.Minimum.GetValueOrDefault() + row.Maximum.GetValueOrDefault() + row.Present);
        var actual = _query.Run(CancellationToken.None, _source).ToArray();
        if (actual.Length != expected.Count || actual.Any(row => row != expected[row.Partition]) || Consume() != _expected)
            throw new InvalidOperationException("Numeric qualification differs from independent typed aggregate results.");
    }

    private static Result Reference(int partition, IEnumerable<Input> rows)
    {
        var values = rows.Where(static row => row.Value.HasValue)
            .Select(static row => decimal.CreateChecked(row.Value!.Value)).ToArray();
        return new Result(partition, values.Sum(), values.Min(), values.Max(), values.LongLength);
    }

    [Benchmark]
    public decimal Consume()
    {
        var checksum = 0m;
        foreach (var row in _query.Run(CancellationToken.None, _source))
            checksum += row.Total.GetValueOrDefault() + row.Minimum.GetValueOrDefault() +
                row.Maximum.GetValueOrDefault() + row.Present;
        return checksum;
    }

    public sealed record Input(int Partition, T? Value);
    public readonly record struct Result(int Partition, decimal? Total, decimal? Minimum, decimal? Maximum, long Present);
}
