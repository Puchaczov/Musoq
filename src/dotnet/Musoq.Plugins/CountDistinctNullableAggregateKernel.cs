using System.Collections.Generic;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class CountDistinctNullableAggregateKernel<T>
    where T : struct
{
    public struct State
    {
        public HashSet<T>? Values;
    }

    public static void Set(ref State state, T? value)
        => AggregateCollectionCore.AddNullableDistinct(ref state.Values, value);

    public static long Get(in State state)
        => AggregateCollectionCore.CountDistinct(state.Values);

    public static void Merge(ref State target, in State source)
        => AggregateCollectionCore.MergeDistinct(ref target.Values, source.Values);
}
