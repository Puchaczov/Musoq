using System.Linq;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private static bool CanRenderPluginWindowResults(ExecutionComputePluginWindow plugin)
    {
        return IsBuiltInDirectPluginWindow(plugin) ||
               TryGetTypedPluginWindowCallTypes(plugin, out _, out _);
    }

    private static bool CanRenderWindowAggregateKernel(ExecutionWindowAggregateKernel kernel)
    {
        return ExecutionNodeFacts.TryGetWindowComputation(kernel, out var window) &&
               CanRenderWindowComputationCommon(window, requireOrderKeys: false) &&
               kernel.Results.Type.RequireClrType() == kernel.Descriptor.ResultType.RequireClrType().MakeArrayType() &&
               CanRenderWindowAggregateMode(kernel) &&
               CanReferenceType(kernel.Descriptor.InputType) &&
               CanReferenceType(kernel.Descriptor.ResultType) &&
               CanRenderWindowAggregateFunction(kernel.Descriptor.Function) &&
               CanRenderExpression(kernel.Value) &&
               (kernel.FilterPredicate == null || CanRenderExpression(kernel.FilterPredicate));
    }

    private static bool CanRenderWindowAggregateMode(ExecutionWindowAggregateKernel kernel)
    {
        if (kernel.Descriptor.Mode != ExecutionWindowAggregateMode.BoundedRows)
            return true;

        return kernel.Frame != null;
    }

    private static bool CanRenderWindowAggregateFunction(ExecutionWindowAggregateFunction function)
    {
        return function is ExecutionWindowAggregateFunction.Sum
            or ExecutionWindowAggregateFunction.Count
            or ExecutionWindowAggregateFunction.Avg
            or ExecutionWindowAggregateFunction.Min
            or ExecutionWindowAggregateFunction.Max;
    }

    private static bool CanRenderStreamingPluginWindow(ExecutionComputePluginWindow plugin)
    {
        return TryGetStreamingPluginWindowMode(plugin, out _) &&
               plugin.Partitions != null &&
               (plugin.OrderKeys.Count == 0 || plugin.SortedPartitions != null) &&
               !plugin.RowScopedArguments.Any(static rowScoped => rowScoped) &&
               CanRenderTypedPluginWindowArguments(plugin) &&
               CanRenderPluginWindowResults(plugin);
    }

    private static bool CanRenderTypedPluginWindowArguments(ExecutionComputePluginWindow plugin)
    {
        return plugin.Arguments.Count <= 7 &&
               plugin.Arguments.All(static argument => argument.ReturnType.RequireClrType() != typeof(object));
    }

    private static bool TryGetStreamingPluginWindowMode(
        ExecutionComputePluginWindow plugin,
        out StreamingPluginWindowMode mode)
    {
        if (plugin.Frame == null)
        {
            mode = plugin.OrderKeys.Count == 0
                ? StreamingPluginWindowMode.WholePartition
                : StreamingPluginWindowMode.Running;
            return true;
        }

        if (IsUnboundedPrecedingToCurrentRow(plugin.Frame))
        {
            mode = plugin.Frame.Kind == ExecutionWindowFrameKind.Range
                ? StreamingPluginWindowMode.RunningPeers
                : StreamingPluginWindowMode.Running;
            return true;
        }

        if (IsUnboundedPrecedingToUnboundedFollowing(plugin.Frame))
        {
            mode = StreamingPluginWindowMode.WholePartition;
            return true;
        }

        mode = default;
        return false;
    }

    private static bool IsUnboundedPrecedingToCurrentRow(ExecutionWindowFrame frame)
    {
        return frame.Start.Kind == ExecutionWindowFrameBoundKind.UnboundedPreceding &&
               frame.End.Kind == ExecutionWindowFrameBoundKind.CurrentRow;
    }

    private static bool IsUnboundedPrecedingToUnboundedFollowing(ExecutionWindowFrame frame)
    {
        return frame.Start.Kind == ExecutionWindowFrameBoundKind.UnboundedPreceding &&
               frame.End.Kind == ExecutionWindowFrameBoundKind.UnboundedFollowing;
    }
}
