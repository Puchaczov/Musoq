using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class PredicateApplyCorrelationShape
{
    public static bool IsNullGuardedRange(Node expression)
    {
        var predicates = SubqueryCorrelationUtilities.SplitConjuncts(expression);
        var guardedRanges = predicates.Where(static predicate => predicate is OrNode).ToArray();

        return guardedRanges.Length == 1 &&
               predicates.All(predicate => predicate is EqualityNode || ReferenceEquals(predicate, guardedRanges[0])) &&
               IsNullGuardedRangeDisjunction(guardedRanges[0]);
    }

    private static bool IsNullGuardedRangeDisjunction(Node expression)
    {
        var terms = SplitDisjuncts(expression);
        var ranges = terms.Where(SubqueryCorrelationUtilities.IsSingleRangeCorrelation).ToArray();
        var nullGuards = terms.OfType<IsNullNode>().ToArray();

        if (ranges.Length != 1 || nullGuards.Length != 2 || terms.Length != 3)
            return false;

        var rangeColumns = SubqueryCorrelationUtilities.CollectAccessColumns(ranges[0])
            .Select(CreateColumnKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var guardedColumns = nullGuards
            .SelectMany(static guard => SubqueryCorrelationUtilities.CollectAccessColumns(guard.Expression))
            .Select(CreateColumnKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return rangeColumns.Count == 2 && rangeColumns.SetEquals(guardedColumns);
    }

    private static Node[] SplitDisjuncts(Node expression)
    {
        if (expression is not OrNode or)
            return [expression];

        return [..SplitDisjuncts(or.Left), ..SplitDisjuncts(or.Right)];
    }

    private static string CreateColumnKey(AccessColumnNode column) => $"{column.Alias}.{column.Name}";
}
