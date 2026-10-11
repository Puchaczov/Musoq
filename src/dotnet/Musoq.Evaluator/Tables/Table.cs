using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

public partial class Table : IReadOnlyCollection<Row>, IReadOnlyTable, IDisposable
{
    private readonly List<Row> _rows = [];
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

    public Row this[int index]
    {
        get
        {
            FlushPendingRows();
            return _rows[index];
        }
    }

    public IReadOnlyList<Row> Rows
    {
        get
        {
            FlushPendingRows();
            return _rows;
        }
    }

    public IEnumerator<Row> GetEnumerator()
    {
        FlushPendingRows();
        return _rows.GetEnumerator();
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
            return _rows;
        }
    }

    public int Count
    {
        get
        {
            FlushPendingRows();
            return _rows.Count;
        }
    }

    public bool Contains(Row value)
    {
        FlushPendingRows();
        return _rows.Contains(value);
    }

    public bool Contains(Row value, Func<Row, Row, bool> comparer)
    {
        FlushPendingRows();
        ArgumentNullException.ThrowIfNull(comparer);
        foreach (var row in _rows)
            if (comparer(row, value))
                return true;
        return false;
    }

    private void AddColumns(params Column[] columns)
    {
        foreach (var column in columns)
        {
            _columnsByIndex.Add(column.ColumnIndex, column);

            if (_columnsByName.TryGetValue(column.ColumnName, out var value))
            {
                value.Add(column);
                continue;
            }

            _columnsByName.Add(column.ColumnName, [column]);
        }
    }
}
