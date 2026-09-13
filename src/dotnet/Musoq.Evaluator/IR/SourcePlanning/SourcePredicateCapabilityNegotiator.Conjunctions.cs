using System.Collections.Generic;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static partial class SourcePredicateCapabilityNegotiator
{
    private static IReadOnlyList<SourcePredicateExpression> FlattenConjunction(SourcePredicateExpression predicate)
    {
        var result = new List<SourcePredicateExpression>();
        AddConjuncts(predicate, result);
        return result;
    }

    private static void AddConjuncts(
        SourcePredicateExpression predicate,
        ICollection<SourcePredicateExpression> result)
    {
        if (predicate is SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical)
        {
            AddConjuncts(logical.Left, result);
            AddConjuncts(logical.Right, result);
            return;
        }

        result.Add(predicate);
    }

    private static SourcePredicateExpression? Combine(IReadOnlyList<SourcePredicateExpression> predicates)
    {
        if (predicates.Count == 0)
            return null;

        var result = predicates[0];
        for (var index = 1; index < predicates.Count; index++)
        {
            result = new SourcePredicateLogical(
                SourcePredicateLogicalOperator.And,
                result,
                predicates[index]);
        }

        return result;
    }
}
