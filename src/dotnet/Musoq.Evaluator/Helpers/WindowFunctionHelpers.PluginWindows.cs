using System.Collections.Generic;
using Musoq.Plugins;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static object[] ComputePluginWindowFunction(
        int rowCount, List<List<int>> partitions,
        bool isSorted, object[] values, IWindowFunction windowFunction,
        object?[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputePluginWindowFunction<object>(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static object[] ComputePluginWindowFunction(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, object[] values, IWindowFunction windowFunction,
        object?[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputePluginWindowFunction<object>(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static object[] ComputePluginWindowFunction(
        int rowCount, List<List<int>> partitions,
        bool isSorted, object[] values, IWindowFunction windowFunction)
    {
        return ComputePluginWindowFunction<object>(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static object[] ComputePluginWindowFunction(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, object[] values, IWindowFunction windowFunction)
    {
        return ComputePluginWindowFunction<object>(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static object[] ComputePluginWindowFunction(
        int rowCount, List<List<int>> partitions,
        bool isSorted, object[] values, IWindowFunction windowFunction,
        object[][] extraArgsByRow)
    {
        return ComputePluginWindowFunction<object>(rowCount, partitions, isSorted, values, windowFunction, extraArgsByRow);
    }

    public static object[] ComputePluginWindowFunction(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, object[] values, IWindowFunction windowFunction,
        object[][] extraArgsByRow)
    {
        return ComputePluginWindowFunction<object>(rowCount, partitions, isSorted, values, windowFunction, extraArgsByRow);
    }

    public static TResult[] ComputeTypedPluginWindowFunction<TInput, TResult>(
        int rowCount, List<List<int>> partitions,
        bool isSorted, TInput[] values, IWindowFunction<TInput, TResult> windowFunction,
        object[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputeTypedPluginWindowFunction(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static TResult[] ComputeTypedPluginWindowFunction<TInput, TResult>(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, TInput[] values, IWindowFunction<TInput, TResult> windowFunction,
        object[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputeTypedPluginWindowFunction(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static TResult[] ComputeTypedPluginWindowFunction<TInput, TResult>(
        int rowCount, List<List<int>> partitions,
        bool isSorted, TInput[] values, IWindowFunction<TInput, TResult> windowFunction)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        return ComputeTypedPluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), isSorted, values, windowFunction);
    }

    public static TResult[] ComputeTypedPluginWindowFunction<TInput, TResult>(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, TInput[] values, IWindowFunction<TInput, TResult> windowFunction)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        return ComputeTypedPluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), isSorted, values, windowFunction);
    }

    private static TResult[] ComputeTypedPluginWindowFunction<TInput, TResult>(
        int rowCount, WindowPartitionEnumerable partitions,
        bool isSorted, TInput[] values, IWindowFunction<TInput, TResult> windowFunction)
    {
        var result = new TResult[rowCount];

        foreach (var partition in partitions)
        {
            var count = partition.Count;
            windowFunction.SetPartitionSize(count);
            windowFunction.PartitionStart();

            if (isSorted)
            {
                for (var i = 0; i < count; i++)
                {
                    var idx = partition[i];
                    windowFunction.Accumulate(values[idx]);
                    result[idx] = windowFunction.GetValue();
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                    windowFunction.Accumulate(values[partition[i]]);

                var finalValue = windowFunction.GetValue();
                for (var i = 0; i < count; i++)
                    result[partition[i]] = finalValue;
            }
        }

        return result;
    }

    public static TResult[] ComputeTypedPluginWindowFunction<TInput, TResult>(
        int rowCount, List<List<int>> partitions,
        bool isSorted, TInput[] values, IWindowFunction<TInput, TResult> windowFunction,
        object[][] extraArgsByRow)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        ArgumentNullException.ThrowIfNull(extraArgsByRow);
        return ComputeTypedPluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), isSorted, values, windowFunction, extraArgsByRow);
    }

    public static TResult[] ComputeTypedPluginWindowFunction<TInput, TResult>(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, TInput[] values, IWindowFunction<TInput, TResult> windowFunction,
        object[][] extraArgsByRow)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        ArgumentNullException.ThrowIfNull(extraArgsByRow);
        return ComputeTypedPluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), isSorted, values, windowFunction, extraArgsByRow);
    }

    private static TResult[] ComputeTypedPluginWindowFunction<TInput, TResult>(
        int rowCount, WindowPartitionEnumerable partitions,
        bool isSorted, TInput[] values, IWindowFunction<TInput, TResult> windowFunction,
        object[][] extraArgsByRow)
    {
        var result = new TResult[rowCount];

        foreach (var partition in partitions)
        {
            var count = partition.Count;
            windowFunction.SetPartitionSize(count);

            for (var i = 0; i < count; i++)
            {
                var currentIndex = partition[i];
                windowFunction.SetArguments(extraArgsByRow[currentIndex]);
                windowFunction.PartitionStart();

                var endIndex = isSorted ? i : count - 1;
                for (var j = 0; j <= endIndex; j++)
                    windowFunction.Accumulate(values[partition[j]]);

                result[currentIndex] = windowFunction.GetValue();
            }
        }

        return result;
    }

    public static object[] ComputePluginWindowFunction<T>(
        int rowCount, List<List<int>> partitions,
        bool isSorted, T[] values, IWindowFunction windowFunction,
        object?[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputePluginWindowFunction(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static object[] ComputePluginWindowFunction<T>(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, T[] values, IWindowFunction windowFunction,
        object?[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputePluginWindowFunction(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static object[] ComputePluginWindowFunction<T>(
        int rowCount, List<List<int>> partitions,
        bool isSorted, T[] values, IWindowFunction windowFunction)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        return ComputePluginWindowFunction(rowCount, EnumeratePartitions(partitions), isSorted, values, windowFunction);
    }

    public static object[] ComputePluginWindowFunction<T>(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, T[] values, IWindowFunction windowFunction)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        return ComputePluginWindowFunction(rowCount, EnumeratePartitions(partitions), isSorted, values, windowFunction);
    }

    private static object[] ComputePluginWindowFunction<T>(
        int rowCount, WindowPartitionEnumerable partitions,
        bool isSorted, T[] values, IWindowFunction windowFunction)
    {
        var result = new object[rowCount];

        foreach (var partition in partitions)
        {
            var count = partition.Count;
            windowFunction.SetPartitionSize(count);
            windowFunction.PartitionStart();

            if (isSorted)
            {
                for (var i = 0; i < count; i++)
                {
                    var idx = partition[i];
                    windowFunction.AccumulateValue(values[idx]);
                    result[idx] = windowFunction.GetCurrentValue()!;
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                    windowFunction.AccumulateValue(values[partition[i]]);

                var finalValue = windowFunction.GetCurrentValue()!;
                for (var i = 0; i < count; i++)
                    result[partition[i]] = finalValue;
            }
        }

        return result;
    }

    public static object[] ComputePluginWindowFunction<T>(
        int rowCount, List<List<int>> partitions,
        bool isSorted, T[] values, IWindowFunction windowFunction,
        object[][] extraArgsByRow)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        ArgumentNullException.ThrowIfNull(extraArgsByRow);
        return ComputePluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), isSorted, values, windowFunction, extraArgsByRow);
    }

    public static object[] ComputePluginWindowFunction<T>(
        int rowCount, WindowPartitionSet partitions,
        bool isSorted, T[] values, IWindowFunction windowFunction,
        object[][] extraArgsByRow)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        ArgumentNullException.ThrowIfNull(extraArgsByRow);
        return ComputePluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), isSorted, values, windowFunction, extraArgsByRow);
    }

    private static object[] ComputePluginWindowFunction<T>(
        int rowCount, WindowPartitionEnumerable partitions,
        bool isSorted, T[] values, IWindowFunction windowFunction,
        object[][] extraArgsByRow)
    {
        var result = new object[rowCount];

        foreach (var partition in partitions)
        {
            var count = partition.Count;
            windowFunction.SetPartitionSize(count);

            for (var i = 0; i < count; i++)
            {
                var currentIndex = partition[i];
                windowFunction.SetArguments(extraArgsByRow[currentIndex]);
                windowFunction.PartitionStart();

                var endIndex = isSorted ? i : count - 1;
                for (var j = 0; j <= endIndex; j++)
                    windowFunction.AccumulateValue(values[partition[j]]);

                result[currentIndex] = windowFunction.GetCurrentValue()!;
            }
        }

        return result;
    }
}
