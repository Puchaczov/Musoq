namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class MaxComparableAggregateKernel<T>
    where T : struct, IComparable<T>
{
    public struct State
    {
        public bool HasValue;
        public T Value;
    }

    public static void Set(ref State state, T? value)
        => NullableAggregateCore.SetBest(ref state.HasValue, ref state.Value, value, static (current, best) => current.CompareTo(best) > 0);

    public static T? Get(in State state)
        => NullableAggregateCore.GetNullable(state.HasValue, state.Value);

    public static void Merge(ref State target, in State source)
        => NullableAggregateCore.MergeBest(ref target.HasValue, ref target.Value, source.HasValue, source.Value, static (current, best) => current.CompareTo(best) > 0);
}
