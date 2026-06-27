using Musoq.Plugins;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static object[] ComputeOrderedPluginWindowFunction(
        int rowCount,
        object[] partitionKeys,
        object[] orderKeys,
        bool[] orderDescending,
        object[] values,
        IWindowFunction windowFunction,
        object?[] extraArgs)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputePluginWindowFunction(rowCount, sorted, true, values, windowFunction, extraArgs);
    }

    public static object[] ComputeOrderedPluginWindowFunction(
        int rowCount,
        object[] partitionKeys,
        object[] orderKeys,
        bool[] orderDescending,
        object[] values,
        IWindowFunction windowFunction,
        object[][] extraArgsByRow)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputePluginWindowFunction(rowCount, sorted, true, values, windowFunction, extraArgsByRow);
    }

    public static object[] ComputeUnorderedPluginWindowFunction(
        int rowCount,
        object[] partitionKeys,
        object[] values,
        IWindowFunction windowFunction,
        object?[] extraArgs)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        return ComputePluginWindowFunction(rowCount, partitions, false, values, windowFunction, extraArgs);
    }

    public static object[] ComputeUnorderedPluginWindowFunction(
        int rowCount,
        object[] partitionKeys,
        object[] values,
        IWindowFunction windowFunction,
        object[][] extraArgsByRow)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        return ComputePluginWindowFunction(rowCount, partitions, false, values, windowFunction, extraArgsByRow);
    }

    public static object[] ComputeOrderedFramedPluginWindowFunction(
        int rowCount,
        object[] partitionKeys,
        object[] orderKeys,
        bool[] orderDescending,
        object[] values,
        IWindowFunction windowFunction,
        FrameBounds frame,
        object[] extraArgs)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeFramedPluginWindowFunction(rowCount, sorted, values, windowFunction, frame, extraArgs);
    }

    public static object[] ComputeOrderedFramedPluginWindowFunction(
        int rowCount,
        object[] partitionKeys,
        object[] orderKeys,
        bool[] orderDescending,
        object[] values,
        IWindowFunction windowFunction,
        FrameBounds frame,
        object[][] extraArgsByRow)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        var sorted = SortPartitionSet(partitions, orderKeys, orderDescending);
        return ComputeFramedPluginWindowFunction(rowCount, sorted, values, windowFunction, frame, extraArgsByRow);
    }

    public static object[] ComputeUnorderedFramedPluginWindowFunction(
        int rowCount,
        object[] partitionKeys,
        object[] values,
        IWindowFunction windowFunction,
        FrameBounds frame,
        object[] extraArgs)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        return ComputeFramedPluginWindowFunction(rowCount, partitions, values, windowFunction, frame, extraArgs);
    }

    public static object[] ComputeUnorderedFramedPluginWindowFunction(
        int rowCount,
        object[] partitionKeys,
        object[] values,
        IWindowFunction windowFunction,
        FrameBounds frame,
        object[][] extraArgsByRow)
    {
        var partitions = ResolvePartitionSet(rowCount, partitionKeys);
        return ComputeFramedPluginWindowFunction(rowCount, partitions, values, windowFunction, frame, extraArgsByRow);
    }
}
