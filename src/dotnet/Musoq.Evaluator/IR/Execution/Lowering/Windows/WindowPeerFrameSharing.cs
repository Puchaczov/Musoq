using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static void BindPeerFrameBounds(List<WindowComputationBuildResult> computations)
    {
        var owners =
            new Dictionary<(ExecutionVariable?, ExecutionVariable, ExecutionVariable, ExecutionWindowFrame), int>();
        for (var i = 0; i < computations.Count; i++)
        {
            if (
                computations[i].Node
                    is not ExecutionWindowAggregateKernel
                    {
                        ReuseBoundedPeers: true,
                        OrderKeyArray: { } order,
                        Frame: { } frame
                    } kernel
                || (kernel.SortedPartitions ?? kernel.Partitions) is not { } partitions
            )
                continue;
            var key = (kernel.PartitionKeyArray?.Variable, partitions.Variable, order.Variable, frame);
            if (owners.TryAdd(key, i))
                continue;
            var ownerIndex = owners[key];
            var owner = (ExecutionWindowAggregateKernel)computations[ownerIndex].Node!;
            computations[ownerIndex] = computations[ownerIndex] with
            {
                Node = owner with { BoundariesOwner = owner.Results },
            };
            computations[i] = computations[i] with { Node = kernel with { BoundariesOwner = owner.Results } };
        }
    }
}
