using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public static class QueryRows
{
    public static TableRows<TRow> FromTable<TRow>(Table table)
        where TRow : Row
    {
        return new TableRows<TRow>(table);
    }

    public static QueryShardedEnumerable<T> FromShards<T>(IReadOnlyList<ValueShard<T>> shards)
    {
        return new QueryShardedEnumerable<T>(shards);
    }

    public static QueryRowShardedEnumerable<TRow> FromRowShards<TRow>(IReadOnlyList<RowShard<TRow>> shards)
        where TRow : Row
    {
        return new QueryRowShardedEnumerable<TRow>(shards);
    }

    public static Table MaterializeTable<TRow>(
        string name,
        Column[] columns,
        IEnumerable<TRow> rows)
        where TRow : Row
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        if (rows is ITableMaterializationSource materializationSource &&
            materializationSource.TryMaterializeTable(name, columns, out var materializedTable))
        {
            return materializedTable;
        }

        var table = new Table(name, columns);
        AddRowsToTable(table, rows);

        return table;
    }

    public static Table DeferredTable<TRow>(
        string name,
        Column[] columns,
        Func<CancellationToken, IEnumerable<TRow>> rowsFactory,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rowsFactory);

        var table = new Table(name, columns);
        table.DeferMaterialization(target =>
        {
            try
            {
                var rows = new QueryTableEnumerable<TRow>(rowsFactory, cancellationToken);
                AddRowsToTable(target, rows);
            }
            catch (ScriptParameterBindingException ex)
            {
                throw QueryExecutionException.ForScriptParameterBinding(ex);
            }
        });

        return table;
    }

    private static void AddRowsToTable<TRow>(Table table, IEnumerable<TRow> rows)
        where TRow : Row
    {
        if (rows is ITableRowBatchSource<TRow> batchSource)
        {
            batchSource.AddTo(table);
            return;
        }

        if (rows.TryGetNonEnumeratedCount(out var count))
            table.EnsureCapacity(count);

        foreach (var row in rows)
            table.AddDirect(row);
    }
}
