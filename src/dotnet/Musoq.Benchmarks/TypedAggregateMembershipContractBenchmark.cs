using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class TypedAggregateMembershipContractBenchmark
{
    private ICompiledTypedQuery<Result> _query = null!;
    private MusoqSourceRows _source = null!;
    private long _expected;

    [Params("filter", "input")]
    public string Operation { get; set; } = "filter";

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, 4096)
            .Select(static index => new Input(index % 11 == 0 ? null : index % 3 == 0 ? "match" : "other"))
            .ToArray();
        _source = MusoqApi.Source("#rows", "items", new IReadOnlyList<Input>[] { rows });
        var sql = Operation == "filter"
            ? "select Count(*) filter (where Label not in ('match', null)) as Present from #rows.items()"
            : "select Count(Label in ('match')) as Present from #rows.items()";
        _expected = Operation == "filter" ? 0L : rows.LongCount(static row => row.Label is not null);
        _query = MusoqApi.Query(sql).Source<Input>("#rows", "items")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None))
            .Compile<Result>();
        if (Consume() != _expected)
            throw new InvalidOperationException("Aggregate membership differs from the independent count oracle.");
    }

    [Benchmark]
    public long Consume()
    {
        var count = 0;
        var result = 0L;
        foreach (var row in _query.Run(CancellationToken.None, _source))
        {
            count++;
            result += row.Present;
        }
        if (count != 1)
            throw new InvalidOperationException("Aggregate membership must return exactly one row.");
        return result;
    }

    public sealed record Input(string? Label);
    public readonly record struct Result(long Present);
}
