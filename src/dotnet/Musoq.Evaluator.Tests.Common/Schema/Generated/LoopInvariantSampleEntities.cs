using System;
using System.Collections.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema.Generated;

public static class LoopInvariantSampleCounters
{
    public static int OuterStableValueReads { get; private set; }

    public static int OuterVolatileValueReads { get; private set; }

    public static int MiddleStableValueReads { get; private set; }

    public static int MiddleVolatileValueReads { get; private set; }

    public static int LeafStableValueReads { get; private set; }

    public static int StableOfCalls { get; internal set; }

    public static int StablePairCalls { get; internal set; }

    public static int VolatileOfCalls { get; internal set; }

    public static void Reset()
    {
        OuterStableValueReads = 0;
        OuterVolatileValueReads = 0;
        MiddleStableValueReads = 0;
        MiddleVolatileValueReads = 0;
        LeafStableValueReads = 0;
        StableOfCalls = 0;
        StablePairCalls = 0;
        VolatileOfCalls = 0;
    }

    internal static int ReadOuterStable(int value)
    {
        OuterStableValueReads++;
        return value;
    }

    internal static int ReadOuterVolatile(int value)
    {
        OuterVolatileValueReads++;
        return value;
    }

    internal static int ReadMiddleStable(int value)
    {
        MiddleStableValueReads++;
        return value;
    }

    internal static int ReadMiddleVolatile(int value)
    {
        MiddleVolatileValueReads++;
        return value;
    }

    internal static int ReadLeafStable(int value)
    {
        LeafStableValueReads++;
        return value;
    }

}

public sealed class LoopInvariantSampleOuter
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(Id)] = 0,
            [nameof(Value)] = 1,
            [nameof(VolatileValue)] = 2,
            [nameof(Middles)] = 3,
            [nameof(EmptyMiddles)] = 4
        };

    public static readonly IReadOnlyDictionary<int, Func<LoopInvariantSampleOuter, object?>> IndexToObjectAccessMap =
        new Dictionary<int, Func<LoopInvariantSampleOuter, object?>>
        {
            [0] = row => row.Id,
            [1] = row => row.Value,
            [2] = row => row.VolatileValue,
            [3] = row => row.Middles,
            [4] = row => row.EmptyMiddles
        };

    public int Id { get; init; }

    public int Value => LoopInvariantSampleCounters.ReadOuterStable(Id * 10);

    [NonDeterministic]
    public int VolatileValue => LoopInvariantSampleCounters.ReadOuterVolatile(Id * 100);

    public LoopInvariantSampleMiddle[] Middles { get; init; } = [];

    public LoopInvariantSampleMiddle[] EmptyMiddles { get; init; } = [];
}

public sealed class LoopInvariantSampleMiddle
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(Id)] = 0,
            [nameof(Value)] = 1,
            [nameof(VolatileValue)] = 2,
            [nameof(Leaves)] = 3
        };

    public static readonly IReadOnlyDictionary<int, Func<LoopInvariantSampleMiddle, object?>> IndexToObjectAccessMap =
        new Dictionary<int, Func<LoopInvariantSampleMiddle, object?>>
        {
            [0] = row => row.Id,
            [1] = row => row.Value,
            [2] = row => row.VolatileValue,
            [3] = row => row.Leaves
        };

    public int Id { get; init; }

    public int Value => LoopInvariantSampleCounters.ReadMiddleStable(Id);

    [NonDeterministic]
    public int VolatileValue => LoopInvariantSampleCounters.ReadMiddleVolatile(Id * 1000);

    public LoopInvariantSampleLeaf[] Leaves { get; init; } = [];
}

public sealed class LoopInvariantSampleLeaf
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(Id)] = 0,
            [nameof(Value)] = 1
        };

    public static readonly IReadOnlyDictionary<int, Func<LoopInvariantSampleLeaf, object?>> IndexToObjectAccessMap =
        new Dictionary<int, Func<LoopInvariantSampleLeaf, object?>>
        {
            [0] = row => row.Id,
            [1] = row => row.Value
        };

    public int Id { get; init; }

    public int Value => LoopInvariantSampleCounters.ReadLeafStable(Id);

}
