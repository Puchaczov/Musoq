using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

public class Table : IndexedList<Key, Row>, IEnumerable<Row>, IReadOnlyTable
{
    private readonly Dictionary<int, Column> _columnsByIndex;
    private readonly Dictionary<string, List<Column>> _columnsByName;
    private readonly object _guard;
    private readonly ConcurrentQueue<Row> _pendingRows;
    private volatile bool _hasPendingRows;

    public Table(string name, Column[] columns)
    {
        Name = name;

        _columnsByIndex = new Dictionary<int, Column>();
        _columnsByName = new Dictionary<string, List<Column>>();
        _guard = new object();
        _pendingRows = new ConcurrentQueue<Row>();
        _hasPendingRows = false;

        AddColumns(columns);
    }

    public string Name { get; }

    public IEnumerable<Column> Columns => _columnsByIndex.Values;

    public override Row this[int index]
    {
        get
        {
            FlushPendingRows();
            return base[index];
        }
    }

    public override IEnumerable<Row> this[Key key]
    {
        get
        {
            FlushPendingRows();
            return base[key];
        }
    }

    public IEnumerator<Row> GetEnumerator()
    {
        FlushPendingRows();
        return Rows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IReadOnlyList<IReadOnlyRow> IReadOnlyTable.Rows
    {
        get
        {
            FlushPendingRows();
            return Rows;
        }
    }

    public override int Count
    {
        get
        {
            FlushPendingRows();
            return base.Count;
        }
    }

    public override bool Contains(Row value)
    {
        FlushPendingRows();
        return base.Contains(value);
    }

    public override bool Contains(Row value, Func<Row, Row, bool> comparer)
    {
        FlushPendingRows();
        return base.Contains(value, comparer);
    }

    public override bool Contains(Key key, Row value)
    {
        FlushPendingRows();
        return base.Contains(key, value);
    }

    public override bool ContainsKey(Key key)
    {
        FlushPendingRows();
        return base.ContainsKey(key);
    }

    public override bool TryGetIndexedValues(Key key, out IReadOnlyList<Row> values)
    {
        FlushPendingRows();
        return base.TryGetIndexedValues(key, out values);
    }

    public void Add(Row value)
    {
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

    public void AddRange(IEnumerable<Row> values)
    {
        foreach (var value in values) Add(value);
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

            while (_pendingRows.TryDequeue(out var row)) Rows.Add(row);
            _hasPendingRows = false;
        }
    }

    private void AddColumns(params Column[] columns)
    {
        foreach (var column in columns)
        {
            _columnsByIndex.Add(column.ColumnIndex, column);

            if (_columnsByName.TryGetValue(column.ColumnName, out var value))
            {
                var firstValue = value.First();

                if (firstValue.ColumnType != column.ColumnType)
                    throw new NotSupportedException(
                        $"({nameof(AddColumns)}) Mismatched types. {firstValue.ColumnType.Name} is not assignable from {column.ColumnType.Name}");

                value.Add(column);
                continue;
            }

            _columnsByName.Add(column.ColumnName, [column]);
        }
    }
}
