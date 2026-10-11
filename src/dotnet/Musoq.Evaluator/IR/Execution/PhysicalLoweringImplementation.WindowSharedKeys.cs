using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static void MarkSharedWindowOrderKeys(List<WindowComputationBuildResult> computations)
    {
        var shared = computations.Select(computation => GetWindowOrderKeyArray(computation.Node))
            .Where(static keys => keys != null)
            .GroupBy(static keys => keys!.Variable.Name, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        for (var index = 0; index < computations.Count; index++)
        {
            var computation = computations[index];
            var keys = GetWindowOrderKeyArray(computation.Node);
            if (keys == null || !shared.Contains(keys.Variable.Name))
                continue;

            var marked = keys with { IsShared = true };
            var node = computation.Node switch
            {
                ExecutionWindowAggregateKernel aggregate => aggregate with { OrderKeyArray = marked },
                ExecutionComputeRankingWindow ranking => ranking with { OrderKeyArray = marked },
                ExecutionComputeOffsetWindow offset => offset with { OrderKeyArray = marked },
                ExecutionComputePluginWindow plugin => plugin with { OrderKeyArray = marked },
                _ => computation.Node
            };
            computations[index] = computation with { Node = node };
        }
    }

    private static ExecutionWindowKeyArray? GetWindowOrderKeyArray(ExecutionNode node)
    {
        return node switch
        {
            ExecutionWindowAggregateKernel aggregate => aggregate.OrderKeyArray,
            ExecutionComputeRankingWindow ranking => ranking.OrderKeyArray,
            ExecutionComputeOffsetWindow offset => offset.OrderKeyArray,
            ExecutionComputePluginWindow plugin => plugin.OrderKeyArray,
            _ => null
        };
    }
}
