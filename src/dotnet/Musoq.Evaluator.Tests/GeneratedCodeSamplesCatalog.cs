using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    public static IReadOnlyList<GeneratedCodeSample> Samples { get; } = CreateSamples();

    public static IReadOnlyList<GeneratedCodeSample> InterpretationSamples { get; } = Samples
        .Where(sample => sample.Format == GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode)
        .ToArray();

    public static GeneratedCodeSample GetByFileName(string fileName)
    {
        return Samples.Single(sample => sample.FileName == fileName);
    }

    private static List<GeneratedCodeSample> CreateSamples()
    {
        var samples = new List<GeneratedCodeSample>();

        samples.AddRange(CreateCoreSamples());
        samples.AddRange(CreateRuntimeV2CorpusSamples());
        samples.AddRange(CreateValuesParametersAndVariablesSamples());
        samples.AddRange(CreateSubquerySamples());
        samples.AddRange(CreateRuntimeV2CastGroupingSamples());
        samples.AddRange(CreateUnpivotSamples());
        samples.AddRange(CreatePivotSamples());
        samples.AddRange(CreateClassicTailSamples());
        samples.AddRange(CreateRecursiveCteSamples());
        samples.AddRange(CreatePerformanceSamples());
        samples.AddRange(CreateRuntimeDynamicSamples());
        samples.Add(NullableProviderMethodLeftJoin());

        return samples;
    }
}
