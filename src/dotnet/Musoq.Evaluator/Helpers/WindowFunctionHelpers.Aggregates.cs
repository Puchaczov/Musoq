using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static object?[] ComputeWindowedAggregate(
        int rowCount, List<List<int>> partitions,
        bool isSorted, object?[] values, string aggregateName)
    {
        return ComputeWindowedAggregate(rowCount, EnumeratePartitions(partitions), isSorted, values, aggregateName);
    }

    public static object?[] ComputeWindowedAggregate(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, object?[] values, string aggregateName)
    {
        return ComputeWindowedAggregate(rowCount, EnumeratePartitions(partitions), isSorted, values, aggregateName);
    }

    private static object?[] ComputeWindowedAggregate(
        int rowCount, WindowPartitionEnumerable partitions,
        bool isSorted, object?[] values, string aggregateName)
    {
        var result = new object?[rowCount];
        var aggType = ParseAggregateType(aggregateName);

        foreach (var partition in partitions)
        {
            if (isSorted)
            {
                ComputeRunningAggregate(partition, values, aggType, result);
            }
            else
            {
                var wholeValue = ComputeWholePartitionAggregate(partition, values, aggType);
                var count = partition.Count;
                for (var i = 0; i < count; i++)
                    result[partition[i]] = wholeValue;
            }
        }

        return result;
    }

    public static object?[] ComputeWindowedAggregate(
        int rowCount, object[] partitionKeys, object[] orderKeys,
        bool hasOrderBy, bool[] orderDescending, object?[] values, string aggregateName)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);

        if (hasOrderBy)
        {
            var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
            return ComputeWindowedAggregate(rowCount, sorted, true, values, aggregateName);
        }

        return ComputeWindowedAggregate(rowCount, partitions, false, values, aggregateName);
    }

    private enum AggregateType
    {
        Sum,
        Count,
        Avg,
        Min,
        Max
    }

    private static AggregateType ParseAggregateType(string aggregateName)
    {
        return aggregateName.ToUpperInvariant() switch
        {
            "SUM" => AggregateType.Sum,
            "COUNT" => AggregateType.Count,
            "AVG" => AggregateType.Avg,
            "MIN" => AggregateType.Min,
            "MAX" => AggregateType.Max,
            _ => throw new NotSupportedException($"Window aggregate function '{aggregateName}' is not supported.")
        };
    }

    private static void ComputeRunningAggregate(
        WindowPartition partition, object?[] values,
        AggregateType aggregateType, object?[] result)
    {
        switch (aggregateType)
        {
            case AggregateType.Sum:
                ComputeRunningSum(partition, values, result);
                break;
            case AggregateType.Count:
                ComputeRunningCount(partition, values, result);
                break;
            case AggregateType.Avg:
                ComputeRunningAvg(partition, values, result);
                break;
            case AggregateType.Min:
                ComputeRunningExtremum(partition, values, result, isMin: true);
                break;
            case AggregateType.Max:
                ComputeRunningExtremum(partition, values, result, isMin: false);
                break;
        }
    }
}
