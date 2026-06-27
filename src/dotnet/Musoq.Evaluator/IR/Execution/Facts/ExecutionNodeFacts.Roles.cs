using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static bool TryGetTablePostOperation(
        ExecutionNode node,
        [NotNullWhen(true)] out ExecutionTablePostOperationMetadata? metadata)
    {
        metadata = node switch
        {
            ExecutionDistinctTable distinct => new ExecutionTablePostOperationMetadata(
                distinct.Source,
                distinct.Target,
                null,
                ExecutionAppendMode.Checked,
                null),
            ExecutionSortTable sort => new ExecutionTablePostOperationMetadata(
                sort.Source,
                sort.Target,
                sort.CapacityHint,
                sort.AppendMode,
                sort.ColumnMetadata),
            ExecutionTopNTable topN => new ExecutionTablePostOperationMetadata(
                topN.Source,
                topN.Target,
                topN.CapacityHint,
                topN.AppendMode,
                topN.ColumnMetadata),
            ExecutionTopOffsetTable topOffset => new ExecutionTablePostOperationMetadata(
                topOffset.Source,
                topOffset.Target,
                topOffset.CapacityHint,
                topOffset.AppendMode,
                topOffset.ColumnMetadata),
            ExecutionSkipTable skip => new ExecutionTablePostOperationMetadata(
                skip.Source,
                skip.Target,
                skip.CapacityHint,
                skip.AppendMode,
                skip.ColumnMetadata),
            ExecutionTakeTable take => new ExecutionTablePostOperationMetadata(
                take.Source,
                take.Target,
                take.CapacityHint,
                take.AppendMode,
                take.ColumnMetadata),
            ExecutionSliceTable slice => new ExecutionTablePostOperationMetadata(
                slice.Source,
                slice.Target,
                slice.CapacityHint,
                slice.AppendMode,
                slice.ColumnMetadata),
            ExecutionProjectTable project => new ExecutionTablePostOperationMetadata(
                project.Source,
                project.Target,
                project.CapacityHint,
                project.AppendMode,
                null),
            ExecutionMaterializeRecordListToTable materialize => new ExecutionTablePostOperationMetadata(
                materialize.Source,
                materialize.Target,
                materialize.CapacityHint,
                materialize.AppendMode,
                null),
            _ => null
        };

        return metadata != null;
    }

    internal static bool TryGetWindowComputation(
        ExecutionNode node,
        [NotNullWhen(true)] out ExecutionWindowComputationMetadata? metadata)
    {
        metadata = node switch
        {
            ExecutionComputeRankingWindow ranking => new ExecutionWindowComputationMetadata(
                ranking.Buffer,
                ranking.Item,
                ranking.RowAccessMode,
                ranking.PartitionKey,
                ranking.OrderKeys,
                ranking.Results,
                ranking.PartitionKeyArray,
                ranking.OrderKeyArray,
                ranking.Partitions,
                ranking.SortedPartitions),
            ExecutionComputeOffsetWindow offset => new ExecutionWindowComputationMetadata(
                offset.Buffer,
                offset.Item,
                offset.RowAccessMode,
                offset.PartitionKey,
                offset.OrderKeys,
                offset.Results,
                offset.PartitionKeyArray,
                offset.OrderKeyArray,
                offset.Partitions,
                offset.SortedPartitions),
            ExecutionComputePluginWindow plugin => new ExecutionWindowComputationMetadata(
                plugin.Buffer,
                plugin.Item,
                plugin.RowAccessMode,
                plugin.PartitionKey,
                plugin.OrderKeys,
                plugin.Results,
                plugin.PartitionKeyArray,
                plugin.OrderKeyArray,
                plugin.Partitions,
                plugin.SortedPartitions),
            ExecutionWindowAggregateKernel kernel => new ExecutionWindowComputationMetadata(
                kernel.Buffer,
                kernel.Item,
                kernel.RowAccessMode,
                kernel.PartitionKey,
                kernel.OrderKeys,
                kernel.Results,
                kernel.PartitionKeyArray,
                kernel.OrderKeyArray,
                kernel.Partitions,
                kernel.SortedPartitions),
            _ => null
        };

        return metadata != null;
    }
}
