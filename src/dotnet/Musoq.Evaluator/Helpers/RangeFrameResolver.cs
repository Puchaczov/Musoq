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
        return ResolveRangeFrameStart<object>(
            orderKeys,
            [],
            partitionIndices,
            partitionStart,
            partitionCount,
            currentPartitionIndex,
            offsetInSortDirection,
            descending,
            !descending);
    }

    public static int ResolveRangeFrameStart<TOrder>(
        Array orderKeys,
        TOrder[] peerOrderKeys,
        int[] partitionIndices,
        int partitionStart,
        int partitionCount,
        int currentPartitionIndex,
        int offsetInSortDirection,
        bool descending,
        bool nullsFirst)
    {
        if (GetCurrentRangeKey(orderKeys, partitionIndices, partitionStart, currentPartitionIndex) == null &&
            peerOrderKeys.Length != 0)
        {
            return ResolveRangePeerFrameStart(
                peerOrderKeys,
                partitionIndices,
                partitionStart,
                partitionCount,
                currentPartitionIndex);
        }

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
            if (CompareRangeKeys(candidate, target, descending, nullsFirst) < 0)
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
        return ResolveRangeFrameEnd<object>(
            orderKeys,
            [],
            partitionIndices,
            partitionStart,
            partitionCount,
            currentPartitionIndex,
            offsetInSortDirection,
            descending,
            !descending);
    }

    public static int ResolveRangeFrameEnd<TOrder>(
        Array orderKeys,
        TOrder[] peerOrderKeys,
        int[] partitionIndices,
        int partitionStart,
        int partitionCount,
        int currentPartitionIndex,
        int offsetInSortDirection,
        bool descending,
        bool nullsFirst)
    {
        if (GetCurrentRangeKey(orderKeys, partitionIndices, partitionStart, currentPartitionIndex) == null &&
            peerOrderKeys.Length != 0)
        {
            return ResolveRangePeerFrameEnd(
                peerOrderKeys,
                partitionIndices,
                partitionStart,
                partitionCount,
                currentPartitionIndex);
        }

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
            if (CompareRangeKeys(candidate, target, descending, nullsFirst) <= 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low - 1;
    }

    public static int ResolveRangePeerFrameStart<TOrder>(
        TOrder[] orderKeys,
        int[] partitionIndices,
        int partitionStart,
        int partitionCount,
        int currentPartitionIndex)
    {
        var current = orderKeys[partitionIndices[partitionStart + currentPartitionIndex]];
        var low = 0;
        var high = partitionCount;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = orderKeys[partitionIndices[partitionStart + middle]];
            if (System.Collections.Generic.Comparer<TOrder>.Default.Compare(candidate, current) < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    public static int ResolveRangePeerFrameEnd<TOrder>(
        TOrder[] orderKeys,
        int[] partitionIndices,
        int partitionStart,
        int partitionCount,
        int currentPartitionIndex)
    {
        var current = orderKeys[partitionIndices[partitionStart + currentPartitionIndex]];
        var low = 0;
        var high = partitionCount;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = orderKeys[partitionIndices[partitionStart + middle]];
            if (System.Collections.Generic.Comparer<TOrder>.Default.Compare(candidate, current) <= 0)
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
            return null;
        var numericCurrent = Convert.ToDecimal(current, CultureInfo.InvariantCulture);
        return descending
            ? numericCurrent - offsetInSortDirection
            : numericCurrent + offsetInSortDirection;
    }

    private static object? GetRangeKey(Array orderKeys, int rowIndex)
    {
        return orderKeys.GetValue(rowIndex);
    }

    private static object? GetCurrentRangeKey(
        Array orderKeys,
        int[] partitionIndices,
        int partitionStart,
        int currentPartitionIndex)
    {
        return GetRangeKey(orderKeys, partitionIndices[partitionStart + currentPartitionIndex]);
    }

    private static int CompareRangeKeys(
        object? left,
        object? right,
        bool descending,
        bool nullsFirst)
    {
        if (left == null)
            return right == null ? 0 : nullsFirst ? -1 : 1;

        if (right == null)
            return nullsFirst ? 1 : -1;

        var comparison = right is decimal numericRight
            ? Convert.ToDecimal(left, CultureInfo.InvariantCulture).CompareTo(numericRight)
            : Comparer.DefaultInvariant.Compare(left, right);
        return descending ? -comparison : comparison;
    }
}
