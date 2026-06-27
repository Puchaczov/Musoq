using System;
using System.Collections;
using System.Collections.Generic;

namespace Musoq.Schema.DataSources;

public sealed class RowChunk<T> : IReadOnlyList<T>
{
    public RowChunk(IReadOnlyList<T> source, int offset, int count)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));

        if ((uint)offset > (uint)source.Count)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (count < 0 || count > source.Count - offset)
            throw new ArgumentOutOfRangeException(nameof(count));

        Offset = offset;
        Count = count;
    }

    public IReadOnlyList<T> Source { get; }

    public int Offset { get; }

    public int Count { get; }

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Source[Offset + index];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var index = 0; index < Count; index++)
            yield return Source[Offset + index];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
