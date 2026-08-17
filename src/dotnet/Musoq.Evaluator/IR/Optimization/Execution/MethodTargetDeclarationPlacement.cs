using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal static class MethodTargetDeclarationPlacement
{
    public static int InsertCreatedTargetDeclarations(
        ExecutionBlockRewriteBuilder builder,
        IEnumerable<ExecutionVariable> targets,
        ExecutionBlock block,
        int nodeIndex)
    {
        var declarations = targets
            .Select(static target => (ExecutionNode)new ExecutionCreateObject(target))
            .ToArray();
        if (declarations.Length == 0)
            return 0;

        var insertionIndex = GetAggregateLoopDeclarationIndex(block, nodeIndex, builder);
        if (insertionIndex is { } index)
            builder.InsertRange(index, declarations);
        else
            builder.AddRange(declarations);

        return declarations.Length;
    }

    private static int? GetAggregateLoopDeclarationIndex(
        ExecutionBlock block,
        int index,
        ExecutionBlockRewriteBuilder builder)
    {
        if (index == 0 ||
            block.Nodes[index] is not ExecutionForEach and
                not ExecutionForEachWithOrdinality and
                not ExecutionForEachIndexed ||
            !IsAggregateContext(block.Nodes[index - 1]))
        {
            return null;
        }

        var tableName = FindNearestCreatedTableName(block, index - 1);
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            for (var rewrittenIndex = builder.Count - 1; rewrittenIndex >= 0; rewrittenIndex--)
            {
                if (builder[rewrittenIndex] is ExecutionCreateTable createTable &&
                    string.Equals(createTable.Table.Name, tableName, StringComparison.Ordinal))
                {
                    return rewrittenIndex;
                }
            }
        }

        for (var rewrittenIndex = builder.Count - 1; rewrittenIndex >= 0; rewrittenIndex--)
        {
            if (IsAggregateContext(builder[rewrittenIndex]))
                return rewrittenIndex;
        }

        return null;
    }

    private static string? FindNearestCreatedTableName(ExecutionBlock block, int beforeIndex)
    {
        for (var index = beforeIndex; index >= 0; index--)
        {
            if (block.Nodes[index] is ExecutionCreateTable createTable)
                return createTable.Table.Name;
        }

        return null;
    }

    private static bool IsAggregateContext(ExecutionNode node) =>
        node is ExecutionCreateSingleKeyAggregateContext or ExecutionCreateValueTupleAggregateContext;
}

