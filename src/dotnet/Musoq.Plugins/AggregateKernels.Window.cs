using System.Collections.Generic;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class WindowDecimalAggregateKernel
{
    public struct State
    {
        public List<decimal>? Values;
    }

    public static void Set(ref State state, decimal? value)
    {
        if (value.HasValue)
            AggregateCollectionCore.AddOrdered(ref state.Values, value.GetValueOrDefault());
    }

    public static IEnumerable<decimal> Get(in State state)
        => state.Values ?? [];

    public static void Merge(ref State target, in State source)
        => AggregateCollectionCore.MergeOrdered(ref target.Values, source.Values);
}
