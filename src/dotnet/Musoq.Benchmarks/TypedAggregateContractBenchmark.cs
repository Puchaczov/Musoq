using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TypedAggregateContractBenchmark
{
    private ICompiledTypedQuery<AverageRow> _compiled = null!;
    private MusoqSourceRows _source = null!;
    private string _query = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, 10_000).Select(static index =>
            new InputRow(index % 16, index % 7 == 0 ? null : index % 113 - 56)).ToArray();
        _source = MusoqApi.Source("#input", "rows", BenchmarkSourceChunks.Create(rows));
        var integralAverage = typeof(LibraryBase).GetMethod(nameof(LibraryBase.Avg), [typeof(int?), typeof(int)])!;
        var input = "Value::Decimal";
        if (integralAverage.ReturnType == typeof(decimal?))
            input = "Value";
        _query = $"select Partition, Avg({input}) as Average, Avg(distinct {input}) as UniqueAverage from #input.rows() group by Partition";
        _compiled = Compile(_query);

        var expected = rows.GroupBy(static row => row.Partition).Sum(static group =>
            group.Where(static row => row.Value.HasValue).Select(static row => (decimal)row.Value!.Value).Average() +
            group.Where(static row => row.Value.HasValue).Select(static row => (decimal)row.Value!.Value).Distinct().Average());
        if (ConsumeGroupedAverages() != expected)
            throw new InvalidOperationException("Aggregate benchmark differs from the independent decimal reference.");
    }

    [Benchmark]
    public decimal ConsumeGroupedAverages() => Consume(_compiled);

    [Benchmark]
    public decimal CompileAndConsumeAverages_Cold()
    {
        var identity = Guid.NewGuid().ToString("N");
        var query = _query.Replace(" group by", $" where '{identity}' = '{identity}' group by", StringComparison.Ordinal);
        return Consume(Compile(query));
    }

    private decimal Consume(ICompiledTypedQuery<AverageRow> compiled)
    {
        var checksum = 0m;
        foreach (var row in compiled.Run(CancellationToken.None, _source))
            checksum += row.Average.GetValueOrDefault() + row.UniqueAverage.GetValueOrDefault();
        return checksum;
    }

    private static ICompiledTypedQuery<AverageRow> Compile(string query)
        => MusoqApi.Query(query).Source<InputRow>("#input", "rows")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None)).Compile<AverageRow>();

    public sealed record InputRow(int Partition, int? Value);
    public readonly record struct AverageRow(int Partition, decimal? Average, decimal? UniqueAverage);
}
