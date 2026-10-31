using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static Table ToDistinctTable<TKey>(Table table, Func<Row, TKey> key)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(key);
        var result = new Table(table.Name, table.Columns.ToArray());
        var seen = new HashSet<TKey>();
        foreach (var row in table)
            if (seen.Add(key(row)))
                result.AddUnchecked(row);
        return result;
    }

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

}
