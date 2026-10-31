using System.Globalization;
using Musoq.Benchmarks.Scenarios;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

public static class ExecutionSettings
{
    public static CompilationOptions Create(Scenario scenario, int inputCount = FixtureData.TestSize)
    {
        var requirements = FixtureCatalog.GetRequirements(scenario, inputCount);
        var options = new CompilationOptions(usePrimitiveTypeValidation: requirements.ValidatePrimitiveTypes,
            maxDegreeOfParallelismOverride: Environment.ProcessorCount);
        return requirements.RecursiveMaxIterations is { } iterations
            ? options.WithRecursiveCteLimits(new RecursiveCteExecutionLimits(maxIterations: iterations))
            : options;
    }

    public static SortedDictionary<string, string> Arguments(Scenario scenario, int inputCount)
    {
        var options = Create(scenario, inputCount);
        var requirements = FixtureCatalog.GetRequirements(scenario, inputCount);
        var structuralLimits = requirements.StructuralLimits;
        var arguments = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["primitiveTypeValidation"] = requirements.ValidatePrimitiveTypes.ToString(),
            ["maxDegreeOfParallelism"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            ["structuralMaxDepth"] = structuralLimits.MaxDepth.ToString(CultureInfo.InvariantCulture),
            ["structuralMaxNodes"] = structuralLimits.MaxNodes.ToString(CultureInfo.InvariantCulture),
            ["structuralMaxStringBytes"] = structuralLimits.MaxStringBytes.ToString(CultureInfo.InvariantCulture)
        };
        if (scenario.FixtureId != "standard" || scenario.DataProfileId != "default")
        {
            arguments["fixtureId"] = scenario.FixtureId;
            arguments["dataProfileId"] = scenario.DataProfileId;
        }
        if (requirements.RecursiveMaxIterations is null)
            return arguments;
        var limits = options.RecursiveCteLimits;
        arguments["recursiveMaxIterations"] = limits.MaxIterations.ToString(CultureInfo.InvariantCulture);
        arguments["recursiveMaxRows"] = limits.MaxRows.ToString(CultureInfo.InvariantCulture);
        arguments["recursiveMaxSnapshotRows"] = limits.MaxSnapshotRows.ToString(CultureInfo.InvariantCulture);
        return arguments;
    }
}
