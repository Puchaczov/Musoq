using System.Collections.Generic;
using System.Numerics;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class AverageDistinctKernel<TInput, TResult>
    where TInput : struct, INumber<TInput>
    where TResult : struct, INumber<TResult>
{
    public struct State
    {
        public HashSet<TInput>? Values;
        public AverageKernel<TInput, TResult>.State Average;
    }

    public static void Reset(ref State state) => state = default;

    public static void Set(ref State state, TInput? value)
    {
        if (!value.HasValue || !(state.Values ??= []).Add(value.GetValueOrDefault()))
            return;

        AverageKernel<TInput, TResult>.Set(ref state.Average, value);
    }

    public static TResult? Get(in State state)
        => AverageKernel<TInput, TResult>.Get(in state.Average);

    public static void Merge(ref State target, in State source)
    {
        if (source.Values is null)
            return;

        foreach (var value in source.Values)
            Set(ref target, value);
    }
}
