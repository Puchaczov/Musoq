using System.Numerics;
using System.Runtime.CompilerServices;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class SumAggregateKernel<T>
    where T : struct, INumber<T>
{
    /// <summary>Initializes independently owned kernel state.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Reset(ref State state) => state = default;

    public struct State
    {
        public bool HasValue;
        public T Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(ref State state, T? value)
        => NullableAggregateCore.SetSum(ref state.HasValue, ref state.Value, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? Get(in State state)
        => NullableAggregateCore.GetNullable(state.HasValue, state.Value);

    public static void Merge(ref State target, in State source)
        => NullableAggregateCore.MergeSum(ref target.HasValue, ref target.Value, source.HasValue, source.Value);
}
