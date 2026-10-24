using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionSubqueryPlans
{
    internal static IEnumerable<ExecutionPlan> SelfAndDescendants(ExecutionPlan plan)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        return Enumerate(plan, visited);
    }

    private static IEnumerable<ExecutionPlan> Enumerate(ExecutionPlan plan, HashSet<string> visited)
    {
        if (!visited.Add(plan.Identifier))
            yield break;
        yield return plan;
        foreach (var subquery in ExecutionIrAnalysis.FlattenExpressions(plan.Body).OfType<ExecutionSubquery>())
            foreach (var nested in Enumerate(subquery.Plan, visited))
                yield return nested;
    }
}
