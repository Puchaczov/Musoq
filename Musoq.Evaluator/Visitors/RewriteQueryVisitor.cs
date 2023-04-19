using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Parser;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using JoinFromNode = Musoq.Parser.Nodes.From.JoinFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinsNode = Musoq.Parser.Nodes.From.JoinsNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;
using SchemaMethodFromNode = Musoq.Parser.Nodes.From.SchemaMethodFromNode;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteQueryVisitor : IScopeAwareExpressionVisitor
    {
        private readonly List<JoinFromNode> _joinedTables = new();
        private int _queryIndex = 0;
        private Scope _scope;

        protected Stack<Node> Nodes { get; } = new();

        public RootNode RootScript { get; private set; }

        public virtual void Visit(Node node)
        {
        }

        public virtual void Visit(DescNode node)
        {
            var from = (SchemaFromNode) Nodes.Pop();
            Nodes.Push(new DescNode(from, node.Type));
        }

        public virtual void Visit(StarNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new StarNode(left, right));
        }

        public virtual void Visit(FSlashNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new FSlashNode(left, right));
        }

        public virtual void Visit(ModuloNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ModuloNode(left, right));
        }

        public virtual void Visit(AddNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new AddNode(left, right));
        }

        public virtual void Visit(HyphenNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new HyphenNode(left, right));
        }

        public virtual void Visit(AndNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new AndNode(left, right));
        }

        public virtual void Visit(OrNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new OrNode(left, right));
        }

        public virtual void Visit(ShortCircuitingNodeLeft node)
        {
            Nodes.Push(new ShortCircuitingNodeLeft(Nodes.Pop(), node.UsedFor));
        }

        public virtual void Visit(ShortCircuitingNodeRight node)
        {
            Nodes.Push(new ShortCircuitingNodeRight(Nodes.Pop(), node.UsedFor));
        }

        public virtual void Visit(EqualityNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new EqualityNode(left, right));
        }

        public virtual void Visit(GreaterOrEqualNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new GreaterOrEqualNode(left, right));
        }

        public virtual void Visit(LessOrEqualNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LessOrEqualNode(left, right));
        }

        public virtual void Visit(GreaterNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new GreaterNode(left, right));
        }

        public virtual void Visit(LessNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LessNode(left, right));
        }

        public virtual void Visit(DiffNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new DiffNode(left, right));
        }

        public virtual void Visit(NotNode node)
        {
            Nodes.Push(new NotNode(Nodes.Pop()));
        }

        public virtual void Visit(LikeNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LikeNode(left, right));
        }

        public virtual void Visit(RLikeNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new RLikeNode(left, right));
        }

        public virtual void Visit(InNode node)
        {
            var right = (ArgsListNode)Nodes.Pop();
            var left = Nodes.Pop();

            Node exp = new EqualityNode(left, right.Args[0]);

            for (var i = 1; i < right.Args.Length; i++)
            {
                exp = new OrNode(exp, new EqualityNode(left, right.Args[i]));
            }

            Nodes.Push(exp);
        }

        public virtual void Visit(FieldNode node)
        {
            Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName));
        }

        public virtual void Visit(FieldOrderedNode node)
        {
            Nodes.Push(new FieldOrderedNode(Nodes.Pop(), node.FieldOrder, node.FieldName, node.Order));
        }

        public virtual void Visit(SelectNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new SelectNode(fields.ToArray()));
        }

        public virtual void Visit(GroupSelectNode node)
        {
        }

        public virtual void Visit(StringNode node)
        {
            Nodes.Push(new StringNode(node.Value));
        }

        public virtual void Visit(DecimalNode node)
        {
            Nodes.Push(new DecimalNode(node.Value));
        }

        public virtual void Visit(IntegerNode node)
        {
            Nodes.Push(new IntegerNode(node.ObjValue.ToString()));
        }

        public virtual void Visit(BooleanNode node)
        {
            Nodes.Push(new BooleanNode(node.Value));
        }

        public virtual void Visit(WordNode node)
        {
            Nodes.Push(new WordNode(node.Value));
        }

        public virtual void Visit(ContainsNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ContainsNode(left, right as ArgsListNode));
        }

        public virtual void Visit(AccessMethodNode node)
        {
            VisitAccessMethod(node);
        }

        public virtual void Visit(AccessRawIdentifierNode node)
        {
            Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
        }

        public virtual void Visit(IsNullNode node)
        {
            Nodes.Push(new IsNullNode(Nodes.Pop(), node.IsNegated));
        }

        public virtual void Visit(AccessRefreshAggreationScoreNode node)
        {
            VisitAccessMethod(node);
        }

        public virtual void Visit(AccessColumnNode node)
        {
            Nodes.Push(new AccessColumnNode(node.Name, node.Alias, node.ReturnType, node.Span));
        }

        public virtual void Visit(AllColumnsNode node)
        {
            Nodes.Push(new AllColumnsNode());
        }

        public virtual void Visit(IdentifierNode node)
        {
            Nodes.Push(new IdentifierNode(node.Name));
        }

        public virtual void Visit(AccessObjectArrayNode node)
        {
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.PropertyInfo));
        }

        public virtual void Visit(AccessObjectKeyNode node)
        {
            Nodes.Push(new AccessObjectKeyNode(node.Token, node.PropertyInfo));
        }

        public virtual void Visit(PropertyValueNode node)
        {
            Nodes.Push(new PropertyValueNode(node.Name, node.PropertyInfo));
        }

        public virtual void Visit(DotNode node)
        {
            var exp = Nodes.Pop();
            var root = Nodes.Pop();

            if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(node.ReturnType))
            {
                Nodes.Push(new DotNode(root, exp, node.IsOuter, node.Name, typeof(IDynamicMetaObjectProvider)));
            }
            else
            {
                if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(root.ReturnType))
                    Nodes.Push(new DotNode(root, exp, node.IsOuter, node.Name, typeof(IDynamicMetaObjectProvider)));
                else
                    Nodes.Push(new DotNode(root, exp, node.IsOuter, node.Name, exp.ReturnType));
            }
        }

        public virtual void Visit(AccessCallChainNode node)
        {
        }

        public virtual void Visit(ArgsListNode node)
        {
            var args = new Node[node.Args.Length];

            for (var i = node.Args.Length - 1; i >= 0; --i)
                args[i] = Nodes.Pop();

            Nodes.Push(new ArgsListNode(args));
        }

        public virtual void Visit(WhereNode node)
        {
            Nodes.Push(new WhereNode(Nodes.Pop()));
        }

        public virtual void Visit(GroupByNode node)
        {
            var having = Nodes.Peek() as HavingNode;

            if (having != null)
                Nodes.Pop();

            var fields = new FieldNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
                fields[i] = Nodes.Pop() as FieldNode;


            Nodes.Push(new GroupByNode(fields, having));
        }

        public virtual void Visit(HavingNode node)
        {
            Nodes.Push(new HavingNode(Nodes.Pop()));
        }

        public virtual void Visit(SkipNode node)
        {
            Nodes.Push(new SkipNode((IntegerNode) node.Expression));
        }

        public virtual void Visit(TakeNode node)
        {
            Nodes.Push(new TakeNode((IntegerNode) node.Expression));
        }

        public virtual void Visit(SchemaFromNode node)
        {
            var positionalSchemaFromNode = node as PositionalSchemaFromNode;
            
            if (positionalSchemaFromNode == null)
                throw new NotSupportedException("Only positional schema is supported");
            
            Nodes.Push(new PositionalSchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias, positionalSchemaFromNode.InSourcePosition));
        }

        public virtual void Visit(JoinSourcesTableFromNode node)
        {
        }

        public virtual void Visit(JoinFromNode node)
        {
            var exp = Nodes.Pop();
            var right = (FromNode) Nodes.Pop();
            var left = (FromNode) Nodes.Pop();
            Nodes.Push(new Parser.JoinFromNode(left, right, exp, node.JoinType));
            _joinedTables.Add(node);
        }

        public virtual void Visit(ExpressionFromNode node)
        {
            Nodes.Push(new Parser.ExpressionFromNode((FromNode) Nodes.Pop()));
        }

        public virtual void Visit(InMemoryTableFromNode node)
        {
            Nodes.Push(new Parser.InMemoryTableFromNode(node.VariableName, node.Alias));
        }

        public virtual void Visit(CreateTransformationTableNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new CreateTransformationTableNode(node.Name, node.Keys, fields, node.ForGrouping));
        }

        public virtual void Visit(RenameTableNode node)
        {
            Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
        }

        public virtual void Visit(TranslatedSetTreeNode node)
        {
        }

        public virtual void Visit(IntoNode node)
        {
            Nodes.Push(new IntoNode(node.Name));
        }

        public virtual void Visit(QueryScope node)
        {
        }

        public virtual void Visit(ShouldBePresentInTheTable node)
        {
            Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
        }

        public virtual void Visit(TranslatedSetOperatorNode node)
        {
        }

        public virtual void Visit(QueryNode node)
        {
            var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
            var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

            var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
            var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

            var select = Nodes.Pop() as SelectNode;
            var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
            var from = Nodes.Pop() as ExpressionFromNode;

            var scoreSelect = select;
            var scoreWhere = where;

            QueryNode query;

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

            if (from.Expression is JoinsNode)
            {
                var current = _joinedTables[0];
                var left = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.Source.Alias);
                var right = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.With.Alias);

                var scopeCreateTable = _scope.AddScope("Table");
                var scopeJoinedQuery = _scope.AddScope("Query");

                var bothForCreateTable = CreateAndConcatFields(left, current.Source.Alias, right, current.With.Alias,
                    (name, alias) => NamingHelper.ToColumnName(alias, name));
                var bothForSelect = CreateAndConcatFields(left, current.Source.Alias, right, current.With.Alias,
                    (name, alias) => name);

                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.Source.Alias, left);
                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.With.Alias, right);

                var targetTableName = $"{current.Source.Alias}{current.With.Alias}";

                aliasesPositionsSymbol.AliasesPositions.Add(current.Source.Alias, aliasIndex++);
                aliasesPositionsSymbol.AliasesPositions.Add(current.With.Alias, aliasIndex++);

                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(targetTableName,
                    _scope.ScopeSymbolTable.GetSymbol(targetTableName));

                scopeJoinedQuery[MetaAttributes.SelectIntoVariableName] = targetTableName.ToTransitionTable();
                scopeJoinedQuery[MetaAttributes.OriginAlias] = targetTableName;
                scopeJoinedQuery[MetaAttributes.Contexts] = $"{current.Source.Alias},{current.With.Alias}";
                scopeCreateTable[MetaAttributes.CreateTableVariableName] = targetTableName.ToTransitionTable();

                var joinedQuery = new InternalQueryNode(
                    new SelectNode(bothForSelect),
                    new Parser.ExpressionFromNode(
                        new Parser.JoinSourcesTableFromNode(
                            current.Source, 
                            current.With,
                            current.Expression, 
                            current.JoinType)),
                    null,
                    null,
                    null,
                    null,
                    null,
                    new RefreshNode(Array.Empty<AccessMethodNode>()));

                var targetTable = new CreateTransformationTableNode(targetTableName, Array.Empty<string>(), bothForCreateTable, false);

                splitNodes.Add(targetTable);
                splitNodes.Add(joinedQuery);

                lastJoinQuery = joinedQuery;
                source = targetTableName.ToTransitionTable().ToTransformedRowsSource();

                var usedTables = new Dictionary<string, string>
                {
                    {current.Source.Alias, targetTableName},
                    {current.With.Alias, targetTableName}
                };

                for (var i = 1; i < _joinedTables.Count; i++)
                {
                    current = _joinedTables[i];
                    left = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.Source.Alias);
                    right = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.With.Alias);

                    targetTableName = $"{current.Source.Alias}{current.With.Alias}";

                    aliasesPositionsSymbol.AliasesPositions.Add(current.Source.Alias, aliasIndex++);
                    aliasesPositionsSymbol.AliasesPositions.Add(current.With.Alias, aliasIndex++);

                    scopeCreateTable = _scope.AddScope("Table");
                    scopeJoinedQuery = _scope.AddScope("Query");

                    bothForCreateTable = CreateAndConcatFields(left, current.Source.Alias, right, current.With.Alias,
                        (name, alias) => NamingHelper.ToColumnName(alias, name));
                    bothForSelect = CreateAndConcatFields(
                        left,
                        current.Source.Alias,
                        right,
                        current.With.Alias,
                        (name, alias) => NamingHelper.ToColumnName(alias, name),
                        (name, alias) => name,
                        (name, alias) => NamingHelper.ToColumnName(alias, name),
                        (name, alias) => name);

                    scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.Source.Alias, left);
                    scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.With.Alias, right);

                    scopeJoinedQuery.ScopeSymbolTable.AddSymbol(targetTableName,
                        _scope.ScopeSymbolTable.GetSymbol(targetTableName));
                    scopeJoinedQuery[MetaAttributes.SelectIntoVariableName] = targetTableName.ToTransitionTable();
                    scopeJoinedQuery[MetaAttributes.OriginAlias] = targetTableName;
                    scopeJoinedQuery[MetaAttributes.Contexts] = $"{current.Source.Alias},{current.With.Alias}";
                    scopeCreateTable[MetaAttributes.CreateTableVariableName] = targetTableName.ToTransitionTable();

                    scopeJoinedQuery.ScopeSymbolTable.AddSymbol(
                        MetaAttributes.OuterJoinSelect,
                        new FieldsNamesSymbol(bothForSelect.Select(f => f.FieldName).ToArray()));

                    var expressionUpdater = new RewriteWhereConditionWithUpdatedColumnAccess(usedTables);
                    var traverser = new CloneTraverseVisitor(expressionUpdater);

                    new WhereNode(current.Expression).Accept(traverser);

                    foreach (var key in usedTables.Keys.ToArray())
                        usedTables[key] = targetTableName;

                    usedTables[current.Source.Alias] = targetTableName;
                    usedTables.Add(current.With.Alias, targetTableName);

                    joinedQuery = new InternalQueryNode(
                        new SelectNode(bothForSelect),
                        new Parser.ExpressionFromNode(
                            new Parser.JoinInMemoryWithSourceTableFromNode(
                                current.Source.Alias,
                                current.With, 
                                expressionUpdater.Where.Expression,
                                current.JoinType)),
                        null,
                        null,
                        null,
                        null,
                        null,
                        new RefreshNode(Array.Empty<AccessMethodNode>()));

                    targetTable = new CreateTransformationTableNode(targetTableName, Array.Empty<string>(), bothForCreateTable, false);

                    splitNodes.Add(targetTable);
                    splitNodes.Add(joinedQuery);

                    lastJoinQuery = joinedQuery;
                    source = targetTableName.ToTransitionTable().ToTransformedRowsSource();
                }

                var rewriter = new RewritePartsToUseJoinTransitionTable();
                var partsTraverser = new CloneTraverseVisitor(rewriter);

                select.Accept(partsTraverser);
                where?.Accept(partsTraverser);

                scoreSelect = rewriter.ChangedSelect;
                scoreWhere = rewriter.ChangedWhere;
            }

            if (groupBy != null)
            {
                var nestedFrom = splitNodes.Count > 0
                    ? new Parser.ExpressionFromNode(new Parser.InMemoryGroupedFromNode(lastJoinQuery.From.Alias))
                    : from;

                var split = SplitBetweenAggregateAndNonAggregate(select.Fields, groupBy.Fields, true);
                var refreshMethods = CreateRefreshMethods(usedRefreshMethods);
                var aggSelect = new SelectNode(ConcatAggregateFieldsWithGroupByFields(split[0], groupBy.Fields)
                    .Reverse().ToArray());
                var outSelect = new SelectNode(split[1]);

                var scopeCreateTransformingTable = _scope.AddScope("Table");
                var scopeTransformedQuery = _scope.AddScope("Query");
                var scopeCreateResultTable = _scope.AddScope("Table");
                var scopeResultQuery = _scope.AddScope("Query");

                scopeCreateTransformingTable[MetaAttributes.CreateTableVariableName] = nestedFrom.Alias.ToGroupingTable();
                scopeCreateResultTable[MetaAttributes.CreateTableVariableName] = nestedFrom.Alias.ToScoreTable();

                var destination = nestedFrom.Alias.ToGroupingTable().ToTransformedRowsSource();
                scopeTransformedQuery[MetaAttributes.SelectIntoVariableName] = destination;
                scopeTransformedQuery[MetaAttributes.SourceName] = splitNodes.Count > 0
                    ? nestedFrom.Alias.ToTransitionTable().ToTransformedRowsSource()
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
                        newRefreshMethods.Add(new AccessMethodNode(method.FToken, newArgs,
                            method.ExtraAggregateArguments, method.CanSkipInjectSource, method.Method));
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
                aliasesPositionsSymbol.AliasesPositions.Add(returnScore, aliasIndex++);

                query = new DetailedQueryNode(
                    outSelect,
                    new Parser.ExpressionFromNode(
                        new Parser.InMemoryGroupedFromNode(returnScore)),
                    null,
                    null,
                    null,
                    skip,
                    take,
                    returnScore);

                splitNodes.Add(new CreateTransformationTableNode(destination, Array.Empty<string>(), transformingQuery.Select.Fields, true));
                splitNodes.Add(transformingQuery);
                splitNodes.Add(new CreateTransformationTableNode(query.From.Alias, Array.Empty<string>(), query.Select.Fields, false));
                splitNodes.Add(query);

                Nodes.Push(
                    new MultiStatementNode(
                        splitNodes.ToArray(),
                        null));
            }
            else
            {
                var splitted = SplitBetweenAggregateAndNonAggregate(select.Fields, Array.Empty<FieldNode>(), true);
                
                if (IsQueryWithMixedAggregateAndNonAggregateMethods(splitted))
                {
                    query = new InternalQueryNode(select, from, where, null, null, skip, take,
                        CreateRefreshMethods(usedRefreshMethods));
                }
                else
                {
                    var scopeCreateResultTable = _scope.AddScope("Table");
                    var scopeResultQuery = _scope.AddScope("Query");

                    scopeCreateResultTable[MetaAttributes.CreateTableVariableName] = from.Alias.ToScoreTable();
                    scopeCreateResultTable[MetaAttributes.OriginAlias] = from.Alias;
                    scopeResultQuery[MetaAttributes.SelectIntoVariableName] = from.Alias.ToScoreTable();
                    scopeResultQuery[MetaAttributes.Contexts] = from.Alias;
                    scopeResultQuery[MetaAttributes.SourceName] = source;

                    var newFrom = lastJoinQuery != null
                        ? new Parser.ExpressionFromNode(
                            new Parser.InMemoryGroupedFromNode(lastJoinQuery.From.Alias))
                        : from;

                    aliasesPositionsSymbol.AliasesPositions.Add(newFrom.Alias, aliasIndex++);

                    splitNodes.Add(new CreateTransformationTableNode(scopeResultQuery[MetaAttributes.SelectIntoVariableName], Array.Empty<string>(), select.Fields, false));
                    splitNodes.Add(new DetailedQueryNode(scoreSelect, newFrom, scoreWhere, null, null, skip, take,
                        scopeResultQuery[MetaAttributes.SelectIntoVariableName]));

                    Nodes.Push(
                        new MultiStatementNode(
                            splitNodes.ToArray(),
                            null));
                }
            }

            _scope.ScopeSymbolTable.AddSymbol(MetaAttributes.AllQueryContexts, aliasesPositionsSymbol);

            _joinedTables.Clear();
        }

        public virtual void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            var exp = Nodes.Pop();
            var from = (FromNode) Nodes.Pop();
            Nodes.Push(new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType));
        }

        public virtual void Visit(InternalQueryNode node)
        {
            throw new NotSupportedException();
        }

        public virtual void Visit(RootNode node)
        {
            RootScript = new RootNode(Nodes.Pop());
        }

        public virtual void Visit(SingleSetNode node)
        {
            var query = (InternalQueryNode) Nodes.Pop();

            var nodes = new Node[] {new CreateTransformationTableNode(query.From.Alias, Array.Empty<string>(), query.Select.Fields, false), query};

            Nodes.Push(new MultiStatementNode(nodes, null));
        }

        public virtual void Visit(RefreshNode node)
        {
        }

        public virtual void Visit(UnionNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public virtual void Visit(UnionAllNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
                node.IsTheLastOne));
        }

        public virtual void Visit(ExceptNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public virtual void Visit(IntersectNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(
                new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public virtual void Visit(PutTrueNode node)
        {
            Nodes.Push(new PutTrueNode());
        }

        public virtual void Visit(MultiStatementNode node)
        {
            var items = new Node[node.Nodes.Length];

            for (var i = node.Nodes.Length - 1; i >= 0; --i)
                items[i] = Nodes.Pop();

            Nodes.Push(new MultiStatementNode(items, node.ReturnType));
        }

        public virtual void Visit(CteExpressionNode node)
        {
            var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

            var set = Nodes.Pop();

            for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
                sets[i] = (CteInnerExpressionNode) Nodes.Pop();

            Nodes.Push(new CteExpressionNode(sets, set));
        }

        public virtual void Visit(CteInnerExpressionNode node)
        {
            Nodes.Push(new CteInnerExpressionNode(Nodes.Pop(), node.Name));
        }

        public virtual void Visit(JoinsNode node)
        {
            Nodes.Push(new Parser.JoinsNode((Parser.JoinFromNode) Nodes.Pop()));
        }

        public virtual void Visit(JoinNode node)
        {
        }

        public virtual void Visit(OrderByNode node)
        {
            var fields = new FieldOrderedNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
                fields[i] = (FieldOrderedNode)Nodes.Pop();

            Nodes.Push(new OrderByNode(fields));
        }

        public virtual void SetScope(Scope scope)
        {
            _scope = scope;
        }

        public virtual void Visit(CreateTableNode node)
        {
        }

        public virtual void Visit(CoupleNode node)
        {
        }

        public virtual void Visit(SchemaMethodFromNode node)
        {
        }

        public virtual void Visit(AliasedFromNode node)
        {
        }

        public virtual void Visit(StatementsArrayNode node)
        {
        }

        public virtual void Visit(StatementNode node)
        {
        }

        public virtual void Visit(CaseNode node)
        {
            var whenThenPairs = new List<(Node When, Node Then)>();

            for (int i = 0; i < node.WhenThenPairs.Length; ++i)
            {
                var then = Nodes.Pop();
                var when = Nodes.Pop();
                whenThenPairs.Add((when, then));
            }

            var elseNode = Nodes.Pop();

            Nodes.Push(new CaseNode(whenThenPairs.ToArray(), elseNode, node.ReturnType));
        }

        public virtual void Visit(FieldLinkNode node)
        {
            Nodes.Push(new FieldLinkNode($"::{node.Index}", node.ReturnType));
        }

        private bool IsQueryWithMixedAggregateAndNonAggregateMethods(FieldNode[][] splitted)
        {
            return splitted[0].Length > 0 && splitted[0].Length != splitted[1].Length;
        }

        private FieldNode[] ConcatAggregateFieldsWithGroupByFields(FieldNode[] selectFields, FieldNode[] groupByFields)
        {
            var fields = new List<FieldNode>(selectFields);
            var nextOrder = -1;

            if (selectFields.Length > 0)
                nextOrder = selectFields.Max(f => f.FieldOrder);

            foreach (var groupField in groupByFields)
            {
                var hasField =
                    selectFields.Any(field => field.Expression.ToString() == groupField.Expression.ToString());

                if (!hasField) fields.Add(new FieldNode(groupField.Expression, ++nextOrder, string.Empty));
            }

            return fields.ToArray();
        }

        private void VisitAccessMethod(AccessMethodNode node)
        {
            var args = Nodes.Pop() as ArgsListNode;

            Nodes.Push(new AccessMethodNode(node.FToken, args, null, node.CanSkipInjectSource, node.Method, node.Alias));
        }

        private FieldNode[][] SplitBetweenAggregateAndNonAggregate(FieldNode[] fieldsToSplit, FieldNode[] groupByFields,
            bool useOuterFields)
        {
            var nestedFields = new List<FieldNode>();
            var outerFields = new List<FieldNode>();
            var rawNestedFields = new List<FieldNode>();

            var fieldOrder = 0;

            foreach (var root in fieldsToSplit)
            {
                var subNodes = new Stack<Node>();

                subNodes.Push(root.Expression);

                while (subNodes.Count > 0)
                {
                    var subNode = subNodes.Pop();

                    if (subNode is AccessMethodNode aggregateMethod && aggregateMethod.IsAggregateMethod())
                    {
                        var subNodeStr = subNode.ToString();
                        if (nestedFields.Select(f => f.Expression.ToString()).Contains(subNodeStr))
                            continue;

                        var nameArg = (WordNode) aggregateMethod.Arguments.Args[0];
                        nestedFields.Add(new FieldNode(subNode, fieldOrder, nameArg.Value));
                        rawNestedFields.Add(new FieldNode(subNode, fieldOrder, string.Empty));
                        fieldOrder += 1;
                    }
                    else if (subNode is AccessMethodNode method)
                    {
                        foreach (var arg in method.Arguments.Args)
                            subNodes.Push(arg);
                    }
                    else if (subNode is BinaryNode binary)
                    {
                        subNodes.Push(binary.Left);
                        subNodes.Push(binary.Right);
                    }
                }

                if (!useOuterFields)
                    continue;

                var rewriter = new RewriteFieldWithGroupMethodCall(groupByFields);
                var traverser = new CloneTraverseVisitor(rewriter);

                root.Accept(traverser);

                outerFields.Add(rewriter.Expression);
            }

            var retFields = new FieldNode[3][];

            retFields[0] = nestedFields.ToArray();
            retFields[1] = outerFields.ToArray();
            retFields[2] = rawNestedFields.ToArray();

            return retFields;
        }

        private FieldNode[] CreateFields(FieldNode[] oldFields)
        {
            var reorderedList = new FieldNode[oldFields.Length];
            var fields = new List<FieldNode>(reorderedList.Length);

            for (var i = reorderedList.Length - 1; i >= 0; i--) reorderedList[i] = Nodes.Pop() as FieldNode;


            for (int i = 0, j = reorderedList.Length, p = 0; i < j; ++i)
            {
                var field = reorderedList[i];

                if (field.Expression is AllColumnsNode)
                {
                    fields.AddRange(Array.Empty<FieldNode>());
                    continue;
                }

                fields.Add(new FieldNode(field.Expression, p++, field.FieldName));
            }

            return fields.ToArray();
        }

        private FieldNode[] CreateAndConcatFields(TableSymbol left, string lAlias, TableSymbol right, string rAlias,
            Func<string, string, string> func)
        {
            return CreateAndConcatFields(left, lAlias, right, rAlias, func, func, (name, alias) => name,
                (name, alias) => name);
        }

        private FieldNode[] CreateAndConcatFields(TableSymbol left, string lAlias, TableSymbol right, string rAlias,
            Func<string, string, string> lfunc, Func<string, string, string> rfunc, Func<string, string, string> lcfunc,
            Func<string, string, string> rcfunc)
        {
            var fields = new List<FieldNode>();

            var i = 0;

            foreach (var compoundTable in left.CompoundTables)
            foreach (var column in left.GetColumns(compoundTable))
                fields.Add(
                    new FieldNode(
                        new AccessColumnNode(
                            lcfunc(column.ColumnName, compoundTable),
                            lAlias,
                            column.ColumnType,
                            TextSpan.Empty),
                        i++,
                        lfunc(column.ColumnName, compoundTable)));

            foreach (var compoundTable in right.CompoundTables)
            foreach (var column in right.GetColumns(compoundTable))
                fields.Add(
                    new FieldNode(
                        new AccessColumnNode(
                            rcfunc(column.ColumnName, compoundTable),
                            rAlias,
                            column.ColumnType,
                            TextSpan.Empty),
                        i++,
                        rfunc(column.ColumnName, compoundTable)));

            return fields.ToArray();
        }

        private RefreshNode CreateRefreshMethods(IReadOnlyList<AccessMethodNode> refreshMethods)
        {
            var methods = new List<AccessMethodNode>();

            foreach (var method in refreshMethods)
            {
                if (method.Method.GetCustomAttribute<AggregateSetDoNotResolveAttribute>() != null)
                    continue;

                if (!HasMethod(methods, method))
                    methods.Add(method);
            }

            return new RefreshNode(methods.ToArray());
        }

        private bool HasMethod(IEnumerable<AccessMethodNode> methods, AccessMethodNode node)
        {
            return methods.Any(f => f.ToString() == node.ToString());
        }
    }
}