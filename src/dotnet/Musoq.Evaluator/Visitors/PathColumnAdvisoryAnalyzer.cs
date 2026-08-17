using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class PathColumnAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        Visit(context, context.Query, new HashSet<Node>(ReferenceEqualityComparer.Instance));
    }

    private static void Visit(
        SemanticAdvisoryContext context,
        Node node,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        switch (node)
        {
            case EqualityNode equality:
                AnalyzeColumnLiteralPair(context, equality.Left, equality.Right);
                break;
            case DiffNode diff:
                AnalyzeColumnLiteralPair(context, diff.Left, diff.Right);
                break;
            case LikeNode like:
                AnalyzeColumnLiteralPair(context, like.Left, like.Right);
                break;
            case InNode @in:
                AnalyzeInArguments(context, @in);
                break;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            Visit(context, child, visited);
    }

    private static void AnalyzeColumnLiteralPair(
        SemanticAdvisoryContext context,
        Node left,
        Node right)
    {
        if (IsPathStringColumn(left))
        {
            ReportLiteral(context, right);
            return;
        }

        if (IsPathStringColumn(right))
            ReportLiteral(context, left);
    }

    private static void AnalyzeInArguments(
        SemanticAdvisoryContext context,
        InNode @in)
    {
        if (!IsPathStringColumn(@in.Left) || @in.Right is not ArgsListNode arguments)
            return;

        foreach (var argument in arguments.Args)
            ReportLiteral(context, argument);
    }

    private static bool IsPathStringColumn(Node node)
    {
        return node is AccessColumnNode column &&
               column.ReturnType == typeof(string) &&
               SuspiciousOrdinaryStringEscapeDiagnostics.IsPathSensitiveName(column.Name);
    }

    private static void ReportLiteral(
        SemanticAdvisoryContext context,
        Node literal)
    {
        if (!context.Literals.TryResolve(literal, out var origin) || origin.IsRaw)
            return;

        if (StringEscapeRiskDetector.Find(origin.Content, origin.ContentStart) is not
            { IsRootedPath: false, HasNonEscapeContent: true } risk)
            return;

        context.Report(
            DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape,
            ErrorCatalog.GetMessage(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, risk.EscapeText),
            risk.Span);
    }
}
