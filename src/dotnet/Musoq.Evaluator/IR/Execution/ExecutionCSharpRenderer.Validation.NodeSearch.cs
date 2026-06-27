using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool ContainsAggregateNode(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectNodes<ExecutionCreateAggregateContext>(block).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionCreateSingleKeyAggregateContext>(block).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionCreateValueTupleAggregateContext>(block).Any();
    }

    private static bool ContainsNode<TNode>(ExecutionBlock block)
        where TNode : ExecutionNode
    {
        return ExecutionIrAnalysis.CollectNodes<TNode>(block).Any();
    }

    private static bool CanRenderBlock(ExecutionBlock block)
    {
        return block.Nodes.All(CanRenderNode);
    }

    private static bool CanRenderOptionalBlock(ExecutionBlock? block)
    {
        return block == null || CanRenderBlock(block);
    }

    private static string? GetUnsupportedParallelBlockReason(ExecutionParallelBlock parallel)
    {
        if (parallel.MaxDegreeOfParallelism <= 0)
            return "Execution IR C# backend requires parallel blocks to declare a positive max degree of parallelism.";

        if (parallel.Tasks.Count == 0)
            return "Execution IR C# backend requires parallel blocks to contain at least one task.";

        var unsupportedOutput = parallel.Tasks.FirstOrDefault(static task => !CanRenderParallelTaskOutput(task));
        if (unsupportedOutput != null)
            return $"Execution IR C# backend requires parallel task {unsupportedOutput.Name} to produce a Table, generated row-buffer, or sidecar-only output.";

        foreach (var task in parallel.Tasks)
        {
            var reason = GetUnsupportedNodeReason(task.Body);
            if (reason != null)
                return reason;
        }

        return GetUnsupportedNodeReason(parallel.Merge.Body);
    }
}
