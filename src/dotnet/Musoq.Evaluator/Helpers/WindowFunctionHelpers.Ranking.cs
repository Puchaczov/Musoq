using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static long[] ComputeRowNumber(int rowCount, List<List<int>> sortedPartitions)
    {
        return ComputeRowNumber(rowCount, EnumeratePartitions(sortedPartitions));
    }

    public static long[] ComputeRowNumber(int rowCount, WindowPartitionSet sortedPartitions)
    {
        return ComputeRowNumber(rowCount, EnumeratePartitions(sortedPartitions));
    }

    private static long[] ComputeRowNumber(int rowCount, WindowPartitionEnumerable sortedPartitions)
    {
        var result = new long[rowCount];

        foreach (var partition in sortedPartitions)
        {
            long rowNum = 1;
            var count = partition.Count;
            for (var i = 0; i < count; i++)
                result[partition[i]] = rowNum++;
        }

        return result;
    }

    public static long[] ComputeRowNumberTopN(int rowCount, List<List<int>> sortedPartitions, long maxRowNumber)
    {
        return ComputeRowNumberTopN(rowCount, EnumeratePartitions(sortedPartitions), maxRowNumber);
    }

    public static long[] ComputeRowNumberTopN(int rowCount, WindowPartitionSet sortedPartitions, long maxRowNumber)
    {
        return ComputeRowNumberTopN(rowCount, EnumeratePartitions(sortedPartitions), maxRowNumber);
    }

    private static long[] ComputeRowNumberTopN(
        int rowCount, WindowPartitionEnumerable sortedPartitions, long maxRowNumber)
    {
        var result = new long[rowCount];
        if (maxRowNumber < 1)
            return result;

        foreach (var partition in sortedPartitions)
        {
            var count = Math.Min(partition.Count, maxRowNumber);
            for (var i = 0; i < count; i++)
                result[partition[i]] = i + 1L;
        }

        return result;
    }

    public static long[] ComputeRank(
        int rowCount, List<List<int>> sortedPartitions, object[] orderKeys)
    {
        return ComputeRank<object>(rowCount, sortedPartitions, orderKeys);
    }

    public static long[] ComputeRank(
        int rowCount, WindowPartitionSet sortedPartitions, object[] orderKeys)
    {
        return ComputeRank<object>(rowCount, sortedPartitions, orderKeys);
    }

    public static long[] ComputeRank<T>(
        int rowCount, List<List<int>> sortedPartitions, T[] orderKeys)
    {
        return ComputeRank(rowCount, EnumeratePartitions(sortedPartitions), orderKeys);
    }

    public static long[] ComputeRank<T>(
        int rowCount, WindowPartitionSet sortedPartitions, T[] orderKeys)
    {
        return ComputeRank(rowCount, EnumeratePartitions(sortedPartitions), orderKeys);
    }

    private static long[] ComputeRank<T>(
        int rowCount, WindowPartitionEnumerable sortedPartitions, T[] orderKeys)
    {
        if (orderKeys == null)
            return ComputeRowNumber(rowCount, sortedPartitions);

        var result = new long[rowCount];

        foreach (var partition in sortedPartitions)
        {
            long rank = 1;
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var currentIndex = partition[i];
                if (i > 0 && !EqualityComparer<T>.Default.Equals(orderKeys[currentIndex], orderKeys[partition[i - 1]]))
                    rank = i + 1;

                result[currentIndex] = rank;
            }
        }

        return result;
    }

    public static long[] ComputeRankTopN(
        int rowCount, List<List<int>> sortedPartitions, object[] orderKeys, long maxRank)
    {
        return ComputeRankTopN<object>(rowCount, sortedPartitions, orderKeys, maxRank);
    }

    public static long[] ComputeRankTopN(
        int rowCount, WindowPartitionSet sortedPartitions, object[] orderKeys, long maxRank)
    {
        return ComputeRankTopN<object>(rowCount, sortedPartitions, orderKeys, maxRank);
    }

    public static long[] ComputeRankTopN<T>(
        int rowCount, List<List<int>> sortedPartitions, T[] orderKeys, long maxRank)
    {
        return ComputeRankTopN(rowCount, EnumeratePartitions(sortedPartitions), orderKeys, maxRank);
    }

    public static long[] ComputeRankTopN<T>(
        int rowCount, WindowPartitionSet sortedPartitions, T[] orderKeys, long maxRank)
    {
        return ComputeRankTopN(rowCount, EnumeratePartitions(sortedPartitions), orderKeys, maxRank);
    }

    private static long[] ComputeRankTopN<T>(
        int rowCount, WindowPartitionEnumerable sortedPartitions, T[] orderKeys, long maxRank)
    {
        if (orderKeys == null)
            return ComputeRowNumberTopN(rowCount, sortedPartitions, maxRank);

        var result = new long[rowCount];
        if (maxRank < 1)
            return result;

        foreach (var partition in sortedPartitions)
        {
            long rank = 1;
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var currentIndex = partition[i];
                if (i > 0 && !EqualityComparer<T>.Default.Equals(orderKeys[currentIndex], orderKeys[partition[i - 1]]))
                    rank = i + 1L;

                if (rank > maxRank)
                    break;

                result[currentIndex] = rank;
            }
        }

        return result;
    }

    public static long[] ComputeRank<TOrder>(
        int rowCount, TOrder[] orderKeys, bool[] orderDescending)
        where TOrder : IComparable<TOrder>
    {
        ArgumentNullException.ThrowIfNull(orderDescending);
        var sorted = ResolveSortedUnpartitioned(rowCount, orderKeys, orderDescending);
        return ComputeRank(rowCount, sorted, orderKeys);
    }

    public static long[] ComputeRank<TPartition, TOrder>(
        int rowCount, TPartition[] partitionKeys, TOrder[] orderKeys, bool[] orderDescending)
        where TOrder : IComparable<TOrder>
    {
        var sorted = ResolveSortedPartitions(rowCount, partitionKeys, orderKeys, orderDescending);
        return ComputeRank(rowCount, sorted, orderKeys);
    }
}
