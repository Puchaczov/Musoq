using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema.Optimization;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class LibraryExecutionConcurrencyBenchmark
{
    private ICompiledTypedQuery<ScalarOutput> _serialScalar = null!;
    private ICompiledTypedQuery<ScalarOutput> _parallelScalar = null!;
    private ICompiledTypedQuery<GroupOutput> _serialCount = null!;
    private ICompiledTypedQuery<GroupOutput> _parallelCount = null!;
    private MusoqSourceRows _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, 10_000)
            .Select(static index => new Input(index, index % 16, $"v{index % 97}")).ToArray();
        _source = MusoqApi.Source("#input", "rows", BenchmarkSourceChunks.Create(rows));
        const string scalar = "select ToUpper(Value) as Value from #input.rows()";
        const string count = "select Partition, Count(Position) as Total from #input.rows() group by Partition";
        _serialScalar = Compile<ScalarOutput>(scalar, ParallelizationMode.None);
        _parallelScalar = Compile<ScalarOutput>(scalar, ParallelizationMode.Full);
        _serialCount = Compile<GroupOutput>(count, ParallelizationMode.None);
        _parallelCount = Compile<GroupOutput>(count, ParallelizationMode.Full);
        var expected = rows.Sum(static row => row.Value.Length);
        if (SerialScalar() != expected || PermittedParallelScalar() != expected ||
            SerialCount() != rows.Length || PermittedParallelCount() != rows.Length)
            throw new InvalidOperationException("Permission benchmark results differ from the independent input reference.");
    }

    [Benchmark]
    public long SerialScalar() => ConsumeScalar(_serialScalar);

    [Benchmark]
    public long PermittedParallelScalar() => ConsumeScalar(_parallelScalar);

    [Benchmark]
    public long SerialCount() => ConsumeCount(_serialCount);

    [Benchmark]
    public long PermittedParallelCount() => ConsumeCount(_parallelCount);

    private long ConsumeScalar(ICompiledTypedQuery<ScalarOutput> query)
    {
        var checksum = 0L;
        foreach (var row in query.Run(CancellationToken.None, _source))
            checksum += row.Value.Length;
        return checksum;
    }

    private long ConsumeCount(ICompiledTypedQuery<GroupOutput> query)
    {
        var checksum = 0L;
        foreach (var row in query.Run(CancellationToken.None, _source))
            checksum += row.Total;
        return checksum;
    }

    private static ICompiledTypedQuery<T> Compile<T>(string query, ParallelizationMode mode)
        => MusoqApi.Query(query)
            .Source<Input>("#input", "rows", SourceExecutionPermissions.ConcurrentRowProcessing)
            .WithCompilationOptions(new CompilationOptions(mode, maxDegreeOfParallelismOverride: 2))
            .Compile<T>();

    public sealed record Input(int Position, int Partition, string Value);
    public readonly record struct ScalarOutput(string Value);
    public readonly record struct GroupOutput(int Partition, long Total);
}
