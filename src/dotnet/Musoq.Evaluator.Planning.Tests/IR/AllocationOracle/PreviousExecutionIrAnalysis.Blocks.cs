using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class PreviousExecutionIrAnalysis
{
    internal static IEnumerable<ExecutionNode> FlattenNodes(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            yield return node;
            foreach (var childBlock in GetChildBlocks(node))
            {
                foreach (var childNode in FlattenNodes(childBlock))
                    yield return childNode;
            }
        }
    }

    internal static IEnumerable<ExecutionBlock> GetChildBlocks(ExecutionNode node)
    {
        return PreviousNodeBlocks(node);
    }
}
