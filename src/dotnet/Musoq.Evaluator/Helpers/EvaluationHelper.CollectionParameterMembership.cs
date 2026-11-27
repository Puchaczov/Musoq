using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static bool CollectionParameterContains<T>(T value, IReadOnlyList<T> values) =>
        CollectionParameterMembership(value, values) == true;

    public static bool CollectionParameterNotContains<T>(T value, IReadOnlyList<T> values) =>
        CollectionParameterMembership(value, values) == false && value is not null;

    public static bool? NullableProbeCollectionMembership<T>(T? value, IReadOnlyList<T> values)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(values);
        return value.HasValue ? CollectionParameterContains(value.Value, values)
            : values.Count == 0 ? false
            : null;
    }

    public static bool? CollectionParameterMembership<T>(T value, IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (value is null)
            return values.Count == 0 ? false : null;

        if (values is CapturedInt32Collection integers && value is int key)
            return integers.Matches(key);

        var comparer = EqualityComparer<T>.Default;
        var containsNull = false;
        for (var index = 0; index < values.Count; index++)
        {
            var item = values[index];
            if (item is null)
                containsNull = true;
            else if (comparer.Equals(value, item))
                return true;
        }

        return containsNull ? null : false;
    }
}
