using Musoq.Schema;

namespace Musoq.Benchmarks;

public sealed class OptimizationBenchmarkSchemaProvider(
    IReadOnlyDictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> rowsBySchema,
    OptimizationBenchmarkPlanningMode mode = OptimizationBenchmarkPlanningMode.RejectAll)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!rowsBySchema.TryGetValue(schema, out var rows) &&
            !rowsBySchema.TryGetValue(NormalizeSchemaName(schema), out rows))
        {
            throw new KeyNotFoundException($"No optimization benchmark schema rows registered for '{schema}'.");
        }

        return new OptimizationBenchmarkSchema(NormalizeSchemaName(schema), rows, mode);
    }

    private static string NormalizeSchemaName(string schema)
    {
        return schema.TrimStart('#');
    }
}
