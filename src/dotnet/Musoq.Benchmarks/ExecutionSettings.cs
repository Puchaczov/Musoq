using System.Globalization;
using Musoq.Benchmarks.Scenarios;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

public static class ExecutionSettings
{
    public static CompilationOptions Create(Scenario scenario, int inputCount = FixtureData.TestSize)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentOutOfRangeException.ThrowIfNegative(inputCount);
        var options = new CompilationOptions(usePrimitiveTypeValidation: scenario.ValidatePrimitiveTypes,
            maxDegreeOfParallelismOverride: Environment.ProcessorCount);
        if (scenario.RecursiveTerminationSteps is not { } terminalSteps)
            return options;
        ArgumentOutOfRangeException.ThrowIfNegative(terminalSteps);
        var iterations = Math.Max(1, checked(inputCount + terminalSteps));
        return options.WithRecursiveCteLimits(new RecursiveCteExecutionLimits(maxIterations: iterations));
    }

    public static SortedDictionary<string, string> Arguments(Scenario scenario, int inputCount)
    {
        var options = Create(scenario, inputCount);
        var structuralLimits = FixtureSchema.StructuredLimits(inputCount);
        var arguments = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["primitiveTypeValidation"] = scenario.ValidatePrimitiveTypes.ToString(),
            ["maxDegreeOfParallelism"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            ["structuralMaxDepth"] = structuralLimits.MaxDepth.ToString(CultureInfo.InvariantCulture),
            ["structuralMaxNodes"] = structuralLimits.MaxNodes.ToString(CultureInfo.InvariantCulture),
            ["structuralMaxStringBytes"] = structuralLimits.MaxStringBytes.ToString(CultureInfo.InvariantCulture)
        };
        if (scenario.RecursiveTerminationSteps is null)
            return arguments;
        var limits = options.RecursiveCteLimits;
        arguments["recursiveMaxIterations"] = limits.MaxIterations.ToString(CultureInfo.InvariantCulture);
        arguments["recursiveMaxRows"] = limits.MaxRows.ToString(CultureInfo.InvariantCulture);
        arguments["recursiveMaxSnapshotRows"] = limits.MaxSnapshotRows.ToString(CultureInfo.InvariantCulture);
        return arguments;
    }
}
