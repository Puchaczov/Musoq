namespace Musoq.Evaluator.Helpers;

internal static class RowOrderingComparison
{
    public static int CompareValues(object? left, object? right, bool descending, int nullOrdering)
    {
        if (ReferenceEquals(left, right))
            return 0;

        if (left is null)
            return CompareLeftNull(right, descending, nullOrdering);

        if (right is null)
            return CompareRightNull(descending, nullOrdering);

        var comparison = left is string leftString && right is string rightString
            ? StringComparer.Ordinal.Compare(leftString, rightString)
            : left is IComparable comparable
                ? comparable.CompareTo(right)
                : throw new InvalidOperationException($"ORDER BY key type '{left.GetType().FullName}' does not support comparison.");

        return descending ? -comparison : comparison;
    }

    private static int CompareLeftNull(object? right, bool descending, int nullOrdering)
    {
        if (right is null)
            return 0;

        return nullOrdering switch
        {
            2 => 1,
            1 => -1,
            _ => descending ? 1 : -1
        };
    }

    private static int CompareRightNull(bool descending, int nullOrdering)
    {
        return nullOrdering switch
        {
            2 => -1,
            1 => 1,
            _ => descending ? -1 : 1
        };
    }
}
