using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Benchmarks.Scenarios;

internal static class ComputedProjectionFixture
{
    internal static FixtureRequirements Requirements(Scenario scenario, int count) =>
        new(scenario.ValidatePrimitiveTypes, null, StructuralInputLimits.Default);

    internal static PreparedFixture Prepare(Scenario scenario, int count)
    {
        var chunks = Enumerable.Range(0, count).Chunk(4096).ToArray();
        return new(scenario, count, Requirements(scenario, count), () => new ComputedProjectionProvider(chunks));
    }

    internal static string Distribution(Scenario scenario) =>
        "Prepared native Id chunks, Id=0..N-1, chunk size 4096; accepted and residual sources contain identical inputs. " +
        "Both use query-scoped typed transfer; only accepted advertises portable computation. Output Value=(long)Id*2, with SQL cast nullability.";
}

public sealed class ComputedProjectionProvider(int[][] chunks) : ISchemaProvider
{
    public ComputedProjectionSchema Schema { get; } = new(chunks);
    public ISchema GetSchema(string schema) => schema.TrimStart('#').Equals("computed", StringComparison.OrdinalIgnoreCase)
        ? Schema : throw new InvalidOperationException($"Unknown computed fixture schema '{schema}'.");
}
