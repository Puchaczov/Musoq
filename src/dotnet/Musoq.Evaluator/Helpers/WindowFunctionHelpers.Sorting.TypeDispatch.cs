using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private static void SortAllPartitions(
        List<List<int>> partitions, object[] orderKeys, bool[] descendingFlags)
    {
        var sample = FindSampleKey(partitions, orderKeys);

        if (sample == null)
            return;

        if (sample is CompositeKeyValue)
        {
            foreach (var partition in partitions)
                SortCompositePartition(partition, orderKeys, descendingFlags);
            return;
        }

        var descending = descendingFlags.Length > 0 && descendingFlags[0];

        switch (sample)
        {
            case int:
                SortPartitionsTyped<int>(partitions, orderKeys, descending);
                break;
            case long:
                SortPartitionsTyped<long>(partitions, orderKeys, descending);
                break;
            case decimal:
                SortPartitionsTyped<decimal>(partitions, orderKeys, descending);
                break;
            case double:
                SortPartitionsTyped<double>(partitions, orderKeys, descending);
                break;
            case float:
                SortPartitionsTyped<float>(partitions, orderKeys, descending);
                break;
            case string:
                SortPartitionsTyped<string>(partitions, orderKeys, descending);
                break;
            case DateTime:
                SortPartitionsTyped<DateTime>(partitions, orderKeys, descending);
                break;
            default:
                SortPartitionsGeneric(partitions, orderKeys, descending);
                break;
        }
    }

    private static object? FindSampleKey(List<List<int>> partitions, object[] orderKeys)
    {
        foreach (var partition in partitions)
        {
            foreach (var index in partition)
            {
                var key = orderKeys[index];
                if (key != null)
                    return key;
            }
        }

        return null;
    }

    private static IComparer<int> CreateObjectPartitionSetComparer(
        object sample, object[] orderKeys, bool[] descendingFlags)
    {
        var descending = descendingFlags.Length > 0 && descendingFlags[0];

        return sample switch
        {
            int => new BoxedTypedPartitionSetComparer<int>(orderKeys, descending),
            long => new BoxedTypedPartitionSetComparer<long>(orderKeys, descending),
            decimal => new BoxedTypedPartitionSetComparer<decimal>(orderKeys, descending),
            double => new BoxedTypedPartitionSetComparer<double>(orderKeys, descending),
            float => new BoxedTypedPartitionSetComparer<float>(orderKeys, descending),
            string => new BoxedTypedPartitionSetComparer<string>(orderKeys, descending),
            DateTime => new BoxedTypedPartitionSetComparer<DateTime>(orderKeys, descending),
            _ => new GenericObjectPartitionSetComparer(orderKeys, descending)
        };
    }

    private static object? FindSampleKey(WindowPartitionSet partitions, object[] orderKeys)
    {
        var indices = partitions.Indices;
        for (var partitionIndex = 0; partitionIndex < partitions.PartitionCount; partitionIndex++)
        {
            var start = partitions.GetStart(partitionIndex);
            var count = partitions.GetLength(partitionIndex);
            for (var index = 0; index < count; index++)
            {
                var key = orderKeys[indices[start + index]];
                if (key != null)
                    return key;
            }
        }

        return null;
    }
}
