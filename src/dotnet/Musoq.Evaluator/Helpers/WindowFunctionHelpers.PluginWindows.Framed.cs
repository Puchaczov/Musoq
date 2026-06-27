using System.Collections.Generic;
using Musoq.Plugins;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static object[] ComputeFramedPluginWindowFunction(
        int rowCount, WindowPartitionSet partitions,
        object[] values, IWindowFunction windowFunction,
        FrameBounds frame)
    {
        return ComputeFramedPluginWindowFunction<object>(
            rowCount, partitions, values, windowFunction, frame);
    }

    public static object[] ComputeFramedPluginWindowFunction(
        int rowCount, WindowPartitionSet partitions,
        object[] values, IWindowFunction windowFunction,
        FrameBounds frame,
        object?[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputeFramedPluginWindowFunction<object>(
            rowCount, partitions, values, windowFunction, frame);
    }

    public static object[] ComputeFramedPluginWindowFunction(
        int rowCount, WindowPartitionSet partitions,
        object[] values, IWindowFunction windowFunction,
        FrameBounds frame,
        object[][] extraArgsByRow)
    {
        return ComputeFramedPluginWindowFunction<object>(
            rowCount, partitions, values, windowFunction, frame, extraArgsByRow);
    }

    public static object[] ComputeFramedPluginWindowFunction<T>(
        int rowCount, WindowPartitionSet partitions,
        T[] values, IWindowFunction windowFunction,
        FrameBounds frame)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        ArgumentNullException.ThrowIfNull(frame);
        return ComputeFramedPluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), values, windowFunction, frame);
    }

    public static object[] ComputeFramedPluginWindowFunction<T>(
        int rowCount, WindowPartitionSet partitions,
        T[] values, IWindowFunction windowFunction,
        FrameBounds frame,
        object?[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputeFramedPluginWindowFunction(
            rowCount, partitions, values, windowFunction, frame);
    }

    public static object[] ComputeFramedPluginWindowFunction<T>(
        int rowCount, WindowPartitionSet partitions,
        T[] values, IWindowFunction windowFunction,
        FrameBounds frame,
        object[][] extraArgsByRow)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(extraArgsByRow);
        return ComputeFramedPluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), values, windowFunction, frame, extraArgsByRow);
    }

    public static object[] ComputeFramedPluginWindowFunction(
        int rowCount, List<List<int>> partitions,
        object[] values, IWindowFunction windowFunction,
        FrameBounds frame)
    {
        return ComputeFramedPluginWindowFunction<object>(
            rowCount, partitions, values, windowFunction, frame);
    }

    public static object[] ComputeFramedPluginWindowFunction(
        int rowCount, List<List<int>> partitions,
        object[] values, IWindowFunction windowFunction,
        FrameBounds frame,
        object?[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputeFramedPluginWindowFunction<object>(
            rowCount, partitions, values, windowFunction, frame);
    }

    public static object[] ComputeFramedPluginWindowFunction(
        int rowCount, List<List<int>> partitions,
        object[] values, IWindowFunction windowFunction,
        FrameBounds frame,
        object[][] extraArgsByRow)
    {
        return ComputeFramedPluginWindowFunction<object>(
            rowCount, partitions, values, windowFunction, frame, extraArgsByRow);
    }

    public static object[] ComputeFramedPluginWindowFunction<T>(
        int rowCount, List<List<int>> partitions,
        T[] values, IWindowFunction windowFunction,
        FrameBounds frame)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        ArgumentNullException.ThrowIfNull(frame);
        return ComputeFramedPluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), values, windowFunction, frame);
    }

    public static object[] ComputeFramedPluginWindowFunction<T>(
        int rowCount, List<List<int>> partitions,
        T[] values, IWindowFunction windowFunction,
        FrameBounds frame,
        object?[] extraArgs)
    {
        ArgumentNullException.ThrowIfNull(windowFunction);
        windowFunction.SetArguments(extraArgs);
        return ComputeFramedPluginWindowFunction(
            rowCount, partitions, values, windowFunction, frame);
    }

    public static object[] ComputeFramedPluginWindowFunction<T>(
        int rowCount, List<List<int>> partitions,
        T[] values, IWindowFunction windowFunction,
        FrameBounds frame,
        object[][] extraArgsByRow)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(windowFunction);
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(extraArgsByRow);
        return ComputeFramedPluginWindowFunction(
            rowCount, EnumeratePartitions(partitions), values, windowFunction, frame, extraArgsByRow);
    }

    private static object[] ComputeFramedPluginWindowFunction<T>(
        int rowCount, WindowPartitionEnumerable partitions,
        T[] values, IWindowFunction windowFunction,
        FrameBounds frame)
    {
        var result = new object[rowCount];

        foreach (var partition in partitions)
        {
            var count = partition.Count;
            windowFunction.SetPartitionSize(count);

            for (var i = 0; i < count; i++)
            {
                var frameStart = ResolveFrameStartIndex(i, count, frame.Start);
                var frameEnd = ResolveFrameEndIndex(i, count, frame.End);

                windowFunction.PartitionStart();

                for (var j = frameStart; j <= frameEnd; j++)
                    windowFunction.AccumulateValue(values[partition[j]]);

                result[partition[i]] = windowFunction.GetCurrentValue()!;
            }
        }

        return result;
    }

    private static object[] ComputeFramedPluginWindowFunction<T>(
        int rowCount, WindowPartitionEnumerable partitions,
        T[] values, IWindowFunction windowFunction,
        FrameBounds frame,
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
                var frameStart = ResolveFrameStartIndex(i, count, frame.Start);
                var frameEnd = ResolveFrameEndIndex(i, count, frame.End);

                windowFunction.SetArguments(extraArgsByRow[currentIndex]);
                windowFunction.PartitionStart();

                for (var j = frameStart; j <= frameEnd; j++)
                    windowFunction.AccumulateValue(values[partition[j]]);

                result[currentIndex] = windowFunction.GetCurrentValue()!;
            }
        }

        return result;
    }

    private static int ResolveFrameStartIndex(int rowIndex, int rowCount, FrameBound bound)
    {
        return bound.Kind switch
        {
            FrameBoundKind.UnboundedPreceding => 0,
            FrameBoundKind.UnboundedFollowing => rowCount - 1,
            FrameBoundKind.CurrentRow => rowIndex,
            FrameBoundKind.OffsetPreceding => Math.Max(0, rowIndex - bound.Offset),
            FrameBoundKind.OffsetFollowing => Math.Min(rowCount, rowIndex + bound.Offset),
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null)
        };
    }

    private static int ResolveFrameEndIndex(int rowIndex, int rowCount, FrameBound bound)
    {
        return bound.Kind switch
        {
            FrameBoundKind.UnboundedPreceding => 0,
            FrameBoundKind.UnboundedFollowing => rowCount - 1,
            FrameBoundKind.CurrentRow => rowIndex,
            FrameBoundKind.OffsetPreceding => Math.Min(rowCount - 1, rowIndex - bound.Offset),
            FrameBoundKind.OffsetFollowing => Math.Min(rowCount - 1, rowIndex + bound.Offset),
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null)
        };
    }
}
