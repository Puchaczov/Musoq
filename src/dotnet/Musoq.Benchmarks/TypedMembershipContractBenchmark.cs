using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class TypedMembershipContractBenchmark
{
    private ICompiledTypedQuery<Match> _query = null!;
    private MusoqSourceRows _source = null!;
    private int _expected;

    [Params("in", "contains")]
    public string Operation { get; set; } = "in";

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, 4096)
            .Select(static i => new Input(i % 11 == 0 ? null : i % 7,
                i % 13 == 0 ? null : i % 5, i % 3)).ToArray();
        _source = MusoqApi.Source("#rows", "items", new IReadOnlyList<Input>[] { rows });
        _query = MusoqApi.Query($"select Value {Operation} (First, Second) as Matched from #rows.items()")
            .Source<Input>("#rows", "items")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None))
            .Compile<Match>();
        _expected = rows.Count(row => (Operation == "contains" || row.Value.HasValue) &&
            (row.Value == row.First || row.Value == row.Second));
        var actual = Consume();
        if (actual != _expected)
            throw new InvalidOperationException($"Membership result {actual} differs from independent expected {_expected}.");
    }

    [Benchmark]
    public int Consume()
    {
        var matches = 0;
        foreach (var row in _query.Run(CancellationToken.None, _source))
            if (row.Matched == true)
                matches++;
        return matches;
    }

    public sealed record Input(int? Value, int? First, int? Second);
    public readonly record struct Match(bool? Matched);
}
