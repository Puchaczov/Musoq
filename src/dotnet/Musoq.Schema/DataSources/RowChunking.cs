using System.Collections.Generic;

namespace Musoq.Schema.DataSources;

public static class RowChunking
{
    public const int DefaultChunkSize = 4096;

    public static IEnumerable<IReadOnlyList<T>> FromEnumerableOutput<T>(
        IEnumerable<T>? rows,
        int chunkSize = DefaultChunkSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);

        if (rows is null)
            yield break;

        if (rows is T[] array)
        {
            foreach (var chunk in CreateChunkViews(array, 0, array.Length, chunkSize))
                yield return chunk;
            yield break;
        }

        if (rows is List<T> list)
        {
            foreach (var chunk in CreateChunkViews(list, 0, list.Count, chunkSize))
                yield return chunk;
            yield break;
        }

        if (rows is RowChunk<T> rowChunk)
        {
            foreach (var chunk in CreateChunkViews(rowChunk.Source, rowChunk.Offset, rowChunk.Count, chunkSize))
                yield return chunk;
            yield break;
        }

        foreach (var chunk in CreateBufferedChunks(rows, chunkSize))
            yield return chunk;
    }

    public static IEnumerable<IReadOnlyList<T>> NormalizeSourceChunks<T>(
        IEnumerable<IReadOnlyList<T>>? chunks,
        int chunkSize = DefaultChunkSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);

        if (chunks is null)
            yield break;

        foreach (var chunk in chunks)
        {
            foreach (var normalized in NormalizeSourceChunk(chunk, chunkSize))
                yield return normalized;
        }
    }

    public static IEnumerable<IReadOnlyList<T>> NormalizeSourceChunk<T>(
        IReadOnlyList<T>? chunk,
        int chunkSize = DefaultChunkSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);

        if (chunk is null || chunk.Count == 0)
            yield break;

        if (chunk is T[] array)
        {
            foreach (var normalized in CreateChunkViews(array, 0, array.Length, chunkSize))
                yield return normalized;
            yield break;
        }

        if (chunk is List<T> list)
        {
            foreach (var normalized in CreateChunkViews(list, 0, list.Count, chunkSize))
                yield return normalized;
            yield break;
        }

        if (chunk is RowChunk<T> rowChunk)
        {
            foreach (var normalized in CreateChunkViews(rowChunk.Source, rowChunk.Offset, rowChunk.Count, chunkSize))
                yield return normalized;
            yield break;
        }

        foreach (var normalized in CreateChunkViews(chunk, 0, chunk.Count, chunkSize))
            yield return normalized;
    }

    private static IEnumerable<IReadOnlyList<T>> CreateChunkViews<T>(
        IReadOnlyList<T> rows,
        int offset,
        int count,
        int chunkSize)
    {
        for (var chunkOffset = 0; chunkOffset < count; chunkOffset += chunkSize)
        {
            yield return new RowChunk<T>(
                rows,
                offset + chunkOffset,
                Math.Min(chunkSize, count - chunkOffset));
        }
    }

    private static IEnumerable<IReadOnlyList<T>> CreateBufferedChunks<T>(
        IEnumerable<T> rows,
        int chunkSize)
    {
        var buffer = new T[chunkSize];
        var count = 0;

        foreach (var row in rows)
        {
            buffer[count++] = row;
            if (count != buffer.Length)
                continue;

            yield return buffer;
            buffer = new T[chunkSize];
            count = 0;
        }

        if (count == 0)
            yield break;

        var remainder = new T[count];
        Array.Copy(buffer, remainder, count);
        yield return remainder;
    }
}
