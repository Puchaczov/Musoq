using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static long[] ComputeDenseRank(
        int rowCount, List<List<int>> sortedPartitions, object[] orderKeys)
    {
        return ComputeDenseRank<object>(rowCount, sortedPartitions, orderKeys);
    }

    public static long[] ComputeDenseRank(
        int rowCount, WindowPartitionSet sortedPartitions, object[] orderKeys)
    {
        return ComputeDenseRank<object>(rowCount, sortedPartitions, orderKeys);
    }

    public static long[] ComputeDenseRank<T>(
        int rowCount, List<List<int>> sortedPartitions, T[] orderKeys)
    {
        return ComputeDenseRank(rowCount, EnumeratePartitions(sortedPartitions), orderKeys);
    }

    public static long[] ComputeDenseRank<T>(
        int rowCount, WindowPartitionSet sortedPartitions, T[] orderKeys)
    {
        return ComputeDenseRank(rowCount, EnumeratePartitions(sortedPartitions), orderKeys);
    }

    private static long[] ComputeDenseRank<T>(
        int rowCount, WindowPartitionEnumerable sortedPartitions, T[] orderKeys)
    {
        if (orderKeys == null)
            return ComputeRowNumber(rowCount, sortedPartitions);

        var result = new long[rowCount];

        foreach (var partition in sortedPartitions)
        {
            long denseRank = 1;
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var currentIndex = partition[i];
                if (i > 0 && !EqualityComparer<T>.Default.Equals(orderKeys[currentIndex], orderKeys[partition[i - 1]]))
                    denseRank++;

                result[currentIndex] = denseRank;
            }
        }

        return result;
    }

    public static long[] ComputeDenseRankTopN(
        int rowCount, List<List<int>> sortedPartitions, object[] orderKeys, long maxRank)
    {
        return ComputeDenseRankTopN<object>(rowCount, sortedPartitions, orderKeys, maxRank);
    }

    public static long[] ComputeDenseRankTopN(
        int rowCount, WindowPartitionSet sortedPartitions, object[] orderKeys, long maxRank)
    {
        return ComputeDenseRankTopN<object>(rowCount, sortedPartitions, orderKeys, maxRank);
    }

    public static long[] ComputeDenseRankTopN<T>(
        int rowCount, List<List<int>> sortedPartitions, T[] orderKeys, long maxRank)
    {
        return ComputeDenseRankTopN(rowCount, EnumeratePartitions(sortedPartitions), orderKeys, maxRank);
    }

    public static long[] ComputeDenseRankTopN<T>(
        int rowCount, WindowPartitionSet sortedPartitions, T[] orderKeys, long maxRank)
    {
        return ComputeDenseRankTopN(rowCount, EnumeratePartitions(sortedPartitions), orderKeys, maxRank);
    }

    private static long[] ComputeDenseRankTopN<T>(
        int rowCount, WindowPartitionEnumerable sortedPartitions, T[] orderKeys, long maxRank)
    {
        if (orderKeys == null)
            return ComputeRowNumberTopN(rowCount, sortedPartitions, maxRank);

        var result = new long[rowCount];
        if (maxRank < 1)
            return result;

        foreach (var partition in sortedPartitions)
        {
            long denseRank = 1;
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var currentIndex = partition[i];
                if (i > 0 && !EqualityComparer<T>.Default.Equals(orderKeys[currentIndex], orderKeys[partition[i - 1]]))
                    denseRank++;

                if (denseRank > maxRank)
                    break;

                result[currentIndex] = denseRank;
            }
        }

        return result;
    }

    public static long[] ComputeDenseRank<TOrder>(
        int rowCount, TOrder[] orderKeys, bool[] orderDescending)
        where TOrder : IComparable<TOrder>
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var sorted = ResolveSortedUnpartitioned(rowCount, orderKeys, orderDescending);
        return ComputeDenseRank(rowCount, sorted, orderKeys);
    }

    public static long[] ComputeDenseRank<TPartition, TOrder>(
        int rowCount, TPartition[] partitionKeys, TOrder[] orderKeys, bool[] orderDescending)
        where TOrder : IComparable<TOrder>
    {
        var sorted = ResolveSortedPartitions(rowCount, partitionKeys, orderKeys, orderDescending);
        return ComputeDenseRank(rowCount, sorted, orderKeys);
    }

    public static long[] ComputeRowNumber(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeRowNumber(rowCount, sorted);
    }

    public static long[] ComputeRowNumber<TOrder>(
        int rowCount, TOrder[] orderKeys, bool[] orderDescending)
        where TOrder : IComparable<TOrder>
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var sorted = ResolveSortedUnpartitioned(rowCount, orderKeys, orderDescending);
        return ComputeRowNumber(rowCount, sorted);
    }

    public static long[] ComputeRowNumber<TPartition, TOrder>(
        int rowCount, TPartition[] partitionKeys, TOrder[] orderKeys, bool[] orderDescending)
        where TOrder : IComparable<TOrder>
    {
        var sorted = ResolveSortedPartitions(rowCount, partitionKeys, orderKeys, orderDescending);
        return ComputeRowNumber(rowCount, sorted);
    }

    public static long[] ComputeRank(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeRank(rowCount, sorted, orderKeys);
    }

    public static long[] ComputeDenseRank(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeDenseRank(rowCount, sorted, orderKeys);
    }
}
