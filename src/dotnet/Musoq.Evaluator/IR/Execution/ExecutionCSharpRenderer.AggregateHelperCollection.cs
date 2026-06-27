using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private delegate THelper? CreateAggregateHelper<THelper>(IReadOnlyList<ExecutionNode> nodes, int startIndex)
        where THelper : class;

    private delegate THelper AssignAggregateHelperNames<THelper>(THelper helper, int helperIndex);

    private static IEnumerable<(THelper Helper, int Index)> CollectAggregateHelpersWithIndexes<THelper>(
        ExecutionBlock block,
        CreateAggregateHelper<THelper> createHelper,
        AssignAggregateHelperNames<THelper> assignNames)
        where THelper : class
    {
        var helperIndex = 0;
        var nodes = block.Nodes;
        var pending = new List<ExecutionNode>();

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (node is ExecutionStoreTable store &&
                TryCreateStoredTableBuild(nodes, index, pending, store, out _))
            {
                pending.Clear();
                continue;
            }

            if (IsInsidePendingStoredTableBuild(nodes, index, pending))
            {
                pending.Add(node);
                continue;
            }

            var helper = createHelper(nodes, index);
            if (helper is null)
            {
                pending.Add(node);
                continue;
            }

            yield return (assignNames(helper, helperIndex), index);
            helperIndex++;
            index += 3;
            pending.Add(node);
        }
    }
}
