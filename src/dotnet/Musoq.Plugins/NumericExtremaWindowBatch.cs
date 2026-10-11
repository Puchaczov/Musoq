using System.Buffers;
using System.Numerics;

namespace Musoq.Plugins;

/// <summary>Evaluates monotone minimum frames without changing equal-value or null semantics.</summary>
public static class MinWindowBatch<T> where T : struct, INumber<T>
{
    /// <summary>Returns false before writing results when frames require serial recomputation.</summary>
    public static bool TryEvaluate(in WindowBatchContext<T?, T?> context)
        => NumericExtremaWindowBatch<T, MinimumComparison<T>>.TryEvaluate(in context);
}

/// <summary>Evaluates monotone maximum frames without changing equal-value or null semantics.</summary>
public static class MaxWindowBatch<T> where T : struct, INumber<T>
{
    /// <summary>Returns false before writing results when frames require serial recomputation.</summary>
    public static bool TryEvaluate(in WindowBatchContext<T?, T?> context)
        => NumericExtremaWindowBatch<T, MaximumComparison<T>>.TryEvaluate(in context);
}

internal interface IExtremaComparison<T> where T : struct, INumber<T>
{
    static abstract bool IsBetter(T current, T best);
}

internal readonly struct MinimumComparison<T> : IExtremaComparison<T> where T : struct, INumber<T>
{
    public static bool IsBetter(T current, T best) => current < best;
}

internal readonly struct MaximumComparison<T> : IExtremaComparison<T> where T : struct, INumber<T>
{
    public static bool IsBetter(T current, T best) => current > best;
}

internal static class NumericExtremaWindowBatch<T, TComparison>
    where T : struct, INumber<T>
    where TComparison : struct, IExtremaComparison<T>
{
    public static bool TryEvaluate(in WindowBatchContext<T?, T?> context)
    {
        if (!CanEvaluate(in context))
            return false;
        var queue = ArrayPool<int>.Shared.Rent(context.Indices.Length);
        try
        {
            WriteFrames(in context, queue);
            return true;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(queue);
        }
    }

    private static bool CanEvaluate(in WindowBatchContext<T?, T?> context)
    {
        for (var position = 0; position < context.Indices.Length; position++)
        {
            var value = context.Values[context.Indices[position]];
            if (value.HasValue && T.IsNaN(value.GetValueOrDefault()))
                return false;
            if (position > 0 && (context.FrameStarts[position] < context.FrameStarts[position - 1] ||
                                 context.FrameEnds[position] < context.FrameEnds[position - 1]))
                return false;
        }
        return true;
    }

    private static void WriteFrames(in WindowBatchContext<T?, T?> context, Span<int> queue)
    {
        var head = 0;
        var tail = 0;
        var consumed = 0;
        for (var position = 0; position < context.Indices.Length; position++)
        {
            var first = Math.Max(0, context.FrameStarts[position]);
            var last = Math.Min(context.Indices.Length - 1, context.FrameEnds[position]);
            while (consumed <= last)
            {
                var value = context.Values[context.Indices[consumed]];
                if (value.HasValue)
                {
                    while (tail > head && TComparison.IsBetter(value.GetValueOrDefault(),
                               context.Values[context.Indices[queue[tail - 1]]].GetValueOrDefault()))
                        tail--;
                    queue[tail++] = consumed;
                }
                consumed++;
            }
            while (head < tail && queue[head] < first)
                head++;
            context.Results[context.Indices[position]] = first <= last && head < tail
                ? context.Values[context.Indices[queue[head]]]
                : null;
        }
    }
}
