using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal abstract class ExecutionCteCandidateExpansionRewriter : ExecutionIrRewriter
{
    public override ExecutionBlock RewriteBlock(ExecutionBlock block)
    {
        var builder = new ExecutionBlockRewriteBuilder(block);

        for (var index = 0; index < block.Nodes.Count; index++)
        {
            var node = block.Nodes[index];
            if (TryExpandCandidate(node, out var expandedNodes))
            {
                builder.EnsureStartedAt(index);
                builder.AddRange(expandedNodes);
                continue;
            }

            var rewrittenNode = RewriteNode(node);
            if (ReferenceEquals(rewrittenNode, node) && !builder.HasChanges)
                continue;

            builder.EnsureStartedAt(index);
            builder.Add(rewrittenNode);
        }

        return builder.ToBlock();
    }

    protected abstract bool TryExpandCandidate(
        ExecutionNode node,
        out IReadOnlyList<ExecutionNode> expandedNodes);
}

