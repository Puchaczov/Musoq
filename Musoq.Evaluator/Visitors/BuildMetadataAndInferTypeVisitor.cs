using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
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
    public class BuildMetadataAndInferTypeVisitor : IAwareExpressionVisitor
    {
        private readonly ISchemaProvider _provider;
        private readonly IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> _positionalEnvironmentVariables;
        private readonly List<AccessMethodNode> _refreshMethods = new();
        private readonly List<object> _schemaFromArgs = new();
        private readonly List<string> _generatedAliases = new();

        private readonly IDictionary<string, ISchemaTable> _explicitlyDefinedTables =
            new Dictionary<string, ISchemaTable>();

        private readonly IDictionary<string, string> _explicitlyCoupledTablesWithAliases =
            new Dictionary<string, string>();

        private readonly IDictionary<string, SchemaMethodFromNode> _explicitlyUsedAliases =
            new Dictionary<string, SchemaMethodFromNode>();

        private readonly IDictionary<SchemaFromNode, ISchemaColumn[]> _inferredColumns =
            new Dictionary<SchemaFromNode, ISchemaColumn[]>();

        private readonly IDictionary<SchemaFromNode, List<ISchemaColumn>> _usedColumns =
            new Dictionary<SchemaFromNode, List<ISchemaColumn>>();

        private readonly IDictionary<SchemaFromNode, WhereNode> _usedWhereNodes =
            new Dictionary<SchemaFromNode, WhereNode>();

        private readonly List<FieldNode> _groupByFields = new();
        private readonly List<Type> _nullSuspiciousTypes;
        private readonly IReadOnlyDictionary<string, string[]> _columns;

        private int _setKey;
        private int _schemaFromKey;
        private uint _positionalEnvironmentVariablesKey;
        private Scope _currentScope;
        private FieldNode[] _generatedColumns = Array.Empty<FieldNode>();
        private string _identifier;
        private string _queryAlias;
        private IdentifierNode _theMostInnerIdentifier;

        private Stack<string> Methods { get; } = new();

        public BuildMetadataAndInferTypeVisitor(
            ISchemaProvider provider,
            IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables,
            IReadOnlyDictionary<string, string[]> columns)
        {
            _provider = provider;
            _positionalEnvironmentVariables = positionalEnvironmentVariables;
            _columns = columns;
            _positionalEnvironmentVariablesKey = 0;
            _nullSuspiciousTypes = new List<Type>();
            
        }

        protected Stack<Node> Nodes { get; } = new();

        public List<Assembly> Assemblies { get; } = new();

        public IDictionary<string, int[]> SetOperatorFieldPositions { get; } = new Dictionary<string, int[]>();


        public IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> InferredColumns
        {
            get
            {
                var result = new Dictionary<SchemaFromNode, ISchemaColumn[]>();

                foreach (var aliasColumnsPair in _inferredColumns)
                {
                    result.Add(aliasColumnsPair.Key, aliasColumnsPair.Value.ToArray());
                }

                return result;
            }
        }

        public IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> UsedColumns
        {
            get
            {
                var result = new Dictionary<SchemaFromNode, ISchemaColumn[]>();

                foreach (var aliasColumnsPair in _usedColumns)
                {
                    result.Add(aliasColumnsPair.Key, aliasColumnsPair.Value.ToArray());
                }

                return result;
            }
        }

        public IReadOnlyDictionary<SchemaFromNode, WhereNode> UsedWhereNodes
        {
            get
            {
                return _usedWhereNodes.ToDictionary(aliasColumnsPair => aliasColumnsPair.Key,
                    aliasColumnsPair => aliasColumnsPair.Value);
            }
        }

        public RootNode Root => (RootNode) Nodes.Peek();

        public void Visit(Node node)
        {
        }

        public void Visit(DescNode node)
        {
            Nodes.Push(new DescNode((FromNode) Nodes.Pop(), node.Type));
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

        public void Visit(RLikeNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new RLikeNode(left, right));
        }

        public void Visit(InNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new InNode(left, (ArgsListNode) right));
        }

        public virtual void Visit(FieldNode node)
        {
            Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName));
        }

        public void Visit(FieldOrderedNode node)
        {
            Nodes.Push(new FieldOrderedNode(Nodes.Pop(), node.FieldOrder, node.FieldName, node.Order));
        }

        public void Visit(SelectNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new SelectNode(fields.ToArray()));
        }

        public void Visit(GroupSelectNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new GroupSelectNode(fields.ToArray()));
        }

        public void Visit(StringNode node)
        {
            AddAssembly(typeof(string).Assembly);
            Nodes.Push(new StringNode(node.Value));
            _schemaFromArgs.Add(node.Value);
        }

        public void Visit(DecimalNode node)
        {
            AddAssembly(typeof(decimal).Assembly);
            Nodes.Push(new DecimalNode(node.Value));
            _schemaFromArgs.Add(node.Value);
        }

        public void Visit(IntegerNode node)
        {
            AddAssembly(typeof(int).Assembly);
            Nodes.Push(new IntegerNode(node.ObjValue));
            _schemaFromArgs.Add(node.ObjValue);
        }

        public void Visit(BooleanNode node)
        {
            AddAssembly(typeof(bool).Assembly);
            Nodes.Push(new BooleanNode(node.Value));
            _schemaFromArgs.Add(node.Value);
        }

        public void Visit(WordNode node)
        {
            AddAssembly(typeof(string).Assembly);
            Nodes.Push(new WordNode(node.Value));
            _schemaFromArgs.Add(node.Value);
        }

        public void Visit(NullNode node)
        {
            Nodes.Push(new NullNode(node.ReturnType));
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
                (token, node1, exargs, arg3, alias, canSkipInjectSource) =>
                    new AccessMethodNode(token, node1 as ArgsListNode, exargs, canSkipInjectSource, arg3, alias));
        }

        public void Visit(AccessRawIdentifierNode node)
        {
            Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
        }

        public void Visit(IsNullNode node)
        {
            Nodes.Push(new IsNullNode(Nodes.Pop(), node.IsNegated));
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            VisitAccessMethod(node,
                (token, node1, exargs, arg3, alias, canSkipInjectSource) =>
                    new AccessRefreshAggreationScoreNode(token, node1 as ArgsListNode, exargs, node.CanSkipInjectSource,
                        arg3, alias));
        }

        public void Visit(AccessColumnNode node)
        {
            var hasProcessedQueryId = _currentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
            var identifier = hasProcessedQueryId
                ? _currentScope[MetaAttributes.ProcessedQueryId]
                : _identifier;

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);

            var tuple = !string.IsNullOrEmpty(node.Alias)
                ? tableSymbol.GetTableByAlias(node.Alias)
                : tableSymbol.GetTableByColumnName(node.Name);

            ISchemaColumn column;
            try
            {
                column = tuple.Table.GetColumnByName(node.Name);
            }
            catch (InvalidOperationException)
            {
                column = null;
            }

            if (column == null)
            {
                PrepareAndThrowUnknownColumnExceptionMessage(node.Name, tuple.Table.Columns);
                return;
            }

            AddAssembly(column.ColumnType.Assembly);
            node.ChangeReturnType(column.ColumnType);

            var columns = _usedColumns
                .Where(c => c.Key.Alias == tuple.TableName && c.Key.QueryId == _schemaFromKey)
                .Select(f => f.Value)
                .FirstOrDefault();

            if (columns is not null)
            {
                if (columns.All(c => c.ColumnName != column.ColumnName))
                {
                    columns.Add(column);
                }
            }

            var accessColumn = new AccessColumnNode(column.ColumnName, tuple.TableName, column.ColumnType, node.Span);

            Nodes.Push(accessColumn);
        }

        public void Visit(AllColumnsNode node)
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
            var tuple = tableSymbol.GetTableByAlias(_identifier);
            var table = tuple.Table;
            _generatedColumns = new FieldNode[table.Columns.Length];

            for (var i = 0; i < table.Columns.Length; i++)
            {
                var column = table.Columns[i];

                AddAssembly(column.ColumnType.Assembly);
                _generatedColumns[i] =
                    new FieldNode(
                        new AccessColumnNode(
                            column.ColumnName,
                            _identifier,
                            column.ColumnType,
                            TextSpan.Empty
                        ),
                        i,
                        tableSymbol.HasAlias ? _identifier : column.ColumnName);
            }

            Nodes.Push(node);
        }

        public void Visit(IdentifierNode node)
        {
            if (node.Name != _identifier && _queryPart != QueryPart.From)
            {
                var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
                var column = tableSymbol.GetColumnByAliasAndName(_identifier, node.Name);

                if (column == null)
                    PrepareAndThrowUnknownColumnExceptionMessage(node.Name, tableSymbol.GetColumns());

                Visit(new AccessColumnNode(node.Name, string.Empty, column?.ColumnType, TextSpan.Empty));
                return;
            }

            Nodes.Push(new IdentifierNode(node.Name));
        }

        public void Visit(AccessObjectArrayNode node)
        {
            var parentNode = Nodes.Peek();
            var parentNodeType = Nodes.Peek().ReturnType;
            if (parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
            {
                var typeHintingAttributes = 
                    parentNodeType.GetCustomAttributes<DynamicObjectPropertyTypeHintAttribute>().ToArray();

                foreach (var t in typeHintingAttributes)
                {
                    if (t.Name != node.Name) continue;
                    
                    Nodes.Push(new AccessObjectArrayNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, t.Type)));
                    return;
                }
                
                var defaultTypeHintingAttributes = 
                    parentNodeType.GetCustomAttribute<DynamicObjectPropertyDefaultTypeHintAttribute>();

                if (defaultTypeHintingAttributes is not null)
                {
                    Nodes.Push(new AccessObjectArrayNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                    return;
                }
                
                var type = parentNode.ReturnType.GetProperty(node.Name)?.PropertyType ?? (_theMostInnerIdentifier.Name == node.Name ? typeof(object[]) : typeof(ExpandoObject[]));
                Nodes.Push(
                    new AccessObjectArrayNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, type)));
            }
            else
            {
                var isNotRoot = parentNode is not AccessColumnNode;
                bool isArray;
                bool isIndexer;

                if (isNotRoot)
                {
                    var propertyAccess = parentNodeType.GetProperty(node.Name);

                    isArray = propertyAccess?.PropertyType.IsArray == true;
                    isIndexer = HasIndexer(propertyAccess?.PropertyType);

                    if (!isArray && !isIndexer)
                    {
                        throw new ObjectIsNotAnArrayException(
                            $"Object {parentNodeType.Name} is not an array.");
                    }

                    Nodes.Push(new AccessObjectArrayNode(node.Token, propertyAccess));

                    return;
                }

                var property = parentNodeType.GetProperty(node.Name);

                isArray = property?.PropertyType.IsArray == true;
                isIndexer = HasIndexer(property?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    throw new ObjectIsNotAnArrayException(
                        $"Object {node.Name} is not an array.");
                }

                Nodes.Push(new AccessObjectArrayNode(node.Token, property));
            }
        }

        public void Visit(AccessObjectKeyNode node)
        {
            if (node.DestinationKind == AccessObjectKeyNode.Destination.Variable)
            {
                throw new ConstructionNotYetSupported($"Construction ${node.ToString()} is not yet supported.");
            }
            
            var parentNode = Nodes.Peek();
            var parentNodeType = parentNode.ReturnType;
            if (parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
            {
                var typeHintingAttributes = 
                    parentNodeType.GetCustomAttributes<DynamicObjectPropertyTypeHintAttribute>().ToArray();

                foreach (var t in typeHintingAttributes)
                {
                    if (t.Name != node.Name) continue;
                    
                    Nodes.Push(new AccessObjectKeyNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, t.Type)));
                    return;
                }
                
                var defaultTypeHintingAttributes = 
                    parentNodeType.GetCustomAttribute<DynamicObjectPropertyDefaultTypeHintAttribute>();

                if (defaultTypeHintingAttributes is not null)
                {
                    Nodes.Push(new AccessObjectKeyNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                    return;
                }
                
                var type = parentNode.ReturnType.GetProperty(node.Name)?.PropertyType ?? (_theMostInnerIdentifier.Name == node.Name ? typeof(object) : typeof(ExpandoObject));
                Nodes.Push(
                    new AccessObjectKeyNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, type)));
            }
            else
            {
                var isRoot = parentNode is AccessColumnNode;
                bool isIndexer;

                if (!isRoot)
                {
                    var propertyAccess = parentNodeType.GetProperty(node.Name);
                    isIndexer = HasIndexer(propertyAccess?.PropertyType);

                    if (!isIndexer)
                    {
                        throw new ObjectDoesNotImplementIndexerException(
                            $"Object {parentNodeType.Name} does not implement indexer.");
                    }

                    Nodes.Push(new AccessObjectKeyNode(node.Token, propertyAccess));

                    return;
                }
                
                var property = parentNodeType.GetProperty(node.Name);
                    
                isIndexer = HasIndexer(property?.PropertyType);
                
                if (!isIndexer)
                {
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Object {node.Name} does not implement indexer.");
                }
                
                Nodes.Push(new AccessObjectKeyNode(node.Token, property));
            }
        }

        public void Visit(PropertyValueNode node)
        {
            var parentNode = Nodes.Peek();
            var parentNodeType = parentNode.ReturnType;
            if (parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
            {
                var typeHintingAttributes = 
                    parentNodeType.GetCustomAttributes<DynamicObjectPropertyTypeHintAttribute>().ToArray();

                foreach (var t in typeHintingAttributes)
                {
                    if (t.Name != node.Name) continue;
                    
                    Nodes.Push(new PropertyValueNode(node.Name, new ExpandoObjectPropertyInfo(node.Name, t.Type)));
                    return;
                }
                
                var defaultTypeHintingAttributes = 
                    parentNodeType.GetCustomAttribute<DynamicObjectPropertyDefaultTypeHintAttribute>();

                if (defaultTypeHintingAttributes is not null)
                {
                    Nodes.Push(new PropertyValueNode(node.Name, new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                    return;
                }
                
                var type = parentNode.ReturnType.GetProperty(node.Name)?.PropertyType ?? (_theMostInnerIdentifier.Name == node.Name ? typeof(object) : typeof(ExpandoObject));
                Nodes.Push(new PropertyValueNode(node.Name, new ExpandoObjectPropertyInfo(node.Name, type)));
            }
            else
            {
                Nodes.Push(new PropertyValueNode(node.Name, parentNodeType.GetProperty(node.Name)));
            }
        }

        public void Visit(DotNode node)
        {
            var exp = Nodes.Pop();
            var root = Nodes.Pop();
            
            DotNode newNode;
            if (root.ReturnType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
            {
                newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
            }
            else
            {
                if (exp is not IdentifierNode identifierNode)
                {
                    throw new NotSupportedException();
                }

                var hasProperty = root.ReturnType.GetProperty(identifierNode.Name) != null;

                if (!hasProperty)
                {
                    PrepareAndThrowUnknownPropertyExceptionMessage(identifierNode.Name,
                        root.ReturnType.GetProperties());
                }

                newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
            }

            Nodes.Push(newNode);
        }

        public virtual void Visit(AccessCallChainNode node)
        {
            var chainPretend = Nodes.Pop();

            Nodes.Push(chainPretend is AccessColumnNode
                ? chainPretend
                : new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
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
            var hasProcessedQueryId = _currentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
            var identifier = hasProcessedQueryId
                ? _currentScope[MetaAttributes.ProcessedQueryId]
                : _identifier;

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);
            var rewrittenWhereNode = new WhereNode(Nodes.Pop());

            var usedIdentifiers = _usedWhereNodes
                .Where(f => f.Key.QueryId == _schemaFromKey)
                .Select(f => f.Key)
                .ToArray();

            foreach (var aliasSchemaPair in tableSymbol.CompoundTables.Join(usedIdentifiers, t => t, f => f.Alias,
                         (t, f) => (Alias: t, Schema: f)))
            {
                _usedWhereNodes[aliasSchemaPair.Schema] = rewrittenWhereNode;
            }

            Nodes.Push(rewrittenWhereNode);
        }

        public void Visit(GroupByNode node)
        {
            var having = Nodes.Peek() as HavingNode;

            if (having != null)
                Nodes.Pop();

            var fields = new FieldNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
            {
                var field = Nodes.Pop() as FieldNode;
                _groupByFields.Insert(0, field);
                fields[i] = field;
            }

            Nodes.Push(new GroupByNode(fields, having));
        }

        public void Visit(HavingNode node)
        {
            Nodes.Push(new HavingNode(Nodes.Pop()));
        }

        public void Visit(SkipNode node)
        {
            Nodes.Push(new SkipNode((IntegerNode) node.Expression));
        }

        public void Visit(TakeNode node)
        {
            Nodes.Push(new TakeNode((IntegerNode) node.Expression));
        }

        private static readonly WhereNode AllTrueWhereNode =
            new(new EqualityNode(new IntegerNode("1", "s"), new IntegerNode("1", "s")));

        private QueryPart _queryPart;

        public void Visit(SchemaFromNode node)
        {
            var schema = _provider.GetSchema(node.Schema);

            _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());
            _generatedAliases.Add(_queryAlias);
 
            var table = _currentScope.Name != "Desc"
                ? schema.GetTableByName(
                    node.Method, 
                    new RuntimeContext(
                        CancellationToken.None,
                        _columns[_queryAlias + _schemaFromKey].Select((f, i) => new SchemaColumn(f, i, typeof(object))).ToArray(),
                        _positionalEnvironmentVariables.ContainsKey(_positionalEnvironmentVariablesKey)
                            ? _positionalEnvironmentVariables[_positionalEnvironmentVariablesKey]
                            : new Dictionary<string, string>(),
                        (node, Array.Empty<ISchemaColumn>(), AllTrueWhereNode)
                    ), 
                    _schemaFromArgs.ToArray())
                : new DynamicTable(Array.Empty<ISchemaColumn>());

            _positionalEnvironmentVariablesKey += 1;
            _schemaFromArgs.Clear();

            AddAssembly(schema.GetType().Assembly);

            var tableSymbol = new TableSymbol(_queryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
            _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);
            _currentScope[node.Id] = _queryAlias;

            var aliasedSchemaFromNode = new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode) Nodes.Pop(),
                _queryAlias, node.QueryId);

            if (!_inferredColumns.ContainsKey(aliasedSchemaFromNode))
                _inferredColumns.Add(aliasedSchemaFromNode, table.Columns);

            if (!_usedColumns.ContainsKey(aliasedSchemaFromNode))
                _usedColumns.Add(aliasedSchemaFromNode, new List<ISchemaColumn>());

            _usedWhereNodes.TryAdd(aliasedSchemaFromNode, AllTrueWhereNode);

            Nodes.Push(aliasedSchemaFromNode);
        }

        public void Visit(SchemaMethodFromNode node)
        {
            Nodes.Push(new Parser.SchemaMethodFromNode(node.Schema, node.Method));
        }

        public void Visit(AliasedFromNode node)
        {
            var schemaInfo = _explicitlyUsedAliases[node.Identifier];
            var tableName = _explicitlyCoupledTablesWithAliases[node.Identifier];
            var table = _explicitlyDefinedTables[tableName];

            var schema = _provider.GetSchema(schemaInfo.Schema);

            AddAssembly(schema.GetType().Assembly);

            _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());
            _generatedAliases.Add(_queryAlias);

            var aliasedSchemaFromNode = new Parser.SchemaFromNode(schemaInfo.Schema, schemaInfo.Method, (ArgsListNode) Nodes.Pop(),
                _queryAlias, node.InSourcePosition);

            var tableSymbol = new TableSymbol(
                _queryAlias, 
                schema, 
                schema.GetTableByName(
                    schemaInfo.Method,
                    new RuntimeContext(
                        CancellationToken.None,
                        table.Columns,
                        _positionalEnvironmentVariables.ContainsKey(_positionalEnvironmentVariablesKey)
                            ? _positionalEnvironmentVariables[_positionalEnvironmentVariablesKey]
                            : new Dictionary<string, string>(),
                        (aliasedSchemaFromNode, Array.Empty<ISchemaColumn>(), AllTrueWhereNode)
                    ),
                    _schemaFromArgs.ToArray()
                ) ?? table, 
                !string.IsNullOrEmpty(node.Alias)
            );
            
            _schemaFromArgs.Clear();
            
            _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);
            _currentScope[node.Id] = _queryAlias;

            if (!_inferredColumns.ContainsKey(aliasedSchemaFromNode))
                _inferredColumns.Add(aliasedSchemaFromNode, table.Columns);

            if (!_usedColumns.ContainsKey(aliasedSchemaFromNode))
                _usedColumns.Add(aliasedSchemaFromNode, new List<ISchemaColumn>());

            _usedWhereNodes.TryAdd(aliasedSchemaFromNode, AllTrueWhereNode);

            Nodes.Push(aliasedSchemaFromNode);
        }

        public void Visit(JoinSourcesTableFromNode node)
        {
            var exp = Nodes.Pop();
            var b = (FromNode) Nodes.Pop();
            var a = (FromNode) Nodes.Pop();

            Nodes.Push(new Parser.JoinSourcesTableFromNode(a, b, exp, node.JoinType));
        }

        public void Visit(InMemoryTableFromNode node)
        {
            _queryAlias = string.IsNullOrEmpty(node.Alias) ? node.VariableName : node.Alias;
            _generatedAliases.Add(_queryAlias);

            TableSymbol tableSymbol;

            if (_currentScope.Parent.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(node.VariableName))
            {
                tableSymbol = _currentScope.Parent.ScopeSymbolTable.GetSymbol<TableSymbol>(node.VariableName);
            }
            else
            {
                var scope = _currentScope;
                
                while (scope != null && scope.Name != "CTE") scope = scope.Parent;
                
                if (scope is null)
                    throw new NotSupportedException($"Table {node.VariableName} is not defined.");

                tableSymbol = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(node.VariableName);
            }

            var tableSchemaPair = tableSymbol.GetTableByAlias(node.VariableName);
            _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias,
                new TableSymbol(_queryAlias, tableSchemaPair.Schema, tableSchemaPair.Table, node.Alias == _queryAlias));
            _currentScope[node.Id] = _queryAlias;

            Nodes.Push(new Parser.InMemoryTableFromNode(node.VariableName, _queryAlias));
        }

        public void Visit(JoinFromNode node)
        {
            var expression = Nodes.Pop();
            var joinedTable = (FromNode) Nodes.Pop();
            var source = (FromNode) Nodes.Pop();
            var joinedFrom = new Parser.JoinFromNode(source, joinedTable, expression, node.JoinType);
            _identifier = joinedFrom.Alias;
            _schemaFromArgs.Clear();
            Nodes.Push(joinedFrom);
        }

        public void Visit(ExpressionFromNode node)
        {
            var from = (FromNode) Nodes.Pop();
            _identifier = from.Alias;
            Nodes.Push(new Parser.ExpressionFromNode(from));

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);

            foreach (var tableAlias in tableSymbol.CompoundTables)
            {
                var tuple = tableSymbol.GetTableByAlias(tableAlias);

                foreach (var column in tuple.Table.Columns)
                    AddAssembly(column.ColumnType.Assembly);
            }
        }

        public void Visit(CreateTransformationTableNode node)
        {
            var fields = CreateFields(node.Fields);

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
            var take = node.Take != null ? Nodes.Pop() as TakeNode : null;
            var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
            var select = Nodes.Pop() as SelectNode;
            var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;
            var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
            var from = Nodes.Pop() as FromNode;

            if (groupBy == null && _refreshMethods.Count > 0)
            {
                groupBy = new GroupByNode(
                    new[] {new FieldNode(new IntegerNode("1", "s"), 0, string.Empty)}, null);
            }

            _currentScope.ScopeSymbolTable.AddSymbol(from.Alias.ToRefreshMethodsSymbolName(),
                new RefreshMethodsSymbol(_refreshMethods));
            _refreshMethods.Clear();

            if (_currentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(string.Empty))
                _currentScope.ScopeSymbolTable.MoveSymbol(string.Empty, from.Alias);

            Methods.Push(from.Alias);
            Nodes.Push(new QueryNode(select, from, where, groupBy, orderBy, skip, take));

            _schemaFromArgs.Clear();
        }

        public void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            var exp = Nodes.Pop();
            var from = (FromNode) Nodes.Pop();
            Nodes.Push(
                new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType));
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
            var key = CreateSetOperatorPositionKey();
            _currentScope[MetaAttributes.SetOperatorName] = key;
            SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode) node.Left, node.Keys));

            var right = Nodes.Pop();
            var left = Nodes.Pop();

            var rightMethodName = Methods.Pop();
            var leftMethodName = Methods.Pop();

            var methodName = $"{leftMethodName}_Union_{rightMethodName}";
            Methods.Push(methodName);
            _currentScope.ScopeSymbolTable.AddSymbol(methodName,
                _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode) left).From.Alias));

            Nodes.Push(new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public void Visit(UnionAllNode node)
        {
            var key = CreateSetOperatorPositionKey();
            _currentScope[MetaAttributes.SetOperatorName] = key;
            SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode) node.Left, node.Keys));

            var right = Nodes.Pop();
            var left = Nodes.Pop();

            var rightMethodName = Methods.Pop();
            var leftMethodName = Methods.Pop();

            var methodName = $"{leftMethodName}_UnionAll_{rightMethodName}";
            Methods.Push(methodName);
            _currentScope.ScopeSymbolTable.AddSymbol(methodName,
                _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode) left).From.Alias));

            Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
                node.IsTheLastOne));
        }

        public void Visit(ExceptNode node)
        {
            var key = CreateSetOperatorPositionKey();
            _currentScope[MetaAttributes.SetOperatorName] = key;
            SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode) node.Left, node.Keys));

            var right = Nodes.Pop();
            var left = Nodes.Pop();

            var rightMethodName = Methods.Pop();
            var leftMethodName = Methods.Pop();

            var methodName = $"{leftMethodName}_Except_{rightMethodName}";
            Methods.Push(methodName);
            _currentScope.ScopeSymbolTable.AddSymbol(methodName,
                _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode) left).From.Alias));

            Nodes.Push(new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public void Visit(IntersectNode node)
        {
            var key = CreateSetOperatorPositionKey();
            _currentScope[MetaAttributes.SetOperatorName] = key;
            SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode) node.Left, node.Keys));

            var right = Nodes.Pop();
            var left = Nodes.Pop();

            var rightMethodName = Methods.Pop();
            var leftMethodName = Methods.Pop();

            var methodName = $"{leftMethodName}_Intersect_{rightMethodName}";
            Methods.Push(methodName);
            _currentScope.ScopeSymbolTable.AddSymbol(methodName,
                _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode) left).From.Alias));

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
                sets[i] = (CteInnerExpressionNode) Nodes.Pop();

            Nodes.Push(new CteExpressionNode(sets, set));
        }

        public void Visit(CteInnerExpressionNode node)
        {
            var set = Nodes.Pop();

            var collector = new GetSelectFieldsVisitor();
            var traverser = new GetSelectFieldsTraverseVisitor(collector);

            set.Accept(traverser);

            var table = new VariableTable(collector.CollectedFieldNames);
            _currentScope.Parent.ScopeSymbolTable.AddSymbol(node.Name,
                new TableSymbol(node.Name, new TransitionSchema(node.Name, table), table, false));

            Nodes.Push(new CteInnerExpressionNode(set, node.Name));
        }

        public void Visit(JoinsNode node)
        {
            _identifier = node.Alias;
            Nodes.Push(new Parser.JoinsNode((Parser.JoinFromNode) Nodes.Pop()));
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

        public void SetScope(Scope scope)
        {
            _currentScope = scope;
        }

        private void AddAssembly(Assembly asm)
        {
            if (Assemblies.Contains(asm))
                return;

            Assemblies.Add(asm);
        }

        private void AddBaseTypeAssembly(Type entityType)
        {
            if (entityType.BaseType == null)
                return;

            AddAssembly(entityType.BaseType.Assembly);
            AddBaseTypeAssembly(entityType.BaseType);
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
                    fields.AddRange(_generatedColumns.Select(column =>
                        new FieldNode(column.Expression, p++, column.FieldName)));
                    continue;
                }

                fields.Add(new FieldNode(field.Expression, p++, field.FieldName));
            }

            return fields.ToArray();
        }

        private void VisitAccessMethod(
            AccessMethodNode node,
            Func<FunctionToken, Node, ArgsListNode, MethodInfo, string, bool, AccessMethodNode> func)
        {
            var args = Nodes.Pop() as ArgsListNode;
            
            if (args is null)
                throw new NotSupportedException($"Cannot resolve method {node.Name}. Arguments are null.");

            var groupArgs = new List<Type> {typeof(string)};
            groupArgs.AddRange(args.Args.Skip(1).Select(f => f.ReturnType));

            var alias = !string.IsNullOrEmpty(node.Alias) ? node.Alias : _identifier;

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(alias);
            var schemaTablePair = tableSymbol.GetTableByAlias(alias);
            var entityType = schemaTablePair.Table.Metadata.TableEntityType;
            
            AddAssembly(entityType.Assembly);
            AddBaseTypeAssembly(entityType);
            
            var canSkipInjectSource = false;
            if (!schemaTablePair.Schema.TryResolveAggregationMethod(node.Name, groupArgs.ToArray(), entityType,
                    out var method))
            {
                if (!schemaTablePair.Schema.TryResolveMethod(node.Name, args.Args.Select(f => f.ReturnType).ToArray(),
                        entityType, out method))
                {
                    if (!schemaTablePair.Schema.TryResolveRawMethod(node.Name,
                            args.Args.Select(f => f.ReturnType).ToArray(), out method))
                    {
                        var types = args.Args.Length > 0
                            ? args.Args.Select(f => f.ReturnType.ToString()).Aggregate((a, b) => a + ", " + b)
                            : string.Empty;

                        throw new UnresolvableMethodException($"{node.Name}({types}) cannot be resolved.");
                    }

                    canSkipInjectSource = true;
                }
            }

            var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

            if (!isAggregateMethod && method.IsGenericMethod && TryReduceDimensions(method, args, out var reducedMethod))
            {
                method = reducedMethod;
            }

            if (!isAggregateMethod && method.IsGenericMethod && !method.IsConstructedGenericMethod &&
                TryConstructGenericMethod(method, args, out var constructedMethod))
            {
                method = constructedMethod;
            }

            AccessMethodNode accessMethod;
            if (isAggregateMethod)
            {
                accessMethod = func(node.FToken, args, node.ExtraAggregateArguments, method, alias, false);
                var identifier = accessMethod.ToString();

                var newArgs = new List<Node> {new WordNode(identifier)};
                newArgs.AddRange(args.Args.Skip(1));

                var newSetArgs = new List<Node> {new WordNode(identifier)};
                newSetArgs.AddRange(args.Args);

                var setMethodName = $"Set{method.Name}";
                var argTypes = newSetArgs.Select(f => f.ReturnType).ToArray();

                if (!schemaTablePair.Schema.TryResolveAggregationMethod(
                        setMethodName,
                        argTypes,
                        entityType,
                        out var setMethod))
                {
                    var names = argTypes.Length == 0
                        ? string.Empty
                        : argTypes.Select(arg => arg.Name).Aggregate((a, b) => a + ", " + b);

                    throw new NotSupportedException($"Cannot resolve method {setMethodName} with parameters {names}");
                }

                if (setMethod.IsGenericMethodDefinition)
                {
                    var setParams = setMethod.GetParameters();
                    var genericArguments = setMethod.GetGenericArguments();
                    var genericArgumentsDistinct = new List<Type>();

                    foreach (var genericArgument in genericArguments)
                    {
                        for (int i = 0; i < setParams.Length; i++)
                        {
                            var setParam = setParams[i];

                            if (setParam.ParameterType == genericArgument)
                            {
                                genericArgumentsDistinct.Add(newSetArgs.Where((arg, index) => index == i - 1).Single()
                                    .ReturnType);
                            }
                        }
                    }

                    var genericArgumentsConcreteTypes = genericArgumentsDistinct.Distinct().ToArray();

                    method = method.MakeGenericMethod(genericArgumentsConcreteTypes);
                    setMethod = setMethod.MakeGenericMethod(genericArgumentsConcreteTypes);
                }

                var setMethodNode = func(new FunctionToken(setMethodName, TextSpan.Empty),
                    new ArgsListNode(newSetArgs.ToArray()), null, setMethod,
                    alias, false);

                _refreshMethods.Add(setMethodNode);

                accessMethod = func(node.FToken, new ArgsListNode(newArgs.ToArray()), null, method, alias,
                    canSkipInjectSource);
            }
            else
            {
                accessMethod = func(node.FToken, args, new ArgsListNode(Array.Empty<Node>()), method, alias,
                    canSkipInjectSource);
            }

            if (method.DeclaringType == null)
                throw new InvalidOperationException("Method must have a declaring type.");

            AddAssembly(method.DeclaringType.Assembly);
            AddAssembly(method.ReturnType.Assembly);

            node.ChangeMethod(method);

            Nodes.Push(accessMethod);
        }

        public void Visit(OrderByNode node)
        {
            var fields = new FieldOrderedNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
                fields[i] = (FieldOrderedNode) Nodes.Pop();

            Nodes.Push(new OrderByNode(fields));
        }

        public void Visit(CreateTableNode node)
        {
            var columns = new List<ISchemaColumn>();

            for (var i = 0; i < node.TableTypePairs.Length; i++)
            {
                var (columnName, typeName) = node.TableTypePairs[i];

                var remappedType = EvaluationHelper.RemapPrimitiveTypes(typeName);

                var type = EvaluationHelper.RemapPrimitiveTypeAsNullable(remappedType);

                if (type == null)
                    throw new TypeNotFoundException($"Type '{remappedType}' could not be found.");

                columns.Add(new SchemaColumn(columnName, i, type));
            }

            var table = new DynamicTable(columns.ToArray());
            _explicitlyDefinedTables.Add(node.Name, table);

            Nodes.Push(new CreateTableNode(node.Name, node.TableTypePairs));
        }

        public void Visit(CoupleNode node)
        {
            _explicitlyCoupledTablesWithAliases.Add(node.MappedSchemaName, node.TableName);
            _explicitlyUsedAliases.Add(node.MappedSchemaName, node.SchemaMethodNode);
            Nodes.Push(new CoupleNode(node.SchemaMethodNode, node.TableName, node.MappedSchemaName));
        }

        public void Visit(StatementsArrayNode node)
        {
            var statements = new StatementNode[node.Statements.Length];
            for (var i = 0; i < node.Statements.Length; ++i)
            {
                statements[node.Statements.Length - 1 - i] = (StatementNode) Nodes.Pop();
            }

            Nodes.Push(new StatementsArrayNode(statements));
        }

        public void Visit(StatementNode node)
        {
            Nodes.Push(new StatementNode(Nodes.Pop()));
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

            if (_nullSuspiciousTypes.All(type => type != NullNode.NullType.Instance))
            {
                var anyWasNullable = _nullSuspiciousTypes.Any(type => type.GetUnderlyingNullable() != null);
                var greatestCommonSubtype = FindGreatestCommonSubtype();
                var caseNode = anyWasNullable
                    ? new CaseNode(whenThenPairs.ToArray(), elseNode, greatestCommonSubtype)
                    : new CaseNode(whenThenPairs.ToArray(), elseNode, MakeTypeNullable(greatestCommonSubtype));

                Nodes.Push(caseNode);
            }
            else
            {
                var greatestCommonSubtype = FindGreatestCommonSubtype();
                var nullableGreatestCommonSubtype = MakeTypeNullable(greatestCommonSubtype);
                var caseNode = new CaseNode(whenThenPairs.ToArray(), elseNode, nullableGreatestCommonSubtype);

                var rewritePartsWithProperNullHandling =
                    new RewritePartsWithProperNullHandlingVisitor(greatestCommonSubtype);
                var rewritePartsWithProperNullHandlingTraverser =
                    new RewritePartsWithProperNullHandlingTraverseVisitor(rewritePartsWithProperNullHandling);

                caseNode.Accept(rewritePartsWithProperNullHandlingTraverser);

                Nodes.Push(rewritePartsWithProperNullHandling.RewrittenNode);
            }

            _nullSuspiciousTypes.Clear();
        }

        public void Visit(WhenNode node)
        {
            var newNode = new WhenNode(Nodes.Pop());

            Nodes.Push(newNode);
        }

        public void Visit(ThenNode node)
        {
            var newNode = new ThenNode(Nodes.Pop());

            _nullSuspiciousTypes.Add(newNode.ReturnType);

            Nodes.Push(newNode);
        }

        public void Visit(ElseNode node)
        {
            var newNode = new ElseNode(Nodes.Pop());

            _nullSuspiciousTypes.Add(newNode.ReturnType);

            Nodes.Push(newNode);
        }

        public void Visit(FieldLinkNode node)
        {
            var index = node.Index - 1;

            if (_groupByFields.Count <= index)
                throw new FieldLinkIndexOutOfRangeException(index, _groupByFields.Count);

            Nodes.Push(_groupByFields[index].Expression);
        }

        public void SetQueryPart(QueryPart part)
        {
            _queryPart = part;
        }

        public void QueryBegins()
        {
            _schemaFromKey += 1;
        }

        public void QueryEnds()
        {
        }

        public void SetTheMostInnerIdentifierOfDotNode(IdentifierNode node)
        {
            _theMostInnerIdentifier = node;
        }

        private Type FindGreatestCommonSubtype()
        {
            var types = _nullSuspiciousTypes.Where(type => type != NullNode.NullType.Instance).Select(StripNullable)
                .Distinct().ToArray();

            if (types.Length == 0)
            {
                return null;
            }

            var greatestCommonSubtype = types[0];

            foreach (var currentType in types.Skip(1))
            {
                if (greatestCommonSubtype.IsAssignableTo(currentType))
                {
                    continue;
                }

                greatestCommonSubtype =
                    currentType.IsAssignableTo(greatestCommonSubtype)
                        ? currentType
                        : FindClosestCommonParent(greatestCommonSubtype, currentType);
            }

            return greatestCommonSubtype;
        }

        private string CreateSetOperatorPositionKey()
        {
            var key = _setKey++;
            return key.ToString().ToSetOperatorKey(key.ToString());
        }

        private static int[] CreateSetOperatorPositionIndexes(QueryNode node, string[] keys)
        {
            var indexes = new int[keys.Length];

            var fieldIndex = 0;
            var index = 0;

            foreach (var field in node.Select.Fields)
            {
                if (keys.Contains(field.FieldName))
                    indexes[index++] = fieldIndex;

                fieldIndex += 1;
            }

            return indexes;
        }

        private static void PrepareAndThrowUnknownColumnExceptionMessage(string identifier, ISchemaColumn[] columns)
        {
            var library = new TransitionLibrary();
            var candidates = new StringBuilder();

            var candidatesColumns = columns.Where(
                col =>
                    library.Soundex(col.ColumnName) == library.Soundex(identifier) ||
                    library.LevenshteinDistance(col.ColumnName, identifier) < 3).ToArray();

            for (var i = 0; i < candidatesColumns.Length - 1; i++)
            {
                var candidate = candidatesColumns[i];
                candidates.Append(candidate.ColumnName);
                candidates.Append(", ");
            }

            if (candidatesColumns.Length > 0)
            {
                candidates.Append(candidatesColumns[^1].ColumnName);

                throw new UnknownColumnException(
                    $"Column '{identifier}' could not be found. Did you mean to use [{candidates}]?");
            }

            throw new UnknownColumnException($"Column {identifier} could not be found.");
        }

        private static void PrepareAndThrowUnknownPropertyExceptionMessage(string identifier, PropertyInfo[] properties)
        {
            var library = new TransitionLibrary();
            var candidates = new StringBuilder();

            var candidatesProperties = properties.Where(
                prop =>
                    library.Soundex(prop.Name) == library.Soundex(identifier) ||
                    library.LevenshteinDistance(prop.Name, identifier) < 3).ToArray();

            for (var i = 0; i < candidatesProperties.Length - 1; i++)
            {
                var candidate = candidatesProperties[i];
                candidates.Append(candidate.Name);
                candidates.Append(", ");
            }

            if (candidatesProperties.Length > 0)
            {
                candidates.Append(candidatesProperties[^1].Name);

                throw new UnknownPropertyException(
                    $"Column '{identifier}' could not be found. Did you mean to use [{candidates}]?");
            }

            throw new UnknownPropertyException($"Column {identifier} could not be found.");
        }

        private static Type FindClosestCommonParent(Type type1, Type type2)
        {
            var type1Ancestors = new HashSet<Type>();

            while (type1 != null)
            {
                type1Ancestors.Add(type1);
                type1 = type1.BaseType;
            }

            while (type2 != null)
            {
                if (type1Ancestors.Contains(type2))
                {
                    return type2;
                }

                type2 = type2.BaseType;
            }

            return typeof(object);
        }

        private static Type MakeTypeNullable(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                || !type.IsValueType)
            {
                return type;
            }

            return typeof(Nullable<>).MakeGenericType(type);
        }

        private static Type StripNullable(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Nullable.GetUnderlyingType(type);
            }

            return type;
        }

        private static bool HasIndexer(Type type)
        {
            return type is not null && type.GetProperties().Any(f => f.GetIndexParameters().Length > 0);
        }

        private static bool TryReduceDimensions(MethodInfo method, ArgsListNode args, out MethodInfo reducedMethod)
        {
            var parameters = method.GetParameters();
            var paramsParameter = parameters
                .FirstOrDefault(f => f.GetCustomAttribute<ParamArrayAttribute>() != null);

            if (paramsParameter is null)
            {
                reducedMethod = null;
                return false;
            }
            
            var paramsParameterIndex = paramsParameter.Position;
            var typesToReduce = args.Args.Skip(paramsParameterIndex).Select(f => f.ReturnType).ToArray();

            var typeToReduce = typesToReduce.Length > 1 ? typesToReduce.First().MakeArrayType() : typesToReduce.First();

            if (typeToReduce is null)
            {
                reducedMethod = null;
                return false;
            }

            var lastNonNullType = typeToReduce;
            while (typeToReduce is not null)
            {
                lastNonNullType = typeToReduce;
                typeToReduce = typeToReduce.GetElementType();
            }
                
            reducedMethod = method.MakeGenericMethod(lastNonNullType);
            return true;
        }

        private static bool TryConstructGenericMethod(MethodInfo methodInfo, ArgsListNode args, out MethodInfo constructedMethod)
        {
            var genericArguments = methodInfo.GetGenericArguments();
            var genericArgumentsDistinct = new List<Type>();
            var parameters = methodInfo.GetParameters();
            
            foreach (var genericArgument in genericArguments)
            {
                var i = 0;
                var shiftArgsWhenInjectSpecificSourcePresent = 0;
                if (parameters[0].GetCustomAttribute<InjectSpecificSourceAttribute>() != null)
                {
                    i = 1;
                    shiftArgsWhenInjectSpecificSourcePresent = 1;
                }
                for (; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var returnType = args.Args.Where((_, index) => index == i - shiftArgsWhenInjectSpecificSourcePresent).Single().ReturnType;
                    var elementType = returnType.GetElementType();

                    if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == parameter.ParameterType.GetGenericTypeDefinition())
                    {
                        genericArgumentsDistinct.Add(returnType.GetGenericArguments()[0]);
                        continue;
                    }
                    
                    if (parameter.ParameterType.IsGenericType && parameter.ParameterType.IsAssignableTo(typeof(IEnumerable<>).MakeGenericType(genericArgument)) && elementType is not null)
                    {
                        genericArgumentsDistinct.Add(elementType);
                        continue;
                    }
                    
                    if (parameter.ParameterType.IsGenericType)
                    {
                        var assignableInterfaces = returnType
                            .GetInterfaces()
                            .Where(type => type.IsConstructedGenericType)
                            .Select(type => new { type, definition = type.GetGenericTypeDefinition() })
                            .ToArray();

                        var firstAssignableInterface =
                            assignableInterfaces.FirstOrDefault(f => f.definition.IsAssignableFrom(typeof(IEnumerable<>)));

                        if (firstAssignableInterface is null)
                        {
                            continue;
                        }
                        
                        var elementTypeOfFirstAssignableInterface = firstAssignableInterface.type.GetElementType() ?? firstAssignableInterface.type.GetGenericArguments()[0];
                        
                        genericArgumentsDistinct.Add(elementTypeOfFirstAssignableInterface);
                    }

                    if (parameter.ParameterType == genericArgument)
                    {
                        genericArgumentsDistinct.Add(returnType);
                    }
                }
            }
            
            var genericArgumentsConcreteTypes = genericArgumentsDistinct.Distinct().ToArray();
            
            constructedMethod = methodInfo.MakeGenericMethod(genericArgumentsConcreteTypes);
            return true;
        }
    }
}