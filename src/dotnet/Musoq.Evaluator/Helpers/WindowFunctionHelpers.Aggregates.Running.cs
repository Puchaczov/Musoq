namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private static void ComputeRunningSum(WindowPartition partition, object?[] values, object?[] result)
    {
        decimal runningSum = 0;
        var count = partition.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = partition[i];
            var val = values[idx];
            if (val != null)
                runningSum += ToDecimalFast(val);
            result[idx] = runningSum;
        }
    }

    private static void ComputeRunningCount(WindowPartition partition, object?[] values, object?[] result)
    {
        var runningCount = 0;
        var count = partition.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = partition[i];
            if (values[idx] != null)
                runningCount++;
            result[idx] = runningCount;
        }
    }

    private static void ComputeRunningAvg(WindowPartition partition, object?[] values, object?[] result)
    {
        decimal sum = 0;
        var runningCount = 0;
        var count = partition.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = partition[i];
            var val = values[idx];
            if (val != null)
            {
                sum += ToDecimalFast(val);
                runningCount++;
            }

            result[idx] = runningCount > 0 ? sum / runningCount : 0m;
        }
    }

    private static void ComputeRunningExtremum(WindowPartition partition, object?[] values, object?[] result, bool isMin)
    {
        IComparable? current = null;
        var count = partition.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = partition[i];
            if (values[idx] is IComparable comparable)
            {
                if (current == null || (isMin ? comparable.CompareTo(current) < 0 : comparable.CompareTo(current) > 0))
                    current = comparable;
            }

            result[idx] = current;
        }
    }
}
