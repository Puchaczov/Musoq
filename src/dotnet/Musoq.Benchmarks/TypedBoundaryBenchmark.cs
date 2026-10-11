using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TypedBoundaryBenchmark
{
    private ICompiledTypedQuery<ValueRow> _typed = null!;
    private MusoqSourceRows _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        IReadOnlyList<InputRow>[] chunks = [Enumerable.Range(0, 10_000).Select(static index => new InputRow(index)).ToArray()];
        _source = MusoqApi.Source("#input", "rows", chunks);
        _typed = Compile();
        _typed.TypedParameters.Set("offset", 7);
        if (TypedSlots() != 50_065_000)
            throw new InvalidOperationException("Parameter benchmark does not match the arithmetic-series reference.");
    }

    [Benchmark]
    public long TypedSlots() => Consume(_typed);

    private long Consume(ICompiledTypedQuery<ValueRow> query)
    {
        long total = 0;
        foreach (var row in query.Run(CancellationToken.None, _source)) total += row.Value;
        return total;
    }

    private static ICompiledTypedQuery<ValueRow> Compile() => MusoqApi.Query(
            "param(offset: int); select Value + $offset as Value from #input.rows()")
        .Source<InputRow>("#input", "rows")
        .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None))
        .Compile<ValueRow>();

    public readonly record struct InputRow(int Value);
    public readonly record struct ValueRow(int Value);
}
