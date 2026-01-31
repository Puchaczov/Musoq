using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
///     Binary schema provider for benchmarks.
/// </summary>
public class BenchmarkBinarySchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<BenchmarkBinaryEntity>> _values;

    public BenchmarkBinarySchemaProvider(IDictionary<string, IEnumerable<BenchmarkBinaryEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        if (_values.TryGetValue(schema, out var entities)) return new BenchmarkBinarySchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
