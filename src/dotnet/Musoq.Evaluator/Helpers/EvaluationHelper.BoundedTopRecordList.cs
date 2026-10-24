using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public sealed class BoundedTopRecordList<T> : IEnumerable<T>
    {
        private readonly int _skipCount;
        private readonly int _takeCount;
        private readonly int _limit;
        private readonly TopOffsetRecordComparer<T> _recordComparer;
        private readonly PriorityQueue<TopOffsetRecord<T>, TopOffsetRecord<T>>? _queue;
        private List<T>? _selectedRows;
        private readonly List<TopOffsetRecord<T>>? _overflowRows;
        private int _seenCount;

        public BoundedTopRecordList(int takeCount, IComparer<T> comparer)
            : this(0, takeCount, comparer)
        {
        }

        public BoundedTopRecordList(int skipCount, int takeCount, IComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);
            _skipCount = skipCount;
            _takeCount = takeCount;
            _limit = takeCount <= 0 ? 0 : CalculateTopOffsetLimit(skipCount, takeCount);
            _recordComparer = new TopOffsetRecordComparer<T>(comparer);
            if ((long)Math.Max(skipCount, 0) + takeCount > int.MaxValue)
            {
                _overflowRows = [];
                return;
            }
            _queue = _limit <= 0
                ? null
                : new PriorityQueue<TopOffsetRecord<T>, TopOffsetRecord<T>>(
                    new ReverseTopOffsetRecordComparer<T>(_recordComparer));
        }

        public int Count => _selectedRows?.Count ?? _seenCount;

        public void Add(T record)
        {
            if (_selectedRows != null)
                throw new InvalidOperationException("Cannot append to a finalized bounded top record list.");

            var candidate = new TopOffsetRecord<T>(record, _seenCount);
            _seenCount++;

            if (_queue == null)
            {
                _overflowRows?.Add(candidate);
                return;
            }

            if (_queue.Count < _limit)
            {
                _queue.Enqueue(candidate, candidate);
                return;
            }

            var worstKept = _queue.Peek();
            if (_recordComparer.Compare(candidate, worstKept) >= 0)
                return;

            _queue.Dequeue();
            _queue.Enqueue(candidate, candidate);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return MaterializeSelectedRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<T> MaterializeSelectedRows()
        {
            if (_selectedRows != null)
                return _selectedRows;

            if (_overflowRows != null)
            {
                _overflowRows.Sort(_recordComparer);
                _selectedRows = _overflowRows.Skip(_skipCount).Take(_takeCount).Select(static item => item.Record).ToList();
                return _selectedRows;
            }

            _selectedRows = _queue == null
                ? []
                : CopySelectedTopOffsetRecords(_queue, _skipCount, _takeCount);

            return _selectedRows;
        }
    }
}
