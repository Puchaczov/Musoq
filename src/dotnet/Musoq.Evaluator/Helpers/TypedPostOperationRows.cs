using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static class TypedPostOperationRows
{
    public static IEnumerable<TRow> Distinct<TRow>(IEnumerable<TRow> rows)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(rows);
        var seenRows = new HashSet<Row>();
        foreach (var row in rows)
        {
            if (seenRows.Add(row))
                yield return row;
        }
    }

    public static IOrderedEnumerable<TRow> Order<TRow>(
        IEnumerable<TRow> rows,
        IReadOnlyList<TypedRowOrderKey<TRow>> orderKeys)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(orderKeys);
        return rows.OrderBy(static row => row, new TypedRowOrderComparer<TRow>(orderKeys));
    }

    public static IEnumerable<TOut> Project<TRow, TOut>(
        IEnumerable<TRow> rows,
        Func<TRow, TOut> projector)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(projector);
        foreach (var row in rows)
            yield return projector(row);
    }

    private sealed class TypedRowOrderComparer<TRow>(IReadOnlyList<TypedRowOrderKey<TRow>> orderKeys) : IComparer<TRow>
        where TRow : Row
    {
        public int Compare(TRow? left, TRow? right)
        {
            if (ReferenceEquals(left, right))
                return 0;

            if (left == null)
                return -1;

            if (right == null)
                return 1;

            foreach (var key in orderKeys)
            {
                var comparison = RowOrderingComparison.CompareValues(
                    key.Selector(left),
                    key.Selector(right),
                    key.Descending,
                    key.NullOrdering);
                if (comparison != 0)
                    return comparison;
            }

            return 0;
        }
    }
}
