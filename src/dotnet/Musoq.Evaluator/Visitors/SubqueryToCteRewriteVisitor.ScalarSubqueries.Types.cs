using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private sealed record ScalarRewriteResult(
        SelectNode Select,
        FromNode From,
        Node? WhereExpression,
        GroupByNode? GroupBy,
        OrderByNode? OrderBy,
        WindowNode? Window,
        QualifyNode? Qualify);

    private sealed record ScalarSubqueryRewrite(QueryNode Query, Node JoinExpression);

    private sealed record ScalarSubqueryJoin(
        InMemoryTableFromNode CteRef,
        Node JoinExpression,
        Node Replacement,
        JoinType JoinType);
}
