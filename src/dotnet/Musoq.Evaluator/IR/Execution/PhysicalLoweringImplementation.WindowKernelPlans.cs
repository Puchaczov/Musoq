using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static IReadOnlyList<ExecutionNode> CreateWindowKernelPlanNodes(
        IReadOnlyList<WindowComputationBuildResult> computations)
    {
        var nodes = new List<ExecutionNode>(computations.Count);
        var pending = new List<ExecutionNode>();
        string? pendingSignature = null;
        ExecutionWindowKernelPlanStrategy? pendingStrategy = null;

        foreach (var computation in computations)
        {
            var node = computation.Node;
            if (!TryCreateWindowKernelPlanSignature(node, out var signature, out var strategy))
            {
                FlushWindowKernelPlan(nodes, pending, pendingSignature, pendingStrategy);
                pendingSignature = null;
                pendingStrategy = null;
                nodes.Add(node);
                continue;
            }

            if (pending.Count > 0 &&
                (!string.Equals(pendingSignature, signature, StringComparison.Ordinal) ||
                 pendingStrategy != strategy))
            {
                FlushWindowKernelPlan(nodes, pending, pendingSignature, pendingStrategy);
                pending.Clear();
            }

            pendingSignature = signature;
            pendingStrategy = strategy;
            pending.Add(node);
        }

        FlushWindowKernelPlan(nodes, pending, pendingSignature, pendingStrategy);
        return nodes;
    }

    private static void FlushWindowKernelPlan(
        List<ExecutionNode> nodes,
        List<ExecutionNode> pending,
        string? signature,
        ExecutionWindowKernelPlanStrategy? strategy)
    {
        if (pending.Count == 0)
            return;

        if (pending.Count > 1 && signature != null && strategy.HasValue)
        {
            nodes.Add(new ExecutionWindowKernelPlan(signature, strategy.Value, pending.ToArray()));
            pending.Clear();
            return;
        }

        nodes.AddRange(pending);
        pending.Clear();
    }

    private static bool TryCreateWindowKernelPlanSignature(
        ExecutionNode node,
        out string signature,
        out ExecutionWindowKernelPlanStrategy strategy)
    {
        signature = string.Empty;
        strategy = default;

        if (node is not ExecutionComputeRankingWindow ranking)
            return false;

        var partitionVariable = ranking.Partitions?.Variable.Name ?? string.Empty;
        var sortedPartitionVariable = ranking.SortedPartitions?.Variable.Name ?? partitionVariable;
        if (string.IsNullOrWhiteSpace(sortedPartitionVariable))
            return false;

        signature = string.Join(
            "|",
            "ranking",
            ranking.Buffer.Name,
            partitionVariable,
            sortedPartitionVariable,
            ranking.PartitionKeyArray?.Variable.Name ?? string.Empty,
            ranking.OrderKeyArray?.Variable.Name ?? string.Empty);
        strategy = CreateWindowKernelPlanStrategy(ranking.PartitionKeyArray, ranking.OrderKeyArray);
        return true;
    }

    private static ExecutionWindowKernelPlanStrategy CreateWindowKernelPlanStrategy(
        ExecutionWindowKeyArray? partitionKeys,
        ExecutionWindowKeyArray? orderKeys)
    {
        if (partitionKeys == null && orderKeys == null)
            return ExecutionWindowKernelPlanStrategy.NoPartition;

        return ExecutionWindowKernelPlanStrategy.HashPartitionPerPartitionSort;
    }
}
