using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FQL.Parser;
using FQL.Parser.Nodes;
using FQL.Parser.Tokens;
using FQL.Plugins.Attributes;
using FQL.Schema;

namespace FQL.Evaluator.Visitors
{
    public class RewriteTreeVisitor : ISchemaAwareExpressionVisitor
    {
        protected Stack<Node> Nodes { get; } = new Stack<Node>();
        private readonly List<CreateTableNode> _preCreatedTables = new List<CreateTableNode>();
        private readonly Stack<AccessMethodNode> _queryMethods = new Stack<AccessMethodNode>();
        private readonly Stack<PropertyInfo> _properties = new Stack<PropertyInfo>();
        private readonly ISchemaProvider _schemaProvider;

        private FieldNode[] _generatedColumns = new FieldNode[0];

        private string _currentSchema;

        private ISchema _schema;
        private ISchemaTable _table;

        public RewriteTreeVisitor(ISchemaProvider schemaProvider)
        {
            _schemaProvider = schemaProvider;
        }

        public RootNode RootScript { get; private set; }

        public void Visit(Node node)
        {
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
                var method = _schema.ResolveProperty(node.Name);
                node.ChangeReturnType(method.ReturnType);
                Nodes.Push(new AccessMethodNode(new FunctionToken(method.Name, TextSpan.Empty), new ArgsListNode(new Node[0]), null, method));
            }
            else
            {
                node.ChangeReturnType(column.ColumnType);
                Nodes.Push(new DetailedAccessColumnNode(column.ColumnName, column.ColumnIndex, column.ColumnType));
            }
        }

        public void Visit(AllColumnsNode node)
        {
            _generatedColumns = new FieldNode[_table.Columns.Length];

            for (int i = 0; i < _table.Columns.Length; i++)
            {
                var column = _table.Columns[i];
                switch (column.ColumnType.Name)
                {
                    case nameof(Int16):
                    case nameof(Int32):
                    case nameof(Int64):
                    case nameof(String):
                    case nameof(Decimal):
                        _generatedColumns[i] = new FieldNode(new DetailedAccessColumnNode(column.ColumnName, i, column.ColumnType), i, string.Empty);
                        break;
                    default:
                        var func = new FunctionToken("ToString", TextSpan.Empty);
                        var args = new ArgsListNode(new []{ new DetailedAccessColumnNode(column.ColumnName, i, column.ColumnType) });
                        var method = _schema.ResolveMethod(func.Value, new[] {column.ColumnType});

                        _generatedColumns[i] = new FieldNode(new AccessMethodNode(func, args, null, method), i, String.Empty);
                        break;
                }
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

        public void Visit(AccessPropertyNode node)
        {
            if(node.IsOuter == false)
                return;

            var propsChain = new Stack<Node>();
            var properties = new List<(PropertyInfo Prop, object Arg)>();

            var columnNode = node.Root as AccessColumnNode;
            var column = _table.Columns.Single(f => f.ColumnName == columnNode.Name);

            Type currentType = column.ColumnType;

            propsChain.Push(node.Expression);

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
                else if (prop is AccessPropertyNode accesPropNode)
                {
                    propsChain.Push(accesPropNode.Expression);
                }
            }

            Nodes.Push(new AccessCallChainNode(column.ColumnName, column.ColumnType, properties.ToArray()));
        }

        public void Visit(AccessCallChainNode node)
        {
            Nodes.Push(new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props));
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

        public void Visit(SchemaFromNode node)
        {
            Nodes.Push(new SchemaFromNode(node.Schema, node.Method, node.Parameters, node.Alias));
        }

        public void Visit(NestedQueryFromNode node)
        {
            var query = Nodes.Pop() as QueryNode;

            Nodes.Push(new NestedQueryFromNode(query, node.Schema, node.Method, node.ColumnToIndexMap));
        }

        public void Visit(CreateTableNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new CreateTableNode(node.Schema, node.Method, node.Parameters, node.Keys, fields,
                node.CreatedFrom));
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
            var select = Nodes.Pop() as SelectNode;
            var where = Nodes.Pop() as WhereNode;
            var from = Nodes.Pop() as FromNode;
            
            QueryNode query;

            if (Nodes.Count > 0 && Nodes.Peek() is GroupByNode groupBy)
            {
                Nodes.Pop();

                var parameters = from.Parameters.Length == 0
                    ? "()"
                    : from.Parameters.Aggregate((a, b) => $"{a.ToString()},{b.ToString()}");
                
                var alias = $"nested.{from.Schema}.{from.Method}.{parameters}";
                var nestedFrom = new SchemaFromNode(from.Schema, from.Method, from.Parameters, alias);
                var splitted = SplitBetweenAggreateAndNonAggreagate(select.Fields, groupBy.Fields, true);
                var refreshMethods = CreateRefreshMethods();
                var aggSelect = new SelectNode(ConcatAggregateFieldsWithGroupByFields(splitted[0], groupBy.Fields));
                var outSelect = new SelectNode(splitted[1]);
                var rawlySplitted = SplitBetweenAggreateAndNonAggreagate(node.Select.Fields, node.GroupBy.Fields, false);
                var rawAggSelect = new SelectNode(ConcatAggregateFieldsWithGroupByFields(rawlySplitted[0], groupBy.Fields));
                var rawAggRenamedSelect = new SelectNode(ConcatAggregateFieldsWithGroupByFields(rawlySplitted[2], groupBy.Fields));
                var groupKeys = groupBy.Fields.Select(f => f.FieldName).ToArray();
                var nestedQuery = new InternalQueryNode(aggSelect, nestedFrom, where, groupBy,
                    new IntoGroupNode(alias, CreateIndexToColumnMap(rawAggSelect.Fields)),
                    new ShouldBePresentInTheTable(alias, true, groupKeys), false, alias, false, refreshMethods);
                _preCreatedTables.Add(new CreateTableNode(alias, string.Empty, new string[0], groupKeys, aggSelect.Fields,
                    string.Empty));
                query = new InternalQueryNode(outSelect, new NestedQueryFromNode(nestedQuery, alias, string.Empty, CreateColumnToIndexMap(rawAggRenamedSelect.Fields)),
                    new WhereNode(new PutTrueNode()), null, new IntoNode(from.Schema), null, true, from.Schema, false, null);
            }
            else
            {
                var splitted = SplitBetweenAggreateAndNonAggreagate(select.Fields, new FieldNode[0], true);

                if (splitted[0].Length > 0)
                {
                    var fakeField = new FieldNode(new IntegerNode("1"), 0, String.Empty);
                    var fakeGroupBy = new GroupByNode(new []{ fakeField }, null);
                    Nodes.Push(fakeGroupBy);
                    Nodes.Push(from);
                    Nodes.Push(where);
                    Nodes.Push(select);
                    Visit(new QueryNode(node.Select, node.From, node.Where, fakeGroupBy));
                    query = Nodes.Pop() as QueryNode;
                }
                else
                {
                    query = new InternalQueryNode(select, from, where, null, new IntoNode(from.Schema), null, true,
                        from.Schema, false, null);
                }
            }

            Nodes.Push(query);
        }

        private FieldNode[] ConcatAggregateFieldsWithGroupByFields(FieldNode[] selectFields, FieldNode[] groupByFields)
        {
            var fields = new List<FieldNode>(selectFields);
            var nextOrder = selectFields.Max(f => f.FieldOrder);
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

            nodes.Add(Nodes.Pop());

            RootScript = new RootNode(new MultiStatementNode(nodes.ToArray(), null));
        }

        public void Visit(RefreshNode node)
        {
        }

        public void Visit(UnionNode node)
        {
            TranslatedSetTreeNode translatedTree;

            var rightNode = Nodes.Pop();
            var leftNode = Nodes.Pop();
            if (!node.IsNested)
                Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            else
                translatedTree = (TranslatedSetTreeNode) Nodes.Peek();

            var leftQuery = leftNode as InternalQueryNode;
            var rightQuery = rightNode as InternalQueryNode;

            CreateTableNode fTable;
            if (!node.IsNested)
                fTable = new CreateTableNode($"{leftQuery.From.Schema}{rightQuery.From.Schema}", string.Empty,
                    new string[0], node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields),
                    rightQuery.From.Schema);
            else
                fTable = new CreateTableNode(
                    $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.From.Schema}",
                    string.Empty, new string[0], node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields),
                    rightQuery.From.Schema);

            InternalQueryNode trLQuery;
            if (node.IsNested)
            {
                var columns = ChangeMethodCallsForColumnAccess(leftQuery.Select);
                var exTable =
                    new ExistingTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName,
                        string.Empty);
                trLQuery = new InternalQueryNode(columns, exTable, new WhereNode(new PutTrueNode()), null,
                    new IntoNode(fTable.Schema), null, false, string.Empty, true, null);
            }
            else
            {
                trLQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy,
                    new IntoNode(fTable.Schema), null, false, string.Empty, false, null);
            }

            var trQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
                rightQuery.GroupBy, new IntoNode(fTable.Schema),
                new ShouldBePresentInTheTable(fTable.Schema, true, node.Keys), node.IsTheLastOne, fTable.Schema, false,
                null);

            translatedTree.Nodes.Add(new TranslatedSetOperatorNode(new[] {fTable}, trLQuery, trQuery, fTable.Schema,
                node.Keys));
        }

        public void Visit(UnionAllNode node)
        {
            TranslatedSetTreeNode translatedTree;

            var rightNode = Nodes.Pop();
            var leftNode = Nodes.Pop();

            if (!node.IsNested)
                Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            else
                translatedTree = (TranslatedSetTreeNode) Nodes.Peek();

            var leftQuery = leftNode as InternalQueryNode;
            var rightQuery = rightNode as InternalQueryNode;

            CreateTableNode fTable;
            if (!node.IsNested)
                fTable = new CreateTableNode($"{leftQuery.From.Schema}{rightQuery.From.Schema}", string.Empty,
                    new string[0], node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields),
                    rightQuery.From.Schema);
            else
                fTable = new CreateTableNode(
                    $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.From.Schema}",
                    string.Empty, new string[0], node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields),
                    rightQuery.From.Schema);

            InternalQueryNode trLQuery;
            if (node.IsNested)
            {
                var columns = ChangeMethodCallsForColumnAccess(leftQuery.Select);
                var exTable =
                    new ExistingTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName,
                        string.Empty);
                trLQuery = new InternalQueryNode(columns, exTable, new WhereNode(new PutTrueNode()), null,
                    new IntoNode(fTable.Schema), null, false, string.Empty, true, null);
            }
            else
            {
                trLQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy,
                    new IntoNode(fTable.Schema), null, false, string.Empty, false, null);
            }

            var trQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
                rightQuery.GroupBy, new IntoNode(fTable.Schema), null, node.IsTheLastOne, fTable.Schema, false, null);

            translatedTree.Nodes.Add(new TranslatedSetOperatorNode(new[] {fTable}, trLQuery, trQuery, fTable.Schema,
                node.Keys));
        }

        public void Visit(ExceptNode node)
        {
            TranslatedSetTreeNode translatedTree;

            var rightNode = Nodes.Pop();
            var leftNode = Nodes.Pop();

            if (!node.IsNested)
                Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            else
                translatedTree = (TranslatedSetTreeNode) Nodes.Peek();


            var leftQuery = leftNode as InternalQueryNode;
            var rightQuery = rightNode as InternalQueryNode;

            CreateTableNode fTable;
            if (!node.IsNested)
                fTable = new CreateTableNode($"{leftQuery.From.Schema}{rightQuery.From.Schema}", string.Empty,
                    new string[0], node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields),
                    rightQuery.From.Schema);
            else
                fTable = new CreateTableNode(
                    $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.From.Schema}",
                    string.Empty, new string[0], node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields),
                    rightQuery.From.Schema);

            var sTable = new CreateTableNode($"{rightQuery.From.Schema}", string.Empty, new string[0], node.Keys,
                rightQuery.Select.Fields, rightQuery.From.Schema);

            var trLQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
                rightQuery.GroupBy, new IntoNode(rightQuery.From.Schema), null, false, string.Empty, false, null);

            InternalQueryNode trQuery;
            if (node.IsNested)
                trQuery = new InternalQueryNode(ChangeMethodCallsForColumnAccess(leftQuery.Select),
                    new ExistingTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName,
                        string.Empty), new WhereNode(new PutTrueNode()), null, new IntoNode($"{fTable.Schema}"),
                    new ShouldBePresentInTheTable(rightQuery.From.Schema, true, node.Keys), node.IsTheLastOne,
                    fTable.Schema, true, null);
            else
                trQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy,
                    new IntoNode($"{fTable.Schema}"),
                    new ShouldBePresentInTheTable(rightQuery.From.Schema, true, node.Keys), node.IsTheLastOne,
                    fTable.Schema, false, null);

            translatedTree.Nodes.Add(new TranslatedSetOperatorNode(new[] {fTable, sTable}, trLQuery, trQuery,
                fTable.Schema, node.Keys));
        }

        public void Visit(IntersectNode node)
        {
            TranslatedSetTreeNode translatedTree;

            var rightNode = Nodes.Pop();
            var leftNode = Nodes.Pop();

            if (!node.IsNested)
                Nodes.Push(translatedTree = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>()));
            else
                translatedTree = (TranslatedSetTreeNode) Nodes.Peek();


            var leftQuery = leftNode as InternalQueryNode;
            var rightQuery = rightNode as InternalQueryNode;

            CreateTableNode fTable;
            if (!node.IsNested)
                fTable = new CreateTableNode($"{leftQuery.From.Schema}{rightQuery.From.Schema}", string.Empty,
                    new string[0], node.Keys, TurnIntoFieldColumnAccess(leftQuery.Select.Fields),
                    rightQuery.From.Schema);
            else
                fTable = new CreateTableNode(
                    $"{translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName}{rightQuery.From.Schema}",
                    string.Empty, new string[0], node.Keys, TurnIntoFieldColumnAccess(rightQuery.Select.Fields),
                    rightQuery.From.Schema);

            var sTable = new CreateTableNode($"{rightQuery.From.Schema}", string.Empty, new string[0], node.Keys,
                rightQuery.Select.Fields, rightQuery.From.Schema);

            var trLQuery = new InternalQueryNode(rightQuery.Select, rightQuery.From, rightQuery.Where,
                rightQuery.GroupBy, new IntoNode(rightQuery.From.Schema), null, false, string.Empty, false, null);

            InternalQueryNode trQuery;
            if (node.IsNested)
                trQuery = new InternalQueryNode(ChangeMethodCallsForColumnAccess(leftQuery.Select),
                    new ExistingTableFromNode(translatedTree.Nodes[translatedTree.Nodes.Count - 1].ResultTableName,
                        string.Empty), new WhereNode(new PutTrueNode()), null, new IntoNode(fTable.Schema),
                    new ShouldBePresentInTheTable(rightQuery.From.Schema, false, node.Keys), node.IsTheLastOne,
                    fTable.Schema, true, null);
            else
                trQuery = new InternalQueryNode(leftQuery.Select, leftQuery.From, leftQuery.Where, leftQuery.GroupBy,
                    new IntoNode($"{fTable.Schema}"),
                    new ShouldBePresentInTheTable(rightQuery.From.Schema, false, node.Keys), node.IsTheLastOne,
                    fTable.Schema, false, null);

            translatedTree.Nodes.Add(new TranslatedSetOperatorNode(new[] {fTable, sTable}, trLQuery, trQuery,
                fTable.Schema, node.Keys));
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

        private void VisitAccessMethod(AccessMethodNode node,
            Func<FunctionToken, Node, ArgsListNode, MethodInfo, AccessMethodNode> func)
        {
            var args = Nodes.Pop() as ArgsListNode;

            var groupArgs = new List<Type>();
            groupArgs.Add(typeof(string));
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
                var newExtraArgs = new ArgsListNode(new []{ accessMethod.Arguments.Args.Last() });
                
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
                        if(nestedFields.Select(f => f.Expression.ToString()).Contains(subNode.ToString()))
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
                    else if (subNode is UnaryNode unary && !(subNode is AccessPropertyNode))
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
                if(item.Method.GetCustomAttribute<AggregateSetDoNotResolveAttribute>() != null)
                    continue;

                var name = $"Set{item.Method.Name}";

                var types = item.Arguments.Args
                    .Select(f => f.ReturnType)
                    .Concat(
                        item.ExtraAggregateArguments.Args.Select(f => f.ReturnType))
                    .Where(f => f != null).ToArray();

                var resolved = false;

                if (_schema.TryResolveAggreationMethod(name, types, out var methodInfo))
                {
                    resolved = true;
                }
                else if (_schema.TryResolveAggreationMethod(name, types, out methodInfo))
                {
                    resolved = true;
                }

                if (resolved)
                {
                    var newArgs = new ArgsListNode(item.Arguments.Args.Concat(item.ExtraAggregateArguments.Args).ToArray());
                    var newAccessMethod = new AccessRefreshAggreationScoreNode(new FunctionToken(name, TextSpan.Empty), newArgs, null, methodInfo);
                    if (!HasMethod(methods, newAccessMethod))
                        methods.Add(newAccessMethod);
                }
            }

            return new RefreshNode(methods.ToArray());
        }

        private bool HasMethod(IEnumerable<AccessMethodNode> methods, AccessMethodNode node)
        {
            return methods.Any(f => f.ToString() == node.ToString());
        }

        private static FieldNode[] TurnIntoFieldColumnAccess(FieldNode[] fields)
        {
            return fields.Select(f =>
                new FieldNode(new DetailedAccessColumnNode(f.FieldName, f.FieldOrder, f.ReturnType), f.FieldOrder,
                    f.FieldName)).ToArray();
        }

        private static SelectNode ChangeMethodCallsForColumnAccess(SelectNode select)
        {
            return new SelectNode(TurnIntoFieldColumnAccess(select.Fields));
        }
    }
}