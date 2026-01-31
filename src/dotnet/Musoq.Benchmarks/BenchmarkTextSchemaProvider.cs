using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
///     Text schema provider for benchmarks.
/// </summary>
public class BenchmarkTextSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<BenchmarkTextEntity>> _values;

    public BenchmarkTextSchemaProvider(IDictionary<string, IEnumerable<BenchmarkTextEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        if (_values.TryGetValue(schema, out var entities)) return new BenchmarkTextSchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
