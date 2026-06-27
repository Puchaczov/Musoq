using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution;

internal static class CteSourceBackedExpressionRewriter
{
    public static bool TryCreateSourceBackedLoop(
        CteSourceBackedMaterialization materialization,
        ExecutionForEach producerLoop,
        [NotNullWhen(true)] out ExecutionSourceLoop? sourceLoop)
    {
        sourceLoop = null;
        var fieldMap = CteSourceBackedFieldMap.Create(materialization.AppendRow);
        if (!CteSourceBackedNodeRewriter.TryRewriteBlock(
                producerLoop.Body,
                producerLoop.Item.Name,
                fieldMap,
                materialization.AppendRow,
                out var producerBody))
        {
            return false;
        }

        var loopBody = ReplaceAppend(
            materialization.Loop.Body,
            materialization.AppendRow,
            producerBody.Nodes);
        sourceLoop = materialization.Loop with { Body = new ExecutionBlock(loopBody) };
        return true;
    }

    private static IReadOnlyList<ExecutionNode> ReplaceAppend(
        ExecutionBlock block,
        ExecutionAppendRow target,
        IReadOnlyList<ExecutionNode> replacement)
    {
        var nodes = new List<ExecutionNode>(block.Nodes.Count + replacement.Count);
        foreach (var node in block.Nodes)
        {
            if (ReferenceEquals(node, target))
            {
                nodes.AddRange(replacement);
                continue;
            }

            nodes.Add(node switch
            {
                ExecutionIf branch => branch with
                {
                    Body = new ExecutionBlock(ReplaceAppend(branch.Body, target, replacement))
                },
                _ => node
            });
        }

        return nodes;
    }
}
