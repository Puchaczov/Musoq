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
        Visit(context, context.SourceQuery.Expression, insideSetChain: false, outerOrdering: false);
    }

    private static void Visit(
        SemanticAdvisoryContext context,
        Node node,
        bool insideSetChain,
        bool outerOrdering)
    {
        switch (node)
        {
            case QueryNode query:
                VisitQuery(context, query, outerOrdering);
                return;

            case SetOperatorNode setOperator:
                if (!insideSetChain)
                    ReportSetOperationScope(context, setOperator, outerOrdering);

                Visit(context, setOperator.Left, insideSetChain: true, outerOrdering);
                Visit(context, setOperator.Right, insideSetChain: true, outerOrdering);
                return;

            case CteExpressionNode cte:
                var cteOuterOrdering = outerOrdering || HasTopLevelOrdering(cte.OuterExpression);
                Visit(context, cte.OuterExpression, insideSetChain: false, outerOrdering);
                foreach (var innerExpression in cte.InnerExpression)
                    Visit(context, innerExpression.Value, insideSetChain: false, cteOuterOrdering);
                return;

            case CteInnerExpressionNode cteInner:
                Visit(context, cteInner.Value, insideSetChain: false, outerOrdering);
                return;

            case DerivedTableFromNode derivedTable:
                Visit(context, derivedTable.Query, insideSetChain: false, outerOrdering);
                return;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            Visit(context, child, insideSetChain: false, outerOrdering);
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

        Visit(context, query.From, insideSetChain: false, childOuterOrdering);

        if (query.Where != null)
            Visit(context, query.Where, insideSetChain: false, outerOrdering);

        Visit(context, query.Select, insideSetChain: false, outerOrdering);

        if (query.Take != null)
            Visit(context, query.Take, insideSetChain: false, outerOrdering);

        if (query.Skip != null)
            Visit(context, query.Skip, insideSetChain: false, outerOrdering);

        if (query.GroupBy != null)
            Visit(context, query.GroupBy, insideSetChain: false, outerOrdering);

        if (query.Window != null)
            Visit(context, query.Window, insideSetChain: false, outerOrdering);

        if (query.Qualify != null)
            Visit(context, query.Qualify, insideSetChain: false, outerOrdering);

        if (query.OrderBy != null)
            Visit(context, query.OrderBy, insideSetChain: false, outerOrdering);
    }

    private static void ReportSetOperationScope(
        SemanticAdvisoryContext context,
        SetOperatorNode setOperator,
        bool outerOrdering)
    {
        if (outerOrdering)
            return;

        var rightmostQuery = FindRightmostQuery(setOperator.Right);
        if (rightmostQuery?.OrderBy == null ||
            rightmostQuery.Take != null ||
            rightmostQuery.Skip is { Value: > 0 })
        {
            return;
        }

        context.Report(
            DiagnosticCode.MQ5020_SetOperationOrderByScope,
            ErrorCatalog.GetMessage(DiagnosticCode.MQ5020_SetOperationOrderByScope),
            rightmostQuery.OrderBy.SpanOrEmpty());
    }

    private static QueryNode? FindRightmostQuery(Node node)
    {
        while (node is SetOperatorNode setOperator)
            node = setOperator.Right;

        return node switch
        {
            QueryNode query => query,
            SingleSetNode singleSet => singleSet.Query,
            _ => null
        };
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
