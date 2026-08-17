using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class LikePatternAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        Visit(context, context.Query, new HashSet<Node>(ReferenceEqualityComparer.Instance));
    }

    private static void Visit(SemanticAdvisoryContext context, Node node, HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        if (node is LikeNode like)
            ReportIfGlobLike(context, like.Right);

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            Visit(context, child, visited);
    }

    private static void ReportIfGlobLike(SemanticAdvisoryContext context, Node pattern)
    {
        if (!context.Literals.TryResolve(pattern, out var origin))
            return;

        var content = origin.Content;
        if (content.IndexOf('%') >= 0 || content.IndexOf('_') >= 0)
            return;

        var wildcard = content.IndexOf('*');
        if (wildcard < 0)
        {
            wildcard = content.IndexOf('?');
            if (wildcard < 0 || ContainsWhitespace(content) ||
                content.IndexOf('.') < 0 && content.IndexOf('/') < 0 && content.IndexOf('\\') < 0)
            {
                return;
            }
        }

        context.Report(
            DiagnosticCode.MQ5016_GlobWildcardInLike,
            ErrorCatalog.GetMessage(DiagnosticCode.MQ5016_GlobWildcardInLike, content[wildcard].ToString()),
            origin.ContentSpan(wildcard, 1));
    }

    private static bool ContainsWhitespace(ReadOnlySpan<char> content)
    {
        foreach (var character in content)
            if (char.IsWhiteSpace(character))
                return true;

        return false;
    }
}
