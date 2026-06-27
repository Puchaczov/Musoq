using System.Collections;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tables;

public readonly struct RowShard<TRow> : IReadOnlyList<TRow>
    where TRow : Row
{
    public static readonly RowShard<TRow> Empty = new(Array.Empty<TRow>(), 0);

    public RowShard(TRow[] rows, int count)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if ((uint)count > (uint)rows.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        Rows = rows;
        Count = count;
    }

    public TRow[] Rows { get; }

    public int Count { get; }

    public TRow this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Rows[index];
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(Rows, Count);
    }

    IEnumerator<TRow> IEnumerable<TRow>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public struct Enumerator : IEnumerator<TRow>
    {
        private readonly TRow[] _rows;
        private readonly int _count;
        private int _index;

        internal Enumerator(TRow[] rows, int count)
        {
            _rows = rows;
            _count = count;
            _index = -1;
        }

        public TRow Current => _rows[_index];

        object IEnumerator.Current => Current;

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
