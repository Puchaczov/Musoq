using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static List<List<int>> ResolvePartitions(int rowCount, object?[]? partitionKeys)
    {
        if (partitionKeys == null)
            return [CreateSequentialIndices(rowCount)];

        return GroupByPartition(rowCount, partitionKeys);
    }

    public static List<List<int>> ResolvePartitions<T>(int rowCount, T[]? partitionKeys)
    {
        if (partitionKeys == null)
            return [CreateSequentialIndices(rowCount)];

        return GroupByPartition(rowCount, partitionKeys);
    }

    public static WindowPartitionSet ResolvePartitionSet(int rowCount, object?[]? partitionKeys)
    {
        if (partitionKeys == null)
            return WindowPartitionSet.Sequential(rowCount);

        return GroupPartitionSet(rowCount, partitionKeys);
    }

    public static WindowPartitionSet ResolvePartitionSet<T>(int rowCount, T[]? partitionKeys)
    {
        if (partitionKeys == null)
            return WindowPartitionSet.Sequential(rowCount);

        return GroupPartitionSet(rowCount, partitionKeys);
    }

    private static List<int> CreateSequentialIndices(int rowCount)
    {
        var list = new List<int>(rowCount);
        for (var i = 0; i < rowCount; i++)
            list.Add(i);
        return list;
    }

    private static WindowPartitionSet GroupPartitionSet(int rowCount, object?[] partitionKeys)
    {
        if (rowCount == 0)
            return WindowPartitionSet.Empty(rowCount);

        var builder = new WindowPartitionBuilder<object>(rowCount, nullPartitionFirst: false);
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            builder.Add(partitionKeys[rowIndex] ?? DBNull.Value, rowIndex);

        return builder.ToPartitionSet();
    }

    private static WindowPartitionSet GroupPartitionSet<T>(int rowCount, T[] partitionKeys)
    {
        if (rowCount == 0)
            return WindowPartitionSet.Empty(rowCount);

        var builder = new WindowPartitionBuilder<T>(rowCount);
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            builder.Add(partitionKeys[rowIndex], rowIndex);

        return builder.ToPartitionSet();
    }

    private static List<List<int>> GroupByPartition(int rowCount, object?[] partitionKeys)
    {
        if (rowCount == 0)
            return [];

        var groups = new Dictionary<object, List<int>>(ObjectKeyComparer.Instance);
        for (var i = 0; i < rowCount; i++)
        {
            var key = partitionKeys[i] ?? DBNull.Value;
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<int>();
                groups[key] = list;
            }

            list.Add(i);
        }

        var result = new List<List<int>>(groups.Count);
        foreach (var kvp in groups)
            result.Add(kvp.Value);

        return result;
    }

    private static List<List<int>> GroupByPartition<T>(int rowCount, T[] partitionKeys)
    {
        if (rowCount == 0)
            return [];

        var groups = new Dictionary<object, List<int>>(ObjectKeyComparer.Instance);
        List<int>? nullGroup = null;

        for (var i = 0; i < rowCount; i++)
        {
            var value = partitionKeys[i];
            if (value == null)
            {
                nullGroup ??= [];
                nullGroup.Add(i);
                continue;
            }

            var key = (object)value;

            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<int>();
                groups[key] = list;
            }

            list.Add(i);
        }

        var result = new List<List<int>>(groups.Count + (nullGroup != null ? 1 : 0));
        if (nullGroup != null)
            result.Add(nullGroup);

        foreach (var kvp in groups)
            result.Add(kvp.Value);

        return result;
    }

    private sealed class ObjectKeyComparer : IEqualityComparer<object>
    {
        public static readonly ObjectKeyComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object? x, object? y) => Equals(x, y);

        public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
    }
}
