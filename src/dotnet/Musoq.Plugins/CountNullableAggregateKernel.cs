namespace Musoq.Plugins;

/// <summary>
///     Typed nullable value-count kernel used by runtime-v2 generated aggregate code.
/// </summary>
/// <typeparam name="T">Concrete value type tested for a non-null value.</typeparam>
public static class CountNullableAggregateKernel<T>
    where T : struct
{
    /// <summary>
    ///     Query-specific aggregate state stored directly on generated group classes.
    /// </summary>
    public struct State
    {
        /// <summary>Number of non-null values accumulated.</summary>
        public long Count;
    }

    /// <summary>
    ///     Counts a nullable value when it is present.
    /// </summary>
    public static void Set(ref State state, T? value)
    {
        if (value.HasValue)
            state.Count = checked(state.Count + 1);
    }

    /// <summary>
    ///     Gets the aggregate count.
    /// </summary>
    public static long Get(in State state)
    {
        return state.Count;
    }

    /// <summary>
    ///     Merges another partial count into the target state.
    /// </summary>
    public static void Merge(ref State target, in State source)
    {
        target.Count = checked(target.Count + source.Count);
    }
}
