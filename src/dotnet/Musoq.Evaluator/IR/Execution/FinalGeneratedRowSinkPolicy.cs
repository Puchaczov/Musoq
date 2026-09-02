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
}
