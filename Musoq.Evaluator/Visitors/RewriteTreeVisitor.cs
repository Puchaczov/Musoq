using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteTreeVisitor : ISchemaAwareExpressionVisitor
    {
        protected Stack<Node> Nodes { get; } = new Stack<Node>();
        protected List<QueryNode> _preQueries = new List<QueryNode>();
        private readonly List<CreateTableNode> _preCreatedTables = new List<CreateTableNode>();
        private readonly Stack<AccessMethodNode> _queryMethods = new Stack<AccessMethodNode>();
        private readonly Dictionary<string, int> _tmpVariableNames = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _fromIdToAlias = new Dictionary<string, string>();
        private readonly Dictionary<string, (ISchema Schema, ISchemaTable Table, int MapIndex)> _aliases = new Dictionary<string, (ISchema Schema, ISchemaTable Table, int MapIndex)>();
        private InternalQueryNode _setLeftNode;

        private FieldNode[] _generatedColumns = new FieldNode[0];
        private int _mapIndex = 0;
        
        private readonly TransitionSchemaProvider _schemaProvider;
        private string _currentSchema;
        private string _currentCte;

        private ISchema _schema;
        private ISchemaTable _table;

        private CtePart _ctePart = CtePart.None;

        public RewriteTreeVisitor(TransitionSchemaProvider schemaProvider)
        {
            _schemaProvider = schemaProvider;
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

        public void ClearAliases()
        {
            _aliases.Clear();
        }

        public void Visit(Node node)
        {
        }

        public void Visit(DescNode node)
        {
            var from = (FromNode) Nodes.Pop();

            var fields = new List<FieldNode>
            {
                new FieldNode(new DetailedAccessColumnNode(nameof(ISchemaColumn.ColumnName), 0, typeof(string), string.Empty), 0, "Name"),
                new FieldNode(new DetailedAccessColumnNode(nameof(ISchemaColumn.ColumnIndex), 1, typeof(int), string.Empty), 1, "Index"),
                new FieldNode(new DetailedAccessColumnNode(nameof(ISchemaColumn.ColumnType), 2, typeof(string), string.Empty), 2, "Type")
            };


            var table = new DynamicTable(new ISchemaColumn[]
            {
                new SchemaColumn(nameof(ISchemaColumn.ColumnName), 0, typeof(string)),
                new SchemaColumn(nameof(ISchemaColumn.ColumnIndex), 1, typeof(int)),
                new SchemaColumn(nameof(ISchemaColumn.ColumnType), 2, typeof(string)),
            });

            var schemaName = $"desc.{from.Schema}";
            const string method = "notimportant";
            var parameters = new string[0];
            _schemaProvider.AddTransitionSchema(new DescSchema(schemaName, table, _table.Columns));
            var select = new SelectNode(fields.ToArray());
            var newFrom = new SchemaFromNode(schemaName, method, parameters, string.Empty);

            var newQuery = new QueryNode(select, newFrom, null, new WhereNode(new PutTrueNode()), null, null, null, null);

            Nodes.Push(newFrom);
            Nodes.Push(new WhereNode(new PutTrueNode()));
            Nodes.Push(select);

            var oldSchema = _schema;
            var oTable = _table;
            var oldSchemaName = CurrentSchema;
            CurrentSchema = schemaName;

            SetCurrentTable(schemaName, parameters);

            Visit(newQuery);

            _schema = oldSchema;
            _table = oTable;
            CurrentSchema = oldSchemaName;

            var newInternalQuery = (InternalQueryNode)Nodes.Pop();
            var nodes = new Node[] { new CreateTableNode(newInternalQuery.Into.Name, new string[0], newInternalQuery.Select.Fields), newInternalQuery };

            Nodes.Push(new MultiStatementNode(nodes, null));
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
            if (left.ReturnType == typeof(string) && right.ReturnType == typeof(string))
            {
                var methodName = "Concat";
                var token = new FunctionToken(methodName, TextSpan.Empty);
                var args = new ArgsListNode(new []{ left, right });
                var method = _schema.ResolveMethod(methodName, new[] { left.ReturnType, right.ReturnType });

                Nodes.Push(new AccessMethodNode(token, args, null, method));
            }
            else
            {
                Nodes.Push(new AddNode(left, right));
            }
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
            const string methodName = "Like";

            var right = Nodes.Pop();
            var left = Nodes.Pop();
            var method = _schema.ResolveMethod(methodName, new [] {left.ReturnType, right.ReturnType});
            var fToken = new FunctionToken(methodName, TextSpan.Empty);
            var argList = new ArgsListNode(new []{ left, right });

            var accessMethod = new AccessMethodNode(fToken, argList, null, method);

            Nodes.Push(accessMethod);
        }

        public virtual void Visit(FieldNode node)
        {
            Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName));
        }

        public void Visit(SelectNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new SelectNode(fields.ToArray()));
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
            VisitAccessMethod(node,
                (token, node1, exargs, arg3) => new AccessMethodNode(token, node1 as ArgsListNode, exargs, arg3));
        }

        public void Visit(GroupByAccessMethodNode node)
        {
            VisitAccessMethod(node,
                (token, node1, exargs, arg3) => new GroupByAccessMethodNode(token, node1 as ArgsListNode, exargs, arg3));
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            VisitAccessMethod(node,
                (token, node1, exargs, arg3) =>
                    new AccessRefreshAggreationScoreNode(token, node1 as ArgsListNode, exargs, arg3));
        }

        public void Visit(AccessColumnNode node)
        {
            var column = _table.Columns.SingleOrDefault(f => f.ColumnName == node.Name);
            if (column == null)
            {
                if(!_schema.TryResolveProperty(node.Name, out var method))
                    throw new UnknownColumnException($"Unknown column '{node.Name}'.");

                node.ChangeReturnType(method.ReturnType);
                Nodes.Push(new AccessMethodNode(new FunctionToken(method.Name, TextSpan.Empty), new ArgsListNode(new Node[0]), null, method));
            }
            else
            {
                node.ChangeReturnType(column.ColumnType);
                Nodes.Push(new DetailedAccessColumnNode(column.ColumnName, column.ColumnIndex, column.ColumnType, string.Empty));
            }
        }

        public void Visit(AllColumnsNode node)
        {
            _generatedColumns = new FieldNode[_table.Columns.Length];

            for (int i = 0; i < _table.Columns.Length; i++)
            {
                var column = _table.Columns[i];

                _generatedColumns[i] = new FieldNode(new DetailedAccessColumnNode(column.ColumnName, i, column.ColumnType, string.Empty), i, string.Empty);
            }

            Nodes.Push(node);
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
            var propsChain = new Stack<Node>();
            var properties = new List<(PropertyInfo Prop, object Arg)>();

            var root = node;

            while (root.Root is DotNode dotNode)
            {
                root = dotNode;
            }

            var columnNode = root.Root as AccessColumnNode;

            var isAliased = false;
            var alias = string.Empty;
            if (_aliases.ContainsKey(columnNode.Name))
            {
                alias = columnNode.Name;
                columnNode = new AccessColumnNode(((PropertyValueNode)root.Expression).Name, TextSpan.Empty);
                isAliased = true;
            }
            else
            {
                propsChain.Push(root.Expression);
            }

            var column = _table.Columns.SingleOrDefault(f => f.ColumnName == columnNode.Name);
            var currentType = column.ColumnType;

            while (propsChain.Count > 0)
            {
                var prop = propsChain.Pop();

                if (prop is AccessObjectKeyNode keyNode)
                {
                    var p = currentType.GetProperty(keyNode.ObjectName, keyNode.Token.Key.GetType());
                    currentType = p.PropertyType;
                    properties.Add((p, keyNode.Token.Key));
                }
                else if (prop is AccessObjectArrayNode arrayNode)
                {
                    var p = currentType.GetProperty(arrayNode.ObjectName);
                    currentType = p.PropertyType;
                    properties.Add((p, arrayNode.Token.Index));
                }
                else if (prop is PropertyValueNode propNode)
                {
                    var p = currentType.GetProperty(propNode.Name);
                    currentType = p.PropertyType;
                    properties.Add((p, null));
                }
                else if (prop is DotNode accesPropNode)
                {
                    propsChain.Push(accesPropNode.Expression);
                }
            }

            if (properties.Count == 0)
            {
                if (isAliased)
                {
                    Nodes.Push(new DetailedAccessColumnNode(column.ColumnName, column.ColumnIndex, column.ColumnType, alias));
                }
                else
                {
                    Nodes.Push(new DetailedAccessColumnNode(column.ColumnName, column.ColumnIndex, column.ColumnType, string.Empty));
                }
            }
            else
            {
                if (isAliased)
                {
                    Nodes.Push(new AccessCallChainNode(column.ColumnName, column.ColumnType, properties.ToArray(), alias));
                }
                else
                {
                    Nodes.Push(new AccessCallChainNode(column.ColumnName, column.ColumnType, properties.ToArray(), string.Empty));
                }
            }
        }

        public virtual void Visit(AccessCallChainNode node)
        {
            Nodes.Push(new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
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
            Nodes.Push(new WhereNode(Nodes.Pop()));
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
            if (!string.IsNullOrEmpty(node.Alias))
            {
                _aliases.Add(node.Alias, (_schema, _table, _mapIndex++));
                _fromIdToAlias.Add(node.Id, node.Alias);
            }
            Nodes.Push(new SchemaFromNode(node.Schema, node.Method, node.Parameters, node.Alias));
        }

        public void Visit(NestedQueryFromNode node)
        {
            var query = Nodes.Pop() as QueryNode;

            Nodes.Push(new NestedQueryFromNode(query, node.Schema, node.Method, node.ColumnToIndexMap));
        }

        public void Visit(CteFromNode node)
        {
            Nodes.Push(new CteFromNode(node.VariableName));
        }

        public void Visit(JoinFromNode node)
        {
            var source = (FromNode) Nodes.Pop();
            var joinedTable = (FromNode) Nodes.Pop();
            var expression = Nodes.Pop();
            Nodes.Push(new JoinFromNode(source, joinedTable, expression));
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

        public void Visit(IntoGroupNode node)
        {
            Nodes.Push(new IntoGroupNode(node.Name, node.ColumnToValue));
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
            var joins = node.Joins != null ? Nodes.Pop() as JoinsNode : null;
            var from = Nodes.Pop() as FromNode;

            IntoNode outerInto;
            if (from is CteFromNode variableFrom)
            {
                if (!_tmpVariableNames.ContainsKey(variableFrom.VariableName))
                    _tmpVariableNames.Add(variableFrom.VariableName, 1);

                var newTableName = $"{variableFrom.VariableName}{_tmpVariableNames[variableFrom.VariableName]++}";

                from = new CteFromNode(variableFrom.VariableName);
                outerInto = new IntoNode(newTableName);
            }
            else
            {
                outerInto = new IntoNode(from.Schema);
            }

            if (joins != null)
            {
                var joinFrom = from;

                foreach (var currentJoin in joins.Joins)
                {
                    var alias = _fromIdToAlias[currentJoin.From.Id];
                    var table = _aliases[alias].Table;

                    joinFrom = new JoinFromNode(joinFrom, new CteFromNode(alias), currentJoin.Expression);

                    var tmpSelect = new SelectNode(table.Columns.Select(f => new FieldNode(new DetailedAccessColumnNode(f.ColumnName, f.ColumnIndex, f.ColumnType, String.Empty), f.ColumnIndex, f.ColumnName)).ToArray());

                    Nodes.Push(@from);
                    Nodes.Push(@where);
                    Nodes.Push(tmpSelect);
                    Visit(new QueryNode(tmpSelect, currentJoin.From, null, WhereNode.Empty, null, null, null, null));
                    var tmpQuery = (InternalQueryNode) Nodes.Pop();
                    tmpQuery = new InternalQueryNode(tmpQuery.Select, tmpQuery.From, tmpQuery.Where, tmpQuery.GroupBy,
                        tmpQuery.OrderBy, new IntoNode(alias), tmpQuery.ShouldBePresent, tmpQuery.Skip, tmpQuery.Take,
                        tmpQuery.ShouldLoadResultTableAsResult, tmpQuery.ResultTable, tmpQuery.UseColumnAccessInstead,
                        tmpQuery.Refresh);
                    _schemaProvider.AddTransitionSchema(new TransitionSchema(alias, CreateSchemaTable(tmpQuery)));
                    _preQueries.Add(tmpQuery);
                }

                from = joinFrom;
            }

            QueryNode query;

            if (groupBy != null)
            {
                var parameters = from.Parameters.Length == 0
                    ? "()"
                    : from.Parameters.Aggregate((a, b) => $"{a.ToString()},{b.ToString()}");
                
                var alias = $"nested.{from.Schema}.{from.Method}.{parameters}";
                var nestedFrom = from;
                var splitted = SplitBetweenAggreateAndNonAggreagate(select.Fields, groupBy.Fields, true);
                var refreshMethods = CreateRefreshMethods();
                var aggSelect = new SelectNode(ConcatAggregateFieldsWithGroupByFields(splitted[0], groupBy.Fields));
                var outSelect = new SelectNode(splitted[1]);
                var rawlySplitted = SplitBetweenAggreateAndNonAggreagate(node.Select.Fields, node.GroupBy.Fields, false);
                var rawAggSelect = new SelectNode(ConcatAggregateFieldsWithGroupByFields(rawlySplitted[0], groupBy.Fields));
                var rawAggRenamedSelect = new SelectNode(ConcatAggregateFieldsWithGroupByFields(rawlySplitted[2], groupBy.Fields));
                var groupKeys = groupBy.Fields.Select(f => f.FieldName).ToArray();
                var nestedQuery = new InternalQueryNode(aggSelect, nestedFrom, where, groupBy, null,
                    new IntoGroupNode(alias, CreateIndexToColumnMap(rawAggSelect.Fields)),
                    new ShouldBePresentInTheTable(alias, true, groupKeys), null, null, false, alias, false, refreshMethods);
                _preCreatedTables.Add(new CreateTableNode(alias, groupKeys, aggSelect.Fields));
                query = new InternalQueryNode(outSelect, new NestedQueryFromNode(nestedQuery, alias, string.Empty, CreateColumnToIndexMap(rawAggRenamedSelect.Fields)),
                    new WhereNode(new PutTrueNode()), null, null, outerInto, null, skip, take, true, from.Schema, false, null);
            }
            else
            {
                var splitted = SplitBetweenAggreateAndNonAggreagate(select.Fields, new FieldNode[0], true);

                if (IsQueryWithOnlyAggregateMethods(splitted))
                {
                    var fakeField = new FieldNode(new IntegerNode("1"), 0, String.Empty);
                    var fakeGroupBy = new GroupByNode(new []{ fakeField }, null);
                    Nodes.Push(from);
                    Nodes.Push(where);
                    Nodes.Push(select);
                    if (node.Take != null) Nodes.Push(take);
                    if (node.Skip != null) Nodes.Push(skip);
                    Nodes.Push(fakeGroupBy);
                    Visit(new QueryNode(node.Select, node.From, null, node.Where, fakeGroupBy, node.OrderBy, node.Skip, node.Take));
                    query = Nodes.Pop() as QueryNode;
                }
                else if (IsQueryWithMixedAggregateAndNonAggregateMethods(splitted))
                {
                    query = new InternalQueryNode(select, from, where, null, null, outerInto, null, skip, take, true,
                        from.Schema, false, CreateRefreshMethods());
                }
                else
                {
                    query = new InternalQueryNode(select, from, where, null, null, outerInto, null, skip, take, true,
                        from.Schema, false, null);
                }
            }

            Nodes.Push(query);
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
                    fields.Add(new FieldNode(groupField.Expression, ++nextOrder, groupField.FieldName));
                }
            }

            return fields.ToArray();
        }

        public void Visit(ExistingTableFromNode node)
        {
            Nodes.Push(new ExistingTableFromNode(node.Schema, node.Method));
        }

        public void Visit(InternalQueryNode node)
        {
            throw new NotSupportedException();
        }

        public void Visit(RootNode node)
        {
            var nodes = new List<Node>();

            foreach (var item in _preCreatedTables) nodes.Add(item);

            foreach (var queryNode in _preQueries) nodes.Add(queryNode);

            nodes.Add(Nodes.Pop());

            RootScript = new RootNode(new MultiStatementNode(nodes.ToArray(), null));
        }

        public void Visit(SingleSetNode node)
        {
            var query = (InternalQueryNode) Nodes.Pop();

            if (_ctePart == CtePart.Inner)
                _cteLastQueriesByName.Add(_currentCte, query);

            var nodes = new Node[] {new CreateTableNode(query.Into.Name, new string[0], query.Select.Fields), query};

            Nodes.Push(new MultiStatementNode(nodes, null));
        }

        public void Visit(RefreshNode node)
        {
        }

        public void Visit(UnionNode node)
        {
            TranslatedSetTreeNode translatedTree;

            var rightNode = Nodes.Pop();
            var leftNode = _setLeftNode ?? Nodes.Pop();
            if (!node.IsNested)
                Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            else
                translatedTree = (TranslatedSetTreeNode) Nodes.Peek();

            var leftQuery = leftNode as InternalQueryNode;
            var rightQuery = rightNode as InternalQueryNode;

            CreateTableNode fTable;
            if (!node.IsNested)
                fTable = new CreateTableNode($"{leftQuery.Into.Name}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields));
            else
                fTable = new CreateTableNode(
                    $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields));

            InternalQueryNode trLQuery;
            if (node.IsNested)
            {
                var columns = ChangeMethodCallsForColumnAccess(leftQuery.Select);
                var exTable =
                    new ExistingTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName,
                        string.Empty);
                trLQuery = new InternalQueryNode(columns, exTable, new WhereNode(new PutTrueNode()), null, null,
                    new IntoNode(fTable.Name), null, null, null, false, string.Empty, true, null);
            }
            else
            {
                trLQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy, null,
                    new IntoNode(fTable.Name), null, leftQuery.Skip, leftQuery.Take, false, string.Empty, false, null);
            }

            var trQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
                rightQuery.GroupBy, null, new IntoNode(fTable.Name),
                new ShouldBePresentInTheTable(fTable.Name, true, node.Keys), rightQuery.Skip, rightQuery.Take, node.IsTheLastOne, fTable.Name, false,
                null);

            var transitionTables = new List<CreateTableNode> { fTable };

            if (IsRightMostQuery(node) && _ctePart == CtePart.Inner)
                _cteLastQueriesByName.Add(_currentCte, trQuery);

            _setLeftNode = trLQuery;

            translatedTree.Nodes.Add(new TranslatedSetOperatorNode(transitionTables.ToArray(), trLQuery, trQuery, fTable.Name,
                node.Keys));
        }

        public void Visit(UnionAllNode node)
        {
            TranslatedSetTreeNode translatedTree;

            var rightNode = Nodes.Pop();
            var leftNode = _setLeftNode ?? Nodes.Pop();

            if (!node.IsNested)
                Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            else
                translatedTree = (TranslatedSetTreeNode) Nodes.Peek();

            var leftQuery = leftNode as InternalQueryNode;
            var rightQuery = rightNode as InternalQueryNode;

            CreateTableNode fTable;
            if (!node.IsNested)
                fTable = new CreateTableNode($"{leftQuery.Into.Name}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields));
            else
                fTable = new CreateTableNode(
                    $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields));

            InternalQueryNode trLQuery;
            if (node.IsNested)
            {
                var columns = ChangeMethodCallsForColumnAccess(leftQuery.Select);
                var exTable =
                    new ExistingTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName,
                        string.Empty);
                trLQuery = new InternalQueryNode(columns, exTable, new WhereNode(new PutTrueNode()), null, null,
                    new IntoNode(fTable.Name), null, null, null, false, string.Empty, true, null);
            }
            else
            {
                trLQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy, null,
                    new IntoNode(fTable.Name), null, leftQuery.Skip, leftQuery.Take, false, string.Empty, false, null);
            }

            var trQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
                rightQuery.GroupBy, null, new IntoNode(fTable.Name), null, rightQuery.Skip, rightQuery.Take, node.IsTheLastOne, fTable.Name, false, null);

            var transitionTables = new List<CreateTableNode> {fTable};

            if (IsRightMostQuery(node) && _ctePart == CtePart.Inner)
                _cteLastQueriesByName.Add(_currentCte, trQuery);

            _setLeftNode = trLQuery;

            translatedTree.Nodes.Add(new TranslatedSetOperatorNode(transitionTables.ToArray(), trLQuery, trQuery, fTable.Name,
                node.Keys));
        }

        public void Visit(ExceptNode node)
        {
            TranslatedSetTreeNode translatedTree;

            var rightNode = Nodes.Pop();
            var leftNode = _setLeftNode ?? Nodes.Pop();

            if (!node.IsNested)
                Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            else
                translatedTree = (TranslatedSetTreeNode) Nodes.Peek();


            var leftQuery = leftNode as InternalQueryNode;
            var rightQuery = rightNode as InternalQueryNode;

            CreateTableNode fTable;
            if (!node.IsNested)
                fTable = new CreateTableNode($"{leftQuery.Into.Name}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields));
            else
                fTable = new CreateTableNode(
                    $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields));

            var sTable = new CreateTableNode($"{rightQuery.Into.Name}", node.Keys,
                rightQuery.Select.Fields);

            var trLQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
                rightQuery.GroupBy, null, new IntoNode(rightQuery.Into.Name), null, rightQuery.Skip, rightQuery.Take, false, string.Empty, false, null);

            InternalQueryNode trQuery;
            if (node.IsNested)
                trQuery = new InternalQueryNode(ChangeMethodCallsForColumnAccess(leftQuery.Select),
                    new ExistingTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName,
                        string.Empty), new WhereNode(new PutTrueNode()), null, null, new IntoNode($"{fTable.Name}"),
                    new ShouldBePresentInTheTable(rightQuery.Into.Name, true, node.Keys), null, null, node.IsTheLastOne,
                    fTable.Name, true, null);
            else
                trQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy, null,
                    new IntoNode($"{fTable.Name}"),
                    new ShouldBePresentInTheTable(rightQuery.Into.Name, true, node.Keys), leftQuery.Skip, leftQuery.Take, node.IsTheLastOne,
                    fTable.Name, false, null);

            var transitionTables = new List<CreateTableNode> { fTable, sTable };

            if (IsRightMostQuery(node) && _ctePart == CtePart.Inner)
                _cteLastQueriesByName.Add(_currentCte, trQuery);

            _setLeftNode = trLQuery;

            translatedTree.Nodes.Add(new TranslatedSetOperatorNode(transitionTables.ToArray(), trLQuery, trQuery,
                fTable.Name, node.Keys));
        }

        public void Visit(IntersectNode node)
        {
            TranslatedSetTreeNode translatedTree;

            var rightNode = Nodes.Pop();
            var leftNode = _setLeftNode ?? Nodes.Pop();

            if (!node.IsNested)
                Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            else
                translatedTree = (TranslatedSetTreeNode) Nodes.Peek();


            var leftQuery = leftNode as InternalQueryNode;
            var rightQuery = rightNode as InternalQueryNode;

            CreateTableNode fTable;
            if (!node.IsNested)
                fTable = new CreateTableNode($"{leftQuery.Into.Name}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields));
            else
                fTable = new CreateTableNode(
                    $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.Into.Name}", node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields));

            var sTable = new CreateTableNode($"{rightQuery.Into.Name}", node.Keys, rightQuery.Select.Fields);

            var trLQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
                rightQuery.GroupBy, null, new IntoNode(rightQuery.Into.Name), null, rightQuery.Skip, rightQuery.Take, false, string.Empty, false, null);

            InternalQueryNode trQuery;
            if (node.IsNested)
                trQuery = new InternalQueryNode(ChangeMethodCallsForColumnAccess(leftQuery.Select),
                    new ExistingTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName,
                        string.Empty), new WhereNode(new PutTrueNode()), null, null, new IntoNode(fTable.Name),
                    new ShouldBePresentInTheTable(rightQuery.Into.Name, false, node.Keys), null, null, node.IsTheLastOne,
                    fTable.Name, true, null);
            else
                trQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy, null,
                    new IntoNode($"{fTable.Name}"),
                    new ShouldBePresentInTheTable(rightQuery.Into.Name, false, node.Keys), leftQuery.Skip, leftQuery.Take, node.IsTheLastOne,
                    fTable.Name, false, null);

            var transitionTables = new List<CreateTableNode> { fTable, sTable };

            if (IsRightMostQuery(node) && _ctePart == CtePart.Inner)
                _cteLastQueriesByName.Add(_currentCte, trQuery);

            _setLeftNode = trLQuery;

            translatedTree.Nodes.Add(new TranslatedSetOperatorNode(transitionTables.ToArray(), trLQuery, trQuery,
                fTable.Name, node.Keys));
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
                var innerQuery = (CteInnerExpressionNode) Nodes.Pop();

                var cteLatestQuery = _cteLastQueriesByName[innerQuery.Name];
                var renameTable = new RenameTableNode(cteLatestQuery.Into.Name, cteInnerExpressionNode.Name);
                
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

            foreach(var block in blocks)
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

        public void Visit(CteInnerExpressionNode node)
        {
            Nodes.Push(new CteInnerExpressionNode(Nodes.Pop(), node.Name));
        }

        public void Visit(JoinsNode node)
        {
            var joins = new JoinNode[node.Joins.Length];

            for (int i = node.Joins.Length - 1; i >= 0; i--)
            {
                joins[i] = (JoinNode) Nodes.Pop();
            }

            Nodes.Push(new JoinsNode(joins));
        }

        public void Visit(JoinNode node)
        {
            var expression = Nodes.Pop();
            var fromNode = (FromNode) Nodes.Pop();

            if (node is OuterJoinNode outerJoin)
                Nodes.Push(new OuterJoinNode(outerJoin.Type, fromNode, expression));
            else
                Nodes.Push(new InnerJoinNode(fromNode, expression));
        }

        public string[] CurrentParameters { get; private set; }

        public string CurrentSchema
        {
            get => _currentSchema;
            set
            {
                _currentSchema = value;

                if (value == null)
                    return;

                _schema = _schemaProvider.GetSchema(value);
            }
        }

        public string CurrentTable { get; private set; }

        public void SetCurrentTable(string table, string[] parameters)
        {
            CurrentTable = table;
            CurrentParameters = parameters;

            if (table == null)
                return;

            _table = _schema.GetTableByName(table, parameters);
        }

        public void AddCteSchema(string name)
        {
            var query = _cteLastQueriesByName[name];

            _schemaProvider.AddTransitionSchema(new TransitionSchema(name, CreateSchemaTable(query)));
        }

        public void SetCurrentCteName(string name)
        {
            _currentCte = name;
        }

        private ISchemaTable CreateSchemaTable(InternalQueryNode query)
        {
            return new VariableTable(query.Select.Fields.Select(field => new SchemaColumn(field.FieldName, field.FieldOrder, field.ReturnType)).Cast<ISchemaColumn>().ToArray());
        }

        private void VisitAccessMethod(AccessMethodNode node,
            Func<FunctionToken, Node, ArgsListNode, MethodInfo, AccessMethodNode> func)
        {
            var args = Nodes.Pop() as ArgsListNode;

            var groupArgs = new List<Type> {typeof(string)};
            groupArgs.AddRange(args.Args.Where((f, i) => i < args.Args.Length - 1).Select(f => f.ReturnType));

            if (!_schema.TryResolveAggreationMethod(node.Name, groupArgs.ToArray(), out var method))
                method = _schema.ResolveMethod(node.Name, args.Args.Select(f => f.ReturnType).ToArray());

            var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

            AccessMethodNode accessMethod;
            if (isAggregateMethod)
            {
                accessMethod = func(node.FToken, args, node.ExtraAggregateArguments, method);
                var identifier = accessMethod.ToString();

                var newArgs = new List<Node> {new WordNode(identifier)};
                newArgs.AddRange(args.Args.Where((f, i) => i < args.Args.Length - 1));
                var shouldHaveExtraArgs = accessMethod.ArgsCount > 0;
                var newExtraArgs = new ArgsListNode(shouldHaveExtraArgs ? new []{ accessMethod.Arguments.Args.Last() } : new Node[0]);
                
                accessMethod = func(node.FToken, new ArgsListNode(newArgs.ToArray()), newExtraArgs, method);
            }
            else
            {
                accessMethod = func(node.FToken, args, new ArgsListNode(new Node[0]), method);
            }

            node.ChangeMethod(method);

            Nodes.Push(accessMethod);
            _queryMethods.Push(accessMethod);
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
                        if(nestedFields.Select(f => f.Expression.ToString()).Contains(subNodeStr))
                            continue;

                        nestedFields.Add(new FieldNode(subNode, fieldOrder, string.Empty));
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
                    else if (subNode is UnaryNode unary && !(subNode is DotNode))
                    {
                        subNodes.Push(unary);
                    }
                }


                if (!useOuterFields)
                    continue;

                var rewriter = new RewriteFieldWithGroupMethodCall(_schemaProvider, 0, groupByFields) {CurrentSchema = CurrentSchema};
                var traverser = new RewriteTreeTraverseVisitor(rewriter);

                rewriter.SetCurrentTable(CurrentTable, CurrentParameters);
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

        private RefreshNode CreateRefreshMethods()
        {
            var methods = new List<AccessMethodNode>();

            foreach (var item in _queryMethods.Where(f => f.IsAggregateMethod))
            {
                if (item.Method.GetCustomAttribute<AggregateSetDoNotResolveAttribute>() != null)
                    continue;

                var name = $"Set{item.Method.Name}";

                var types = item.Arguments.Args
                    .Select(f => f.ReturnType)
                    .Concat(
                        item.ExtraAggregateArguments.Args.Select(f => f.ReturnType))
                    .Where(f => f != null).ToArray();

                var resolved = _schema.TryResolveAggreationMethod(name, types, out var methodInfo);
                
                if(!resolved)
                {
                    var args = types.Length == 0
                        ? string.Empty
                        : types.Select(t => t.Name).Aggregate((a, b) => a + ", " + b);

                    throw new RefreshMethodNotFoundException($"Could not found method {name}({args})");
                }

                var newArgs = new ArgsListNode(item.Arguments.Args.Concat(item.ExtraAggregateArguments.Args).ToArray());
                var newAccessMethod = new AccessRefreshAggreationScoreNode(new FunctionToken(name, TextSpan.Empty),
                    newArgs, null, methodInfo);
                if (!HasMethod(methods, newAccessMethod))
                    methods.Add(newAccessMethod);
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
                new FieldNode(new DetailedAccessColumnNode(f.FieldName, f.FieldOrder, f.ReturnType, string.Empty), f.FieldOrder,
                    f.FieldName)).ToArray();
        }

        private static SelectNode ChangeMethodCallsForColumnAccess(SelectNode select)
        {
            return new SelectNode(TurnIntoFieldColumnAccess(select.Fields));
        }
    }
}