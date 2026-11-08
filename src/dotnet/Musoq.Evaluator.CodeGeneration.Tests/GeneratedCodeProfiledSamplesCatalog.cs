using System.Collections.Generic;

namespace Musoq.Evaluator.Tests;

internal static class GeneratedCodeProfiledSamplesCatalog
{
    public static IReadOnlyList<GeneratedCodeSample> Samples => EvaluatorProfiledSampleFixtures.Samples;
    public static GeneratedCodeSample GetByFileName(string fileName) => EvaluatorProfiledSampleFixtures.GetByFileName(fileName);
}
