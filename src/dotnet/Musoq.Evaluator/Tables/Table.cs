using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

public partial class Table : IndexedList<Key, Row>, IReadOnlyCollection<Row>, IReadOnlyTable
{
    private readonly Dictionary<int, Column> _columnsByIndex;
    private readonly Dictionary<string, List<Column>> _columnsByName;

    public Table(string name, Column[] columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        Name = name;

        _columnsByIndex = new Dictionary<int, Column>();
        _columnsByName = new Dictionary<string, List<Column>>();
        _guard = new object();
        _pendingRows = new ConcurrentQueue<Row>();
        _pendingDirectRowShards = null;
        _pendingDirectRowCount = 0;
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

    public new IReadOnlyList<Row> Rows
    {
        get
        {
            FlushPendingRows();
            return base.Rows;
        }
    }

    public IEnumerator<Row> GetEnumerator()
    {
        FlushPendingRows();
        return base.Rows.GetEnumerator();
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
            return base.Rows;
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
