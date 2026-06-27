using System.Numerics;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class MinDistinctAggregateKernel<T>
    where T : struct, INumber<T>
{
    public struct State
    {
        public bool HasValue;
        public T Value;
    }

    public static void Set(ref State state, T? value)
        => NullableAggregateCore.SetBest(ref state.HasValue, ref state.Value, value, static (current, best) => current < best);

    public static T? Get(in State state)
        => NullableAggregateCore.GetNullable(state.HasValue, state.Value);

    public static void Merge(ref State target, in State source)
        => NullableAggregateCore.MergeBest(ref target.HasValue, ref target.Value, source.HasValue, source.Value, static (current, best) => current < best);
}
