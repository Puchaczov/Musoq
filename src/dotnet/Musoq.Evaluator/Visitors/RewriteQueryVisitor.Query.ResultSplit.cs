using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using InMemoryGroupedFromNode = Musoq.Evaluator.Parser.InMemoryGroupedFromNode;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    private void PushResultQuerySplit(QueryRewriteContext context)
    {
        QueryRewriteUtilities.ThrowIfUnsupportedAggregateProjection(context.GroupBy, context.Select.Fields);
        if (context.GroupBy == null)
        {
            var split = FieldProcessingHelper.SplitBetweenAggregateAndNonAggregate(context.Select.Fields, [], true);

            if (QueryRewriteUtilities.IsQueryWithMixedAggregateAndNonAggregateMethods(split))
            {
                throw new ConstructionNotYetSupported(
                    "This aggregate projection shape is not supported without GROUP BY.",
                    DiagnosticCode.MQ3097_UnsupportedAggregateProjection,
                    context.Select.SpanOrEmpty());
            }
        }

        var scopeCreateResultTable = Scope.AddScope("Table");
        var scopeResultQuery = Scope.AddScope("Query");

        scopeCreateResultTable[MetaAttributes.CreateTableVariableName] = context.From.Alias.ToScoreTable();
        scopeCreateResultTable[MetaAttributes.OriginAlias] = context.From.Alias;
        scopeResultQuery[MetaAttributes.SelectIntoVariableName] = context.From.Alias.ToScoreTable();
        scopeResultQuery[MetaAttributes.Contexts] = context.From.Alias;
        scopeResultQuery[MetaAttributes.SourceName] = context.Source;

        var newFrom = context.LastJoinQuery != null
            ? new Parser.ExpressionFromNode(
                new InMemoryGroupedFromNode(context.LastJoinQuery.From.Alias)
            )
            : context.From;

        context.AliasesPositionsSymbol.AliasesPositions.Add(newFrom.Alias, context.AliasIndex);

        context.SplitNodes.Add(new CreateTransformationTableNode(scopeResultQuery[MetaAttributes.SelectIntoVariableName],
            [], context.Select.Fields, false));
        QueryNode resultQuery = context.GroupBy != null
            ? new InternalQueryNode(
                context.ScoreSelect,
                newFrom,
                context.ScoreWhere,
                context.GroupBy,
                context.ScoreOrderBy,
                context.Skip,
                context.Take,
                QueryRewriteUtilities.CreateRefreshMethods(context.UsedRefreshMethods ?? []))
            : new DetailedQueryNode(
                context.ScoreSelect,
                newFrom,
                context.ScoreWhere,
                context.GroupBy,
                context.ScoreOrderBy,
                context.Skip,
                context.Take,
                scopeResultQuery[MetaAttributes.SelectIntoVariableName],
                context.Window,
                context.Qualify);

        context.SplitNodes.Add(resultQuery);

        Nodes.Push(
            new MultiStatementNode(
                context.SplitNodes.ToArray(),
                null));
    }
}
