using System.Collections.Generic;

namespace Musoq.Plugins;

internal static class WindowMinMaxAccumulatorCore
{
    public static TResult GetExtremeValue<TInput, TResult>(
        IEnumerable<TInput> values,
        bool compareLessThan)
    {
        IComparable? current = null;
        foreach (var value in values)
        {
            if (value is not IComparable comparable)
                continue;

            if (current == null ||
                (compareLessThan ? comparable.CompareTo(current) < 0 : comparable.CompareTo(current) > 0))
            {
                current = comparable;
            }
        }

        return current == null ? default! : (TResult)current;
    }
}
