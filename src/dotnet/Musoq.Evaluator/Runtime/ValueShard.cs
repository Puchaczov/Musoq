using System.Collections;
using System.Collections.Generic;

namespace Musoq.Evaluator.Runtime;

public readonly struct ValueShard<T> : IReadOnlyList<T>
{
    public static readonly ValueShard<T> Empty = new(Array.Empty<T>(), 0);

    public ValueShard(T[] values, int count)
    {
        ArgumentNullException.ThrowIfNull(values);
        if ((uint)count > (uint)values.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        Values = values;
        Count = count;
    }

    public T[] Values { get; }

    public int Count { get; }

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Values[index];
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(Values, Count);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly T[] _values;
        private readonly int _count;
        private int _index;

        internal Enumerator(T[] values, int count)
        {
            _values = values;
            _count = count;
            _index = -1;
        }

        public T Current => _values[_index];

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            var next = _index + 1;
            if (next >= _count)
                return false;

            _index = next;
            return true;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }
    }
}
