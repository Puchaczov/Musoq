using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
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
    public class BuildMetadataAndInferTypeVisitor : ISchemaAwareExpressionVisitor
    {
        protected Stack<Node> Nodes { get; } = new Stack<Node>();
        private readonly ISchemaProvider _schemaProvider;

        private FieldNode[] _generatedColumns = new FieldNode[0];

        private string _currentSchema;
        private ISchema _schema;
        private ISchemaTable _table;

        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        private void AddAssembly(Assembly asm)
        {
            if(Assemblies.Contains(asm))
                return;

            Assemblies.Add(asm);
        }


        public RootNode Root => (RootNode)Nodes.Peek();

        public BuildMetadataAndInferTypeVisitor(ISchemaProvider schemaProvider)
        {
            _schemaProvider = schemaProvider;
        }

        public void Visit(Node node)
        {
        }

        public void Visit(DescNode node)
        {
            Nodes.Push(new DescNode((FromNode)Nodes.Pop()));
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
            AddAssembly(typeof(string).Assembly);
            Nodes.Push(new StringNode(node.Value));
        }

        public void Visit(DecimalNode node)
        {
            AddAssembly(typeof(decimal).Assembly);
            Nodes.Push(new DecimalNode(node.Value.ToString(CultureInfo.InvariantCulture)));
        }

        public void Visit(IntegerNode node)
        {
            AddAssembly(typeof(int).Assembly);
            Nodes.Push(new IntegerNode(node.Value.ToString()));
        }

        public void Visit(WordNode node)
        {
            AddAssembly(typeof(string).Assembly);
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
                if (!_schema.TryResolveProperty(node.Name, out var method))
                    throw new UnknownColumnException($"Unknown column '{node.Name}'.");

                AddAssembly(method.ReturnType.Assembly);
                node.ChangeReturnType(method.ReturnType);
                Nodes.Push(new AccessMethodNode(new FunctionToken(method.Name, TextSpan.Empty), new ArgsListNode(new Node[0]), null, method));
            }
            else
            {
                AddAssembly(column.ColumnType.Assembly);
                node.ChangeReturnType(column.ColumnType);
                Nodes.Push(new AccessColumnNode(column.ColumnName, column.ColumnIndex, column.ColumnType, node.Span));
            }
        }

        public void Visit(AllColumnsNode node)
        {
            _generatedColumns = new FieldNode[_table.Columns.Length];

            for (int i = 0; i < _table.Columns.Length; i++)
            {
                var column = _table.Columns[i];

                AddAssembly(column.ColumnType.Assembly);
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
            var exp = Nodes.Pop();
            var root = Nodes.Pop();
            Nodes.Push(new DotNode(root, exp, node.IsOuter, node.Name));
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
            Nodes.Push(new SchemaFromNode(node.Schema, node.Method, node.Parameters, CreateAliasIfEmpty(node.Alias)));
        }

        private string CreateAliasIfEmpty(string alias)
        {
            return string.IsNullOrEmpty(alias) ? new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).ToArray()).Substring(0, 4) : alias;
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
            var source = (FromNode)Nodes.Pop();
            var joinedTable = (FromNode)Nodes.Pop();
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

            Nodes.Push(new QueryNode(select, from, joins, where, groupBy, null, skip, take));
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
            Nodes.Push(new RootNode(Nodes.Pop()));
        }

        public void Visit(SingleSetNode node)
        {
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
            Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
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
            Nodes.Push(new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
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

            for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
                sets[i] = (CteInnerExpressionNode)Nodes.Pop();

            Nodes.Push(new CteExpressionNode(sets, Nodes.Pop()));
        }

        public void Visit(CteInnerExpressionNode node)
        {
            Nodes.Push(new CteInnerExpressionNode(Nodes.Pop(), node.Name));
        }

        public void Visit(JoinsNode node)
        {
            var joins = new JoinNode[node.Joins.Length];

            for (int i = node.Joins.Length - 1; i >= 0; i--)
            {
                joins[i] = (JoinNode)Nodes.Pop();
            }

            Nodes.Push(new JoinsNode(joins));
        }

        public void Visit(JoinNode node)
        {
            var expression = Nodes.Pop();
            var fromNode = (FromNode)Nodes.Pop();

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

        private void VisitAccessMethod(AccessMethodNode node,
            Func<FunctionToken, Node, ArgsListNode, MethodInfo, AccessMethodNode> func)
        {
            var args = Nodes.Pop() as ArgsListNode;

            var groupArgs = new List<Type> { typeof(string) };
            groupArgs.AddRange(args.Args.Where((f, i) => i < args.Args.Length - 1).Select(f => f.ReturnType));

            if (!_schema.TryResolveAggreationMethod(node.Name, groupArgs.ToArray(), out var method))
                method = _schema.ResolveMethod(node.Name, args.Args.Select(f => f.ReturnType).ToArray());

            var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

            AccessMethodNode accessMethod;
            if (isAggregateMethod)
            {
                accessMethod = func(node.FToken, args, node.ExtraAggregateArguments, method);
                var identifier = accessMethod.ToString();

                var newArgs = new List<Node> { new WordNode(identifier) };
                newArgs.AddRange(args.Args.Where((f, i) => i < args.Args.Length - 1));
                var shouldHaveExtraArgs = accessMethod.ArgsCount > 0;
                var newExtraArgs = new ArgsListNode(shouldHaveExtraArgs ? new[] { accessMethod.Arguments.Args.Last() } : new Node[0]);

                accessMethod = func(node.FToken, new ArgsListNode(newArgs.ToArray()), newExtraArgs, method);
            }
            else
            {
                accessMethod = func(node.FToken, args, new ArgsListNode(new Node[0]), method);
            }

            AddAssembly(method.DeclaringType.Assembly);
            AddAssembly(method.ReturnType.Assembly);

            node.ChangeMethod(method);

            Nodes.Push(accessMethod);
        }
    }
}
