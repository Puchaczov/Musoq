using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed partial class SubqueryCorrelationAnalyzer
{
    private QueryNode[] MarkExistsProjectionQueries(Node node)
    {
        var queries = new List<QueryNode>();
        CollectExistsProjectionQueries(node, queries);

        foreach (var query in queries)
            _existsProjectionIgnoredQueries.Add(query);

        return queries.ToArray();
    }

    private static void CollectExistsProjectionQueries(Node node, List<QueryNode> queries)
    {
        switch (node)
        {
            case QueryNode query:
                queries.Add(query);
                return;

            case SingleSetNode singleSet:
                queries.Add(singleSet.Query);
                return;

            case SetOperatorNode setOperator:
                CollectExistsProjectionQueries(setOperator.Left, queries);
                CollectExistsProjectionQueries(setOperator.Right, queries);
                return;

            case CteExpressionNode cte:
                CollectExistsProjectionQueries(cte.OuterExpression, queries);
                return;
        }
    }
}
