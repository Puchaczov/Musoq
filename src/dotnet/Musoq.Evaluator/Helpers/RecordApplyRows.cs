using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Helpers;

/// <summary>Typed batches for generated record APPLY loops and chunk-based materializers.</summary>
public readonly struct RecordApplyRows<T>(IEnumerable<T>? rows, CancellationToken streamingCancellation = default)
    : IEnumerable<IReadOnlyList<T>>
{
    public IEnumerator<Batch> GetEnumerator() => ReadBatches(rows, streamingCancellation).GetEnumerator();

    IEnumerator<IReadOnlyList<T>> IEnumerable<IReadOnlyList<T>>.GetEnumerator() =>
        rows is IStreamingApplyEnumerable<T>
            ? ReadStreamingLists().GetEnumerator()
            : RowChunking.FromEnumerableOutput(rows).GetEnumerator();

    private IEnumerable<IReadOnlyList<T>> ReadStreamingLists()
    {
        foreach (var batch in this)
            yield return batch;
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IReadOnlyList<T>>)this).GetEnumerator();

    private static IEnumerable<Batch> ReadBatches(IEnumerable<T>? source, CancellationToken streamingCancellation)
    {
        if (source is null)
            yield break;

        if (source is IStreamingApplyEnumerable<T> streaming)
        {
            foreach (var row in streaming)
            {
                streamingCancellation.ThrowIfCancellationRequested();
                yield return new Batch(row);
            }
            yield break;
        }

        if (source is T[] { Length: <= RowChunking.DefaultChunkSize } array)
        {
            if (array.Length > 0)
                yield return new Batch(array, array.Length);
            yield break;
        }

        if (source is List<T> { Count: <= RowChunking.DefaultChunkSize } list)
        {
            var count = list.Count;
            if (count > 0)
            {
                RowChunk<T>.ValidateRange(list, 0, count);
                yield return new Batch(list, count);
            }
            yield break;
        }

        // Preserve chunk boundaries and list-view validation for larger collections.
        var chunks = source is T[] or List<T> or RowChunk<T>
            ? RowChunking.FromEnumerableOutput(source)
            : RowChunking.CreateBufferedChunks(source, RowChunking.DefaultChunkSize, 8);
        foreach (var chunk in chunks)
            yield return new Batch(chunk, chunk.Count);
    }

    public readonly struct Batch : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T>? _items;
        private readonly int _offset;
        private readonly T? _single;

        internal Batch(T single)
        {
            _single = single;
            Count = 1;
        }

        internal Batch(IReadOnlyList<T> items, int count)
        {
            Count = count;
            if (items is RowChunk<T> view)
            {
                _items = view.Source;
                _offset = view.Offset;
            }
            else
                _items = items;
        }

        public int Count { get; }

        public T this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Count);
                return _items switch
                {
                    T[] array => array[_offset + index],
                    List<T> list => list[_offset + index],
                    null => _single!,
                    _ => _items[_offset + index],
                };
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
