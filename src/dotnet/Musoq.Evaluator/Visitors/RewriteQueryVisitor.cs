#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;
using AccessMethodFromNode = Musoq.Parser.Nodes.From.AccessMethodFromNode;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
using ApplyFromNode = Musoq.Parser.Nodes.From.ApplyFromNode;
using ApplyNode = Musoq.Parser.Nodes.From.ApplyNode;
using ApplySourcesTableFromNode = Musoq.Parser.Nodes.From.ApplySourcesTableFromNode;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using InMemoryGroupedFromNode = Musoq.Evaluator.Parser.InMemoryGroupedFromNode;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using InterpretFromNode = Musoq.Parser.Nodes.From.InterpretFromNode;
using JoinFromNode = Musoq.Parser.Nodes.From.JoinFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinNode = Musoq.Parser.Nodes.From.JoinNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;
using PropertyFromNode = Musoq.Parser.Nodes.From.PropertyFromNode;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;
using SchemaMethodFromNode = Musoq.Parser.Nodes.From.SchemaMethodFromNode;

namespace Musoq.Evaluator.Visitors;

public sealed class RewriteQueryVisitor : IScopeAwareExpressionVisitor
{
    private readonly List<BinaryFromNode> _joinedTables = [];
    private int _queryIndex;
    private Scope _scope;

    private Stack<Node> Nodes { get; } = new();

    public RootNode RootScript { get; private set; }

    public void Visit(Node node)
    {
    }

    public void Visit(DescNode node)
    {
        var from = (SchemaFromNode)Nodes.Pop();
        Nodes.Push(new DescNode(from, node.Type, node.Column));
    }

    public void Visit(StarNode node)
    {
        BinaryOperationVisitorHelper.ProcessStarOperation(Nodes);
    }

    public void Visit(FSlashNode node)
    {
        BinaryOperationVisitorHelper.ProcessFSlashOperation(Nodes);
    }

    public void Visit(ModuloNode node)
    {
        BinaryOperationVisitorHelper.ProcessModuloOperation(Nodes);
    }

    public void Visit(AddNode node)
    {
        BinaryOperationVisitorHelper.ProcessAddOperation(Nodes);
    }

    public void Visit(HyphenNode node)
    {
        BinaryOperationVisitorHelper.ProcessHyphenOperation(Nodes);
    }

    public void Visit(BitwiseAndNode node)
    {
        BinaryOperationVisitorHelper.ProcessBitwiseAndOperation(Nodes);
    }

    public void Visit(BitwiseOrNode node)
    {
        BinaryOperationVisitorHelper.ProcessBitwiseOrOperation(Nodes);
    }

    public void Visit(BitwiseXorNode node)
    {
        BinaryOperationVisitorHelper.ProcessBitwiseXorOperation(Nodes);
    }

    public void Visit(LeftShiftNode node)
    {
        BinaryOperationVisitorHelper.ProcessLeftShiftOperation(Nodes);
    }

    public void Visit(RightShiftNode node)
    {
        BinaryOperationVisitorHelper.ProcessRightShiftOperation(Nodes);
    }

    public void Visit(ArrayIndexNode node)
    {
        var index = Nodes.Pop();
        var array = Nodes.Pop();
        Nodes.Push(new ArrayIndexNode(array, index));
    }

    public void Visit(AndNode node)
    {
        LogicalOperationVisitorHelper.ProcessAndOperation(Nodes, QueryRewriteUtilities.RewriteNullableBoolExpressions);
    }

    public void Visit(OrNode node)
    {
        LogicalOperationVisitorHelper.ProcessOrOperation(Nodes, QueryRewriteUtilities.RewriteNullableBoolExpressions);
    }

    public void Visit(EqualityNode node)
    {
        ComparisonOperationVisitorHelper.ProcessEqualityOperation(Nodes);
    }

    public void Visit(ShortCircuitingNodeLeft node)
    {
        Nodes.Push(new ShortCircuitingNodeLeft(Nodes.Pop(), node.UsedFor));
    }

    public void Visit(ShortCircuitingNodeRight node)
    {
        Nodes.Push(new ShortCircuitingNodeRight(Nodes.Pop(), node.UsedFor));
    }

    public void Visit(GreaterOrEqualNode node)
    {
        ComparisonOperationVisitorHelper.ProcessGreaterOrEqualOperation(Nodes);
    }

    public void Visit(LessOrEqualNode node)
    {
        ComparisonOperationVisitorHelper.ProcessLessOrEqualOperation(Nodes);
    }

    public void Visit(GreaterNode node)
    {
        ComparisonOperationVisitorHelper.ProcessGreaterOperation(Nodes);
    }

    public void Visit(LessNode node)
    {
        ComparisonOperationVisitorHelper.ProcessLessOperation(Nodes);
    }

    public void Visit(DiffNode node)
    {
        ComparisonOperationVisitorHelper.ProcessDiffOperation(Nodes);
    }

    public void Visit(NotNode node)
    {
        LogicalOperationVisitorHelper.ProcessNotOperation(Nodes);
    }

    public void Visit(LikeNode node)
    {
        ComparisonOperationVisitorHelper.ProcessLikeOperation(Nodes);
    }

    public void Visit(RLikeNode node)
    {
        ComparisonOperationVisitorHelper.ProcessRLikeOperation(Nodes);
    }

    public void Visit(InNode node)
    {
        LogicalOperationVisitorHelper.ProcessInOperation(Nodes);
    }

    /// <summary>
    ///     Desugars BETWEEN into: expression >= min AND expression <= max
    /// </summary>
    public void Visit(BetweenNode node)
    {
        var max = Nodes.Pop();
        var min = Nodes.Pop();
        var expression = Nodes.Pop();

        // Create: expression >= min AND expression <= max
        var greaterOrEqual = new GreaterOrEqualNode(expression, min);
        var lessOrEqual = new LessOrEqualNode(expression, max);
        var andNode = new AndNode(greaterOrEqual, lessOrEqual);

        Nodes.Push(andNode);
    }

    public void Visit(FieldNode node)
    {
        Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder,
            QueryRewriteUtilities.RewriteFieldNameWithoutStringPrefixAndSuffix(node.FieldName)));
    }

    public void Visit(FieldOrderedNode node)
    {
        Nodes.Push(new FieldOrderedNode(Nodes.Pop(), node.FieldOrder,
            QueryRewriteUtilities.RewriteFieldNameWithoutStringPrefixAndSuffix(node.FieldName), node.Order));
    }

    public void Visit(SelectNode node)
    {
        var fields = FieldProcessingHelper.CreateFields(node.Fields, Nodes);

        Nodes.Push(new SelectNode(fields.ToArray(), node.IsDistinct));
    }

    public void Visit(GroupSelectNode node)
    {
    }

    public void Visit(StringNode node)
    {
        Nodes.Push(new StringNode(node.Value));
    }

    public void Visit(DecimalNode node)
    {
        Nodes.Push(new DecimalNode(node.Value));
    }

    public void Visit(IntegerNode node)
    {
        Nodes.Push(new IntegerNode(node.ObjValue));
    }

    public void Visit(HexIntegerNode node)
    {
        Nodes.Push(new HexIntegerNode(node.ObjValue));
    }

    public void Visit(BinaryIntegerNode node)
    {
        Nodes.Push(new BinaryIntegerNode(node.ObjValue));
    }

    public void Visit(OctalIntegerNode node)
    {
        Nodes.Push(new OctalIntegerNode(node.ObjValue));
    }

    public void Visit(BooleanNode node)
    {
        Nodes.Push(new BooleanNode(node.Value));
    }

    public void Visit(WordNode node)
    {
        Nodes.Push(new WordNode(node.Value));
    }

    public void Visit(NullNode node)
    {
        Nodes.Push(new NullNode(node.ReturnType));
    }

    public void Visit(ContainsNode node)
    {
        LogicalOperationVisitorHelper.ProcessContainsOperation(Nodes);
    }

    public void Visit(AccessMethodNode node)
    {
        VisitAccessMethod(node);
    }

    public void Visit(AccessRawIdentifierNode node)
    {
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public void Visit(IsNullNode node)
    {
        LogicalOperationVisitorHelper.ProcessIsNullOperation(Nodes, node.IsNegated);
    }

    public void Visit(AccessRefreshAggregationScoreNode node)
    {
        VisitAccessMethod(node);
    }

    public void Visit(AccessColumnNode node)
    {
        Nodes.Push(new AccessColumnNode(node.Name, node.Alias, node.ReturnType, node.Span, node.IntendedTypeName));
    }

    public void Visit(AllColumnsNode node)
    {
        Nodes.Push(new AllColumnsNode(node.Alias));
    }

    public void Visit(IdentifierNode node)
    {
        Nodes.Push(new IdentifierNode(node.Name));
    }

    public void Visit(AccessObjectArrayNode node)
    {
        if (node.IsColumnAccess)
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.ColumnType, node.TableAlias, node.IntendedTypeName));
        else
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.PropertyInfo));
    }

    public void Visit(AccessObjectKeyNode node)
    {
        Nodes.Push(new AccessObjectKeyNode(node.Token, node.PropertyInfo));
    }

    public void Visit(PropertyValueNode node)
    {
        Nodes.Push(new PropertyValueNode(node.Name, node.PropertyInfo));
    }

    public void Visit(DotNode node)
    {
        var exp = Nodes.Pop();
        var root = Nodes.Pop();

        Nodes.Push(new DotNode(root, exp, node.IsTheMostInner, node.Name, exp.ReturnType, node.IntendedTypeName));
    }

    public void Visit(AccessCallChainNode node)
    {
    }

    public void Visit(ArgsListNode node)
    {
        var args = new Node[node.Args.Length];

        for (var i = node.Args.Length - 1; i >= 0; --i)
            args[i] = Nodes.Pop();

        Nodes.Push(new ArgsListNode(args));
    }

    public void Visit(WhereNode node)
    {
        var rewrittenNode = QueryRewriteUtilities.RewriteNullableBoolExpressions(Nodes.Pop());

        Nodes.Push(new WhereNode(rewrittenNode));
    }

    public void Visit(GroupByNode node)
    {
        var having = Nodes.Peek() as HavingNode;

        if (having != null)
            Nodes.Pop();

        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = Nodes.Pop() as FieldNode;

        Nodes.Push(new GroupByNode(fields, having));
    }

    public void Visit(HavingNode node)
    {
        Nodes.Push(new HavingNode(Nodes.Pop()));
    }

    public void Visit(SkipNode node)
    {
        Nodes.Push(new SkipNode((IntegerNode)node.Expression));
    }

    public void Visit(TakeNode node)
    {
        Nodes.Push(new TakeNode((IntegerNode)node.Expression));
    }

    public void Visit(SchemaFromNode node)
    {
        Nodes.Push(
            node is Parser.SchemaFromNode schemaFromNode
                ? new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias,
                    node.QueryId, schemaFromNode.HasExternallyProvidedTypes)
                : new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias,
                    node.QueryId, false));
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        var second = (FromNode)Nodes.Pop();


        var first = (FromNode)Nodes.Pop();


        Nodes.Push(new Parser.ApplySourcesTableFromNode(first, second, node.ApplyType));
    }

    public void Visit(JoinFromNode node)
    {
        var exp = Nodes.Pop();
        var right = (FromNode)Nodes.Pop();
        var left = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.JoinFromNode(left, right, exp, node.JoinType));
        _joinedTables.Add(node);
    }

    public void Visit(ApplyFromNode node)
    {
        var right = (FromNode)Nodes.Pop();
        var left = (FromNode)Nodes.Pop();
        var newApply = new Parser.ApplyFromNode(left, right, node.ApplyType);
        Nodes.Push(newApply);

        _joinedTables.Add(newApply);
    }

    public void Visit(ExpressionFromNode node)
    {
        Nodes.Push(new Parser.ExpressionFromNode((FromNode)Nodes.Pop()));
    }

    public void Visit(InMemoryTableFromNode node)
    {
        Nodes.Push(new Parser.InMemoryTableFromNode(node.VariableName, node.Alias));
    }

    public void Visit(AccessMethodFromNode node)
    {
        Nodes.Push(new AccessMethodFromNode(node.Alias, node.SourceAlias, node.AccessMethod, node.ReturnType));
    }

    public void Visit(InterpretFromNode node)
    {
        var interpretCall = Nodes.Pop();
        Nodes.Push(new InterpretFromNode(node.Alias, interpretCall, node.ApplyType, node.ReturnType));
    }

    public void Visit(SchemaMethodFromNode node)
    {
    }

    public void Visit(PropertyFromNode node)
    {
        Nodes.Push(new Parser.PropertyFromNode(node.Alias, node.SourceAlias, node.PropertiesChain));
    }

    public void Visit(AliasedFromNode node)
    {
        var identifier = node.Identifier;
        if (identifier == "Interpret" || identifier == "Parse" || identifier == "InterpretAt") Nodes.Push(node);
    }

    public void Visit(CreateTransformationTableNode node)
    {
        var fields = FieldProcessingHelper.CreateFields(node.Fields, Nodes);

        Nodes.Push(new CreateTransformationTableNode(node.Name, node.Keys, fields, node.ForGrouping));
    }

    public void Visit(RenameTableNode node)
    {
        Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
    }

    public void Visit(TranslatedSetTreeNode node)
    {
    }

    public void Visit(IntoNode node)
    {
        Nodes.Push(new IntoNode(node.Name));
    }

    public void Visit(QueryScope node)
    {
    }

    public void Visit(ShouldBePresentInTheTable node)
    {
        Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
    }

    public void Visit(TranslatedSetOperatorNode node)
    {
    }

    public void Visit(QueryNode node)
    {
        var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

        var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
        var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

        var select = (SelectNode)Nodes.Pop();
        var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
        var from = (ExpressionFromNode)Nodes.Pop();

        var scoreSelect = select;
        var scoreWhere = where;
        var scoreOrderBy = orderBy;

        var splitNodes = new List<Node>();
        var source = from.Alias.ToRowsSource().WithRowsUsage();

        QueryNode lastJoinQuery = null;

        _scope[MetaAttributes.MethodName] = $"ComputeTable_{from.Alias}_{_queryIndex++}";

        IReadOnlyList<AccessMethodNode> usedRefreshMethods = null;

        if (_scope.ScopeSymbolTable.SymbolIsOfType<RefreshMethodsSymbol>(from.Alias.ToRefreshMethodsSymbolName()))
            usedRefreshMethods = _scope.ScopeSymbolTable
                .GetSymbol<RefreshMethodsSymbol>(from.Alias.ToRefreshMethodsSymbolName()).RefreshMethods;

        var aliasIndex = 0;
        var aliasesPositionsSymbol = new AliasesPositionsSymbol();

        if (from.Expression is JoinNode or ApplyNode)
        {
            var indexBasedContextsPositionsSymbol = new IndexBasedContextsPositionsSymbol();
            var orderNumber = 0;
            var extractAccessedColumnsVisitor = new ExtractAccessColumnFromQueryVisitor();
            var extractAccessedColumnsTraverseVisitor =
                new ExtractAccessColumnFromQueryTraverseVisitor(extractAccessedColumnsVisitor);

            node.Accept(extractAccessedColumnsTraverseVisitor);

            foreach (var refreshMethod in usedRefreshMethods ?? [])
                refreshMethod.Accept(extractAccessedColumnsTraverseVisitor);

            var current = _joinedTables[0];
            var accessColumns = extractAccessedColumnsVisitor.GetForAliases(current.Source.Alias, current.With.Alias);
            var left = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.Source.Alias);
            var right = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.With.Alias);

            var scopeCreateTable = _scope.AddScope("Table");
            var scopeJoinedQuery = _scope.AddScope("Query");

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
            var bothForSelect = FieldProcessingHelper.CreateAndConcatFields(trimmedLeft, current.Source.Alias,
                trimmedRight, current.With.Alias,
                (name, alias) => name, QueryRewriteUtilities.IncludeKnownColumns(accessColumns, current));

            scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.Source.Alias, trimmedLeft);
            scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.With.Alias, trimmedRight);

            var targetTableName = $"{current.Source.Alias}{current.With.Alias}";

            aliasesPositionsSymbol.AliasesPositions.Add(current.Source.Alias, aliasIndex++);
            aliasesPositionsSymbol.AliasesPositions.Add(current.With.Alias, aliasIndex++);

            var targetSymbolTable = (TableSymbol)_scope.ScopeSymbolTable.GetSymbol(targetTableName);
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
            scopeJoinedQuery[MetaAttributes.OrderNumber] = orderNumber.ToString();
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
                            currentJoin.JoinType),
                        ApplyFromNode currentApply => new Parser.ApplySourcesTableFromNode(
                            currentApply.Source,
                            currentApply.With,
                            currentApply.ApplyType),
                        _ => throw new ArgumentOutOfRangeException(nameof(current))
                    }),
                null,
                null,
                null,
                null,
                null,
                new RefreshNode([]));

            var targetTable = new CreateTransformationTableNode(targetTableName, [], bothForCreateTable, false);

            splitNodes.Add(targetTable);
            splitNodes.Add(joinedQuery);

            lastJoinQuery = joinedQuery;
            source = targetTableName.ToTransitionTable().ToTransformedRowsSource(false);

            var usedTables = new Dictionary<string, string>
            {
                { current.Source.Alias, targetTableName },
                { current.With.Alias, targetTableName }
            };

            for (var i = 1; i < _joinedTables.Count; i++, orderNumber++)
            {
                current = _joinedTables[i];
                previousAliases.Push(current.With.Alias);
                left = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.Source.Alias);
                right = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.With.Alias);

                var secondAlias = previousAliases.Pop();
                var firstAlias = previousAliases.Pop();
                previousAliases.Push($"{firstAlias},{secondAlias}");
                previousAliases.Push(string.Join("|", firstAlias, secondAlias));

                targetTableName = $"{current.Source.Alias}{current.With.Alias}";

                aliasesPositionsSymbol.AliasesPositions.Add(current.Source.Alias, aliasIndex++);
                aliasesPositionsSymbol.AliasesPositions.Add(current.With.Alias, aliasIndex++);

                scopeCreateTable = _scope.AddScope("Table");
                scopeJoinedQuery = _scope.AddScope("Query");

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

                bothForSelect =
                    FieldProcessingHelper.CreateAndConcatFields(
                        trimmedLeft,
                        current.Source.Alias,
                        trimmedRight
                        ,
                        current.With.Alias,
                        (name, alias) => NamingHelper.ToColumnName(alias, name),
                        (name, alias) => name,
                        (name, alias) => NamingHelper.ToColumnName(alias, name),
                        (name, alias) => name,
                        QueryRewriteUtilities.IncludeKnownColumnsForWithOnly(
                            extractAccessedColumnsVisitor.GetForAlias(current.With.Alias), current));

                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.Source.Alias, trimmedLeft);
                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.With.Alias, trimmedRight);

                targetSymbolTable = (TableSymbol)_scope.ScopeSymbolTable.GetSymbol(targetTableName);

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
                scopeJoinedQuery[MetaAttributes.OrderNumber] = orderNumber.ToString();

                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(
                    MetaAttributes.OuterJoinSelect,
                    new FieldsNamesSymbol(bothForSelect.Select(f => f.FieldName).ToArray()));

                var expressionUpdater = new RewriteToUpdatedColumnAccess(usedTables);
                var expressionUpdaterTraverser = new RewriteToUpdatedColumnAccessTraverser(expressionUpdater);

                if (current is JoinFromNode joinFromNode)
                {
                    var whereNode = new WhereNode(joinFromNode.Expression);

                    whereNode.Accept(expressionUpdaterTraverser);

                    joinedQuery = new InternalQueryNode(
                        new SelectNode(bothForSelect),
                        new Parser.ExpressionFromNode(
                            new Parser.JoinInMemoryWithSourceTableFromNode(
                                joinFromNode.Source.Alias,
                                joinFromNode.With,
                                expressionUpdater.Where.Expression,
                                joinFromNode.JoinType)),
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
                                applyFromNode.ApplyType)),
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
                usedTables.Add(current.With.Alias, targetTableName);

                targetTable = new CreateTransformationTableNode(targetTableName, [], bothForCreateTable, false);

                splitNodes.Add(targetTable);
                splitNodes.Add(joinedQuery);

                lastJoinQuery = joinedQuery;
                source = targetTableName.ToTransitionTable().ToTransformedRowsSource(false);
            }

            var rewriter = new RewritePartsToUseJoinTransitionTable();
            var partsTraverser = new CloneTraverseVisitor(rewriter);

            select.Accept(partsTraverser);
            where?.Accept(partsTraverser);
            orderBy?.Accept(partsTraverser);

            scoreSelect = rewriter.ChangedSelect;
            scoreWhere = rewriter.ChangedWhere;
            scoreOrderBy = rewriter.ChangedOrderBy;

            previousAliases.Pop();
            indexBasedContextsPositionsSymbol.Add(previousAliases.ToArray());
        }

        if (groupBy != null)
        {
            var nestedFrom = splitNodes.Count > 0
                ? new Parser.ExpressionFromNode(new InMemoryGroupedFromNode(lastJoinQuery.From.Alias))
                : from;

            var refreshMethods = QueryRewriteUtilities.CreateRefreshMethods(usedRefreshMethods);
            var splitSelectFields =
                FieldProcessingHelper.SplitBetweenAggregateAndNonAggregate(select.Fields, groupBy.Fields, true);
            var aggSelect = new SelectNode(QueryRewriteUtilities
                .ConcatAggregateFieldsWithGroupByFields(splitSelectFields[0], groupBy.Fields)
                .Reverse().ToArray());
            var outSelect = new SelectNode(splitSelectFields[1], select.IsDistinct);

            var scopeCreateTransformingTable = _scope.AddScope("Table");
            var scopeTransformedQuery = _scope.AddScope("Query");
            var scopeCreateResultTable = _scope.AddScope("Table");
            var scopeResultQuery = _scope.AddScope("Query");

            scopeCreateTransformingTable[MetaAttributes.CreateTableVariableName] = nestedFrom.Alias.ToGroupingTable();
            scopeCreateResultTable[MetaAttributes.CreateTableVariableName] = nestedFrom.Alias.ToScoreTable();

            var destination = nestedFrom.Alias.ToGroupingTable().ToTransformedRowsSource(true);
            scopeTransformedQuery[MetaAttributes.SelectIntoVariableName] = destination;
            scopeTransformedQuery[MetaAttributes.SourceName] = splitNodes.Count > 0
                ? nestedFrom.Alias.ToTransitionTable().ToTransformedRowsSource(false)
                : nestedFrom.Alias.ToRowsSource().WithRowsUsage();
            scopeTransformedQuery[MetaAttributes.OriginAlias] = nestedFrom.Alias;
            scopeTransformedQuery.ScopeSymbolTable.AddSymbol(nestedFrom.Alias,
                _scope.ScopeSymbolTable.GetSymbol(nestedFrom.Alias));
            scopeTransformedQuery[MetaAttributes.Contexts] = $"{nestedFrom.Alias}";

            if (splitNodes.Count > 0)
            {
                var selectRewriter = new RewritePartsToUseJoinTransitionTable(nestedFrom.Alias);
                var selectTraverser = new CloneTraverseVisitor(selectRewriter);

                groupBy.Accept(selectTraverser);
                groupBy = selectRewriter.ChangedGroupBy;
                where?.Accept(selectTraverser);
                where = selectRewriter.ChangedWhere;

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

                    var newArgs = new ArgsListNode(newNodes.ToArray());
                    newRefreshMethods.Add(new AccessMethodNode(method.FunctionToken, newArgs,
                        method.ExtraAggregateArguments, method.CanSkipInjectSource, method.Method, string.Empty, default, method.IsDistinct));
                }

                refreshMethods = new RefreshNode(newRefreshMethods.ToArray());
            }
            else
            {
                scopeTransformedQuery.ScopeSymbolTable.AddSymbol("groupFields",
                    new FieldsNamesSymbol(groupBy.Fields.Select(f => f.Expression.ToString()).ToArray()));
            }

            var transformingQuery = new InternalQueryNode(aggSelect, nestedFrom, where, groupBy, null, null, null,
                refreshMethods);

            var returnScore = nestedFrom.Alias.ToScoreTable();
            scopeResultQuery[MetaAttributes.SelectIntoVariableName] = returnScore;
            scopeResultQuery[MetaAttributes.SourceName] = destination;
            scopeResultQuery[MetaAttributes.Contexts] = $"{nestedFrom.Alias}";

            aliasesPositionsSymbol.AliasesPositions.Add(nestedFrom.Alias, aliasIndex++);
            aliasesPositionsSymbol.AliasesPositions.Add(returnScore, aliasIndex);

            var modifiedOrderBy = orderBy;

            if (orderBy != null)
            {
                var splitOrderBy =
                    FieldProcessingHelper.CreateAfterGroupByOrderByAccessFields(orderBy.Fields, groupBy.Fields);
                modifiedOrderBy = new OrderByNode(splitOrderBy);
            }

            QueryNode query = new DetailedQueryNode(
                outSelect,
                new Parser.ExpressionFromNode(
                    new InMemoryGroupedFromNode(returnScore)),
                null,
                null,
                modifiedOrderBy,
                skip,
                take,
                returnScore);

            splitNodes.Add(new CreateTransformationTableNode(destination, [], transformingQuery.Select.Fields, true));
            splitNodes.Add(transformingQuery);
            splitNodes.Add(new CreateTransformationTableNode(query.From.Alias, [], query.Select.Fields, false));
            splitNodes.Add(query);

            Nodes.Push(
                new MultiStatementNode(
                    splitNodes.ToArray(),
                    null));
        }
        else
        {
            var split = FieldProcessingHelper.SplitBetweenAggregateAndNonAggregate(select.Fields, [], true);

            if (QueryRewriteUtilities.IsQueryWithMixedAggregateAndNonAggregateMethods(split))
                throw new NotImplementedException("Mixing aggregate and non aggregate methods is not implemented yet");

            var scopeCreateResultTable = _scope.AddScope("Table");
            var scopeResultQuery = _scope.AddScope("Query");

            scopeCreateResultTable[MetaAttributes.CreateTableVariableName] = from.Alias.ToScoreTable();
            scopeCreateResultTable[MetaAttributes.OriginAlias] = from.Alias;
            scopeResultQuery[MetaAttributes.SelectIntoVariableName] = from.Alias.ToScoreTable();
            scopeResultQuery[MetaAttributes.Contexts] = from.Alias;
            scopeResultQuery[MetaAttributes.SourceName] = source;

            var newFrom = lastJoinQuery != null
                ? new Parser.ExpressionFromNode(
                    new InMemoryGroupedFromNode(lastJoinQuery.From.Alias)
                )
                : from;

            aliasesPositionsSymbol.AliasesPositions.Add(newFrom.Alias, aliasIndex);

            splitNodes.Add(new CreateTransformationTableNode(scopeResultQuery[MetaAttributes.SelectIntoVariableName],
                [], select.Fields, false));
            splitNodes.Add(new DetailedQueryNode(scoreSelect, newFrom, scoreWhere, null, scoreOrderBy, skip, take,
                scopeResultQuery[MetaAttributes.SelectIntoVariableName]));

            Nodes.Push(
                new MultiStatementNode(
                    splitNodes.ToArray(),
                    null));
        }

        _scope.ScopeSymbolTable.AddSymbol(MetaAttributes.AllQueryContexts, aliasesPositionsSymbol);

        _joinedTables.Clear();
    }

    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        var exp = Nodes.Pop();
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType));
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType));
    }

    public void Visit(InternalQueryNode node)
    {
        throw new NotSupportedException();
    }

    public void Visit(RootNode node)
    {
        var poppedNode = Nodes.Pop();
        RootScript = new RootNode(poppedNode);
    }

    public void Visit(SingleSetNode node)
    {
        var query = (InternalQueryNode)Nodes.Pop();

        var nodes = new Node[]
            { new CreateTransformationTableNode(query.From.Alias, [], query.Select.Fields, false), query };

        Nodes.Push(new MultiStatementNode(nodes, null));
    }

    public void Visit(RefreshNode node)
    {
    }

    public void Visit(UnionNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
    }

    public void Visit(UnionAllNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
            node.IsTheLastOne));
    }

    public void Visit(ExceptNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
    }

    public void Visit(IntersectNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(
            new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
    }

    public void Visit(PutTrueNode node)
    {
        Nodes.Push(new PutTrueNode());
    }

    public void Visit(MultiStatementNode node)
    {
        var items = new Node[node.Nodes.Length];

        for (var i = node.Nodes.Length - 1; i >= 0; --i)
            items[i] = Nodes.Pop();

        Nodes.Push(new MultiStatementNode(items, node.ReturnType));
    }

    public void Visit(CteExpressionNode node)
    {
        var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

        var set = Nodes.Pop();

        for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
            sets[i] = (CteInnerExpressionNode)Nodes.Pop();

        Nodes.Push(new CteExpressionNode(sets, set));
    }

    public void Visit(CteInnerExpressionNode node)
    {
        Nodes.Push(new CteInnerExpressionNode(Nodes.Pop(), node.Name));
    }

    public void Visit(JoinNode node)
    {
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode)Nodes.Pop()));
    }

    public void Visit(ApplyNode node)
    {
        Nodes.Push(new Parser.ApplyNode((Parser.ApplyFromNode)Nodes.Pop()));
    }

    public void Visit(OrderByNode node)
    {
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode)Nodes.Pop();

        Nodes.Push(new OrderByNode(fields));
    }

    public void SetScope(Scope scope)
    {
        _scope = scope;
    }

    public void Visit(CreateTableNode node)
    {
    }

    public void Visit(CoupleNode node)
    {
    }

    public void Visit(StatementsArrayNode node)
    {
    }

    public void Visit(StatementNode node)
    {
    }

    public void Visit(CaseNode node)
    {
        var whenThenPairs = new List<(Node When, Node Then)>();

        for (var i = 0; i < node.WhenThenPairs.Length; ++i)
        {
            var then = Nodes.Pop();
            var when = Nodes.Pop();
            whenThenPairs.Add((when, then));
        }

        var elseNode = Nodes.Pop();

        Nodes.Push(new CaseNode(whenThenPairs.ToArray(), elseNode, node.ReturnType));
    }

    public void Visit(WhenNode node)
    {
        var expression = Nodes.Pop();
        var rewrittenExpression = QueryRewriteUtilities.RewriteNullableBoolExpressions(expression);
        Nodes.Push(new WhenNode(rewrittenExpression));
    }

    public void Visit(ThenNode node)
    {
        var expression = Nodes.Pop();
        Nodes.Push(new ThenNode(expression));
    }

    public void Visit(ElseNode node)
    {
        var expression = Nodes.Pop();
        Nodes.Push(new ElseNode(expression));
    }

    public void Visit(FieldLinkNode node)
    {
        Nodes.Push(new FieldLinkNode($"::{node.Index}", node.ReturnType));
    }

    public void Visit(InterpretCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(ParseCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(TryInterpretCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(TryParseCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(PartialInterpretCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(InterpretAtCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(BinarySchemaNode node)
    {
    }

    public void Visit(TextSchemaNode node)
    {
    }

    public void Visit(FieldDefinitionNode node)
    {
    }

    public void Visit(ComputedFieldNode node)
    {
    }

    public void Visit(TextFieldDefinitionNode node)
    {
    }

    public void Visit(FieldConstraintNode node)
    {
    }

    public void Visit(PrimitiveTypeNode node)
    {
    }

    public void Visit(ByteArrayTypeNode node)
    {
    }

    public void Visit(StringTypeNode node)
    {
    }

    public void Visit(SchemaReferenceTypeNode node)
    {
    }

    public void Visit(ArrayTypeNode node)
    {
    }

    public void Visit(BitsTypeNode node)
    {
    }

    public void Visit(AlignmentNode node)
    {
    }

    public void Visit(RepeatUntilTypeNode node)
    {
    }

    public void Visit(InlineSchemaTypeNode node)
    {
    }

    private void VisitAccessMethod(AccessMethodNode node)
    {
        var args = Nodes.Pop() as ArgsListNode;


        var functionName = node.FunctionToken.Value;

        if (IsInterpretFunctionCall(functionName, args, out var schemaName, out var dataSource))
        {
            Nodes.Push(new InterpretCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsParseFunctionCall(functionName, args, out schemaName, out dataSource))
        {
            Nodes.Push(new ParseCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsInterpretAtFunctionCall(functionName, args, out schemaName, out dataSource, out var offset))
        {
            Nodes.Push(new InterpretAtCallNode(dataSource, offset, schemaName, null));
            return;
        }

        if (IsTryInterpretFunctionCall(functionName, args, out schemaName, out dataSource))
        {
            Nodes.Push(new TryInterpretCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsTryParseFunctionCall(functionName, args, out schemaName, out dataSource))
        {
            Nodes.Push(new TryParseCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsPartialInterpretFunctionCall(functionName, args, out schemaName, out dataSource))
        {
            Nodes.Push(new PartialInterpretCallNode(dataSource, schemaName, null));
            return;
        }

        Nodes.Push(new AccessMethodNode(node.FunctionToken, args, null, node.CanSkipInjectSource, node.Method,
            node.Alias, default, node.IsDistinct));
    }

    private static bool IsInterpretFunctionCall(string functionName, ArgsListNode? args, out string schemaName,
        out Node dataSource)
    {
        schemaName = string.Empty;
        dataSource = null!;

        if (!string.Equals(functionName, "Interpret", StringComparison.OrdinalIgnoreCase))
            return false;

        if (args?.Args == null || args.Args.Length != 2)
            return false;


        var schemaArg = args.Args[1];
        if (schemaArg is StringNode stringNode)
        {
            schemaName = stringNode.Value;
            dataSource = args.Args[0];
            return true;
        }

        return false;
    }

    private static bool IsParseFunctionCall(string functionName, ArgsListNode? args, out string schemaName,
        out Node dataSource)
    {
        schemaName = string.Empty;
        dataSource = null!;

        if (!string.Equals(functionName, "Parse", StringComparison.OrdinalIgnoreCase))
            return false;

        if (args?.Args == null || args.Args.Length != 2)
            return false;

        var schemaArg = args.Args[1];
        if (schemaArg is StringNode stringNode)
        {
            schemaName = stringNode.Value;
            dataSource = args.Args[0];
            return true;
        }

        return false;
    }

    private static bool IsInterpretAtFunctionCall(string functionName, ArgsListNode? args, out string schemaName,
        out Node dataSource, out Node offset)
    {
        schemaName = string.Empty;
        dataSource = null!;
        offset = null!;

        if (!string.Equals(functionName, "InterpretAt", StringComparison.OrdinalIgnoreCase))
            return false;

        if (args?.Args == null || args.Args.Length != 3)
            return false;

        var schemaArg = args.Args[2];
        if (schemaArg is StringNode stringNode)
        {
            schemaName = stringNode.Value;
            dataSource = args.Args[0];
            offset = args.Args[1];
            return true;
        }

        return false;
    }

    private static bool IsTryInterpretFunctionCall(string functionName, ArgsListNode? args, out string schemaName,
        out Node dataSource)
    {
        schemaName = string.Empty;
        dataSource = null!;

        if (!string.Equals(functionName, "TryInterpret", StringComparison.OrdinalIgnoreCase))
            return false;

        if (args?.Args == null || args.Args.Length != 2)
            return false;

        var schemaArg = args.Args[1];
        if (schemaArg is StringNode stringNode)
        {
            schemaName = stringNode.Value;
            dataSource = args.Args[0];
            return true;
        }

        return false;
    }

    private static bool IsTryParseFunctionCall(string functionName, ArgsListNode? args, out string schemaName,
        out Node dataSource)
    {
        schemaName = string.Empty;
        dataSource = null!;

        if (!string.Equals(functionName, "TryParse", StringComparison.OrdinalIgnoreCase))
            return false;

        if (args?.Args == null || args.Args.Length != 2)
            return false;

        var schemaArg = args.Args[1];
        if (schemaArg is StringNode stringNode)
        {
            schemaName = stringNode.Value;
            dataSource = args.Args[0];
            return true;
        }

        return false;
    }

    private static bool IsPartialInterpretFunctionCall(string functionName, ArgsListNode? args, out string schemaName,
        out Node dataSource)
    {
        schemaName = string.Empty;
        dataSource = null!;

        if (!string.Equals(functionName, "PartialInterpret", StringComparison.OrdinalIgnoreCase))
            return false;

        if (args?.Args == null || args.Args.Length != 2)
            return false;

        var schemaArg = args.Args[1];
        if (schemaArg is StringNode stringNode)
        {
            schemaName = stringNode.Value;
            dataSource = args.Args[0];
            return true;
        }

        return false;
    }
}
