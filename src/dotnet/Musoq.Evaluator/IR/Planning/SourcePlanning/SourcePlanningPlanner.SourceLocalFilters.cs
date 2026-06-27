using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.SourcePlanning;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static partial class SourcePlanningPlanner
{
    private static bool CanTraverseSourceLocalFilters(
        IReadOnlyList<IrExpression> filterPredicates,
        SchemaScanNode scan)
    {
        if (filterPredicates.Count == 0)
            return true;

        foreach (var predicate in filterPredicates)
        {
            if (!SourcePredicateExpressionConverter.TryConvertPredicate(predicate, scan.Alias, out _))
                return false;
        }

        return true;
    }
}
