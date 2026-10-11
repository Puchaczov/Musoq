using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TypedProfileBoundaryBenchmark
{
    private ICompiledTypedProfileQuery<ValueRow> _compiled = null!;
    private MusoqSourceRows _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        IReadOnlyList<InputRow>[] chunks = [Enumerable.Range(0, 10_000).Select(static index => new InputRow(index)).ToArray()];
        _source = MusoqApi.Source("#input", "rows", chunks);
        _compiled = MusoqApi.Query("select Value from #input.rows()")
            .Source<InputRow>("#input", "rows")
            .WithCompilationOptions(new CompilationOptions(parallelizationMode: ParallelizationMode.None))
            .CompileForProfile<ValueRow>();
        if (ConsumeProfiledDtoRows() != 49_995_000)
            throw new InvalidOperationException("Profile benchmark does not match the arithmetic-series reference.");
    }

    [Benchmark]
    public long ConsumeProfiledDtoRows()
    {
        long total = 0;
        foreach (var row in _compiled.RunWithProfile(CancellationToken.None, _source).Rows) total += row.Value;
        return total;
    }

    public readonly record struct InputRow(int Value);
    public readonly record struct ValueRow(int Value);
}
