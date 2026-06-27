using System.Numerics;

namespace Musoq.Plugins;

/// <summary>
///     Typed nullable average kernel used by runtime-v2 generated aggregate code.
/// </summary>
/// <typeparam name="T">Concrete numeric value type preserved by the aggregate result.</typeparam>
public static class AvgAggregateKernel<T>
    where T : struct, INumber<T>
{
    /// <summary>
    ///     Query-specific aggregate state stored directly on generated group classes.
    /// </summary>
    public struct State
    {
        /// <summary>Indicates whether at least one non-null value was accumulated.</summary>
        public bool HasValue;

        /// <summary>Concrete accumulated value.</summary>
        public T Sum;

        /// <summary>Number of non-null values accumulated.</summary>
        public long Count;
    }

    /// <summary>
    ///     Adds a nullable value to the state, ignoring null inputs.
    /// </summary>
    public static void Set(ref State state, T? value)
    {
        if (!value.HasValue)
            return;

        var current = value.GetValueOrDefault();
        state.Sum = state.HasValue
            ? checked(state.Sum + current)
            : current;
        state.Count = checked(state.Count + 1);
        state.HasValue = true;
    }

    /// <summary>
    ///     Gets the nullable aggregate result.
    /// </summary>
    public static T? Get(in State state)
    {
        return state.HasValue
            ? state.Sum / T.CreateChecked(state.Count)
            : null;
    }

    /// <summary>
    ///     Merges another partial average into the target state.
    /// </summary>
    public static void Merge(ref State target, in State source)
    {
        if (!source.HasValue)
            return;

        target.Sum = target.HasValue
            ? checked(target.Sum + source.Sum)
            : source.Sum;
        target.Count = checked(target.Count + source.Count);
        target.HasValue = true;
    }
}
