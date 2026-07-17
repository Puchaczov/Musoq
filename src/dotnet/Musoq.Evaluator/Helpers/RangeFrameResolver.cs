using System.Collections;
using System.Globalization;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static int ResolveRangeFrameStart(
        Array orderKeys,
        int[] partitionIndices,
        int partitionStart,
        int partitionCount,
        int currentPartitionIndex,
        int offsetInSortDirection,
        bool descending)
    {
        var target = ResolveRangeTarget(
            orderKeys,
            partitionIndices,
            partitionStart,
            currentPartitionIndex,
            offsetInSortDirection,
            descending);
        var low = 0;
        var high = partitionCount;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = GetRangeKey(orderKeys, partitionIndices[partitionStart + middle]);
            if (CompareRangeKeys(candidate, target, descending) < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    public static int ResolveRangeFrameEnd(
        Array orderKeys,
        int[] partitionIndices,
        int partitionStart,
        int partitionCount,
        int currentPartitionIndex,
        int offsetInSortDirection,
        bool descending)
    {
        var target = ResolveRangeTarget(
            orderKeys,
            partitionIndices,
            partitionStart,
            currentPartitionIndex,
            offsetInSortDirection,
            descending);
        var low = 0;
        var high = partitionCount;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = GetRangeKey(orderKeys, partitionIndices[partitionStart + middle]);
            if (CompareRangeKeys(candidate, target, descending) <= 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low - 1;
    }

    private static object? ResolveRangeTarget(
        Array orderKeys,
        int[] partitionIndices,
        int partitionStart,
        int currentPartitionIndex,
        int offsetInSortDirection,
        bool descending)
    {
        var current = GetRangeKey(
            orderKeys,
            partitionIndices[partitionStart + currentPartitionIndex]);
        if (offsetInSortDirection == 0)
            return current;

        if (current == null)
            throw new InvalidOperationException("Offset RANGE frames require non-null numeric order keys.");
        var numericCurrent = Convert.ToDecimal(current, CultureInfo.InvariantCulture);
        return descending
            ? numericCurrent - offsetInSortDirection
            : numericCurrent + offsetInSortDirection;
    }

    private static object? GetRangeKey(Array orderKeys, int rowIndex)
    {
        return orderKeys.GetValue(rowIndex);
    }

    private static int CompareRangeKeys(object? left, object? right, bool descending)
    {
        var comparison = right is decimal numericRight && left != null
            ? Convert.ToDecimal(left, CultureInfo.InvariantCulture).CompareTo(numericRight)
            : Comparer.DefaultInvariant.Compare(left, right);
        return descending ? -comparison : comparison;
    }
}
