using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanUseFusedIntOrderRankingWindow(ExecutionComputeRankingWindow ranking)
    {
        if (ranking.Function is not (ExecutionRankingWindowFunction.Rank or ExecutionRankingWindowFunction.DenseRank))
            return false;

        if (!CanUseFusedIntOrderWindow(ranking.OrderKeys))
            return false;

        if (ranking.Partitions is { ShouldCreate: false } ||
            ranking.SortedPartitions is { ShouldCreate: false })
        {
            return false;
        }

        var partitionKeys = ResolveRankingPartitionKeyArray(ranking);
        return partitionKeys == null || CanUseTypedWindowPartitionKey(partitionKeys);
    }

    private static bool CanUseTypedWindowPartitionKey(ExecutionWindowKeyArray partitionKeys)
    {
        if (HasGeneratedWindowKeyType(partitionKeys))
        {
            return partitionKeys.Shape?.GeneratedParts?.All(static part => part.Type != typeof(object)) == true;
        }

        if (partitionKeys.Shape == null)
            return GetArrayElementType(partitionKeys.Variable) != typeof(object);

        return partitionKeys.Shape.IsTyped &&
               partitionKeys.Shape.ElementType != typeof(object);
    }
}
