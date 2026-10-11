using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static IEnumerable<T> WrapScalarForCrossApply<T>(T? value) where T : class
    {
        if (value == null)
            return [];

        return [value];
    }
}
