using System.Collections.Generic;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class AggregateValuesStringDelimitedKernel
{
    public struct State
    {
        public List<string>? Values;
        public string? Delimiter;
    }

    public static void Set(ref State state, string? value, string delimiter)
    {
        state.Delimiter = delimiter;
        if (value is not null)
            AggregateCollectionCore.AddOrdered(ref state.Values, value);
    }

    public static string Get(in State state)
        => AggregateCollectionCore.Join(state.Values, state.Delimiter ?? string.Empty);

    public static void Merge(ref State target, in State source)
    {
        if (source.Delimiter is not null)
            target.Delimiter = source.Delimiter;

        AggregateCollectionCore.MergeOrdered(ref target.Values, source.Values);
    }
}
