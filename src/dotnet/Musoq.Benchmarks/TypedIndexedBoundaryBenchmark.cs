using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TypedIndexedBoundaryBenchmark
{
    private ICompiledTypedQuery<OutputRow> _compiled = null!;
    private MusoqSourceRows _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, 10_000).Select(static index => new InputRow(index,
            [index % 7 + 1m], new Dictionary<string, decimal> { ["amount"] = index % 7 + 1m })).ToArray();
        _source = MusoqApi.Source("#input", "rows", BenchmarkSourceChunks.Create(rows));
        _compiled = MusoqApi.Query("select a.Position as Position, a.Amounts[0] + a.Costs['amount'] as Amount from #input.rows() a")
            .Source<InputRow>("#input", "rows")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None))
            .Compile<OutputRow>();
        if (ConsumeIndexedValues() != rows.Sum(static row => row.Amounts[0] * 2))
            throw new InvalidOperationException("Indexed benchmark differs from the independent reference.");
    }

    [Benchmark]
    public decimal ConsumeIndexedValues()
    {
        decimal checksum = 0;
        foreach (var row in _compiled.Run(CancellationToken.None, _source))
            checksum += row.Amount;
        return checksum;
    }

    public sealed record InputRow(int Position, decimal[] Amounts, Dictionary<string, decimal> Costs);
    public readonly record struct OutputRow(int Position, decimal Amount);
}
