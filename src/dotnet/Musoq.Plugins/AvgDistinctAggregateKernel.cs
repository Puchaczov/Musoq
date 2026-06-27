using System.Collections.Generic;
using System.Numerics;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class AvgDistinctAggregateKernel<T>
    where T : struct, INumber<T>
{
    public struct State
    {
        public HashSet<T>? Values;
    }

    public static void Set(ref State state, T? value)
        => AggregateCollectionCore.AddNullableDistinct(ref state.Values, value);

    public static T? Get(in State state)
        => AggregateCollectionCore.AverageDistinct(state.Values);

    public static void Merge(ref State target, in State source)
        => AggregateCollectionCore.MergeDistinct(ref target.Values, source.Values);
}
