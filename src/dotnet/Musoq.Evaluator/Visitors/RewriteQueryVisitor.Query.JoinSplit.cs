using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using ApplyFromNode = Musoq.Parser.Nodes.From.ApplyFromNode;
using ApplyNode = Musoq.Parser.Nodes.From.ApplyNode;
using JoinFromNode = Musoq.Parser.Nodes.From.JoinFromNode;
using JoinNode = Musoq.Parser.Nodes.From.JoinNode;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    private void SplitJoinChainIfNeeded(QueryNode node, QueryRewriteContext context)
    {
        if (context.PreserveDirectApplyChain || context.From.Expression is not (JoinNode or ApplyNode) ||
            _joinedTables.Any(static node => node is JoinFromNode { JoinType: JoinType.LeftSemi or JoinType.LeftAntiSemi }))
        {
            return;
        }

        var indexBasedContextsPositionsSymbol = new IndexBasedContextsPositionsSymbol();
        var orderNumber = 0;
        var extractAccessedColumnsVisitor = new ExtractAccessColumnFromQueryVisitor();
        var extractAccessedColumnsTraverseVisitor =
            new ExtractAccessColumnFromQueryTraverseVisitor(extractAccessedColumnsVisitor);

        node.Accept(extractAccessedColumnsTraverseVisitor);

        foreach (var refreshMethod in context.UsedRefreshMethods ?? [])
            refreshMethod.Accept(extractAccessedColumnsTraverseVisitor);

        var current = _joinedTables[0];
        var accessColumns = extractAccessedColumnsVisitor.GetForAliases(current.Source.Alias, current.With.Alias);
        var left = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.Source.Alias);
        var right = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.With.Alias);

        var scopeCreateTable = Scope.AddScope("Table");
        var scopeJoinedQuery = Scope.AddScope("Query");

        var trimmedLeft = left.LimitColumnsTo(new Dictionary<string, string[]>
        {
            {
                left.CompoundTables[0],
                accessColumns.Where(f => f.Alias == left.CompoundTables[0]).Select(f => f.Name)
                    .ToArray()
            }
        });
        var trimmedRight = right.LimitColumnsTo(new Dictionary<string, string[]>
        {
            {
                right.CompoundTables[0],
                accessColumns.Where(f => f.Alias == right.CompoundTables[0]).Select(f => f.Name)
                    .ToArray()
            }
        });
        var bothForCreateTable = FieldProcessingHelper.CreateAndConcatFields(trimmedLeft, current.Source.Alias,
            trimmedRight, current.With.Alias,
            (name, alias) => NamingHelper.ToColumnName(alias, name),
            QueryRewriteUtilities.IncludeKnownColumns(accessColumns, current));
        var bothForSelect = FieldProcessingHelper.CreateAndConcatFields(
            trimmedLeft,
            current.Source.Alias,
            trimmedRight,
            current.With.Alias,
            (name, alias) => NamingHelper.ToColumnName(alias, name),
            (name, alias) => NamingHelper.ToColumnName(alias, name),
            (name, _) => name,
            (name, _) => name,
            QueryRewriteUtilities.IncludeKnownColumns(accessColumns, current));

        scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.Source.Alias, trimmedLeft);
        scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.With.Alias, trimmedRight);

        var targetTableName = $"{current.Source.Alias}{current.With.Alias}";

        context.AliasesPositionsSymbol.AliasesPositions.Add(current.Source.Alias, context.AliasIndex++);
        context.AliasesPositionsSymbol.AliasesPositions.Add(current.With.Alias, context.AliasIndex++);

        var targetSymbolTable = (TableSymbol)Scope.ScopeSymbolTable.GetSymbol(targetTableName);
        var limitedTargetSymbolTable = targetSymbolTable.LimitColumnsTo(new Dictionary<string, string[]>
        {
            {
                targetSymbolTable.CompoundTables[0],
                accessColumns.Where(f => f.Alias == targetSymbolTable.CompoundTables[0]).Select(f => f.Name)
                    .ToArray()
            },
            {
                targetSymbolTable.CompoundTables[1],
                accessColumns.Where(f => f.Alias == targetSymbolTable.CompoundTables[1]).Select(f => f.Name)
                    .ToArray()
            }
        });

        scopeJoinedQuery.ScopeSymbolTable.AddSymbol(targetTableName, limitedTargetSymbolTable);
        scopeJoinedQuery.ScopeSymbolTable.AddSymbol(MetaAttributes.PreformatedContexts,
            indexBasedContextsPositionsSymbol);

        scopeJoinedQuery[MetaAttributes.SelectIntoVariableName] = targetTableName.ToTransitionTable();
        scopeJoinedQuery[MetaAttributes.OriginAlias] = targetTableName;
        scopeJoinedQuery[MetaAttributes.Contexts] = $"{current.Source.Alias},{current.With.Alias}";
        scopeJoinedQuery[MetaAttributes.OrderNumber] = orderNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
        scopeCreateTable[MetaAttributes.CreateTableVariableName] = targetTableName.ToTransitionTable();
        scopeCreateTable[MetaAttributes.PreformatedContexts] = $"{current.Source.Alias},{current.With.Alias}";

        orderNumber += 1;

        var previousAliases = new Stack<string>();

        previousAliases.Push($"{current.Source.Alias},{current.With.Alias}");
        previousAliases.Push(string.Join("|", current.Source.Alias, current.With.Alias));

        var joinedQuery = new InternalQueryNode(
            new SelectNode(bothForSelect),
            new Parser.ExpressionFromNode(
                current switch
                {
                    JoinFromNode currentJoin => new Parser.JoinSourcesTableFromNode(
                        currentJoin.Source,
                        currentJoin.With,
                        currentJoin.Expression,
                        currentJoin.JoinType,
                        currentJoin.TieBreak),
                    ApplyFromNode currentApply => new Parser.ApplySourcesTableFromNode(
                        currentApply.Source,
                        currentApply.With,
                        currentApply.ApplyType,
                        currentApply.WithOrdinality),
                    _ => throw new InvalidOperationException($"Unsupported joined table node type '{current.GetType().Name}'.")
                }),
            null,
            null,
            null,
            null,
            null,
            new RefreshNode([]));

        var targetTable = new CreateTransformationTableNode(targetTableName, [], bothForCreateTable, false);

        context.SplitNodes.Add(targetTable);
        context.SplitNodes.Add(joinedQuery);

        context.LastJoinQuery = joinedQuery;
        context.Source = targetTableName.ToTransitionTable().ToTransformedRowsSource();

        var usedTables = new Dictionary<string, string>
        {
            { current.Source.Alias, targetTableName },
            { current.With.Alias, targetTableName }
        };

        for (var i = 1; i < _joinedTables.Count; i++, orderNumber++)
        {
            current = _joinedTables[i];
            previousAliases.Push(current.With.Alias);
            left = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.Source.Alias);
            right = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.With.Alias);

            var secondAlias = previousAliases.Pop();
            var firstAlias = previousAliases.Pop();
            previousAliases.Push($"{firstAlias},{secondAlias}");
            previousAliases.Push(string.Join("|", firstAlias, secondAlias));

            targetTableName = $"{current.Source.Alias}{current.With.Alias}";

            context.AliasesPositionsSymbol.AliasesPositions.Add(current.Source.Alias, context.AliasIndex++);
            context.AliasesPositionsSymbol.AliasesPositions.Add(current.With.Alias, context.AliasIndex++);

            scopeCreateTable = Scope.AddScope("Table");
            scopeJoinedQuery = Scope.AddScope("Query");

            accessColumns = extractAccessedColumnsVisitor.GetForAliases(left.CompoundTables);
            IEnumerable<KeyValuePair<string, string[]>> limitColumnsKeyValuePair = [];

            foreach (var compoundTable in left.CompoundTables)
            {
                var columns = accessColumns.Where(f => f.Alias == compoundTable).Select(f => f.Name).ToArray();
                limitColumnsKeyValuePair = limitColumnsKeyValuePair.Concat(new Dictionary<string, string[]>
                {
                    { compoundTable, columns }
                });
            }

            trimmedLeft = left.LimitColumnsTo(new Dictionary<string, string[]>(limitColumnsKeyValuePair));
            trimmedRight = right.LimitColumnsTo(new Dictionary<string, string[]>
            {
                {
                    right.CompoundTables[0],
                    extractAccessedColumnsVisitor.GetForAlias(right.CompoundTables[0])
                        .Where(f => f.Alias == right.CompoundTables[0]).Select(f => f.Name)
                        .ToArray()
                }
            });
            bothForCreateTable =
                FieldProcessingHelper.CreateAndConcatFields(trimmedLeft, current.Source.Alias, trimmedRight,
                    current.With.Alias,
                    (name, alias) => NamingHelper.ToColumnName(alias, name),
                    QueryRewriteUtilities.IncludeKnownColumnsForWithOnly(
                        extractAccessedColumnsVisitor.GetForAlias(current.With.Alias), current));

            var hasSubsequentJoin = i < _joinedTables.Count - 1;
            Func<string, string, string> createRightSelectFieldName = current is ApplyFromNode || hasSubsequentJoin
                ? (name, alias) => NamingHelper.ToColumnName(alias, name)
                : (name, _) => name;

            bothForSelect =
                FieldProcessingHelper.CreateAndConcatFields(
                    trimmedLeft,
                    current.Source.Alias,
                    trimmedRight,
                    current.With.Alias,
                    (name, alias) => NamingHelper.ToColumnName(alias, name),
                    createRightSelectFieldName,
                    (name, alias) => NamingHelper.ToColumnName(alias, name),
                    (name, _) => name,
                    QueryRewriteUtilities.IncludeKnownColumnsForWithOnly(
                        extractAccessedColumnsVisitor.GetForAlias(current.With.Alias), current));

            scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.Source.Alias, trimmedLeft);
            scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.With.Alias, trimmedRight);

            targetSymbolTable = (TableSymbol)Scope.ScopeSymbolTable.GetSymbol(targetTableName);

            IEnumerable<KeyValuePair<string, string[]>> pairs = [];
            for (var index = 0; index < targetSymbolTable.CompoundTables.Length - 1; index++)
            {
                var compoundTable = targetSymbolTable.CompoundTables[index];
                var columns = trimmedLeft.GetColumns(compoundTable);
                pairs = pairs.Concat([
                    new KeyValuePair<string, string[]>(compoundTable, columns.Select(f => f.ColumnName).ToArray())
                ]);
            }

            pairs = pairs
                .Concat(trimmedRight.CompoundTables.Select(compoundTable => new KeyValuePair<string, string[]>(
                            compoundTable,
                            trimmedRight.GetColumns(compoundTable).Select(f => f.ColumnName).ToArray()
                        )
                    )
                );

            limitedTargetSymbolTable = targetSymbolTable.LimitColumnsTo(new Dictionary<string, string[]>(pairs));

            scopeJoinedQuery.ScopeSymbolTable.AddSymbol(targetTableName, limitedTargetSymbolTable);
            scopeJoinedQuery.ScopeSymbolTable.AddSymbol(MetaAttributes.PreformatedContexts,
                indexBasedContextsPositionsSymbol);

            scopeJoinedQuery[MetaAttributes.SelectIntoVariableName] = targetTableName.ToTransitionTable();
            scopeJoinedQuery[MetaAttributes.OriginAlias] = targetTableName;
            scopeJoinedQuery[MetaAttributes.Contexts] = $"{current.Source.Alias},{current.With.Alias}";
            scopeCreateTable[MetaAttributes.CreateTableVariableName] = targetTableName.ToTransitionTable();
            scopeJoinedQuery[MetaAttributes.OrderNumber] = orderNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

            scopeJoinedQuery.ScopeSymbolTable.AddSymbol(
                MetaAttributes.OuterJoinSelect,
                new FieldsNamesSymbol(bothForSelect.Select(f => f.FieldName).ToArray()));

            var expressionUpdater = new RewriteToUpdatedColumnAccess(usedTables);
            var expressionUpdaterTraverser = new RewriteToUpdatedColumnAccessTraverser(expressionUpdater);

            if (current is JoinFromNode joinFromNode)
            {
                var whereNode = new WhereNode(joinFromNode.Expression);

                whereNode.Accept(expressionUpdaterTraverser);
                var tieBreak = RewriteTieBreak(joinFromNode.TieBreak, usedTables);

                joinedQuery = new InternalQueryNode(
                    new SelectNode(bothForSelect),
                    new Parser.ExpressionFromNode(
                        new Parser.JoinInMemoryWithSourceTableFromNode(
                            joinFromNode.Source.Alias,
                            joinFromNode.With,
                            expressionUpdater.Where.Expression,
                            joinFromNode.JoinType,
                            (joinFromNode.Source as InMemoryTableFromNode)?.VariableName,
                            tieBreak)),
                    null,
                    null,
                    null,
                    null,
                    null,
                    new RefreshNode([]));
            }
            else
            {
                var applyFromNode = (ApplyFromNode)current;

                applyFromNode.With.Accept(expressionUpdaterTraverser);

                joinedQuery = new InternalQueryNode(
                    new SelectNode(bothForSelect),
                    new Parser.ExpressionFromNode(
                        new Parser.ApplyInMemoryWithSourceTableFromNode(
                            current.Source.Alias,
                            expressionUpdater.From,
                            applyFromNode.ApplyType,
                            applyFromNode.WithOrdinality)),
                    null,
                    null,
                    null,
                    null,
                    null,
                    new RefreshNode([]));
            }

            foreach (var key in usedTables.Keys.ToArray())
                usedTables[key] = targetTableName;

            usedTables[current.Source.Alias] = targetTableName;
            usedTables[current.With.Alias] = targetTableName;

            targetTable = new CreateTransformationTableNode(targetTableName, [], bothForCreateTable, false);

            context.SplitNodes.Add(targetTable);
            context.SplitNodes.Add(joinedQuery);

            context.LastJoinQuery = joinedQuery;
            context.Source = targetTableName.ToTransitionTable().ToTransformedRowsSource();
        }

        var rewriter = new RewritePartsToUseJoinTransitionTable();
        var partsTraverser = new CloneTraverseVisitor(rewriter);

        context.Select.Accept(partsTraverser);
        context.Where?.Accept(partsTraverser);
        context.OrderBy?.Accept(partsTraverser);
        context.Window?.Accept(partsTraverser);

        context.ScoreSelect = rewriter.ChangedSelect ??
                              throw new InvalidOperationException("Expected SELECT to be rewritten for join query split.");
        context.ScoreWhere = rewriter.ChangedWhere;
        context.ScoreOrderBy = rewriter.ChangedOrderBy;

        if (rewriter.ChangedWindow != null)
            context.Window = rewriter.ChangedWindow;

        previousAliases.Pop();
        indexBasedContextsPositionsSymbol.Add(previousAliases.ToArray());
    }

    private static FieldOrderedNode? RewriteTieBreak(
        FieldOrderedNode? tieBreak,
        IReadOnlyDictionary<string, string> usedTables)
    {
        if (tieBreak == null)
            return null;

        var expressionUpdater = new RewriteToUpdatedColumnAccess(usedTables);
        var expressionUpdaterTraverser = new RewriteToUpdatedColumnAccessTraverser(expressionUpdater);
        new WhereNode(tieBreak.Expression).Accept(expressionUpdaterTraverser);

        return new FieldOrderedNode(
            expressionUpdater.Where.Expression,
            tieBreak.FieldOrder,
            tieBreak.FieldName,
            tieBreak.HasExplicitFieldName,
            tieBreak.Order,
            tieBreak.NullOrdering);
    }
}
