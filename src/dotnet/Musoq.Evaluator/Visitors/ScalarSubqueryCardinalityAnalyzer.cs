using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static class ScalarSubqueryCardinalityAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var visitor = new Visitor(context);
        // Cardinality is a property of the authored subquery shape. Use the
        // pre-binding tree so later rewrites cannot hide the source rows.
        context.AuthoredQuery.Accept(visitor);
    }

    private sealed class Visitor(SemanticAdvisoryContext context)
        : RawTraverseVisitor<NoOpExpressionVisitor>(new NoOpVisitor())
    {
        public override void Visit(ScalarSubqueryNode node)
        {
            if (IsProvablyMultiRow(node.Subquery))
            {
                context.ReportError(
                    DiagnosticCode.MQ3095_ScalarSubqueryCardinality,
                    "Scalar subquery may return more than one row.",
                    node.Subquery.SpanOrEmpty());
            }

            base.Visit(node);
        }

        private static bool IsProvablyMultiRow(Node subquery)
        {
            var query = subquery switch
            {
                SingleSetNode singleSet => singleSet.Query,
                QueryNode directQuery => directQuery,
                _ => null
            };

            if (query?.From is { } source)
            {
                while (source is ExpressionFromNode expressionFrom)
                    source = expressionFrom.Expression;

                if (source is not ValuesFromNode values || values.Rows.Count < 2)
                    return false;

                // These clauses can reduce or reshape cardinality, so do not guess.
                if (query.Where != null ||
                    query.GroupBy != null ||
                    query.Skip != null ||
                    query.Take != null ||
                    query.Window != null ||
                    query.Qualify != null ||
                    query.Select.IsDistinct)
                    return false;

                var aggregateCollector = new AggregateCollector();
                query.Select.Accept(new AggregateTraversal(aggregateCollector));
                return !aggregateCollector.Found;
            }

            return false;
        }
    }

    private sealed class NoOpVisitor : NoOpExpressionVisitor
    {
    }

    private sealed class AggregateCollector : NoOpExpressionVisitor
    {
        public bool Found { get; private set; }

        public override void Visit(AccessMethodNode node)
        {
            Found |= node.IsAggregate;
        }
    }

    private sealed class AggregateTraversal(AggregateCollector collector)
        : RawTraverseVisitor<AggregateCollector>(collector)
    {
    }
}
