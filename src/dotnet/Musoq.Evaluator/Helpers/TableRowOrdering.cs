using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static IOrderedEnumerable<Row> OrderRows(
        IEnumerable<Row> rows,
        IReadOnlyList<RowOrderKey> orderKeys)
    {
        return rows.OrderBy(static row => row, new RowOrderComparer(orderKeys));
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
                    key.NullOrdering);
                if (comparison != 0)
                    return comparison;
            }

            return 0;
        }
    }
}
