using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private delegate THelper? CreateAggregateHelper<THelper>(IReadOnlyList<ExecutionNode> nodes, int startIndex)
        where THelper : class;

    private delegate THelper AssignAggregateHelperNames<THelper>(THelper helper, int helperIndex);

    private static IEnumerable<(THelper Helper, int Index)> CollectAggregateHelpersWithIndexes<THelper>(
        ExecutionBlock block,
        CreateAggregateHelper<THelper> createHelper,
        AssignAggregateHelperNames<THelper> assignNames
    )
        where THelper : class
    {
        var helperIndex = 0;
        var nodes = block.Nodes;
        var storedNodes = CollectStoredTableNodes(block);

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (storedNodes.Contains(node))
                continue;

            var helper = createHelper(nodes, index);
            if (helper is null)
            {
                continue;
            }

            yield return (assignNames(helper, helperIndex), index);
            helperIndex++;
            index += 3;
        }
    }
}
