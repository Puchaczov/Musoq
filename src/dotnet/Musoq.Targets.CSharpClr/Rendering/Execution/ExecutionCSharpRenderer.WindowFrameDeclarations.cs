using Microsoft.CodeAnalysis;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static string CreateValueAccessFrameDeclarations(
        ExecutionComputePluginWindow plugin,
        ExecutionWindowKeyArray? orderKeys,
        ExecutionVariable? rangeKeys,
        string partitionIndices,
        string partitionStart,
        string partitionIndex,
        string partitionCount,
        string frameStart,
        string frameEnd)
    {
        if (plugin.Frame == null)
        {
            var end = plugin.OrderKeys.Count == 0
                ? $"{partitionCount} - 1"
                : partitionIndex;

            return
                $"        var {frameStart} = 0;" + Environment.NewLine +
                $"        var {frameEnd} = {end};" + Environment.NewLine;
        }

        var start = WindowRangeFrameSyntax.CreatePluginWindowFrameExpression(
            plugin,
            plugin.Frame.Start,
            true,
            orderKeys,
            rangeKeys,
            partitionIndices,
            partitionStart,
            partitionIndex,
            partitionCount);
        var endExpression = WindowRangeFrameSyntax.CreatePluginWindowFrameExpression(
            plugin,
            plugin.Frame.End,
            false,
            orderKeys,
            rangeKeys,
            partitionIndices,
            partitionStart,
            partitionIndex,
            partitionCount);

        return
            $"        var {frameStart} = {start.NormalizeWhitespace().ToFullString()};" + Environment.NewLine +
            $"        var {frameEnd} = {endExpression.NormalizeWhitespace().ToFullString()};" + Environment.NewLine;
    }

}
