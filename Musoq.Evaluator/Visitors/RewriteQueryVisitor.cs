using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteQueryVisitor : IScopeAwareExpressionVisitor
    {
        protected Stack<Node> Nodes { get; } = new Stack<Node>();
        private readonly TransitionSchemaProvider _schemaProvider;
        private readonly List<AccessMethodNode> _refreshMethods;
        private readonly Dictionary<string, int> _tmpVariableNames = new Dictionary<string, int>();
        private InternalQueryNode _setLeftNode;

        private FieldNode[] _generatedColumns = new FieldNode[0];

        private string _currentSchema;
        private string _currentCte;

        private ISchema _schema;
        private ISchemaTable _table;

        private CtePart _ctePart = CtePart.None;

        public RewriteQueryVisitor(TransitionSchemaProvider schemaProvider, List<AccessMethodNode> refreshMethods)
        {
            _schemaProvider = schemaProvider;
            _refreshMethods = refreshMethods;
        }

        public RootNode RootScript { get; private set; }

        public void BeginCteQueryPart(CteExpressionNode node, CtePart part)
        {
            _ctePart = part;

            if (part == CtePart.Outer)
                _setLeftNode = null;
        }

        public void EndCteQuery()
        {
            _ctePart = CtePart.None;
        }

        public void Visit(Node node)
        {
        }

        public void Visit(DescNode node)
        {
            var from = (SchemaFromNode)Nodes.Pop();

            var fields = new List<FieldNode>
            {
                new FieldNode(new AccessColumnNode(nameof(ISchemaColumn.ColumnName), string.Empty, typeof(string), TextSpan.Empty), 0, "Name"),
                new FieldNode(new AccessColumnNode(nameof(ISchemaColumn.ColumnIndex), string.Empty, typeof(int), TextSpan.Empty), 1, "Index"),
                new FieldNode(new AccessColumnNode(nameof(ISchemaColumn.ColumnType), string.Empty, typeof(string), TextSpan.Empty), 2, "Type")
            };

            var table = new DynamicTable(new ISchemaColumn[]
            {
                new SchemaColumn(nameof(ISchemaColumn.ColumnName), 0, typeof(string)),
                new SchemaColumn(nameof(ISchemaColumn.ColumnIndex), 1, typeof(int)),
                new SchemaColumn(nameof(ISchemaColumn.ColumnType), 2, typeof(string))
            });

            var schemaName = $"{from.Schema}.desc";
            const string method = "notimportant";
            var parameters = new string[0];

            var schema = _schemaProvider.GetSchema(from.Schema);
            var schemaTable = schema.GetTableByName(from.Method, from.Parameters);

            _schemaProvider.AddTransitionSchema(new DescSchema(schemaName, table, schemaTable.Columns));
            var select = new SelectNode(fields.ToArray());
            var newFrom = new SchemaFromNode(schemaName, method, parameters, "desc");

            var newQuery = new QueryNode(select, newFrom, new WhereNode(new PutTrueNode()), null, null, null, null);

            Nodes.Push(new ExpressionFromNode(newFrom));
            Nodes.Push(new WhereNode(new PutTrueNode()));
            Nodes.Push(select);

            Visit(newQuery);
        }

        public void Visit(StarNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new StarNode(left, right));
        }

        public void Visit(FSlashNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new FSlashNode(left, right));
        }

        public void Visit(ModuloNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ModuloNode(left, right));
        }

        public void Visit(AddNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new AddNode(left, right));
        }

        public void Visit(HyphenNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new HyphenNode(left, right));
        }

        public void Visit(AndNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new AndNode(left, right));
        }

        public void Visit(OrNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new OrNode(left, right));
        }

        public void Visit(ShortCircuitingNodeLeft node)
        {
            Nodes.Push(new ShortCircuitingNodeLeft(Nodes.Pop(), node.UsedFor));
        }

        public void Visit(ShortCircuitingNodeRight node)
        {
            Nodes.Push(new ShortCircuitingNodeRight(Nodes.Pop(), node.UsedFor));
        }

        public void Visit(EqualityNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new EqualityNode(left, right));
        }

        public void Visit(GreaterOrEqualNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new GreaterOrEqualNode(left, right));
        }

        public void Visit(LessOrEqualNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LessOrEqualNode(left, right));
        }

        public void Visit(GreaterNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new GreaterNode(left, right));
        }

        public void Visit(LessNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LessNode(left, right));
        }

        public void Visit(DiffNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new DiffNode(left, right));
        }

        public void Visit(NotNode node)
        {
            Nodes.Push(new NotNode(Nodes.Pop()));
        }

        public void Visit(LikeNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LikeNode(left, right));
        }

        public virtual void Visit(FieldNode node)
        {
            Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName));
        }

        public virtual void Visit(SelectNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new SelectNode(fields.ToArray()));
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
            Nodes.Push(new DecimalNode(node.Value.ToString()));
        }

        public void Visit(IntegerNode node)
        {
            Nodes.Push(new IntegerNode(node.Value.ToString()));
        }

        public void Visit(WordNode node)
        {
            Nodes.Push(new WordNode(node.Value));
        }

        public void Visit(ContainsNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ContainsNode(left, right as ArgsListNode));
        }

        public virtual void Visit(AccessMethodNode node)
        {
            VisitAccessMethod(node);
        }

        public void Visit(GroupByAccessMethodNode node)
        {
            VisitAccessMethod(node);
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            VisitAccessMethod(node);
        }

        public virtual void Visit(AccessColumnNode node)
        {
            Nodes.Push(new AccessColumnNode(node.Name, node.Alias, node.ReturnType, node.Span));
        }

        public void Visit(AllColumnsNode node)
        {
            _generatedColumns = new FieldNode[_table.Columns.Length];

            for (int i = 0; i < _table.Columns.Length; i++)
            {
                var column = _table.Columns[i];

                _generatedColumns[i] = new FieldNode(new AccessColumnNode(column.ColumnName, String.Empty, column.ColumnType, TextSpan.Empty), i, string.Empty);
            }

            Nodes.Push(node);
        }

        public void Visit(IdentifierNode node)
        {
            Nodes.Push(new IdentifierNode(node.Name));
        }

        public void Visit(AccessObjectArrayNode node)
        {
            Nodes.Push(new AccessObjectArrayNode(node.Token));
        }

        public void Visit(AccessObjectKeyNode node)
        {
            Nodes.Push(new AccessObjectKeyNode(node.Token));
        }

        public void Visit(PropertyValueNode node)
        {
            Nodes.Push(new PropertyValueNode(node.Name));
        }

        public void Visit(DotNode node)
        {
        }

        public virtual void Visit(AccessCallChainNode node)
        {
        }

        public void Visit(ArgsListNode node)
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
            Nodes.Push(new SchemaFromNode(node.Schema, node.Method, node.Parameters, node.Alias));
        }

        public void Visit(JoinSourcesTableFromNode node)
        {
        }

        public void Visit(JoinFromNode node)
        {
            var exp = Nodes.Pop();
            var right = (FromNode)Nodes.Pop();
            var left = (FromNode)Nodes.Pop();
            Nodes.Push(new JoinFromNode(left, right, exp, node.JoinType));
            _joinedTables.Add(node);
        }

        public void Visit(ExpressionFromNode node)
        {
            Nodes.Push(new ExpressionFromNode((FromNode)Nodes.Pop()));
        }

        public void Visit(InMemoryTableFromNode node)
        {
            Nodes.Push(new InMemoryTableFromNode(node.VariableName));
        }

        public void Visit(CreateTableNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new CreateTableNode(node.Name, node.Keys, fields));
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
            var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

            var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
            var take = node.Take != null ? Nodes.Pop() as TakeNode : null;


            var select = Nodes.Pop() as SelectNode;
            var where = Nodes.Pop() as WhereNode;
            var from = Nodes.Pop() as ExpressionFromNode;

            var scoreSelect = select;

            QueryNode query;

            var splittedNodes = new List<Node>();
            var parent = _scope.Parent;
            var source = from.Alias.ToRowsSource().WithRowsUsage();

            QueryNode lastJoinQuery = null;

            if (from.Expression is JoinsNode)
            {
                var current = _joinedTables[0];
                var left = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.Source.Alias);
                var right = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.With.Alias);

                var scopeCreateTable = parent.AddScope();
                var scopeJoinedQuery = parent.AddScope();

                var bothForCreateTable = CreateAndConcatFields(left, current.Source.Alias, right, current.With.Alias, (name, alias) => NamingHelper.ToColumnName(alias, name));
                var bothForSelect = CreateAndConcatFields(left, current.Source.Alias, right, current.With.Alias, (name, alias) => name);

                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.Source.Alias, left);
                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(current.With.Alias, right);

                var targetTableName = $"{current.Source.Alias}{current.With.Alias}";

                scopeJoinedQuery.ScopeSymbolTable.AddSymbol(targetTableName, _scope.ScopeSymbolTable.GetSymbol(targetTableName));
                scopeJoinedQuery[MetaAttributes.SelectIntoVariableName] = targetTableName.ToTransitionTable();
                scopeCreateTable[MetaAttributes.CreateTableVariableName] = targetTableName.ToTransitionTable();


                var joinedQuery = new InternalQueryNode(
                    new SelectNode(bothForSelect),
                    new ExpressionFromNode(new JoinSourcesTableFromNode(current.Source, current.With, current.Expression)),
                    new WhereNode(new PutTrueNode()),
                    null,
                    null,
                    null,
                    null,
                    new RefreshNode(new AccessMethodNode[0]));

                var targetTable = new CreateTableNode(targetTableName, new string[0], bothForCreateTable);

                splittedNodes.Add(targetTable);
                splittedNodes.Add(joinedQuery);

                source = targetTableName.ToRowsSource();

                var usedTables = new Dictionary<string, string>
                {
                    {current.Source.Alias, targetTableName},
                    {current.With.Alias, targetTableName}
                };

                for (int i = 1; i < _joinedTables.Count; i++)
                {
                    current = _joinedTables[i];
                    left = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.Source.Alias);
                    right = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(current.With.Alias);

                    targetTableName = $"{current.Source.Alias}{current.With.Alias}";

                    scopeCreateTable = parent.AddScope();
                    scopeJoinedQuery = parent.AddScope();

                    bothForCreateTable = CreateAndConcatFields(left, current.Source.Alias, right, current.With.Alias, (name, alias) => NamingHelper.ToColumnName(alias, name));
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

                    scopeJoinedQuery.ScopeSymbolTable.AddSymbol(targetTableName, _scope.ScopeSymbolTable.GetSymbol(targetTableName));
                    scopeJoinedQuery[MetaAttributes.SelectIntoVariableName] = targetTableName.ToTransitionTable();
                    scopeCreateTable[MetaAttributes.CreateTableVariableName] = targetTableName.ToTransitionTable();

                    var expressionUpdater = new RewriteWhereConditionWithUpdatedColumnAccess(_schemaProvider, _refreshMethods, usedTables);
                    var traverser = new RewriteQueryTraverseVisitor(expressionUpdater, new ScopeWalker(_scope));

                    new WhereNode(current.Expression).Accept(traverser);

                    foreach (var key in usedTables.Keys.ToArray())
                        usedTables[key] = targetTableName;

                    usedTables[current.Source.Alias] = targetTableName;
                    usedTables.Add(current.With.Alias, targetTableName);

                    joinedQuery = new InternalQueryNode(
                        new SelectNode(bothForSelect),
                        new ExpressionFromNode(new JoinInMemoryWithSourceTableFromNode(current.Source.Alias, current.With, expressionUpdater.Where.Expression)),
                        new WhereNode(new PutTrueNode()),
                        null,
                        null,
                        null,
                        null,
                        new RefreshNode(new AccessMethodNode[0]));

                    targetTable = new CreateTableNode(targetTableName, new string[0], bothForCreateTable);

                    splittedNodes.Add(targetTable);
                    splittedNodes.Add(joinedQuery);

                    lastJoinQuery = joinedQuery;
                    source = targetTableName.ToTransitionTable().ToTransformedRowsSource();
                }

                var selectRewriter = new RewriteSelectToUseJoinTransitionTable(_schemaProvider, _refreshMethods);
                var selectTranverser = new RewriteQueryTraverseVisitor(selectRewriter, new ScopeWalker(_scope));

                select.Accept(selectTranverser);
                scoreSelect = selectRewriter.ChangedSelect;
            }

            if (groupBy != null)
            {
                var nestedFrom = splittedNodes.Count > 0 ? new ExpressionFromNode(new InMemoryTableFromNode(lastJoinQuery.From.Alias)) : @from;

                var splitted = SplitBetweenAggreateAndNonAggreagate(select.Fields, groupBy.Fields, true);
                var refreshMethods = CreateRefreshMethods();
                var aggSelect = new SelectNode(ConcatAggregateFieldsWithGroupByFields(splitted[0], groupBy.Fields).Reverse().ToArray());
                var outSelect = new SelectNode(splitted[1]);

                var scopeCreateTranformingTable = parent.AddScope();
                var scopeTransformedQuery = parent.AddScope();
                var scopeCreateResultTable = parent.AddScope();
                var scopeResultQuery = parent.AddScope();

                scopeCreateTranformingTable[MetaAttributes.CreateTableVariableName] = nestedFrom.Alias.ToGroupingTable();
                scopeCreateResultTable[MetaAttributes.CreateTableVariableName] = nestedFrom.Alias.ToScoreTable();

                var destination = nestedFrom.Alias.ToGroupingTable().ToTransformedRowsSource();
                scopeTransformedQuery[MetaAttributes.SelectIntoVariableName] = destination;
                scopeTransformedQuery[MetaAttributes.SourceName] = splittedNodes.Count > 0 ? nestedFrom.Alias.ToTransitionTable().ToTransformedRowsSource() : nestedFrom.Alias.ToRowsSource().WithRowsUsage();
                scopeTransformedQuery.ScopeSymbolTable.AddSymbol(nestedFrom.Alias, _scope.ScopeSymbolTable.GetSymbol(nestedFrom.Alias));

                if (splittedNodes.Count > 0)
                {
                    var selectRewriter = new RewriteSelectToUseJoinTransitionTable(_schemaProvider, _refreshMethods, nestedFrom.Alias);
                    var selectTraverser = new RewriteQueryTraverseVisitor(selectRewriter, new ScopeWalker(_scope));

                    groupBy.Accept(selectTraverser);
                    groupBy = selectRewriter.ChangedGroupBy;

                    scopeTransformedQuery.ScopeSymbolTable.AddSymbol("groupFields", new FieldsNamesSymbol(groupBy.Fields.Select(f => f.FieldName).ToArray()));

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
                        newRefreshMethods.Add(new AccessMethodNode(method.FToken, newArgs, method.ExtraAggregateArguments, method.Method));
                    }

                    refreshMethods = new RefreshNode(newRefreshMethods.ToArray());
                }
                else
                {
                    scopeTransformedQuery.ScopeSymbolTable.AddSymbol("groupFields", new FieldsNamesSymbol(groupBy.Fields.Select(f => f.Expression.ToString()).ToArray()));
                }

                var transformingQuery = new InternalQueryNode(aggSelect, nestedFrom, where, groupBy, null, null, null, refreshMethods);

                var returnScore = nestedFrom.Alias.ToScoreTable();
                scopeResultQuery[MetaAttributes.SelectIntoVariableName] = returnScore;
                scopeResultQuery[MetaAttributes.SourceName] = destination;

                query = new DetailedQueryNode(
                    outSelect, 
                    new ExpressionFromNode(
                        new InMemoryTableFromNode(returnScore)),
                    new WhereNode(new PutTrueNode()),
                    null, 
                    null, 
                    skip, 
                    take, 
                    destination, 
                    returnScore, 
                    true);

                splittedNodes.Add(new CreateTableNode(destination, new string[0], transformingQuery.Select.Fields));
                splittedNodes.Add(transformingQuery);
                splittedNodes.Add(new CreateTableNode(query.From.Alias, new string[0], query.Select.Fields));
                splittedNodes.Add(query);

                Nodes.Push(
                    new MultiStatementNode(
                        splittedNodes.ToArray(), 
                        null));

                source = destination;
            }
            else
            {
                var splitted = SplitBetweenAggreateAndNonAggreagate(select.Fields, new FieldNode[0], true);

                if (IsQueryWithOnlyAggregateMethods(splitted))
                {
                    var fakeField = new FieldNode(new IntegerNode("1"), 0, String.Empty);
                    var fakeGroupBy = new GroupByNode(new[] { fakeField }, null);
                    Nodes.Push(from);
                    Nodes.Push(where);
                    Nodes.Push(select);

                    if(node.Take != null)
                        Nodes.Push(node.Take);

                    if(node.Skip != null)
                        Nodes.Push(node.Skip);

                    Nodes.Push(fakeGroupBy);
                    Visit(new QueryNode(node.Select, node.From, node.Where, fakeGroupBy, node.OrderBy, node.Skip, node.Take));
                    return;
                }
                else if (IsQueryWithMixedAggregateAndNonAggregateMethods(splitted))
                {
                    query = new InternalQueryNode(select, from, where, null, null, skip, take, CreateRefreshMethods());
                }
                else
                {
                    var scopeCreateResultTable = parent.AddScope();
                    var scopeResultQuery = parent.AddScope();

                    scopeCreateResultTable[MetaAttributes.CreateTableVariableName] = from.Alias.ToScoreTable();
                    scopeResultQuery[MetaAttributes.SelectIntoVariableName] = from.Alias.ToScoreTable();
                    scopeResultQuery[MetaAttributes.SourceName] = source;

                    var newFrom = lastJoinQuery != null
                        ? new ExpressionFromNode(new InMemoryTableFromNode(lastJoinQuery.From.Alias))
                        : from; 

                    splittedNodes.Add(new CreateTableNode(scopeResultQuery[MetaAttributes.SelectIntoVariableName], new string[0], select.Fields));
                    splittedNodes.Add(new DetailedQueryNode(scoreSelect, newFrom, where, null, null, skip, take, source, scopeResultQuery[MetaAttributes.SelectIntoVariableName], false));

                    Nodes.Push(
                        new MultiStatementNode(
                            splittedNodes.ToArray(), 
                            null));
                }
            }

            var parentScope = _scope.Parent;
            parentScope.RemoveScope(_scope);
            _scope = parentScope.Child[0];
        }

        private bool IsQueryWithOnlyAggregateMethods(FieldNode[][] splitted)
        {
            return splitted[0].Length > 0 && splitted[0].Length == splitted[1].Length;
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
                var hasField = selectFields.Any(field => field.Expression.ToString() == groupField.Expression.ToString());

                if (!hasField)
                {
                    fields.Add(new FieldNode(groupField.Expression, ++nextOrder, String.Empty));
                }
            }

            return fields.ToArray();
        }

        public void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            var exp = Nodes.Pop();
            var from = (FromNode)Nodes.Pop();
            Nodes.Push(new JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp));
        }

        public void Visit(InternalQueryNode node)
        {
            throw new NotSupportedException();
        }

        public void Visit(RootNode node)
        {
            RootScript = new RootNode(Nodes.Pop());
        }

        public void Visit(SingleSetNode node)
        {
            var query = (InternalQueryNode)Nodes.Pop();

            if (_ctePart == CtePart.Inner)
                _cteLastQueriesByName.Add(_currentCte, query);

            var nodes = new Node[] { new CreateTableNode(query.From.Alias, new string[0], query.Select.Fields), query };

            Nodes.Push(new MultiStatementNode(nodes, null));
        }

        public void Visit(RefreshNode node)
        {
        }

        public void Visit(UnionNode node)
        {
            //TranslatedSetTreeNode translatedTree;

            //var rightNode = Nodes.Pop();
            //var leftNode = _setLeftNode ?? Nodes.Pop();
            //if (!node.IsNested)
            //    Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            //else
            //    translatedTree = (TranslatedSetTreeNode)Nodes.Peek();

            //var leftQuery = leftNode as InternalQueryNode;
            //var rightQuery = rightNode as InternalQueryNode;

            //CreateTableNode fTable;
            //if (!node.IsNested)
            //    fTable = new CreateTableNode($"{leftQuery.Into.Name}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields));
            //else
            //    fTable = new CreateTableNode(
            //        $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields));

            //InternalQueryNode trLQuery;
            //if (node.IsNested)
            //{
            //    var columns = ChangeMethodCallsForColumnAccess(leftQuery.Select);
            //    var exTable =
            //        new InMemoryTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName);
            //    trLQuery = new InternalQueryNode(columns, exTable, new WhereNode(new PutTrueNode()), null, null,
            //        new IntoNode(fTable.Name), null, null, null, false, string.Empty, true, null);
            //}
            //else
            //{
            //    trLQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy, null,
            //        new IntoNode(fTable.Name), null, leftQuery.Skip, leftQuery.Take, false, string.Empty, false, null);
            //}

            //var trQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
            //    rightQuery.GroupBy, null, new IntoNode(fTable.Name),
            //    new ShouldBePresentInTheTable(fTable.Name, true, node.Keys), rightQuery.Skip, rightQuery.Take, node.IsTheLastOne, fTable.Name, false,
            //    null);

            //var transitionTables = new List<CreateTableNode> { fTable };

            //if (IsRightMostQuery(node) && _ctePart == CtePart.Inner)
            //    _cteLastQueriesByName.Add(_currentCte, trQuery);

            //_setLeftNode = trLQuery;

            //translatedTree.Nodes.Add(new TranslatedSetOperatorNode(transitionTables.ToArray(), trLQuery, trQuery, fTable.Name,
            //    node.Keys));
        }

        public void Visit(UnionAllNode node)
        {
            //TranslatedSetTreeNode translatedTree;

            //var rightNode = Nodes.Pop();
            //var leftNode = _setLeftNode ?? Nodes.Pop();

            //if (!node.IsNested)
            //    Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            //else
            //    translatedTree = (TranslatedSetTreeNode)Nodes.Peek();

            //var leftQuery = leftNode as InternalQueryNode;
            //var rightQuery = rightNode as InternalQueryNode;

            //CreateTableNode fTable;
            //if (!node.IsNested)
            //    fTable = new CreateTableNode($"{leftQuery.Into.Name}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields));
            //else
            //    fTable = new CreateTableNode(
            //        $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields));

            //InternalQueryNode trLQuery;
            //if (node.IsNested)
            //{
            //    var columns = ChangeMethodCallsForColumnAccess(leftQuery.Select);
            //    var exTable =
            //        new InMemoryTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName);
            //    trLQuery = new InternalQueryNode(columns, exTable, new WhereNode(new PutTrueNode()), null, null,
            //        new IntoNode(fTable.Name), null, null, null, false, string.Empty, true, null);
            //}
            //else
            //{
            //    trLQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy, null,
            //        new IntoNode(fTable.Name), null, leftQuery.Skip, leftQuery.Take, false, string.Empty, false, null);
            //}

            //var trQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
            //    rightQuery.GroupBy, null, new IntoNode(fTable.Name), null, rightQuery.Skip, rightQuery.Take, node.IsTheLastOne, fTable.Name, false, null);

            //var transitionTables = new List<CreateTableNode> { fTable };

            //if (IsRightMostQuery(node) && _ctePart == CtePart.Inner)
            //    _cteLastQueriesByName.Add(_currentCte, trQuery);

            //_setLeftNode = trLQuery;

            //translatedTree.Nodes.Add(new TranslatedSetOperatorNode(transitionTables.ToArray(), trLQuery, trQuery, fTable.Name,
            //    node.Keys));
        }

        public void Visit(ExceptNode node)
        {
            //TranslatedSetTreeNode translatedTree;

            //var rightNode = Nodes.Pop();
            //var leftNode = _setLeftNode ?? Nodes.Pop();

            //if (!node.IsNested)
            //    Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            //else
            //    translatedTree = (TranslatedSetTreeNode)Nodes.Peek();


            //var leftQuery = leftNode as InternalQueryNode;
            //var rightQuery = rightNode as InternalQueryNode;

            //CreateTableNode fTable;
            //if (!node.IsNested)
            //    fTable = new CreateTableNode($"{leftQuery.Into.Name}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields));
            //else
            //    fTable = new CreateTableNode(
            //        $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields));

            //var sTable = new CreateTableNode($"{rightQuery.Into.Name}", node.Keys,
            //    rightQuery.Select.Fields);

            //var trLQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
            //    rightQuery.GroupBy, null, new IntoNode(rightQuery.Into.Name), null, rightQuery.Skip, rightQuery.Take, false, string.Empty, false, null);

            //InternalQueryNode trQuery;
            //if (node.IsNested)
            //    trQuery = new InternalQueryNode(ChangeMethodCallsForColumnAccess(leftQuery.Select),
            //        new InMemoryTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName), new WhereNode(new PutTrueNode()), null, null, new IntoNode($"{fTable.Name}"),
            //        new ShouldBePresentInTheTable(rightQuery.Into.Name, true, node.Keys), null, null, node.IsTheLastOne,
            //        fTable.Name, true, null);
            //else
            //    trQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy, null,
            //        new IntoNode($"{fTable.Name}"),
            //        new ShouldBePresentInTheTable(rightQuery.Into.Name, true, node.Keys), leftQuery.Skip, leftQuery.Take, node.IsTheLastOne,
            //        fTable.Name, false, null);

            //var transitionTables = new List<CreateTableNode> { fTable, sTable };

            //if (IsRightMostQuery(node) && _ctePart == CtePart.Inner)
            //    _cteLastQueriesByName.Add(_currentCte, trQuery);

            //_setLeftNode = trLQuery;

            //translatedTree.Nodes.Add(new TranslatedSetOperatorNode(transitionTables.ToArray(), trLQuery, trQuery,
            //    fTable.Name, node.Keys));
        }

        public void Visit(IntersectNode node)
        {
            //TranslatedSetTreeNode translatedTree;

            //var rightNode = Nodes.Pop();
            //var leftNode = _setLeftNode ?? Nodes.Pop();

            //if (!node.IsNested)
            //    Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            //else
            //    translatedTree = (TranslatedSetTreeNode)Nodes.Peek();


            //var leftQuery = leftNode as InternalQueryNode;
            //var rightQuery = rightNode as InternalQueryNode;

            //CreateTableNode fTable;
            //if (!node.IsNested)
            //    fTable = new CreateTableNode($"{leftQuery.Into.Name}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields));
            //else
            //    fTable = new CreateTableNode(
            //        $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields));

            //var sTable = new CreateTableNode($"{rightQuery.Into.Name}", node.Keys, rightQuery.Select.Fields);

            //var trLQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
            //    rightQuery.GroupBy, null, new IntoNode(rightQuery.Into.Name), null, rightQuery.Skip, rightQuery.Take, false, string.Empty, false, null);

            //InternalQueryNode trQuery;
            //if (node.IsNested)
            //    trQuery = new InternalQueryNode(ChangeMethodCallsForColumnAccess(leftQuery.Select),
            //        new InMemoryTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName), new WhereNode(new PutTrueNode()), null, null, new IntoNode(fTable.Name),
            //        new ShouldBePresentInTheTable(rightQuery.Into.Name, false, node.Keys), null, null, node.IsTheLastOne,
            //        fTable.Name, true, null);
            //else
            //    trQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy, null,
            //        new IntoNode($"{fTable.Name}"),
            //        new ShouldBePresentInTheTable(rightQuery.Into.Name, false, node.Keys), leftQuery.Skip, leftQuery.Take, node.IsTheLastOne,
            //        fTable.Name, false, null);

            //var transitionTables = new List<CreateTableNode> { fTable, sTable };

            //if (IsRightMostQuery(node) && _ctePart == CtePart.Inner)
            //    _cteLastQueriesByName.Add(_currentCte, trQuery);

            //_setLeftNode = trLQuery;

            //translatedTree.Nodes.Add(new TranslatedSetOperatorNode(transitionTables.ToArray(), trLQuery, trQuery,
            //    fTable.Name, node.Keys));
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
            var outerQuery = Nodes.Pop();

            var list = new List<Node>();
            var blocks = new List<List<Node>>();

            for (var i = node.InnerExpression.Length - 1; i >= 0; i--)
            {
                var block = new List<Node>();
                blocks.Add(block);
                var cteInnerExpressionNode = node.InnerExpression[i];
                var innerQuery = (CteInnerExpressionNode)Nodes.Pop();

                var cteLatestQuery = _cteLastQueriesByName[innerQuery.Name];
                var renameTable = new RenameTableNode(cteLatestQuery.From.Alias, cteInnerExpressionNode.Name);

                if (innerQuery.Value is TranslatedSetTreeNode innerSet)
                {
                    foreach (var set in innerSet.Nodes)
                    {
                        block.AddRange(set.CreateTableNodes);
                        block.Add(set.FQuery);
                        block.Add(set.SQuery);
                    }
                }
                else if (innerQuery.Value is MultiStatementNode multiStatementNode)
                {
                    block.AddRange(multiStatementNode.Nodes);
                }
                else
                {
                    block.Add(innerQuery.Value);
                }

                block.Add(renameTable);
            }

            blocks.Reverse();

            foreach (var block in blocks)
                list.AddRange(block);

            if (outerQuery is TranslatedSetTreeNode translatedSet)
            {
                foreach (var set in translatedSet.Nodes)
                {
                    list.AddRange(set.CreateTableNodes);
                    list.Add(set.FQuery);
                    list.Add(set.SQuery);
                }
            }
            else if (outerQuery is MultiStatementNode multiStatementNode)
            {
                list.AddRange(multiStatementNode.Nodes);
            }
            else
            {
                list.Add(outerQuery);
            }

            Nodes.Push(new MultiStatementNode(list.ToArray(), null));
        }

        private readonly Dictionary<string, InternalQueryNode> _cteLastQueriesByName = new Dictionary<string, InternalQueryNode>();
        private Scope _scope;
        private readonly List<JoinFromNode> _joinedTables = new List<JoinFromNode>();

        public void Visit(CteInnerExpressionNode node)
        {
            Nodes.Push(new CteInnerExpressionNode(Nodes.Pop(), node.Name));
        }

        public void Visit(JoinsNode node)
        {
            Nodes.Push(new JoinsNode((JoinFromNode)Nodes.Pop()));
        }

        public void Visit(JoinNode node)
        {
        }

        public void SetScope(Scope scope)
        {
            _scope = scope;
        }

        private void VisitAccessMethod(AccessMethodNode node)
        {
            var args = Nodes.Pop() as ArgsListNode;

            Nodes.Push(new AccessMethodNode(node.FToken, args, null, node.Method, node.Alias));
        }

        private FieldNode[][] SplitBetweenAggreateAndNonAggreagate(FieldNode[] fieldsToSplit, FieldNode[] groupByFields, bool useOuterFields)
        {
            var nestedFields = new List<FieldNode>();
            var outerFields = new List<FieldNode>();
            var rawNestedFields = new List<FieldNode>();

            int fieldOrder = 0;

            foreach (var root in fieldsToSplit)
            {
                var subNodes = new Stack<Node>();

                subNodes.Push(root.Expression);

                while (subNodes.Count > 0)
                {
                    var subNode = subNodes.Pop();

                    if (subNode is AccessMethodNode aggregateMethod && aggregateMethod.IsAggregateMethod)
                    {
                        var subNodeStr = subNode.ToString();
                        if (nestedFields.Select(f => f.Expression.ToString()).Contains(subNodeStr))
                            continue;

                        var nameArg = (WordNode)aggregateMethod.Arguments.Args[0];
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
                    //else if (subNode is UnaryNode unary && !(subNode is AccessPropertyNode))
                    //{
                    //    subNodes.Push(unary);
                    //}
                }

                if (!useOuterFields)
                    continue;

                var rewriter = new RewriteFieldWithGroupMethodCall(_schemaProvider, 0, groupByFields);
                var traverser = new RewriteQueryTraverseVisitor(rewriter, new ScopeWalker(_scope));
                
                root.Accept(traverser);

                outerFields.Add(rewriter.Expression);
            }

            var retFields = new FieldNode[3][];

            retFields[0] = nestedFields.ToArray();
            retFields[1] = outerFields.ToArray();
            retFields[2] = rawNestedFields.ToArray();

            return retFields;
        }

        private FieldNode[] SplitBetweenAggreateAndNonAggreagate(FieldNode[] fieldsToSplit, FieldNode[] aggFields)
        {
            var ids = aggFields.Select(f => f.Expression.ToString());
            return fieldsToSplit.Where(f => ids.Contains(f.Expression.ToString())).ToArray();
        }

        private IDictionary<string, int> CreateColumnToIndexMap(FieldNode[] fields)
        {
            var dict = new Dictionary<string, int>();

            for (var i = 0; i < fields.Length; i++)
                dict.Add(fields[i].FieldName, i);

            return dict;
        }

        private IDictionary<int, string> CreateIndexToColumnMap(FieldNode[] fields)
        {
            var dict = new Dictionary<int, string>();

            for (var i = 0; i < fields.Length; i++)
            {
                dict.Add(i, fields[i].Expression.ToString());
            }

            return dict;
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
                    fields.AddRange(_generatedColumns.Select(column => new FieldNode(column.Expression, p++, column.FieldName)));
                    continue;
                }

                fields.Add(new FieldNode(field.Expression, p++, field.FieldName));
            }

            return fields.ToArray();
        }

        private FieldNode[] CreateAndConcatFields(TableSymbol left, string lAlias, TableSymbol right, string rAlias,
            Func<string, string, string> func)
            => CreateAndConcatFields(left, lAlias, right, rAlias, func, func, (name, alias) => name, (name, alias) => name);

        private FieldNode[] CreateAndConcatFields(TableSymbol left, string lAlias, TableSymbol right, string rAlias, Func<string, string, string> lfunc, Func<string, string, string> rfunc, Func<string, string, string> lcfunc, Func<string, string, string> rcfunc)
        {
            var fields = new List<FieldNode>();

            int i = 0;

            foreach (var compoundTable in left.CompoundTables)
            {
                foreach (var column in left.GetColumns(compoundTable))
                {
                    fields.Add(
                        new FieldNode(
                            new AccessColumnNode(
                                lcfunc(column.ColumnName, compoundTable),
                                lAlias,
                                column.ColumnType,
                                TextSpan.Empty), 
                            i++,
                            lfunc(column.ColumnName, compoundTable)));
                }
            }

            foreach (var compoundTable in right.CompoundTables)
            {
                foreach (var column in right.GetColumns(compoundTable))
                {
                    fields.Add(
                        new FieldNode(
                            new AccessColumnNode(
                                rcfunc(column.ColumnName, compoundTable),
                                rAlias,
                                column.ColumnType,
                                TextSpan.Empty),
                            i++,
                            rfunc(column.ColumnName, compoundTable)));
                }
            }

            return fields.ToArray();
        }

        private RefreshNode CreateRefreshMethods()
        {
            var methods = new List<AccessMethodNode>();

            foreach (var method in _refreshMethods)
            {
                if (method.Method.GetCustomAttribute<AggregateSetDoNotResolveAttribute>() != null)
                    continue;

                if (!HasMethod(methods, method))
                    methods.Add(method);
            }

            return new RefreshNode(methods.ToArray());
        }

        private bool IsRightMostQuery(SetOperatorNode node)
        {
            return node.Right is QueryNode;
        }

        private bool HasMethod(IEnumerable<AccessMethodNode> methods, AccessMethodNode node)
        {
            return methods.Any(f => f.ToString() == node.ToString());
        }

        private static FieldNode[] TurnIntoFieldColumnAccess(FieldNode[] fields)
        {
            return fields.Select(f =>
                new FieldNode(new AccessColumnNode(f.FieldName, string.Empty, f.ReturnType, TextSpan.Empty), f.FieldOrder,
                    f.FieldName)).ToArray();
        }
    }
}