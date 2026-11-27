namespace Musoq.Plugins;

#pragma warning disable CS1591
public static class CorrelatedScalarSubqueryAggregateKernel<T>
{
    /// <summary>Initializes independently owned kernel state.</summary>
    public static void Reset(ref State state) => state = default;

    public struct State
    {
        public T Value;
        public byte Cardinality;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static void Set(ref State state, T value)
    {
        if (state.Cardinality == 0)
            state.Value = value;

        if (state.Cardinality < 2)
            state.Cardinality++;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static CorrelatedScalarSubqueryResult<T> Get(in State state)
    {
        return new CorrelatedScalarSubqueryResult<T>(state.Value, state.Cardinality);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static void Merge(ref State target, in State source)
    {
        if (source.Cardinality == 0)
            return;

        if (target.Cardinality == 0)
        {
            target = source;
            return;
        }

        target.Cardinality = 2;
    }
}
#pragma warning restore CS1591

/// <summary>Preserves the nullable projection type of a scalar value-type subquery.</summary>
public static class NullableCorrelatedScalarSubqueryAggregateKernel<T>
    where T : struct
{
    /// <summary>Tracks cardinality and the first nullable value independently for each group.</summary>
    public struct State
    {
        internal CorrelatedScalarSubqueryAggregateKernel<T?>.State Inner;
    }

    /// <summary>Initializes independently owned kernel state.</summary>
    public static void Reset(ref State state) => CorrelatedScalarSubqueryAggregateKernel<T?>.Reset(ref state.Inner);

    /// <summary>Accumulates one candidate row.</summary>
    public static void Set(ref State state, T? value) =>
        CorrelatedScalarSubqueryAggregateKernel<T?>.Set(ref state.Inner, value);

    /// <summary>Returns the scalar result with its original element type.</summary>
    public static CorrelatedScalarSubqueryResult<T?> Get(in State state) =>
        CorrelatedScalarSubqueryAggregateKernel<T?>.Get(in state.Inner);

    /// <summary>Combines independently accumulated rows.</summary>
    public static void Merge(ref State target, in State source) =>
        CorrelatedScalarSubqueryAggregateKernel<T?>.Merge(ref target.Inner, in source.Inner);
}
