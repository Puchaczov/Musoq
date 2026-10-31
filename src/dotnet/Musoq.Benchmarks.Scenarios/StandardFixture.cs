namespace Musoq.Benchmarks.Scenarios;

internal static class StandardFixture
{
    internal static FixtureRequirements Requirements(Scenario scenario, int count)
    {
        _ = StandardDataProfiles.Get(scenario.DataProfileId);
        int? iterations = null;
        if (scenario.RecursiveTerminationSteps is { } steps)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(steps);
            iterations = Math.Max(1, checked(count + steps));
        }
        return new(scenario.ValidatePrimitiveTypes, iterations, FixtureSchema.StructuredLimits(count));
    }

    internal static PreparedFixture Prepare(Scenario scenario, int count)
    {
        var requirements = Requirements(scenario, count);
        var inputs = FixtureInputs.Create(count, profile: StandardDataProfiles.Get(scenario.DataProfileId));
        return new(scenario, count, requirements,
            () => new FixtureProvider(new FixtureSchema(inputs, requirements.StructuralLimits)));
    }
}
