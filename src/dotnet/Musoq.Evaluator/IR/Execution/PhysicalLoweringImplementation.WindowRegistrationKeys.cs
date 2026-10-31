using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionWindowKeyArray? CreateWindowPartitionKeyArray(
        WindowComputationContext context,
        ExecutionVariable results,
        bool canBuildPartitionSetFromKeys = false)
    {
        if (context.PartitionKey == null || context.PartitionSignature == null)
            return null;

        var candidate = WindowKeyShapePolicy.CreatePartitionKeyArray($"{results.Name}PartitionKeys", context.PartitionKey);
        var shouldMaterialize =
            !canBuildPartitionSetFromKeys ||
            !context.SingleUsePartitionKeySignatures.Contains(context.PartitionSignature);

        return context.KeyArrays.GetOrAdd(
            context.PartitionSignature,
            candidate.Variable,
            candidate.Shape,
            shouldMaterialize);
    }

    private static ExecutionWindowKeyArray? CreateWindowOrderKeyArray(
        WindowComputationContext context,
        ExecutionVariable results)
    {
        if (context.OrderKeys.Count == 0 || context.OrderSignature == null)
            return null;

        var candidate = WindowKeyShapePolicy.CreateOrderKeyArray($"{results.Name}OrderKeys", context.OrderKeys);
        return context.KeyArrays.GetOrAdd(context.OrderSignature, candidate.Variable, candidate.Shape);
    }

    private static ExecutionWindowPartitionSet CreateWindowPartitionSet(
        WindowComputationContext context,
        ExecutionVariable results)
    {
        return context.Partitions.GetOrAdd(
            context.PartitionListSignature,
            new ExecutionVariable($"{results.Name}Partitions", typeof(WindowPartitionSet)));
    }

    private static ExecutionWindowPartitionSet? CreateWindowSortedPartitionSet(
        WindowComputationContext context,
        ExecutionVariable results,
        ExecutionWindowPartitionSet partitions)
    {
        if (context.OrderKeys.Count == 0 || context.SortedPartitionListSignature == null)
            return null;

        if (context.InPlaceSortableSortedPartitionSignatures.Contains(context.SortedPartitionListSignature))
        {
            return context.SortedPartitions.GetOrAdd(
                context.SortedPartitionListSignature,
                partitions.Variable,
                sortInPlace: true);
        }

        return context.SortedPartitions.GetOrAdd(
            context.SortedPartitionListSignature,
            new ExecutionVariable($"{results.Name}SortedPartitions", typeof(WindowPartitionSet)));
    }

}
