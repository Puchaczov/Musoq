using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static List<List<int>> SortPartitions(
        List<List<int>> partitions, object[] orderKeys, bool[] orderDescending)
    {
        ArgumentNullException.ThrowIfNull(partitions);
        ArgumentNullException.ThrowIfNull(orderDescending);
        var sorted = new List<List<int>>(partitions.Count);
        foreach (var partition in partitions)
            sorted.Add([..partition]);

        SortAllPartitions(sorted, orderKeys, orderDescending);
        return sorted;
    }

    public static List<List<int>> SortPartitions<T>(
        List<List<int>> partitions, T[] orderKeys, bool[] orderDescending)
        where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(partitions);
        ArgumentNullException.ThrowIfNull(orderDescending);
        var sorted = new List<List<int>>(partitions.Count);
        foreach (var partition in partitions)
            sorted.Add([..partition]);

        var descending = orderDescending.Length > 0 && orderDescending[0];
        SortPartitionsTypedDirect(sorted, orderKeys, descending);
        return sorted;
    }

    public static WindowPartitionSet SortPartitionSet(
        WindowPartitionSet partitions, object[] orderKeys, bool[] orderDescending)
    {
        ArgumentNullException.ThrowIfNull(partitions);
        var sorted = partitions.Copy();
        return SortPartitionSetInPlace(sorted, orderKeys, orderDescending);
    }

    public static WindowPartitionSet SortPartitionSet<T>(
        WindowPartitionSet partitions, T[] orderKeys, bool[] orderDescending)
        where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(partitions);
        var sorted = partitions.Copy();
        return SortPartitionSetInPlace(sorted, orderKeys, orderDescending);
    }

    public static WindowPartitionSet SortPartitionSet(
        WindowPartitionSet partitions, int[] orderKeys, bool[] orderDescending)
    {
        ArgumentNullException.ThrowIfNull(partitions);
        var sorted = partitions.Copy();
        return SortPartitionSetInPlace(sorted, orderKeys, orderDescending);
    }

    public static WindowPartitionSet SortPartitionSetInPlace(
        WindowPartitionSet partitions, object[] orderKeys, bool[] orderDescending)
    {
        SortPartitionSetObjectInPlace(partitions, orderKeys, orderDescending);
        return partitions;
    }

    public static WindowPartitionSet SortPartitionSetInPlace<T>(
        WindowPartitionSet partitions, T[] orderKeys, bool[] orderDescending)
        where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        return SortPartitionSetInPlace(partitions, orderKeys, orderDescending.Length > 0 && orderDescending[0]);
    }

    public static WindowPartitionSet SortPartitionSetInPlace(
        WindowPartitionSet partitions, int[] orderKeys, bool[] orderDescending)
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        return SortPartitionSetInPlace(partitions, orderKeys, orderDescending.Length > 0 && orderDescending[0]);
    }

    public static WindowPartitionSet SortPartitionSetInPlace(
        WindowPartitionSet partitions, int[] orderKeys, bool descending)
    {
        ArgumentNullException.ThrowIfNull(partitions);
        ArgumentNullException.ThrowIfNull(orderKeys);
        SortPartitionSetByIntKeys(partitions, orderKeys, descending);
        return partitions;
    }

    public static WindowPartitionSet SortPartitionSetInPlace<T>(
        WindowPartitionSet partitions, T[] orderKeys, bool descending)
        where T : IComparable<T>
    {
        SortPartitionSetTypedDirectInPlace(partitions, orderKeys, descending);
        return partitions;
    }

    public static List<List<int>> ResolveSortedPartitions(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var partitions = partitionKeys == null
            ? [CreateSequentialIndices(rowCount)]
            : GroupByPartition(rowCount, partitionKeys);

        SortAllPartitions(partitions, orderKeys, orderDescending);
        return partitions;
    }

    public static List<List<int>> ResolveSortedPartitions<TPart, TOrd>(
        int rowCount, TPart[] partitionKeys, TOrd[] orderKeys, bool[] orderDescending)
        where TOrd : IComparable<TOrd>
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var partitions = partitionKeys == null
            ? [CreateSequentialIndices(rowCount)]
            : GroupByPartition(rowCount, partitionKeys);

        var descending = orderDescending.Length > 0 && orderDescending[0];
        SortPartitionsTypedDirect(partitions, orderKeys, descending);
        return partitions;
    }

    public static List<List<int>> ResolveSortedPartitions<TOrd>(
        int rowCount, object[] partitionKeys, TOrd[] orderKeys, bool[] orderDescending)
        where TOrd : IComparable<TOrd>
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var partitions = partitionKeys == null
            ? [CreateSequentialIndices(rowCount)]
            : GroupByPartition(rowCount, partitionKeys);

        var descending = orderDescending.Length > 0 && orderDescending[0];
        SortPartitionsTypedDirect(partitions, orderKeys, descending);
        return partitions;
    }

    public static WindowPartitionSet ResolveSortedPartitionSet(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var partitions = partitionKeys == null
            ? [CreateSequentialIndices(rowCount)]
            : GroupByPartition(rowCount, partitionKeys);

        SortAllPartitions(partitions, orderKeys, orderDescending);
        return WindowPartitionSet.FromLists(rowCount, partitions);
    }

    public static WindowPartitionSet ResolveSortedPartitionSet<TPart, TOrd>(
        int rowCount, TPart[] partitionKeys, TOrd[] orderKeys, bool[] orderDescending)
        where TOrd : IComparable<TOrd>
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var partitions = partitionKeys == null
            ? [CreateSequentialIndices(rowCount)]
            : GroupByPartition(rowCount, partitionKeys);

        var descending = orderDescending.Length > 0 && orderDescending[0];
        SortPartitionsTypedDirect(partitions, orderKeys, descending);
        return WindowPartitionSet.FromLists(rowCount, partitions);
    }

    public static WindowPartitionSet ResolveSortedPartitionSet<TOrd>(
        int rowCount, object[] partitionKeys, TOrd[] orderKeys, bool[] orderDescending)
        where TOrd : IComparable<TOrd>
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var partitions = partitionKeys == null
            ? [CreateSequentialIndices(rowCount)]
            : GroupByPartition(rowCount, partitionKeys);

        var descending = orderDescending.Length > 0 && orderDescending[0];
        SortPartitionsTypedDirect(partitions, orderKeys, descending);
        return WindowPartitionSet.FromLists(rowCount, partitions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareValues(object a, object b, bool descending)
    {
        if (a is IComparable ca)
        {
            if (b is IComparable cb)
            {
                var cmp = ca.CompareTo(cb);
                return descending ? -cmp : cmp;
            }

            return descending ? -1 : 1;
        }

        if (b is IComparable)
            return descending ? 1 : -1;

        return 0;
    }
}
