using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static class NullSensitiveMembershipAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        var resolver = new PredicateConstantResolver(context.Query);
        VisitStructure(context, resolver, context.Query, new HashSet<Node>(ReferenceEqualityComparer.Instance));
    }

    private static void VisitStructure(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        Node node,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        switch (node)
        {
            case WhereNode where:
                AnalyzePredicate(context, resolver, where.Expression);
                break;
            case HavingNode having:
                AnalyzePredicate(context, resolver, having.Expression);
                break;
            case QualifyNode qualify:
                AnalyzePredicate(context, resolver, qualify.Expression);
                break;
            case JoinFromNode join:
                AnalyzePredicate(context, resolver, join.Expression);
                break;
            case JoinSourcesTableFromNode join:
                AnalyzePredicate(context, resolver, join.Expression);
                break;
            case JoinInMemoryWithSourceTableFromNode join:
                AnalyzePredicate(context, resolver, join.Expression);
                break;
            case AccessMethodNode method when method.FilterExpression is { } filter:
                AnalyzePredicate(context, resolver, filter);
                break;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            VisitStructure(context, resolver, child, visited);
    }

    private static void AnalyzePredicate(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        Node predicate)
    {
        VisitPredicate(context, resolver, predicate,
            new HashSet<Node>(ReferenceEqualityComparer.Instance));
    }

    private static void VisitPredicate(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        Node node,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        if (node is NotNode { Expression: InNode membership } && membership.Right is ArgsListNode args)
        {
            foreach (var candidate in args.Args)
            {
                if (resolver.Resolve(candidate) is NullNode nullValue)
                {
                    context.Report(
                        DiagnosticCode.MQ5024_NullSensitiveNotIn,
                        ErrorCatalog.GetMessage(DiagnosticCode.MQ5024_NullSensitiveNotIn),
                        nullValue.HasSpan ? nullValue.Span : membership.Span);
                    break;
                }
            }
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            VisitPredicate(context, resolver, child, visited);
    }
}
