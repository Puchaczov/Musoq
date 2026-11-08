using System.Collections.Generic;

namespace Musoq.Evaluator.Tests;

internal static class GeneratedCodeSamplesCatalog
{
    internal const int ExpectedSampleCount = EvaluatorSampleFixtures.ExpectedSampleCount;
    public static IReadOnlyList<GeneratedCodeSample> Samples => EvaluatorSampleFixtures.Samples;
    public static IReadOnlyList<GeneratedCodeSample> InterpretationSamples => EvaluatorSampleFixtures.InterpretationSamples;
    public static GeneratedCodeSample GetByFileName(string fileName) => EvaluatorSampleFixtures.GetByFileName(fileName);
    internal static Musoq.Evaluator.Tests.Schema.Basic.BasicSchemaProvider<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> CreateBasicSchemaProvider() => EvaluatorSampleFixtures.CreateBasicSchemaProvider();
}
