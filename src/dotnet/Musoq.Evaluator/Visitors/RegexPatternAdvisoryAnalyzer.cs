using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.Visitors;

internal static class RegexPatternAdvisoryAnalyzer
{
    private static readonly Dictionary<string, int> PatternIndexes =
        new(StringComparer.Ordinal)
        {
            ["Match"] = 0,
            ["RegexMatches"] = 0,
            ["RegexReplace"] = 1,
            ["RegexExtract"] = 1,
            ["RegexExtractAll"] = 1,
            ["IsMatch"] = 1
        };

    public static void Analyze(SemanticAdvisoryContext context)
    {
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
        if (!context.Literals.TryResolve(pattern, out var origin) || origin.IsRaw ||
            !TryFindWordBoundary(origin, out var span))
        {
            return;
        }

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
}
