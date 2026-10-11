using System.Buffers;

namespace Musoq.Plugins;

/// <summary>Evaluates equal-scale positive decimal frames when exact serial equivalence can be established.</summary>
public static class DecimalSumWindowBatch
{
    /// <summary>Writes results only after proving that every serial addition is exactly representable; otherwise returns false.</summary>
    public static bool TryEvaluate(in WindowBatchContext<decimal?, decimal?> context)
    {
        var count = context.Indices.Length;
        var totals = ArrayPool<Int128>.Shared.Rent(count + 1);
        var values = ArrayPool<int>.Shared.Rent(count + 1);
        try
        {
            var scale = TryCreatePrefixes(in context, totals, values);
            if (!scale.HasValue)
                return false;
            WriteFrames(in context, totals, values, scale.Value);
            return true;
        }
        finally
        {
            ArrayPool<Int128>.Shared.Return(totals);
            ArrayPool<int>.Shared.Return(values);
        }
    }

    private static byte? TryCreatePrefixes(in WindowBatchContext<decimal?, decimal?> context,
        Span<Int128> totals, Span<int> values)
    {
        byte scale = 0;
        var hasScale = false;
        totals[0] = 0;
        values[0] = 0;
        Span<int> bits = stackalloc int[4];
        for (var position = 0; position < context.Indices.Length; position++)
        {
            var value = context.Values[context.Indices[position]];
            var coefficient = 0L;
            if (value.HasValue)
            {
                decimal.GetBits(value.GetValueOrDefault(), bits);
                var currentScale = (byte)((uint)bits[3] >> 16);
                if (bits[3] < 0 || bits[2] != 0 || (uint)bits[1] > int.MaxValue ||
                    hasScale && currentScale != scale)
                    return null;
                scale = currentScale;
                hasScale = true;
                coefficient = ((long)(uint)bits[1] << 32) | (uint)bits[0];
            }
            totals[position + 1] = totals[position] + coefficient;
            values[position + 1] = values[position] + (value.HasValue ? 1 : 0);
        }
        return scale;
    }

    private static void WriteFrames(in WindowBatchContext<decimal?, decimal?> context,
        ReadOnlySpan<Int128> totals, ReadOnlySpan<int> values, byte scale)
    {
        for (var position = 0; position < context.Indices.Length; position++)
        {
            var first = Math.Max(0, context.FrameStarts[position]);
            var last = Math.Min(context.Indices.Length - 1, context.FrameEnds[position]);
            decimal? result = null;
            if (first <= last && values[last + 1] != values[first])
            {
                var coefficient = totals[last + 1] - totals[first];
                result = new decimal(unchecked((int)(uint)coefficient), unchecked((int)(uint)(coefficient >> 32)),
                    unchecked((int)(uint)(coefficient >> 64)), false, scale);
            }
            context.Results[context.Indices[position]] = result;
        }
    }
}
