using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class SimpleQueryExecutionBenchmark
{
    private CompiledQuery _query = null!;

    [GlobalSetup]
    public void Setup()
    {
        _query = InstanceCreator.CompileForExecution(
            "select v.Value from values { (Value: 1), (Value: 2), (Value: 3) } v",
            Guid.NewGuid().ToString(),
            new InlineSchemaProvider(),
            new BenchmarkLoggerResolver());

        using var table = _query.Run();
        if (table.Count != 3 || table[0].GetValue<int>(0) != 1 ||
            table[1].GetValue<int>(0) != 2 || table[2].GetValue<int>(0) != 3)
        {
            throw new InvalidOperationException("Expected the inline query to return exactly 1, 2, 3.");
        }
    }

    [Benchmark]
    public int Execute()
    {
        using var table = _query.Run();
        var checksum = 0;
        foreach (var row in table)
        {
            checksum += row.GetValue<int>(0);
        }

        return checksum;
    }

    [GlobalCleanup]
    public void Cleanup() => _query.Dispose();

    private sealed class InlineSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) =>
            throw new InvalidOperationException($"The inline benchmark does not use datasource '{schema}'.");
    }

    private sealed class BenchmarkLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger() => NullLogger.Instance;

        public ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
    }
}
