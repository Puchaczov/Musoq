using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal static class ExpressionCseSkipDiagnostics
{
    public static ExpressionCseSkipDiagnosticSummary Analyze(ExecutionPlan plan)
    {
        var nodes = ExecutionIrAnalysis.FlattenNodes(plan.Body).ToArray();
        return new ExpressionCseSkipDiagnosticSummary(
            nodes.Sum(GetHashKeyGroups),
            nodes.Sum(GetProbePredicateGroups),
            0,
            nodes.Sum(GetWindowHelperBodyGroups),
            nodes.OfType<ExecutionForEachIndexed>()
                .Sum(static node => CountRepeatedGroups(GetUnsupportedScopeExpressions(node.Body))));
    }

    private static int GetHashKeyGroups(ExecutionNode node)
    {
        return 0;
    }

    private static int GetProbePredicateGroups(ExecutionNode node)
    {
        return 0;
    }

    private static int GetWindowHelperBodyGroups(ExecutionNode node)
    {
        var expressions = ExecutionExpressionCseFacts.GetWindowHelperExpressions(node);
        return expressions.Count == 0
            ? 0
            : CountWindowRepeatedGroups(expressions);
    }

    private static int CountWindowRepeatedGroups(IEnumerable<ExecutionExpression> expressions)
    {
        var materialized = expressions.ToArray();
        return materialized.All(ExecutionExpressionCseFacts.IsWindowHelperIndependentExpression)
            ? 0
            : CountRepeatedGroups(materialized);
    }

    private static IEnumerable<ExecutionExpression> GetUnsupportedScopeExpressions(ExecutionBlock block)
    {
        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
        {
            if (node is ExecutionAppendRow or ExecutionAppendRecord)
                continue;

            foreach (var expression in ExecutionIrAnalysis.GetNodeExpressions(node))
                yield return expression;
        }
    }

    private static int CountRepeatedGroups(IEnumerable<ExecutionExpression> expressions)
    {
        return expressions
            .SelectMany(static expression => ExecutionExpressionCseFacts.CollectHoistableOccurrences(expression))
            .GroupBy(static occurrence => occurrence.Signature, StringComparer.Ordinal)
            .Count(static group => group.Count() > 1 && group.Any(static occurrence => occurrence.IsSafeOrigin));
    }
}

