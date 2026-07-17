namespace Musoq.Plugins;

#pragma warning disable CS1591
public static class CorrelatedScalarSubqueryAggregateKernel<T>
{
    public struct State
    {
        public T Value;
        public byte Cardinality;
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static void Set(ref State state, T value)
    {
        if (state.Cardinality == 0)
            state.Value = value;

        if (state.Cardinality < 2)
            state.Cardinality++;
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static CorrelatedScalarSubqueryResult<T> Get(in State state)
    {
        return new CorrelatedScalarSubqueryResult<T>(state.Value, state.Cardinality);
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
