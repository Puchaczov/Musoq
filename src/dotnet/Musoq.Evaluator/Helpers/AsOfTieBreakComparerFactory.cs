using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.Helpers;

internal static class AsOfTieBreakComparerFactory
{
    public static IComparer<TTie> Create<TTie>(bool descending, NullOrdering nullOrdering)
    {
        var valueComparer = typeof(TTie) == typeof(object)
            ? (IComparer<TTie>)(object)AsOfObjectKeyComparer.Instance
            : Comparer<TTie>.Default;

        return new AsOfTieBreakComparer<TTie>(valueComparer, descending, nullOrdering);
    }

    private sealed class AsOfTieBreakComparer<TTie>(
        IComparer<TTie> valueComparer,
        bool descending,
        NullOrdering nullOrdering) : IComparer<TTie>
    {
        private static readonly bool AllowsNull =
            !typeof(TTie).IsValueType || Nullable.GetUnderlyingType(typeof(TTie)) != null;

        public int Compare(TTie? left, TTie? right)
        {
            if (AllowsNull)
            {
                var leftIsNull = left is null;
                var rightIsNull = right is null;
                if (leftIsNull || rightIsNull)
                {
                    if (leftIsNull && rightIsNull)
                        return 0;

                    var nullsFirst = nullOrdering switch
                    {
                        NullOrdering.First => true,
                        NullOrdering.Last => false,
                        _ => !descending
                    };

                    return leftIsNull == nullsFirst ? -1 : 1;
                }
            }

            var comparison = valueComparer.Compare(left!, right!);
            return descending ? -comparison : comparison;
        }
    }
}
