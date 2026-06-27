using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private enum PredicateSubqueryKind
    {
        In,
        Exists
    }

    private sealed record SubqueryInfo(
        PredicateSubqueryKind Kind,
        InQueryNode? InQueryNode,
        ExistsQueryNode? ExistsQueryNode,
        bool IsNegated,
        bool RequiresLeftJoin = false,
        SubqueryCorrelationInfo? Correlation = null)
    {
        public Node PredicateNode => Kind switch
        {
            PredicateSubqueryKind.In => InQueryNode ?? throw new InvalidOperationException("IN subquery metadata requires an IN node."),
            PredicateSubqueryKind.Exists => ExistsQueryNode ?? throw new InvalidOperationException("EXISTS subquery metadata requires an EXISTS node."),
            _ => throw new InvalidOperationException($"Unsupported predicate subquery kind: {Kind}.")
        };

        public Node Subquery => Kind switch
        {
            PredicateSubqueryKind.In => InQueryNode?.Subquery ?? throw new InvalidOperationException("IN subquery metadata requires an IN node."),
            PredicateSubqueryKind.Exists => ExistsQueryNode?.Subquery ?? throw new InvalidOperationException("EXISTS subquery metadata requires an EXISTS node."),
            _ => throw new InvalidOperationException($"Unsupported predicate subquery kind: {Kind}.")
        };

        public bool IsIn => Kind == PredicateSubqueryKind.In;

        public bool IsExists => Kind == PredicateSubqueryKind.Exists;

        public static SubqueryInfo CreateIn(InQueryNode node, bool isNegated)
        {
            return new SubqueryInfo(PredicateSubqueryKind.In, node, null, isNegated);
        }

        public static SubqueryInfo CreateExists(ExistsQueryNode node, bool isNegated)
        {
            return new SubqueryInfo(PredicateSubqueryKind.Exists, null, node, isNegated);
        }
    }
}
