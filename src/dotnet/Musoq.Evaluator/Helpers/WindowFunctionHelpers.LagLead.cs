using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static object?[] ComputeLag(
        int rowCount, List<List<int>> sortedPartitions,
        object[] values, int offset, object? defaultValue)
    {
        return ComputeLag<object>(rowCount, sortedPartitions, values, offset, defaultValue);
    }

    public static object?[] ComputeLag(
        int rowCount, WindowPartitionSet sortedPartitions,
        object[] values, int offset, object? defaultValue)
    {
        return ComputeLag<object>(rowCount, sortedPartitions, values, offset, defaultValue);
    }

    public static object?[] ComputeLag<T>(
        int rowCount, List<List<int>> sortedPartitions,
        T[] values, int offset, object? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        return ComputeLag(rowCount, EnumeratePartitions(sortedPartitions), values, offset, defaultValue);
    }

    public static object?[] ComputeLag<T>(
        int rowCount, WindowPartitionSet sortedPartitions,
        T[] values, int offset, object? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        return ComputeLag(rowCount, EnumeratePartitions(sortedPartitions), values, offset, defaultValue);
    }

    private static object?[] ComputeLag<T>(
        int rowCount, WindowPartitionEnumerable sortedPartitions,
        T[] values, int offset, object? defaultValue)
    {
        var result = new object?[rowCount];

        foreach (var partition in sortedPartitions)
        {
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var lagIndex = i - offset;
                result[partition[i]] = lagIndex >= 0
                    ? values[partition[lagIndex]]
                    : defaultValue;
            }
        }

        return result;
    }

    public static object?[] ComputeLag(
        int rowCount, List<List<int>> sortedPartitions,
        object[] values, int[] offsets, object?[] defaultValues)
    {
        return ComputeLag<object>(rowCount, sortedPartitions, values, offsets, defaultValues);
    }

    public static object?[] ComputeLag(
        int rowCount, WindowPartitionSet sortedPartitions,
        object[] values, int[] offsets, object?[] defaultValues)
    {
        return ComputeLag<object>(rowCount, sortedPartitions, values, offsets, defaultValues);
    }

    public static object?[] ComputeLag<T>(
        int rowCount, List<List<int>> sortedPartitions,
        T[] values, int[] offsets, object?[] defaultValues)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentNullException.ThrowIfNull(defaultValues);
        return ComputeLag(rowCount, EnumeratePartitions(sortedPartitions), values, offsets, defaultValues);
    }

    public static object?[] ComputeLag<T>(
        int rowCount, WindowPartitionSet sortedPartitions,
        T[] values, int[] offsets, object?[] defaultValues)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentNullException.ThrowIfNull(defaultValues);
        return ComputeLag(rowCount, EnumeratePartitions(sortedPartitions), values, offsets, defaultValues);
    }

    private static object?[] ComputeLag<T>(
        int rowCount, WindowPartitionEnumerable sortedPartitions,
        T[] values, int[] offsets, object?[] defaultValues)
    {
        var result = new object?[rowCount];

        foreach (var partition in sortedPartitions)
        {
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var currentIndex = partition[i];
                var lagIndex = i - offsets[currentIndex];
                result[currentIndex] = lagIndex >= 0
                    ? values[partition[lagIndex]]
                    : defaultValues[currentIndex];
            }
        }

        return result;
    }

    public static object?[] ComputeLead(
        int rowCount, List<List<int>> sortedPartitions,
        object[] values, int offset, object? defaultValue)
    {
        return ComputeLead<object>(rowCount, sortedPartitions, values, offset, defaultValue);
    }

    public static object?[] ComputeLead(
        int rowCount, WindowPartitionSet sortedPartitions,
        object[] values, int offset, object? defaultValue)
    {
        return ComputeLead<object>(rowCount, sortedPartitions, values, offset, defaultValue);
    }

    public static object?[] ComputeLead<T>(
        int rowCount, List<List<int>> sortedPartitions,
        T[] values, int offset, object? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        return ComputeLead(rowCount, EnumeratePartitions(sortedPartitions), values, offset, defaultValue);
    }

    public static object?[] ComputeLead<T>(
        int rowCount, WindowPartitionSet sortedPartitions,
        T[] values, int offset, object? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        return ComputeLead(rowCount, EnumeratePartitions(sortedPartitions), values, offset, defaultValue);
    }

    private static object?[] ComputeLead<T>(
        int rowCount, WindowPartitionEnumerable sortedPartitions,
        T[] values, int offset, object? defaultValue)
    {
        var result = new object?[rowCount];

        foreach (var partition in sortedPartitions)
        {
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var leadIndex = i + offset;
                result[partition[i]] = leadIndex < count
                    ? values[partition[leadIndex]]
                    : defaultValue;
            }
        }

        return result;
    }

    public static object?[] ComputeLead(
        int rowCount, List<List<int>> sortedPartitions,
        object[] values, int[] offsets, object?[] defaultValues)
    {
        return ComputeLead<object>(rowCount, sortedPartitions, values, offsets, defaultValues);
    }

    public static object?[] ComputeLead(
        int rowCount, WindowPartitionSet sortedPartitions,
        object[] values, int[] offsets, object?[] defaultValues)
    {
        return ComputeLead<object>(rowCount, sortedPartitions, values, offsets, defaultValues);
    }

    public static object?[] ComputeLead<T>(
        int rowCount, List<List<int>> sortedPartitions,
        T[] values, int[] offsets, object?[] defaultValues)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentNullException.ThrowIfNull(defaultValues);
        return ComputeLead(rowCount, EnumeratePartitions(sortedPartitions), values, offsets, defaultValues);
    }

    public static object?[] ComputeLead<T>(
        int rowCount, WindowPartitionSet sortedPartitions,
        T[] values, int[] offsets, object?[] defaultValues)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentNullException.ThrowIfNull(defaultValues);
        return ComputeLead(rowCount, EnumeratePartitions(sortedPartitions), values, offsets, defaultValues);
    }

    private static object?[] ComputeLead<T>(
        int rowCount, WindowPartitionEnumerable sortedPartitions,
        T[] values, int[] offsets, object?[] defaultValues)
    {
        var result = new object?[rowCount];

        foreach (var partition in sortedPartitions)
        {
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var currentIndex = partition[i];
                var leadIndex = i + offsets[currentIndex];
                result[currentIndex] = leadIndex < count
                    ? values[partition[leadIndex]]
                    : defaultValues[currentIndex];
            }
        }

        return result;
    }

    public static object?[] ComputeLag(
        int rowCount, object[] partitionKeys, object[] orderKeys,
        bool[] orderDescending, object[] values, int offset, object? defaultValue)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeLag(rowCount, sorted, values, offset, defaultValue);
    }

    public static object?[] ComputeLag(
        int rowCount, object[] partitionKeys, object[] orderKeys,
        bool[] orderDescending, object[] values, int[] offsets, object?[] defaultValues)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeLag(rowCount, sorted, values, offsets, defaultValues);
    }

    public static object?[] ComputeLead(
        int rowCount, object[] partitionKeys, object[] orderKeys,
        bool[] orderDescending, object[] values, int offset, object? defaultValue)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeLead(rowCount, sorted, values, offset, defaultValue);
    }

    public static object?[] ComputeLead(
        int rowCount, object[] partitionKeys, object[] orderKeys,
        bool[] orderDescending, object[] values, int[] offsets, object?[] defaultValues)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeLead(rowCount, sorted, values, offsets, defaultValues);
    }
}
