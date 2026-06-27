using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    private readonly struct TopOffsetRow(Row row, int ordinal)
    {
        public Row Row { get; } = row;

        public int Ordinal { get; } = ordinal;
    }

    private readonly struct TopOffsetRecord<T>(T record, int ordinal)
    {
        public T Record { get; } = record;

        public int Ordinal { get; } = ordinal;
    }

    private sealed class TopOffsetRowComparer(IReadOnlyList<RowOrderKey> orderKeys) : IComparer<TopOffsetRow>
    {
        public int Compare(TopOffsetRow left, TopOffsetRow right)
        {
            foreach (var key in orderKeys)
            {
                var comparison = RowOrderingComparison.CompareValues(key.Selector(left.Row), key.Selector(right.Row), key.Descending, key.NullOrdering);
                if (comparison == 0)
                    continue;

                return comparison;
            }

            return left.Ordinal.CompareTo(right.Ordinal);
        }
    }

    private sealed class TopOffsetRecordComparer<T>(IComparer<T> comparer) : IComparer<TopOffsetRecord<T>>
    {
        public int Compare(TopOffsetRecord<T> left, TopOffsetRecord<T> right)
        {
            var comparison = comparer.Compare(left.Record, right.Record);
            return comparison != 0
                ? comparison
                : left.Ordinal.CompareTo(right.Ordinal);
        }
    }

    private sealed class ReverseTopOffsetRowComparer(IComparer<TopOffsetRow> inner) : IComparer<TopOffsetRow>
    {
        public int Compare(TopOffsetRow left, TopOffsetRow right)
        {
            return inner.Compare(right, left);
        }
    }

    private sealed class ReverseTopOffsetRecordComparer<T>(IComparer<TopOffsetRecord<T>> inner) : IComparer<TopOffsetRecord<T>>
    {
        public int Compare(TopOffsetRecord<T> left, TopOffsetRecord<T> right)
        {
            return inner.Compare(right, left);
        }
    }
}
