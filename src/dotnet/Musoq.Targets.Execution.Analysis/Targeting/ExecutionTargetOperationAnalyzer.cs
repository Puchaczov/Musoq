using System;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.Execution.Analysis;

internal static class ExecutionTargetOperationAnalyzer
{
    public static ExecutionTargetOperationReport Analyze(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var operationIds = ExecutionIrAnalysis.FlattenNodes(plan.Body)
            .Select(ExecutionOperationCatalog.Resolve)
            .Concat(ExecutionIrAnalysis.FlattenExpressions(plan.Body)
                .Select(ExecutionOperationCatalog.Resolve));

        var usages = operationIds
            .GroupBy(static operationId => operationId)
            .Select(static group => new ExecutionOperationUsage(group.Key, group.Count()));

        return new ExecutionTargetOperationReport(usages);
    }
}
