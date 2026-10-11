namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class MaxStringAggregateKernel
{
    /// <summary>Initializes independently owned kernel state.</summary>
    public static void Reset(ref State state) => state = default;

    public struct State
    {
        public string? Value;
    }

    public static void Set(ref State state, string? value)
        => ReferenceAggregateCore.SetBest(ref state.Value, value,
            static (current, best) => string.CompareOrdinal(current, best) > 0);

    public static string? Get(in State state)
        => state.Value;

    public static void Merge(ref State target, in State source)
        => Set(ref target, source.Value);
}
