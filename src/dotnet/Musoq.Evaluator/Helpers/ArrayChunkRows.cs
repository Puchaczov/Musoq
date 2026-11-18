using System.Collections.Generic;
using System.Threading;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Helpers;

public static class ArrayChunkRows
{
    // Arbitrary datasource indexers can perform work or throw when a skipped row is read.
    public static IReadOnlyList<T> SkipPrefix<T>(IReadOnlyList<T> chunk, ref int remaining, CancellationToken token)
    {
        if (remaining <= 0)
            return chunk;

        if (!TryGetSegment(chunk, out var segment) || segment.Count == 0)
            return chunk;
        var count = segment.Count;
        token.ThrowIfCancellationRequested();
        var skip = Math.Min(remaining, count);
        remaining -= skip;
        return skip == count ? Array.Empty<T>() : new RowChunk<T>(segment.Array!, segment.Offset + skip, count - skip);
    }

    public static bool TryGetCount<T>(IReadOnlyList<T> chunk, out int count)
    {
        var known = TryGetSegment(chunk, out var segment);
        count = segment.Count;
        return known;
    }

    internal static bool TryGetSegment<T>(IReadOnlyList<T> chunk, out ArraySegment<T> segment)
    {
        switch (chunk)
        {
            case T[] rows:
                segment = new ArraySegment<T>(rows);
                return true;
            case RowChunk<T> { Source: T[] rows } view:
                segment = new ArraySegment<T>(rows, view.Offset, view.Count);
                return true;
            default:
                segment = default;
                return false;
        }
    }
}
