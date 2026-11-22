using System.Collections;
using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    // The execution plan selects this buffer for small, constant bounds and built-in order keys.
    public sealed class SmallBoundedTopRecordList<T> : IEnumerable<T>
    {
        private readonly T[] _records;
        private readonly IComparer<T> _comparer;
        private readonly int _skip;
        private int _length;
        private int _seen;
        private bool _finalized;

        public SmallBoundedTopRecordList(int take, IComparer<T> comparer)
            : this(0, take, comparer) { }

        public SmallBoundedTopRecordList(int skip, int take, IComparer<T> comparer)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(skip);
            ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);
            _records = new T[checked(skip + take)];
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _skip = skip;
        }

        public int Count => _finalized ? Math.Max(_length - _skip, 0) : _seen;

        public void Add(T record)
        {
            if (_finalized)
                throw new InvalidOperationException("Cannot append to a finalized bounded top record list.");
            _seen++;
            if (_length == _records.Length && _comparer.Compare(record, _records[^1]) >= 0)
                return;
            Insert(record);
        }

        private void Insert(T record)
        {
            // Insert after equal records so the original order decides ties, including at the cutoff.
            int end = Math.Min(_length, _records.Length - 1);
            int position = 0;
            if (end > 0 && _comparer.Compare(record, _records[0]) >= 0)
            {
                position = 1;
                int high = end;
                while (position < high)
                {
                    int middle = position + (high - position) / 2;
                    if (_comparer.Compare(record, _records[middle]) >= 0)
                        position = middle + 1;
                    else
                        high = middle;
                }
            }
            int shiftCount = end - position;
            switch (shiftCount)
            {
                case 1:
                    _records[end] = _records[position];
                    break;
                case 2:
                    _records[end] = _records[end - 1];
                    _records[end - 1] = _records[position];
                    break;
                case > 2:
                    Array.Copy(_records, position, _records, position + 1, shiftCount);
                    break;
            }
            _records[position] = record;
            _length = end + 1;
        }

        public IEnumerator<T> GetEnumerator()
        {
            _finalized = true;
            return Selected().GetEnumerator();
        }

        private IEnumerable<T> Selected()
        {
            for (var i = _skip; i < _length; i++)
                yield return _records[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
