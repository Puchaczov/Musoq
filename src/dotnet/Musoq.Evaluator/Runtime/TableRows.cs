using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public sealed class TableRows<TRow> : ITableRowBatchSource<TRow>, ITableMaterializationSource, IKnownCountRows<TRow>
    where TRow : Row
{
    private readonly Table _table;

    public TableRows(Table table)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
    }

    public int Count => _table.Count;

    public void AddTo(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);
        table.EnsureCapacity(table.Count + _table.Count);
        foreach (var row in _table.Rows)
            table.AddDirect(row);
    }

    public bool TryMaterializeTable(string name, IReadOnlyList<Column> columns, out Table table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(columns);

        if (string.Equals(_table.Name, name, StringComparison.Ordinal) &&
            ColumnsMatch(_table, columns))
        {
            table = _table;
            return true;
        }

        table = null!;
        return false;
    }

    public IEnumerator<TRow> GetEnumerator()
    {
        foreach (var row in _table.Rows)
            yield return (TRow)row;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static bool ColumnsMatch(Table table, IReadOnlyList<Column> columns)
    {
        var tableColumns = table.Columns
            .OrderBy(static column => column.ColumnIndex)
            .ToArray();

        if (tableColumns.Length != columns.Count)
            return false;

        for (var index = 0; index < tableColumns.Length; index++)
        {
            if (!tableColumns[index].Equals(columns[index]))
                return false;
        }

        return true;
    }
}
