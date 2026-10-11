using System.Numerics;
using System.Runtime.CompilerServices;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class AverageKernel<TInput, TResult>
    where TInput : struct, INumber<TInput>
    where TResult : struct, INumber<TResult>
{
    public struct State
    {
        public TResult Sum;
        public long Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Reset(ref State state) => state = default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(ref State state, TInput? value)
    {
        if (!value.HasValue)
            return;

        var current = TResult.CreateChecked(value.GetValueOrDefault());
        state.Sum = state.Count == 0 ? current : checked(state.Sum + current);
        state.Count = checked(state.Count + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult? Get(in State state)
    {
        if (state.Count == 0)
            return null;
        return state.Sum / TResult.CreateChecked(state.Count);
    }

    public static void Merge(ref State target, in State source)
    {
        if (source.Count == 0)
            return;

        target.Sum = target.Count == 0 ? source.Sum : checked(target.Sum + source.Sum);
        target.Count = checked(target.Count + source.Count);
    }
}
