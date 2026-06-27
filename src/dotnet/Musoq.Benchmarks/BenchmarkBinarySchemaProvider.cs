using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
///     Binary schema provider for benchmarks.
/// </summary>
public class BenchmarkBinarySchemaProvider(IDictionary<string, IEnumerable<IReadOnlyList<BenchmarkBinaryEntity>>> values)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (values.TryGetValue(schema, out var entities)) return new BenchmarkBinarySchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
