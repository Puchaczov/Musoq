using Musoq.Schema;

namespace Musoq.Benchmarks.Scenarios;

public static class FixtureCatalog
{
    private static readonly IReadOnlyDictionary<string, FixtureDefinition> Fixtures =
        new Dictionary<string, FixtureDefinition>(StringComparer.Ordinal)
        {
            ["standard"] = new(StandardFixture.Prepare, StandardFixture.Requirements, StandardDataProfiles.Distribution)
        };

    public static PreparedFixture Prepare(Scenario scenario, int inputCount) =>
        Definition(scenario, inputCount).Prepare(scenario, inputCount);

    public static FixtureRequirements GetRequirements(Scenario scenario, int inputCount) =>
        Definition(scenario, inputCount).Requirements(scenario, inputCount);

    public static string GetDistribution(Scenario scenario) => Definition(scenario, 0).Distribution(scenario);

    private static FixtureDefinition Definition(Scenario scenario, int inputCount)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentOutOfRangeException.ThrowIfNegative(inputCount);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario.FixtureId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario.DataProfileId);
        return Fixtures.TryGetValue(scenario.FixtureId, out var definition)
            ? definition
            : throw new ArgumentException($"Unknown benchmark fixture '{scenario.FixtureId}'.", nameof(scenario));
    }

    private sealed record FixtureDefinition(Func<Scenario, int, PreparedFixture> Prepare,
        Func<Scenario, int, FixtureRequirements> Requirements, Func<Scenario, string> Distribution);
}

public sealed class PreparedFixture
{
    private readonly Func<ISchemaProvider> _createProvider;

    internal PreparedFixture(Scenario scenario, int inputCount, FixtureRequirements requirements,
        Func<ISchemaProvider> createProvider)
    {
        Scenario = scenario;
        InputCount = inputCount;
        Distribution = FixtureCatalog.GetDistribution(scenario);
        Requirements = requirements;
        _createProvider = createProvider;
    }

    public Scenario Scenario { get; }
    public int InputCount { get; }
    public string Distribution { get; }
    public FixtureRequirements Requirements { get; }
    public ISchemaProvider CreateSchemaProvider() => _createProvider();
}
