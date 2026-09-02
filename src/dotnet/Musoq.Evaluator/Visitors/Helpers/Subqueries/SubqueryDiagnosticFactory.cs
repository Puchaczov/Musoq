using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class SubqueryDiagnosticFactory
{
    private const string VisitorName = "SubqueryToCteRewriteVisitor";

    public static VisitorException InvalidSubquery(
        string operation,
        string message,
        Node node,
        IReadOnlyDictionary<string, string>? arguments = null,
        IReadOnlyList<DiagnosticAction>? suggestedFixes = null)
    {
        return InvalidSubquery(operation, message, GetSpan(node), arguments, suggestedFixes);
    }

    public static VisitorException InvalidSubquery(
        string operation,
        string message,
        TextSpan span,
        IReadOnlyDictionary<string, string>? arguments = null,
        IReadOnlyList<DiagnosticAction>? suggestedFixes = null)
    {
        return new VisitorException(
            VisitorName,
            operation,
            message,
            DiagnosticCode.MQ2024_InvalidSubquery,
            span,
            arguments,
            suggestedFixes);
    }

    public static VisitorException CteDefinitionConsumesOuterAlias(
        IReadOnlySet<string> aliases,
        TextSpan span)
    {
        var outerAliases = string.Join(", ", aliases.OrderBy(static alias => alias, StringComparer.OrdinalIgnoreCase));
        return InvalidSubquery(
            "correlated subquery validation",
            $"CTE definitions cannot reference aliases from the query that consumes the CTE: {outerAliases}.",
            span,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["constraint"] = "cte-definition-outer-alias",
                ["outerAliases"] = outerAliases,
                ["allowedScope"] = "CTE body-local sources only"
            });
    }

    public static VisitorException CteDefinitionConsumesOuterAlias(SubqueryCorrelationAnalysis analysis)
    {
        return CteDefinitionConsumesOuterAlias(
            analysis.IllegalOuterConsumingCteAliases,
            analysis.IllegalOuterConsumingCteReferenceSpan);
    }

    private static TextSpan GetSpan(Node node)
    {
        if (node?.HasSpan == true)
            return node.Span;

        if (node != null && !node.FullSpan.IsEmpty)
            return node.FullSpan;

        if (node != null)
            foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
            {
                var span = GetSpan(child);
                if (!span.IsEmpty)
                    return span;
            }

        return TextSpan.Empty;
    }
}
