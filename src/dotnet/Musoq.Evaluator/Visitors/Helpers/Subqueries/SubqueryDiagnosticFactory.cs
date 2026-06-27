using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class SubqueryDiagnosticFactory
{
    private const string VisitorName = "SubqueryToCteRewriteVisitor";

    public static VisitorException InvalidSubquery(string operation, string message, Node node)
    {
        return InvalidSubquery(operation, message, GetSpan(node));
    }

    public static VisitorException InvalidSubquery(string operation, string message, TextSpan span)
    {
        return new VisitorException(
            VisitorName,
            operation,
            message,
            DiagnosticCode.MQ2024_InvalidSubquery,
            span);
    }

    public static VisitorException CteDefinitionConsumesOuterAlias()
    {
        return InvalidSubquery(
            "correlated subquery validation",
            "CTE definitions cannot reference aliases from the query that consumes the CTE.",
            TextSpan.Empty);
    }

    private static TextSpan GetSpan(Node node)
    {
        return node?.HasSpan == true ? node.Span : TextSpan.Empty;
    }
}
