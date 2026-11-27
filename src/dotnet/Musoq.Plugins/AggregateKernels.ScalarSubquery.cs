namespace Musoq.Plugins;

/// <summary>
///     Aggregates a scalar subquery result while enforcing SQL scalar cardinality.
/// </summary>
/// <typeparam name="T">The scalar value type.</typeparam>
public static class ScalarSubqueryAggregateKernel<T>
{
    /// <summary>Initializes independently owned kernel state.</summary>
    public static void Reset(ref State state) => state = default;

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
            throw new Musoq.Plugins.ScalarSubqueryCardinalityException();

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

/// <summary>Preserves the nullable projection type of a scalar value-type subquery.</summary>
public static class NullableScalarSubqueryAggregateKernel<T>
    where T : struct
{
    /// <summary>Tracks cardinality and the first nullable value independently for each group.</summary>
    public struct State
    {
        internal ScalarSubqueryAggregateKernel<T?>.State Inner;
    }

    /// <summary>Initializes independently owned kernel state.</summary>
    public static void Reset(ref State state) => ScalarSubqueryAggregateKernel<T?>.Reset(ref state.Inner);

    /// <summary>Accumulates one candidate row.</summary>
    public static void Set(ref State state, T? value) => ScalarSubqueryAggregateKernel<T?>.Set(ref state.Inner, value);

    /// <summary>Returns the scalar result with its original element type.</summary>
    public static T? Get(in State state) => ScalarSubqueryAggregateKernel<T?>.Get(in state.Inner);

    /// <summary>Combines independently accumulated rows.</summary>
    public static void Merge(ref State target, in State source) =>
        ScalarSubqueryAggregateKernel<T?>.Merge(ref target.Inner, in source.Inner);
}
