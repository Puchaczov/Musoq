using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static TValue GetOrAddCachedMethod<TTarget, TKey, TValue>(
        ConcurrentDictionary<TKey, TValue> cache,
        TTarget target,
        TKey key,
        Func<TTarget, TKey, TValue> factory)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(factory);
        if (cache.TryGetValue(key, out var value))
            return value;

        value = factory(target, key);
        return cache.GetOrAdd(key, value);
    }

    public static IReadOnlyList<T> GetParallelAggregationRowsOrEmpty<T>(
        IEnumerable<T> source,
        int threshold)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is IReadOnlyList<T> indexedRows)
            return indexedRows;

        return source.ToArray();
    }

    public static IReadOnlyList<T> GetParallelAggregationRowsOrEmpty<T>(
        IEnumerable<IReadOnlyList<T>> sourceChunks,
        int threshold)
    {
        ArgumentNullException.ThrowIfNull(sourceChunks);

        if (!TryGetReusableChunks(sourceChunks, out var chunks, out var rowCount))
            chunks = sourceChunks.ToArray();

        rowCount = 0;
        for (var index = 0; index < chunks.Count; index++)
            checked
            {
                rowCount += chunks[index].Count;
            }

        return chunks.Count switch
        {
            0 => Array.Empty<T>(),
            1 => chunks[0],
            _ => new ChunkedReadOnlyList<T>(chunks, rowCount)
        };
    }

    public static IReadOnlyList<T> GetParallelProjectionRowsOrEmpty<T>(
        IEnumerable<T> source,
        int threshold)
    {
        return GetParallelAggregationRowsOrEmpty(source, threshold);
    }

    public static IReadOnlyList<T> GetParallelProjectionRowsOrEmpty<T>(
        IEnumerable<IReadOnlyList<T>> sourceChunks,
        int threshold)
    {
        return GetParallelAggregationRowsOrEmpty(sourceChunks, threshold);
    }

    public static RowShard<TRow>[] ProjectRowsParallel<TSource, TRow>(
        IReadOnlyList<TSource> rows,
        int maxDegreeOfParallelism,
        Func<TSource, bool> predicate,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        return ProjectionRows.ProjectParallel(
            rows,
            maxDegreeOfParallelism,
            predicate,
            project,
            includeProjected: null,
            PublishShard,
            cancellationToken);
    }

    public static IEnumerable<TRow> ProjectChunkedRowsParallel<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        int maxDegreeOfParallelism,
        Func<TSource, bool> predicate,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        return ProjectionRows.ProjectChunksParallel(
            chunks,
            maxDegreeOfParallelism,
            predicate,
            project,
            includeProjected: null,
            cancellationToken);
    }

    public static RowShard<TRow>[] ProjectRowsParallel<TSource, TRow>(
        IReadOnlyList<TSource> rows,
        int maxDegreeOfParallelism,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        return ProjectionRows.ProjectParallel(
            rows,
            maxDegreeOfParallelism,
            static _ => true,
            project,
            static row => row != null,
            PublishShard,
            cancellationToken);
    }

    public static IEnumerable<TRow> ProjectChunkedRowsParallel<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        int maxDegreeOfParallelism,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        return ProjectionRows.ProjectChunksParallel(
            chunks,
            maxDegreeOfParallelism,
            static _ => true,
            project,
            static row => row != null,
            cancellationToken);
    }

    public static void AddRowsDirect<TRow>(
        Table target,
        IReadOnlyCollection<TRow> rows)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
            return;

        target.EnsureCapacity(target.Count + rows.Count);
        if (rows is IReadOnlyList<TRow> indexedRows)
        {
            foreach (var row in indexedRows)
                target.AddDirect(row);

            return;
        }

        foreach (var row in rows)
            target.AddDirect(row);
    }

    public static void AddRowsDirect<TRow>(
        Table target,
        RowShard<TRow>[] shards)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(shards);
        target.AddDirectDeferred(shards);
    }

    public static void AddRowsDirect<TRow>(
        Table target,
        IReadOnlyList<TRow>[] shards)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(shards);
        target.AddDirectDeferred(shards);
    }

    private static RowShard<TRow> PublishShard<TRow>(TRow[] shard, int count)
        where TRow : Row
    {
        if (count == 0)
            return RowShard<TRow>.Empty;

        return new RowShard<TRow>(shard, count);
    }

    private static bool TryGetReusableChunks<T>(
        IEnumerable<IReadOnlyList<T>> sourceChunks,
        out IReadOnlyList<IReadOnlyList<T>> chunks,
        out int rowCount)
    {
        chunks = Array.Empty<IReadOnlyList<T>>();
        rowCount = 0;

        if (sourceChunks is not IReadOnlyList<IReadOnlyList<T>> indexedChunks)
            return false;

        chunks = indexedChunks;
        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            if (chunk is null)
                return false;

            checked
            {
                rowCount += chunk.Count;
            }
        }

        return true;
    }

    private sealed class ChunkedReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<IReadOnlyList<T>> _chunks;
        private readonly int[] _starts;

        public ChunkedReadOnlyList(IReadOnlyList<IReadOnlyList<T>> chunks, int count)
        {
            _chunks = chunks;
            Count = count;
            _starts = new int[chunks.Count];

            var start = 0;
            for (var index = 0; index < chunks.Count; index++)
            {
                _starts[index] = start;
                start += chunks[index].Count;
            }
        }

        public int Count { get; }

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var chunkIndex = Array.BinarySearch(_starts, index);
                if (chunkIndex < 0)
                    chunkIndex = ~chunkIndex - 1;

                return _chunks[chunkIndex][index - _starts[chunkIndex]];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var chunkIndex = 0; chunkIndex < _chunks.Count; chunkIndex++)
            {
                var chunk = _chunks[chunkIndex];
                for (var rowIndex = 0; rowIndex < chunk.Count; rowIndex++)
                    yield return chunk[rowIndex];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
