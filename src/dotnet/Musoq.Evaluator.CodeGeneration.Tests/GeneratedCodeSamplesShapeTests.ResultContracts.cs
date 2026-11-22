using System;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static bool HasDeclaredResultContract(GeneratedCodeSampleFile sample)
    {
        if (sample.OutputTypeName is not { } type)
            return sample.Content.Contains("BaseOperations, ITableRunnable,", StringComparison.Ordinal);
        return sample.Content.Contains($"BaseOperations, ITypedRunnable<{type}>", StringComparison.Ordinal)
            && sample.Content.Contains("ITypedParameterizedRunnable", StringComparison.Ordinal);
    }
}
