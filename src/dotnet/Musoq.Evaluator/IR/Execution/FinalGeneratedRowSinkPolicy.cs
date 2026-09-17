using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class FinalGeneratedRowSinkPolicy
{
    public static bool CanUse(ExecutionPlan plan, string finalTableName)
    {
        if (plan.FinalResult == null ||
            !string.Equals(plan.FinalResult.TableName, finalTableName, StringComparison.Ordinal))
        {
            return false;
        }

        if (CanUseSingleSerialDynamicLikeProjection(plan, finalTableName))
            return true;

        var finalSetOperations = plan.Body.Nodes
            .OfType<ExecutionSetOperation>()
            .Where(operation => string.Equals(operation.Target.Name, finalTableName, StringComparison.Ordinal))
            .ToArray();
        if (finalSetOperations.Length != 1 || finalSetOperations[0].Kind != Logical.Nodes.SetOpKind.Union)
            return false;

        if (ExecutionIrAnalysis.CollectNodes<ExecutionComputeRankingWindow>(plan.Body).Count() != 2 ||
            plan.Body.Nodes.OfType<ExecutionSourceScan>().Count() != 2 ||
            !ExecutionIrAnalysis.CollectNodes<ExecutionMaterializeList>(plan.Body)
                .Any(materialize => materialize.Source is ExecutionStoredTableRows))
        {
            return false;
        }

        return !plan.Body.Nodes.Any(node =>
            node switch
            {
                ExecutionDistinctTable distinct => string.Equals(distinct.Target.Name, finalTableName, StringComparison.Ordinal),
                ExecutionSortTable sort => string.Equals(sort.Target.Name, finalTableName, StringComparison.Ordinal),
                ExecutionTopNTable topN => string.Equals(topN.Target.Name, finalTableName, StringComparison.Ordinal),
                ExecutionTopOffsetTable topOffset => string.Equals(topOffset.Target.Name, finalTableName, StringComparison.Ordinal),
                ExecutionSkipTable skip => string.Equals(skip.Target.Name, finalTableName, StringComparison.Ordinal),
                ExecutionTakeTable take => string.Equals(take.Target.Name, finalTableName, StringComparison.Ordinal),
                ExecutionSliceTable slice => string.Equals(slice.Target.Name, finalTableName, StringComparison.Ordinal),
                _ => false
            });
    }

    private static bool CanUseSingleSerialDynamicLikeProjection(
        ExecutionPlan plan,
        string finalTableName)
    {
        var sourceLoopCount = 0;
        foreach (var node in plan.Body.Nodes)
        {
            if (node is ExecutionSourceLoop)
                sourceLoopCount++;

            if (node switch
                {
                    ExecutionSetOperation operation => IsFinalTarget(operation.Target),
                    ExecutionDistinctTable distinct => IsFinalTarget(distinct.Target),
                    ExecutionSortTable sort => IsFinalTarget(sort.Target),
                    ExecutionTopNTable topN => IsFinalTarget(topN.Target),
                    ExecutionTopOffsetTable topOffset => IsFinalTarget(topOffset.Target),
                    ExecutionSkipTable skip => IsFinalTarget(skip.Target),
                    ExecutionTakeTable take => IsFinalTarget(take.Target),
                    ExecutionSliceTable slice => IsFinalTarget(slice.Target),
                    ExecutionProjectTable project => IsFinalTarget(project.Target),
                    ExecutionMaterializeRecordListToTable materialize => IsFinalTarget(materialize.Target),
                    _ => false
                })
            {
                return false;
            }
        }

        if (sourceLoopCount != 1)
            return false;

        ExecutionAppendRow? finalAppend = null;
        var containsLikeMatch = false;
        foreach (var node in ExecutionIrAnalysis.FlattenNodes(plan.Body))
        {
            if (node is ExecutionParallelFilterProjectLoop or ExecutionParallelBlock ||
                node is ExecutionAppendExistingRow existing && IsFinalTarget(existing.Table))
            {
                return false;
            }

            if (node is ExecutionAppendRow append && IsFinalTarget(append.Table))
            {
                if (finalAppend != null)
                    return false;

                finalAppend = append;
            }

            if (containsLikeMatch)
                continue;

            foreach (var expression in ExecutionIrAnalysis.GetNodeExpressions(node))
            {
                if (ExecutionIrAnalysis.FlattenExpressions(expression).Any(static current =>
                        current is ExecutionDynamicLikeMatch or ExecutionDynamicRLikeMatch))
                {
                    containsLikeMatch = true;
                    break;
                }
            }
        }

        return containsLikeMatch &&
               finalAppend != null &&
               string.Equals(
                   finalAppend.RowShape.TypeName,
                   plan.FinalResult!.Shape.TypeName,
                   StringComparison.Ordinal);

        bool IsFinalTarget(ExecutionVariable target) =>
            string.Equals(target.Name, finalTableName, StringComparison.Ordinal);
    }
}
