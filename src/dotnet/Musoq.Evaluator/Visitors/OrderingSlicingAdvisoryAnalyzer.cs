using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Reports ordering and slicing constructs whose scope or row selection is likely
/// different from the query author's intent. This analyzer only observes the
/// source tree; it does not participate in planning or execution.
/// </summary>
internal static class OrderingSlicingAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        Visit(context, context.SourceQuery.Expression, outerOrdering: false);
    }

    private static void Visit(
        SemanticAdvisoryContext context,
        Node node,
        bool outerOrdering)
    {
        switch (node)
        {
            case QueryNode query:
                VisitQuery(context, query, outerOrdering);
                return;

            case SetOperatorNode setOperator:
                Visit(context, setOperator.Left, outerOrdering);
                Visit(context, setOperator.Right, outerOrdering);
                return;

            case CteExpressionNode cte:
                var cteOuterOrdering = outerOrdering || HasTopLevelOrdering(cte.OuterExpression);
                Visit(context, cte.OuterExpression, outerOrdering);
                foreach (var innerExpression in cte.InnerExpression)
                    Visit(context, innerExpression.Value, cteOuterOrdering);
                return;

            case CteInnerExpressionNode cteInner:
                Visit(context, cteInner.Value, outerOrdering);
                return;

            case DerivedTableFromNode derivedTable:
                Visit(context, derivedTable.Query, outerOrdering);
                return;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            Visit(context, child, outerOrdering);
    }

    private static void VisitQuery(
        SemanticAdvisoryContext context,
        QueryNode query,
        bool outerOrdering)
    {
        if (query.Skip is { Value: > 0 } skip && query.OrderBy == null)
        {
            context.Report(
                DiagnosticCode.MQ5021_UnorderedSkip,
                ErrorCatalog.GetMessage(DiagnosticCode.MQ5021_UnorderedSkip, skip.Value),
                skip.SpanOrEmpty());
        }

        var childOuterOrdering = outerOrdering || query.OrderBy != null;

        Visit(context, query.From, childOuterOrdering);

        if (query.Where != null)
            Visit(context, query.Where, outerOrdering);

        Visit(context, query.Select, outerOrdering);

        if (query.Take != null)
            Visit(context, query.Take, outerOrdering);

        if (query.Skip != null)
            Visit(context, query.Skip, outerOrdering);

        if (query.GroupBy != null)
            Visit(context, query.GroupBy, outerOrdering);

        if (query.Window != null)
            Visit(context, query.Window, outerOrdering);

        if (query.Qualify != null)
            Visit(context, query.Qualify, outerOrdering);

        if (query.OrderBy != null)
            Visit(context, query.OrderBy, outerOrdering);
    }

    private static bool HasTopLevelOrdering(Node node)
    {
        return node switch
        {
            QueryNode query => query.OrderBy != null,
            SingleSetNode singleSet => singleSet.Query.OrderBy != null,
            _ => false
        };
    }
}
