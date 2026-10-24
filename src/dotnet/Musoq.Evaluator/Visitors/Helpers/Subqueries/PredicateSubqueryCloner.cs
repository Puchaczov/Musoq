using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class PredicateSubqueryCloner
{
    internal static ExistsQueryNode Clone(ExistsQueryNode node, Stack<Node> nodes)
    {
        var query = nodes.Pop();
        if (node is QuantifiedSubqueryNode quantified)
            return new QuantifiedSubqueryNode(nodes.Pop(), quantified.Comparison, quantified.Quantifier, query).CopySpansFrom(node);
        return new ExistsQueryNode(query).CopySpansFrom(node);
    }
}
