namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private static string? GetUnsupportedNodeReason(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            var reason = GetUnsupportedNodeReason(node);
            if (reason != null)
                return reason;
        }

        return null;
    }

    private static string? GetUnsupportedNodeReason(ExecutionNode node)
    {
        if (node is ExecutionParallelBlock parallel)
            return GetUnsupportedParallelBlockReason(parallel) ?? GetUnsupportedCurrentNodeReason(node);

        var expressionReason = GetUnsupportedExpressionReason(ExecutionIrAnalysis.GetNodeExpressions(node));
        if (expressionReason != null)
            return expressionReason;

        foreach (var childBlock in ExecutionIrAnalysis.GetChildBlocks(node))
        {
            var childReason = GetUnsupportedNodeReason(childBlock);
            if (childReason != null)
                return childReason;
        }

        return GetUnsupportedCurrentNodeReason(node);
    }

    private static string? GetUnsupportedCurrentNodeReason(ExecutionNode node)
    {
        if (CanRenderNode(node))
            return null;

        return $"Execution IR C# backend cannot render node {node.GetType().Name}.";
    }
}
