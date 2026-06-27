using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionIrAnalysis
{
    internal static IEnumerable<TNode> CollectNodes<TNode>(ExecutionBlock block)
        where TNode : ExecutionNode
    {
        foreach (var node in FlattenNodes(block))
        {
            if (node is TNode match)
                yield return match;
        }
    }

    internal static IEnumerable<TNode> CollectNodes<TNode>(ExecutionNode node)
        where TNode : ExecutionNode
    {
        if (node is TNode match)
            yield return match;

        foreach (var childBlock in GetChildBlocks(node))
        {
            foreach (var childNode in CollectNodes<TNode>(childBlock))
                yield return childNode;
        }
    }
}
