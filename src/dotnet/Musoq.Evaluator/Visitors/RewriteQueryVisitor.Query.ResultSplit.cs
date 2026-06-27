using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using InMemoryGroupedFromNode = Musoq.Evaluator.Parser.InMemoryGroupedFromNode;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    private void PushResultQuerySplit(QueryRewriteContext context)
    {
        if (context.GroupBy == null)
        {
            var split = FieldProcessingHelper.SplitBetweenAggregateAndNonAggregate(context.Select.Fields, [], true);

            if (QueryRewriteUtilities.IsQueryWithMixedAggregateAndNonAggregateMethods(split))
            {
                throw new ConstructionNotYetSupported(
                    "Mixing aggregate and non aggregate methods without GROUP BY is not supported by query rewrite.");
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
