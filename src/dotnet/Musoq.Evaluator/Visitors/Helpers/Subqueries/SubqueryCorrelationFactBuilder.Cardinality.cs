using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static partial class SubqueryCorrelationFactBuilder
{
    private static SubqueryCardinalityContextFact[] CollectCardinalityContexts(Node wrapper, Node body)
    {
        var contexts = new List<SubqueryCardinalityContextFact>();
        if (wrapper is ScalarSubqueryNode)
            AddContext(contexts, SubqueryCardinalityContextKind.ScalarSubquery);

        CollectCardinalityContexts(body, contexts);
        return contexts
            .GroupBy(static context => context.Kind)
            .Select(static group => group.First())
            .ToArray();
    }

    private static void CollectCardinalityContexts(Node node, List<SubqueryCardinalityContextFact> contexts)
    {
        switch (node)
        {
            case QueryNode query:
                CollectQueryContexts(query, contexts);
                return;

            case SingleSetNode singleSet:
                CollectCardinalityContexts(singleSet.Query, contexts);
                return;

            case SetOperatorNode setOperator:
                AddContext(contexts, SubqueryCardinalityContextKind.SetOperation);
                CollectCardinalityContexts(setOperator.Left, contexts);
                CollectCardinalityContexts(setOperator.Right, contexts);
                return;

            case CteExpressionNode cte:
                foreach (var expression in cte.InnerExpression)
                    CollectCardinalityContexts(expression.Value, contexts);
                CollectCardinalityContexts(cte.OuterExpression, contexts);
                return;
        }
    }

    private static void CollectQueryContexts(QueryNode query, List<SubqueryCardinalityContextFact> contexts)
    {
        if (query.Select.IsDistinct)
            AddContext(contexts, SubqueryCardinalityContextKind.Distinct);
        if (query.GroupBy != null)
            AddContext(contexts, SubqueryCardinalityContextKind.GroupBy);
        if (query.OrderBy != null)
            AddContext(contexts, SubqueryCardinalityContextKind.OrderBy);
        if (query.Skip != null)
            AddContext(contexts, SubqueryCardinalityContextKind.Skip);
        if (query.Take != null)
            AddContext(contexts, SubqueryCardinalityContextKind.Take);
        if (query.Window != null)
            AddContext(contexts, SubqueryCardinalityContextKind.Window);
        if (query.Qualify != null)
            AddContext(contexts, SubqueryCardinalityContextKind.Qualify);
    }

    private static void AddContext(List<SubqueryCardinalityContextFact> contexts, SubqueryCardinalityContextKind kind)
    {
        contexts.Add(new SubqueryCardinalityContextFact(kind, CreateContextReason(kind)));
    }

    private static string CreateContextReason(SubqueryCardinalityContextKind kind)
    {
        return kind switch
        {
            SubqueryCardinalityContextKind.ScalarSubquery => "Scalar subquery result cardinality must be preserved.",
            SubqueryCardinalityContextKind.Distinct => "DISTINCT changes duplicate semantics inside the subquery.",
            SubqueryCardinalityContextKind.GroupBy => "GROUP BY changes row cardinality inside the subquery.",
            SubqueryCardinalityContextKind.OrderBy => "ORDER BY can affect top-per-outer-row subquery semantics.",
            SubqueryCardinalityContextKind.Skip => "SKIP slices subquery rows and is cardinality-sensitive.",
            SubqueryCardinalityContextKind.Take => "TAKE slices subquery rows and is cardinality-sensitive.",
            SubqueryCardinalityContextKind.Window => "WINDOW definitions are phase-sensitive inside the subquery.",
            SubqueryCardinalityContextKind.Qualify => "QUALIFY filters after windowing inside the subquery.",
            SubqueryCardinalityContextKind.SetOperation => "Set operations impose arm and comparer cardinality semantics.",
            _ => "Subquery context is cardinality-sensitive."
        };
    }
}
