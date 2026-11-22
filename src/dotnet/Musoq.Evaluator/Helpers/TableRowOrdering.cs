using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static IOrderedEnumerable<Row> OrderRows(IEnumerable<Row> rows, IReadOnlyList<RowOrderKey> orderKeys)
    {
        return rows.OrderBy(static row => row, new RowOrderComparer(orderKeys));
    }

    public static void SortRecordsIfUnordered<T>(List<T> records, IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(comparer);

        const int MaxInsertionsBeforeFullSort = 8;
        var values = CollectionsMarshal.AsSpan(records);
        var insertionsRemaining = MaxInsertionsBeforeFullSort;
        var shiftsRemaining = values.Length;
        var index = 1;
        for (; index < values.Length; index++)
        {
            var value = values[index];
            if (comparer.Compare(values[index - 1], value) <= 0)
                continue;

            if (insertionsRemaining-- == 0)
                break;

            var insertionIndex = FindUpperBound(values[..index], value, comparer);
            var shift = index - insertionIndex;
            if (shift > shiftsRemaining)
                break;

            values.Slice(insertionIndex, shift).CopyTo(values[(insertionIndex + 1)..]);
            values[insertionIndex] = value;
            shiftsRemaining -= shift;
        }

        if (index < values.Length)
            records.Sort(comparer);
    }

    private static int FindUpperBound<T>(ReadOnlySpan<T> sorted, T value, IComparer<T> comparer)
    {
        var first = 0;
        var end = sorted.Length;
        while (first < end)
        {
            var middle = first + (end - first) / 2;
            if (comparer.Compare(sorted[middle], value) <= 0)
                first = middle + 1;
            else
                end = middle;
        }

        return first;
    }

    private sealed class RowOrderComparer(IReadOnlyList<RowOrderKey> orderKeys) : IComparer<Row>
    {
        public int Compare(Row? left, Row? right)
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
                    key.NullOrdering
                );
                if (comparison != 0)
                    return comparison;
            }

            return 0;
        }
    }
}
