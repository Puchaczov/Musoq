using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionIrCollections
{
    public static IReadOnlyList<T> Freeze<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }

    public static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }

    public static IReadOnlyList<IReadOnlyList<T>> FreezeNested<T>(
        IEnumerable<IReadOnlyList<T>> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.Select(Freeze).ToArray());
    }

    public static IReadOnlyDictionary<TKey, TValue> Freeze<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> values)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(values);
        return new ReadOnlyDictionary<TKey, TValue>(
            values.ToDictionary(static pair => pair.Key, static pair => pair.Value));
    }
}
