using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using InMemoryGroupedFromNode = Musoq.Evaluator.Parser.InMemoryGroupedFromNode;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    private void PushGroupedQuerySplit(QueryRewriteContext context)
    {
        var groupBy = context.GroupBy!;
        var nestedFrom = context.SplitNodes.Count > 0
            ? new Parser.ExpressionFromNode(new InMemoryGroupedFromNode(context.LastJoinQuery!.From.Alias))
            : context.From;

        var refreshMethods = QueryRewriteUtilities.CreateRefreshMethods(context.UsedRefreshMethods ?? []);
        var splitSelectFields =
            FieldProcessingHelper.SplitBetweenAggregateAndNonAggregate(context.Select.Fields, groupBy.Fields, true);
        var aggSelect = new SelectNode(QueryRewriteUtilities
            .ConcatAggregateFieldsWithGroupByFields(splitSelectFields[0], groupBy.Fields)
            .Reverse().ToArray());
        var outSelect = new SelectNode(splitSelectFields[1], context.Select.IsDistinct);

        var scopeCreateTransformingTable = Scope.AddScope("Table");
        var scopeTransformedQuery = Scope.AddScope("Query");
        var scopeCreateResultTable = Scope.AddScope("Table");
        var scopeResultQuery = Scope.AddScope("Query");

        var groupingTableName = nestedFrom.Alias.ToGroupingTable();
        scopeCreateTransformingTable[MetaAttributes.CreateTableVariableName] = groupingTableName;
        scopeCreateResultTable[MetaAttributes.CreateTableVariableName] = nestedFrom.Alias.ToScoreTable();

        scopeTransformedQuery[MetaAttributes.SelectIntoVariableName] = groupingTableName;
        scopeTransformedQuery[MetaAttributes.SourceName] = context.SplitNodes.Count > 0
            ? nestedFrom.Alias.ToTransitionTable().ToTransformedRowsSource()
            : nestedFrom.Alias.ToRowsSource().WithRowsUsage();
        scopeTransformedQuery[MetaAttributes.OriginAlias] = nestedFrom.Alias;
        scopeTransformedQuery.ScopeSymbolTable.AddSymbol(nestedFrom.Alias,
            Scope.ScopeSymbolTable.GetSymbol(nestedFrom.Alias));
        scopeTransformedQuery[MetaAttributes.Contexts] = $"{nestedFrom.Alias}";

        if (context.SplitNodes.Count > 0)
        {
            var selectRewriter = new RewritePartsToUseJoinTransitionTable(nestedFrom.Alias);
            var selectTraverser = new CloneTraverseVisitor(selectRewriter);

            groupBy.Accept(selectTraverser);
            groupBy = selectRewriter.ChangedGroupBy ??
                      throw new InvalidOperationException("Expected GROUP BY to be rewritten for grouped query split.");
            context.Where?.Accept(selectTraverser);
            context.Where = selectRewriter.ChangedWhere;

            scopeTransformedQuery.ScopeSymbolTable.AddSymbol("groupFields",
                new FieldsNamesSymbol(groupBy.Fields.Select(f => f.FieldName).ToArray()));

            var newRefreshMethods = new List<AccessMethodNode>();
            foreach (var method in refreshMethods.Nodes)
            {
                var newNodes = new List<Node>();
                foreach (var arg in method.Arguments.Args)
                {
                    arg.Accept(selectTraverser);
                    newNodes.Add(selectRewriter.RewrittenNode);
                }

                Node? filterExpression = null;
                if (method.FilterExpression != null)
                {
                    method.FilterExpression.Accept(selectTraverser);
                    filterExpression = selectRewriter.RewrittenNode;
                }

                var newArgs = new ArgsListNode(newNodes.ToArray());
                newRefreshMethods.Add(new AccessMethodNode(method.FunctionToken, newArgs,
                    method.ExtraAggregateArguments, method.CanSkipInjectSource, method.Method, method.Alias,
                    default, method.IsDistinct)
                {
                    HasFilter = method.HasFilter,
                    FilterExpression = filterExpression,
                    FilterExpressionText = method.FilterExpressionText,
                    IsPivotGenerated = method.IsPivotGenerated
                });
            }

            refreshMethods = new RefreshNode(newRefreshMethods.ToArray());
        }
        else
        {
            scopeTransformedQuery.ScopeSymbolTable.AddSymbol("groupFields",
                new FieldsNamesSymbol(groupBy.Fields.Select(f => f.Expression.ToString()).ToArray()));
        }

        var transformingQuery = new InternalQueryNode(aggSelect, nestedFrom, context.Where, groupBy, null, null, null,
            refreshMethods);

        var returnScore = nestedFrom.Alias.ToScoreTable();
        scopeResultQuery[MetaAttributes.SelectIntoVariableName] = returnScore;
        scopeResultQuery[MetaAttributes.SourceName] = groupingTableName;
        scopeResultQuery[MetaAttributes.Contexts] = $"{nestedFrom.Alias}";

        context.AliasesPositionsSymbol.AliasesPositions.Add(nestedFrom.Alias, context.AliasIndex++);
        context.AliasesPositionsSymbol.AliasesPositions.Add(returnScore, context.AliasIndex);

        var modifiedOrderBy = context.OrderBy;

        if (context.OrderBy != null)
        {
            var splitOrderBy =
                FieldProcessingHelper.CreateAfterGroupByOrderByAccessFields(context.OrderBy.Fields, groupBy.Fields);
            modifiedOrderBy = new OrderByNode(splitOrderBy);
        }

        QueryNode query = new DetailedQueryNode(
            outSelect,
            new Parser.ExpressionFromNode(
                new InMemoryGroupedFromNode(returnScore)),
            null,
            null,
            modifiedOrderBy,
            context.Skip,
            context.Take,
            returnScore,
            context.Window,
            context.Qualify);

        context.SplitNodes.Add(new CreateTransformationTableNode(groupingTableName, [], transformingQuery.Select.Fields,
            true));
        context.SplitNodes.Add(transformingQuery);
        context.SplitNodes.Add(new CreateTransformationTableNode(query.From.Alias, [], query.Select.Fields, false));
        context.SplitNodes.Add(query);

        Nodes.Push(
            new MultiStatementNode(
                context.SplitNodes.ToArray(),
                null));
    }
}
