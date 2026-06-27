namespace Musoq.Plugins;

/// <summary>
///     Aggregates a scalar subquery result while enforcing SQL scalar cardinality.
/// </summary>
/// <typeparam name="T">The scalar value type.</typeparam>
public static class ScalarSubqueryAggregateKernel<T>
{
    /// <summary>
    ///     Query-specific scalar subquery state.
    /// </summary>
    public struct State
    {
        /// <summary>Indicates whether the first row has been seen.</summary>
        public bool HasValue;

        /// <summary>Indicates whether more than one row has been seen.</summary>
        public bool HasMultipleRows;

        /// <summary>The first scalar value.</summary>
        public T Value;
    }

    /// <summary>
    ///     Adds a scalar candidate row to the state.
    /// </summary>
    public static void Set(ref State state, T value)
    {
        if (state.HasValue)
        {
            state.HasMultipleRows = true;
            return;
        }

        state.Value = value;
        state.HasValue = true;
    }

    /// <summary>
    ///     Gets the scalar value or throws when cardinality is greater than one.
    /// </summary>
    public static T Get(in State state)
    {
        if (state.HasMultipleRows)
            throw new InvalidOperationException("Scalar subquery returned more than one row.");

        return state.HasValue ? state.Value : default!;
    }

    /// <summary>
    ///     Merges another partial scalar state into the target state.
    /// </summary>
    public static void Merge(ref State target, in State source)
    {
        if (!source.HasValue)
            return;

        if (target.HasValue || source.HasMultipleRows)
            target.HasMultipleRows = true;
        else
            target.Value = source.Value;

        target.HasValue = true;
    }
}
