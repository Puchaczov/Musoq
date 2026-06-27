using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static Table ToDistinctTable(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var distinctTable = new Table(table.Name, table.Columns.ToArray());
        var seenRows = new HashSet<Row>(table.Count);

        foreach (var row in table)
            if (seenRows.Add(row))
                distinctTable.AddUnchecked(row);

        return distinctTable;
    }

    public static IReadOnlyList<Row> SelectTopOffsetRows(
        IEnumerable<Row> rows,
        int skipCount,
        int takeCount,
        IReadOnlyList<RowOrderKey> orderKeys)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(orderKeys);
        if (takeCount <= 0)
            return Array.Empty<Row>();

        if (orderKeys.Count == 0)
            return CopySlicedRows(rows, skipCount, takeCount);

        var limit = CalculateTopOffsetLimit(skipCount, takeCount);
        if (limit <= 0)
            return Array.Empty<Row>();

        var queue = CollectTopOffsetCandidates(rows, limit, orderKeys);
        return CopySelectedTopOffsetRows(queue, skipCount, takeCount);
    }

    public static void AppendTopOffsetRowsDirect(
        IEnumerable<Row> rows,
        Table target,
        int skipCount,
        int takeCount,
        IReadOnlyList<RowOrderKey> orderKeys)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(orderKeys);
        if (takeCount <= 0)
            return;

        if (orderKeys.Count == 0)
        {
            AppendSlicedRowsDirect(rows, target, skipCount, takeCount);
            return;
        }

        var limit = CalculateTopOffsetLimit(skipCount, takeCount);
        if (limit <= 0)
            return;

        var queue = CollectTopOffsetCandidates(rows, limit, orderKeys);
        AppendSelectedTopOffsetRowsDirect(queue, target, skipCount, takeCount);
    }

    public static List<T> SelectTopRecords<T>(
        List<T> rows,
        int takeCount,
        IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(comparer);
        if (takeCount <= 0)
            return [];

        if (takeCount >= rows.Count)
        {
            rows.Sort(comparer);
            return rows;
        }

        var queue = CollectTopOffsetCandidates(rows, takeCount, comparer);
        return CopySelectedTopOffsetRecords(queue, 0, takeCount);
    }

    public static List<T> SelectTopOffsetRecords<T>(
        List<T> rows,
        int skipCount,
        int takeCount,
        IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(comparer);
        if (takeCount <= 0)
            return [];

        var limit = CalculateTopOffsetLimit(skipCount, takeCount);
        if (limit <= 0)
            return [];

        if (limit >= rows.Count)
        {
            rows.Sort(comparer);
            return CopySelectedTopOffsetRecords(rows, skipCount, takeCount);
        }

        var queue = CollectTopOffsetCandidates(rows, limit, comparer);
        return CopySelectedTopOffsetRecords(queue, skipCount, takeCount);
    }

    public sealed class BoundedTopRecordList<T> : IEnumerable<T>
    {
        private readonly int _skipCount;
        private readonly int _takeCount;
        private readonly int _limit;
        private readonly TopOffsetRecordComparer<T> _recordComparer;
        private readonly PriorityQueue<TopOffsetRecord<T>, TopOffsetRecord<T>>? _queue;
        private List<T>? _selectedRows;
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
                return;

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

            _selectedRows = _queue == null
                ? []
                : CopySelectedTopOffsetRecords(_queue, _skipCount, _takeCount);

            return _selectedRows;
        }
    }
}
