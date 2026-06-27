using System.Collections.Generic;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class AggregateValuesStringKernel
{
    public struct State
    {
        public List<string>? Values;
    }

    public static void Set(ref State state, string? value)
        => AggregateCollectionCore.AddOrdered(ref state.Values, value ?? string.Empty);

    public static string Get(in State state)
        => AggregateCollectionCore.Join(state.Values, ",");

    public static void Merge(ref State target, in State source)
        => AggregateCollectionCore.MergeOrdered(ref target.Values, source.Values);
}
