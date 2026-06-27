using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
///     Text schema provider for benchmarks.
/// </summary>
public class BenchmarkTextSchemaProvider(IDictionary<string, IEnumerable<IReadOnlyList<BenchmarkTextEntity>>> values)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (values.TryGetValue(schema, out var entities)) return new BenchmarkTextSchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
