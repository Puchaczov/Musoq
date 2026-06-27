using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Tables;

public partial class Table
{
    private readonly object _guard;
    private readonly ConcurrentQueue<Row> _pendingRows;
    private List<DirectRowShard>? _pendingDirectRowShards;
    private int _pendingDirectRowCount;
    private volatile bool _hasPendingRows;

    public void Add(Row value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Count != _columnsByIndex.Count)
            throw new NotSupportedException(
                $"({nameof(Add)}) Current row has {value.Count} values but {_columnsByIndex.Count} required.");

        for (var i = 0; i < value.Count; i++)
        {
            if (value[i] == null)
                continue;

            var t1 = value[i].GetType();
            var t2 = _columnsByIndex[i].ColumnType;
            if (!t2.IsAssignableFrom(t1))
                throw new NotSupportedException(
                    $"({nameof(Add)}) Mismatched types. {t2.Name} is not assignable from {t1.Name}");
        }

        _pendingRows.Enqueue(value);
        _hasPendingRows = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddUnchecked(Row value)
    {
        _pendingRows.Enqueue(value);
        _hasPendingRows = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDirect(Row value)
    {
        base.Rows.Add(value);
    }

    /// <summary>
    ///     Pre-allocates the internal list capacity to avoid repeated resizing
    ///     when the number of rows to add is known ahead of time.
    /// </summary>
    public void EnsureCapacity(int capacity)
    {
        base.Rows.Capacity = Math.Max(base.Rows.Capacity, capacity);
    }

    public void AddRange(IEnumerable<Row> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach (var value in values) Add(value);
    }

    public void AddDirectDeferred<TRow>(RowShard<TRow>[] shards)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(shards);
        var totalCount = 0;
        foreach (var shard in shards)
            totalCount += shard.Count;

        if (totalCount == 0)
            return;

        lock (_guard)
        {
            _pendingDirectRowShards ??= new List<DirectRowShard>(shards.Length);
            foreach (var shard in shards)
            {
                if (shard.Count > 0)
                    _pendingDirectRowShards.Add(DirectRowShard.FromRows(shard.Rows, shard.Count));
            }

            _pendingDirectRowCount += totalCount;
            _hasPendingRows = true;
        }
    }

    public void AddDirectDeferred<TRow>(TRow[] rows, int count)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(rows);
        if ((uint)count > (uint)rows.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count == 0)
            return;

        lock (_guard)
        {
            _pendingDirectRowShards ??= new List<DirectRowShard>(1);
            _pendingDirectRowShards.Add(DirectRowShard.FromRows(rows, count));
            _pendingDirectRowCount += count;
            _hasPendingRows = true;
        }
    }

    public void AddDirectDeferred<TRow>(IReadOnlyList<TRow>[] shards)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(shards);
        var totalCount = 0;
        foreach (var shard in shards)
            totalCount += shard.Count;

        if (totalCount == 0)
            return;

        lock (_guard)
        {
            _pendingDirectRowShards ??= new List<DirectRowShard>(shards.Length);
            foreach (var shard in shards)
            {
                if (shard.Count > 0)
                    _pendingDirectRowShards.Add(DirectRowShard.FromList(shard));
            }

            _pendingDirectRowCount += totalCount;
            _hasPendingRows = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushPendingRows()
    {
        if (!_hasPendingRows)
            return;

        lock (_guard)
        {
            if (!_hasPendingRows)
                return;

            var deferredMaterializer = _deferredMaterializer;
            _deferredMaterializer = null;
            deferredMaterializer?.Invoke(this);

            while (_pendingRows.TryDequeue(out var row)) base.Rows.Add(row);
            FlushPendingDirectRows();
            _hasPendingRows = false;
        }
    }

    private void FlushPendingDirectRows()
    {
        if (_pendingDirectRowCount == 0)
            return;

        base.Rows.Capacity = Math.Max(base.Rows.Capacity, base.Rows.Count + _pendingDirectRowCount);
        var pendingDirectRowShards = _pendingDirectRowShards ??
            throw new InvalidOperationException("Pending direct row count was set without row shards.");
        foreach (var shard in pendingDirectRowShards)
            shard.AppendTo(base.Rows);

        pendingDirectRowShards.Clear();
        _pendingDirectRowCount = 0;
    }

    private readonly struct DirectRowShard
    {
        private readonly Row[]? _rows;
        private readonly IReadOnlyList<Row>? _list;

        private DirectRowShard(Row[]? rows, IReadOnlyList<Row>? list, int count)
        {
            _rows = rows;
            _list = list;
            Count = count;
        }

        public int Count { get; }

        public static DirectRowShard FromRows(Row[] rows, int count) => new(rows, null, count);

        public static DirectRowShard FromRows<TRow>(TRow[] rows, int count) where TRow : Row => new(null, rows, count);

        public static DirectRowShard FromList<TRow>(IReadOnlyList<TRow> rows) where TRow : Row => new(null, rows, rows.Count);

        public void AppendTo(List<Row> target)
        {
            if (_rows != null)
            {
                if (Count == _rows.Length)
                {
                    target.AddRange(_rows);
                    return;
                }

                for (var index = 0; index < Count; index++)
                    target.Add(_rows[index]);

                return;
            }

            var list = _list ?? throw new InvalidOperationException("Direct row shard contains neither array nor list rows.");
            for (var index = 0; index < Count; index++)
                target.Add(list[index]);
        }
    }
}
