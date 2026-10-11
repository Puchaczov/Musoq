using System;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static bool HasDeclaredResultContract(string fileName, string content)
    {
        var output = GeneratedCodeSamplesCatalog.GetByFileName(fileName).OutputType;
        if (output is null)
            return content.Contains("BaseOperations, ITableRunnable,", StringComparison.Ordinal);
        var type = Musoq.Evaluator.Helpers.EvaluationHelper.GetCastableType(output);
        return content.Contains($"BaseOperations, ITypedRunnable<{type}>", StringComparison.Ordinal) &&
               content.Contains("ITypedParameterizedRunnable", StringComparison.Ordinal);
    }
}
