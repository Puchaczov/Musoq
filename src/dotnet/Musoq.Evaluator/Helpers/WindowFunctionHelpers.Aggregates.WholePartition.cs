namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private static object? ComputeWholePartitionAggregate(
        WindowPartition partition, object?[] values, AggregateType aggregateType)
    {
        return aggregateType switch
        {
            AggregateType.Sum => ComputeWholeSum(partition, values),
            AggregateType.Count => ComputeWholeCount(partition, values),
            AggregateType.Avg => ComputeWholeAvg(partition, values),
            AggregateType.Min => ComputeWholeExtremum(partition, values, isMin: true),
            AggregateType.Max => ComputeWholeExtremum(partition, values, isMin: false),
            _ => throw new NotSupportedException($"Window aggregate function '{aggregateType}' is not supported.")
        };
    }

    private static decimal ComputeWholeSum(WindowPartition partition, object?[] values)
    {
        decimal sum = 0;
        var count = partition.Count;
        for (var i = 0; i < count; i++)
        {
            var val = values[partition[i]];
            if (val != null)
                sum += ToDecimalFast(val);
        }

        return sum;
    }

    private static int ComputeWholeCount(WindowPartition partition, object?[] values)
    {
        var count = 0;
        var total = partition.Count;
        for (var i = 0; i < total; i++)
        {
            if (values[partition[i]] != null)
                count++;
        }

        return count;
    }

    private static decimal ComputeWholeAvg(WindowPartition partition, object?[] values)
    {
        decimal sum = 0;
        var count = 0;
        var total = partition.Count;
        for (var i = 0; i < total; i++)
        {
            var val = values[partition[i]];
            if (val != null)
            {
                sum += ToDecimalFast(val);
                count++;
            }
        }

        return count > 0 ? sum / count : 0m;
    }

    private static object? ComputeWholeExtremum(WindowPartition partition, object?[] values, bool isMin)
    {
        IComparable? current = null;
        var count = partition.Count;
        for (var i = 0; i < count; i++)
        {
            if (values[partition[i]] is IComparable comparable)
            {
                if (current == null || (isMin ? comparable.CompareTo(current) < 0 : comparable.CompareTo(current) > 0))
                    current = comparable;
            }
        }

        return current;
    }
}
