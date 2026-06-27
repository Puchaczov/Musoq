namespace Musoq.Plugins;

/// <summary>
///     Typed row-count kernel used by runtime-v2 generated aggregate code.
/// </summary>
public static class CountAllAggregateKernel
{
    /// <summary>
    ///     Query-specific aggregate state stored directly on generated group classes.
    /// </summary>
    public struct State
    {
        /// <summary>Number of accumulated rows.</summary>
        public long Count;
    }

    /// <summary>
    ///     Counts one row.
    /// </summary>
    public static void Set(ref State state)
    {
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
