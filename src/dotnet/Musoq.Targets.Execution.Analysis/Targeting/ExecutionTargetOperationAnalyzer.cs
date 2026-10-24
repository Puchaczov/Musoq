using System;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.Execution.Analysis;

internal static class ExecutionTargetOperationAnalyzer
{
    public static ExecutionTargetOperationReport Analyze(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var scopes = ExecutionSubqueryPlans.SelfAndDescendants(plan).ToArray();
        var operationIds = scopes.SelectMany(static scope => ExecutionIrAnalysis.FlattenNodes(scope.Body))
            .Where(static node => node is not ExecutionPhaseBoundary)
            .Select(ExecutionOperationCatalog.Resolve)
            .Concat(scopes.SelectMany(static scope => ExecutionIrAnalysis.FlattenExpressions(scope.Body))
                .Select(ExecutionOperationCatalog.Resolve));

        var usages = operationIds
            .GroupBy(static operationId => operationId)
            .Select(static group => new ExecutionOperationUsage(group.Key, group.Count()));

        return new ExecutionTargetOperationReport(usages);
    }
}
