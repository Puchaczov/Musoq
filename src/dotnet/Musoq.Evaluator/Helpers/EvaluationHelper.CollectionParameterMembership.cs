using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static bool CollectionParameterContains<T>(T value, IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (value is null)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] is not null && comparer.Equals(value, values[index]))
                return true;
        }

        return false;
    }

    public static bool CollectionParameterNotContains<T>(T value, IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (value is null)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] is null || comparer.Equals(value, values[index]))
                return false;
        }

        return true;
    }
}
