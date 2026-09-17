using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.Visitors;

internal static class RegexPatternAdvisoryAnalyzer
{
    private static readonly Dictionary<string, int> PatternIndexes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Match"] = 0,
            ["RegexMatches"] = 0,
            ["RegexReplace"] = 1,
            ["RegexExtract"] = 1,
            ["RegexExtractAll"] = 1,
            ["IsMatch"] = 1
        };

    internal static IReadOnlyList<Diagnostic> FilterSyntaxDiagnostics(
        RootNode query,
        SourceText sourceText,
        IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var suppressedSpans = new HashSet<(int Start, int End)>();
        var literals = new LiteralOriginResolver(query, sourceText);
        CollectRegexPatterns(
            query,
            literals,
            suppressedSpans,
            new HashSet<Node>(ReferenceEqualityComparer.Instance));

        return diagnostics
            .Where(diagnostic => diagnostic.Code != DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape ||
                                !suppressedSpans.Contains((diagnostic.Span.Start, diagnostic.Span.End)))
            .ToArray();
    }

    internal static void SuppressLexicalDiagnostics(
        RootNode query,
        SourceText sourceText,
        DiagnosticContext diagnostics)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var suppressedSpans = new HashSet<(int Start, int End)>();
        CollectRegexPatterns(
            query,
            new LiteralOriginResolver(query, sourceText),
            suppressedSpans,
            new HashSet<Node>(ReferenceEqualityComparer.Instance));

        diagnostics.SuppressDiagnostics(diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape &&
            suppressedSpans.Contains((diagnostic.Span.Start, diagnostic.Span.End)));
    }

    public static void Analyze(SemanticAdvisoryContext context)
    {
        if (context.Diagnostics.SourceText is { } sourceText)
            SuppressLexicalDiagnostics(context.Query, sourceText, context.Diagnostics);

        Visit(context, context.Query, new HashSet<Node>(ReferenceEqualityComparer.Instance));
    }

    private static void Visit(SemanticAdvisoryContext context, Node node, HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        switch (node)
        {
            case RLikeNode rLike:
                ReportIfHazardous(context, rLike.Right);
                break;
            case AccessMethodNode method:
                ReportMethodPatternIfHazardous(context, method);
                break;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            Visit(context, child, visited);
    }

    private static void ReportMethodPatternIfHazardous(
        SemanticAdvisoryContext context,
        AccessMethodNode method)
    {
        if (method.Method is not { DeclaringType: { } declaringType } boundMethod ||
            !typeof(LibraryBase).IsAssignableFrom(declaringType) ||
            !PatternIndexes.TryGetValue(boundMethod.Name, out var patternIndex) ||
            patternIndex >= method.Arguments.Args.Length)
        {
            return;
        }

        ReportIfHazardous(context, method.Arguments.Args[patternIndex]);
    }

    private static void ReportIfHazardous(SemanticAdvisoryContext context, Node pattern)
    {
        if (!context.Literals.TryResolve(pattern, out var origin) || origin.IsRaw)
        {
            return;
        }

        if (!TryFindWordBoundary(origin, out var span))
            return;

        context.Report(
            DiagnosticCode.MQ5015_SuspiciousRegexEscape,
            ErrorCatalog.GetMessage(DiagnosticCode.MQ5015_SuspiciousRegexEscape, "\\b"),
            span);
    }

    private static bool TryFindWordBoundary(LiteralOrigin origin, out TextSpan span)
    {
        var content = origin.Content;
        var inCharacterClass = false;
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] == '[')
            {
                inCharacterClass = true;
                continue;
            }

            if (content[index] == ']')
            {
                inCharacterClass = false;
                continue;
            }

            if (content[index] != '\\' || index + 1 >= content.Length)
                continue;

            if (content[index + 1] == '\\' || content[index + 1] == '\'')
            {
                index++;
                continue;
            }

            if (!inCharacterClass && content[index + 1] == 'b')
            {
                span = origin.ContentSpan(index, 2);
                return true;
            }
        }

        span = default;
        return false;
    }

    private static void CollectRegexPatterns(
        Node node,
        LiteralOriginResolver literals,
        HashSet<(int Start, int End)> suppressedSpans,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        switch (node)
        {
            case RLikeNode rLike:
                AddSuppressibleSpan(literals, rLike.Right, suppressedSpans);
                break;
            case AccessMethodNode method when PatternIndexes.TryGetValue(method.Name, out var patternIndex) &&
                                             patternIndex < method.Arguments.Args.Length:
                AddSuppressibleSpan(literals, method.Arguments.Args[patternIndex], suppressedSpans);
                break;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            CollectRegexPatterns(child, literals, suppressedSpans, visited);
    }

    private static void AddSuppressibleSpan(
        LiteralOriginResolver literals,
        Node pattern,
        HashSet<(int Start, int End)> suppressedSpans)
    {
        if (literals.TryResolve(pattern, out var origin) &&
            !origin.IsRaw &&
            IsLeadingWordBoundaryEncoding(origin.Content))
        {
            suppressedSpans.Add((origin.ContentStart, origin.ContentStart + 2));
        }
    }

    private static bool IsLeadingWordBoundaryEncoding(ReadOnlySpan<char> content)
    {
        return content.Length >= 2 &&
               content[0] == '\\' &&
               (content[1] == 'b' ||
                content.Length >= 3 && content[1] == '\\' && content[2] == 'b');
    }
}
