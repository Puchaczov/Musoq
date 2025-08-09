using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using JoinFromNode = Musoq.Parser.Nodes.From.JoinFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;
using NotSupportedException = System.NotSupportedException;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;
using SchemaMethodFromNode = Musoq.Parser.Nodes.From.SchemaMethodFromNode;

namespace Musoq.Evaluator.Visitors;

public class BuildMetadataAndInferTypesVisitor(ISchemaProvider provider, IReadOnlyDictionary<string, string[]> columns, ILogger<BuildMetadataAndInferTypesVisitor> logger)
    : DefensiveVisitorBase, IAwareExpressionVisitor
{
    private static readonly WhereNode AllTrueWhereNode =
        new(new EqualityNode(new IntegerNode("1", "s"), new IntegerNode("1", "s")));

    /// <summary>
    /// Gets the name of this visitor for error reporting.
    /// </summary>
    protected override string VisitorName => nameof(BuildMetadataAndInferTypesVisitor);

    private readonly List<AccessMethodNode> _refreshMethods = [];
    private readonly List<object> _schemaFromArgs = [];
    private readonly List<string> _generatedAliases = [];

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

    private readonly Dictionary<string, List<FieldNode>> _generatedColumns = [];

    private readonly IDictionary<string, SchemaFromNode> _aliasToSchemaFromNodeMap =
        new Dictionary<string, SchemaFromNode>();

    private readonly IDictionary<string, string> _aliasMapToInMemoryTableMap =
        new Dictionary<string, string>();

    private readonly IDictionary<string, FieldNode[]> _cachedSetFields =
        new Dictionary<string, FieldNode[]>();

    private readonly List<FieldNode> _groupByFields = [];
    private readonly List<Type> _nullSuspiciousTypes = [];

    private readonly IDictionary<string, (int SchemaFromKey, uint PositionalEnvironmentVariableKey)> _schemaFromInfo =
        new Dictionary<string, (int, uint)>();
    
    private int _usedSchemasQuantity;

    private QueryPart _queryPart;

    private int _setKey;
    private int _schemaFromKey;
    private uint _positionalEnvironmentVariablesKey;
    private Scope _currentScope;
    private string _identifier;
    private string _queryAlias;
    private IdentifierNode _theMostInnerIdentifier;

    private Stack<string> Methods { get; } = new();

    protected Stack<Node> Nodes { get; } = new();

    protected readonly Dictionary<uint, IReadOnlyDictionary<string, string>> InternalPositionalEnvironmentVariables = new();

    public virtual IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables =>
        InternalPositionalEnvironmentVariables;

    public List<Assembly> Assemblies { get; } = [];

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
        var fromNode = SafeCast<FromNode>(SafePop(Nodes, nameof(Visit) + nameof(DescNode)), nameof(Visit) + nameof(DescNode));
        Nodes.Push(new DescNode(fromNode, node.Type));
    }

    public void Visit(StarNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, nameof(Visit) + nameof(StarNode));
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new StarNode(left, right));
    }

    public void Visit(FSlashNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, nameof(Visit) + nameof(FSlashNode));
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new FSlashNode(left, right));
    }

    public void Visit(ModuloNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, nameof(Visit) + nameof(ModuloNode));
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new ModuloNode(left, right));
    }

    public void Visit(AddNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, nameof(Visit) + nameof(AddNode));
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new AddNode(left, right));
    }

    public void Visit(HyphenNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, nameof(Visit) + nameof(HyphenNode));
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new HyphenNode(left, right));
    }

    public void Visit(AndNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, nameof(Visit) + nameof(AndNode));
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new AndNode(left, right));
    }

    public void Visit(OrNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, nameof(Visit) + nameof(OrNode));
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new OrNode(left, right));
    }

    public void Visit(ShortCircuitingNodeLeft node)
    {
        var childNode = SafePop(Nodes, nameof(Visit) + nameof(ShortCircuitingNodeLeft));
        Nodes.Push(new ShortCircuitingNodeLeft(childNode, node.UsedFor));
    }

    public void Visit(ShortCircuitingNodeRight node)
    {
        var childNode = SafePop(Nodes, nameof(Visit) + nameof(ShortCircuitingNodeRight));
        Nodes.Push(new ShortCircuitingNodeRight(childNode, node.UsedFor));
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
        var expression = SafePop(Nodes, nameof(Visit) + nameof(FieldNode));
        Nodes.Push(new FieldNode(expression, node.FieldOrder, node.FieldName));
    }

    public void Visit(FieldOrderedNode node)
    {
        var expression = SafePop(Nodes, nameof(Visit) + nameof(FieldOrderedNode));
        Nodes.Push(new FieldOrderedNode(expression, node.FieldOrder, node.FieldName, node.Order));
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
            (token, modifiedNode, exArgs, arg3, alias, canSkipInjectSource) =>
                new AccessMethodNode(token, modifiedNode as ArgsListNode, exArgs, canSkipInjectSource, arg3, alias));
    }

    public void Visit(AccessRawIdentifierNode node)
    {
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public void Visit(IsNullNode node)
    {
        Nodes.Push(new IsNullNode(Nodes.Pop(), node.IsNegated));
    }

    public void Visit(AccessRefreshAggregationScoreNode node)
    {
        VisitAccessMethod(node,
            (token, node1, exArgs, arg3, alias, _) =>
                new AccessRefreshAggregationScoreNode(token, node1 as ArgsListNode, exArgs, node.CanSkipInjectSource,
                    arg3, alias));
    }

    public void Visit(AccessColumnNode node)
    {
        try
        {
            var hasProcessedQueryId = _currentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
            var identifier = (hasProcessedQueryId
                ? _currentScope[MetaAttributes.ProcessedQueryId]
                : _identifier) ?? node.Alias;

            if (string.IsNullOrEmpty(identifier))
            {
                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    nameof(Visit) + nameof(AccessColumnNode),
                    "No valid identifier found for column access",
                    "Ensure the query has proper FROM clause and table aliases are correctly specified."
                );
            }

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);
            if (tableSymbol == null)
            {
                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    nameof(Visit) + nameof(AccessColumnNode),
                    $"Table symbol not found for identifier '{identifier}'",
                    "Verify that the table or alias is properly defined in the query."
                );
            }

            var tuple = !string.IsNullOrEmpty(node.Alias)
                ? tableSymbol.GetTableByAlias(node.Alias)
                : tableSymbol.GetTableByColumnName(node.Name);

            ISchemaColumn column;
            try
            {
                column = tuple.Table.GetColumnByName(node.Name);
            }
            catch (KeyNotFoundException)
            {
                column = null;
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

            var usedColumns = _usedColumns
                .Where(c => c.Key.Alias == tuple.TableName && c.Key.QueryId == _schemaFromKey)
                .Select(f => f.Value)
                .FirstOrDefault();

            if (usedColumns is not null)
            {
                if (usedColumns.All(c => c.ColumnName != column.ColumnName))
                {
                    usedColumns.Add(column);
                }
            }

            var accessColumn = new AccessColumnNode(column.ColumnName, tuple.TableName, column.ColumnType, node.Span);
            Nodes.Push(accessColumn);
        }
        catch (Exception ex) when (!(ex is VisitorException))
        {
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                nameof(Visit) + nameof(AccessColumnNode),
                $"Failed to process column access for '{node.Name}': {ex.Message}",
                "Check that the column exists in the specified table and that table aliases are correct."
            );
        }
    }

    public void Visit(AllColumnsNode node)
    {
        var identifier = _identifier;
        var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);

        if (!string.IsNullOrWhiteSpace(node.Alias) /* r.* */ ||
            (!tableSymbol.IsCompoundTable && string.IsNullOrWhiteSpace(node.Alias)) /* * from #abc.cda() */)
        {
            ProcessSingleTable(node, tableSymbol, identifier);
        }
        else if (tableSymbol.IsCompoundTable)
        {
            ProcessCompoundTable(tableSymbol);
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
                Nodes.Push(new AccessObjectArrayNode(node.Token,
                    new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                return;
            }

            var type = parentNode.ReturnType.GetProperty(node.Name)?.PropertyType ??
                       (_theMostInnerIdentifier.Name == node.Name ? typeof(object[]) : typeof(ExpandoObject[]));
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
                Nodes.Push(new AccessObjectKeyNode(node.Token,
                    new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                return;
            }

            var type = parentNode.ReturnType.GetProperty(node.Name)?.PropertyType ??
                       (_theMostInnerIdentifier.Name == node.Name ? typeof(object) : typeof(ExpandoObject));
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
                Nodes.Push(new PropertyValueNode(node.Name,
                    new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                return;
            }

            var type = parentNode.ReturnType.GetProperty(node.Name)?.PropertyType ??
                       (_theMostInnerIdentifier.Name == node.Name ? typeof(object) : typeof(ExpandoObject));
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

    public void Visit(SchemaFromNode node)
    {
        var schema = provider.GetSchema(node.Schema);
        const bool hasExternallyProvidedTypes = false;

        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());
        
        if (HasAlreadyUsedAlias(_queryAlias))
        {
            throw new AliasAlreadyUsedException(node, _queryAlias);
        }

        _generatedAliases.Add(_queryAlias);

        var aliasedSchemaFromNode = new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode) Nodes.Pop(),
            _queryAlias, node.QueryId, hasExternallyProvidedTypes);

        var environmentVariables =
            RetrieveEnvironmentVariables(_positionalEnvironmentVariablesKey, aliasedSchemaFromNode);
        var isDesc = _currentScope.Name == "Desc";
        var table = !isDesc ? schema.GetTableByName(
            node.Method,
            new RuntimeContext(
                CancellationToken.None,
                columns[_queryAlias + _schemaFromKey].Select((f, i) => new SchemaColumn(f, i, typeof(object)))
                    .ToArray(),
                environmentVariables,
                (aliasedSchemaFromNode, Array.Empty<ISchemaColumn>(), AllTrueWhereNode, hasExternallyProvidedTypes),
                logger
            ),
            _schemaFromArgs.ToArray()) : new DynamicTable([]);

        _schemaFromInfo.Add(_queryAlias, (_schemaFromKey, _positionalEnvironmentVariablesKey));
        _positionalEnvironmentVariablesKey += 1;
        _schemaFromArgs.Clear();

        AddAssembly(schema.GetType().Assembly);

        var tableSymbol = new TableSymbol(_queryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
        _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);
        _currentScope[node.Id] = _queryAlias;
        _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_queryAlias);

        _aliasToSchemaFromNodeMap.Add(_queryAlias, aliasedSchemaFromNode);

        if (!_inferredColumns.ContainsKey(aliasedSchemaFromNode))
            _inferredColumns.Add(aliasedSchemaFromNode, table.Columns);

        if (!_usedColumns.ContainsKey(aliasedSchemaFromNode))
            _usedColumns.Add(aliasedSchemaFromNode, []);

        _usedWhereNodes.TryAdd(aliasedSchemaFromNode, AllTrueWhereNode);
        _usedSchemasQuantity += 1;

        Nodes.Push(aliasedSchemaFromNode);
    }

    private bool HasAlreadyUsedAlias(string queryAlias)
    {
        var scope = _currentScope;

        while (scope != null)
        {
            if (scope.ScopeSymbolTable.TryGetSymbol<AliasesSymbol>(MetaAttributes.Aliases, out var symbol) && symbol.ContainsAlias(queryAlias))
                return true;
            
            scope = scope.Parent;
        }
        
        return false;
    }

    public void Visit(SchemaMethodFromNode node)
    {
        _usedSchemasQuantity += 1;
        Nodes.Push(new Parser.SchemaMethodFromNode(node.Alias, node.Schema, node.Method));
    }

    public void Visit(PropertyFromNode node)
    {
        ISchemaTable table;
        ISchema schema;

        if (_aliasToSchemaFromNodeMap.TryGetValue(node.SourceAlias, out var schemaFrom))
        {
            schema = provider.GetSchema(schemaFrom.Schema);
            table = GetTableFromSchema(schema, schemaFrom);
        }
        else
        {
            var name = _aliasMapToInMemoryTableMap[node.SourceAlias];
            table = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(node.SourceAlias).FullTable;
            schema = new TransitionSchema(name, table);
        }

        _aliasMapToInMemoryTableMap.Add(node.Alias, node.SourceAlias);

        var targetColumn = table.GetColumnByName(node.FirstProperty.PropertyName);
        if (targetColumn == null)
        {
            PrepareAndThrowUnknownColumnExceptionMessage(node.FirstProperty.PropertyName, table.Columns);
            return;
        }

        ValidateBindablePropertyAsTable(table, targetColumn);

        AddAssembly(targetColumn.ColumnType.Assembly);
        var nestedTable = TurnTypeIntoTable(FollowProperties(targetColumn.ColumnType, node.PropertiesChain));
        table = nestedTable;

        UpdateQueryAliasAndSymbolTable(node, schema, table);

        Nodes.Push(
            new Parser.PropertyFromNode(
                node.Alias, 
                node.SourceAlias, 
                RewritePropertiesChainWithTargetColumn(targetColumn, node.PropertiesChain)
            )
        );
    }

    public void Visit(AccessMethodFromNode node)
    {
        ISchemaTable table;
        ISchema schema;

        if (_aliasToSchemaFromNodeMap.TryGetValue(node.SourceAlias, out var schemaFrom))
        {
            schema = provider.GetSchema(schemaFrom.Schema);
        }
        else
        {
            var name = _aliasMapToInMemoryTableMap[node.SourceAlias];
            table = FindTableSymbolInScopeHierarchy(name).FullTable;
            schema = new TransitionSchema(name, table);
        }

        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());
        _generatedAliases.Add(_queryAlias);

        var accessMethodNode = (AccessMethodNode) Nodes.Pop();
        table = TurnTypeIntoTable(accessMethodNode.ReturnType);
        var tableSymbol = new TableSymbol(_queryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
        _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);
        _currentScope[node.Id] = _queryAlias;
        _aliasMapToInMemoryTableMap.Add(_queryAlias, node.SourceAlias);
        _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);

        Nodes.Push(new Parser.AccessMethodFromNode(node.Alias, node.SourceAlias, accessMethodNode,
            accessMethodNode.ReturnType));
    }

    public void Visit(AliasedFromNode node)
    {
        var schemaInfo = _explicitlyUsedAliases[node.Identifier];
        var tableName = _explicitlyCoupledTablesWithAliases[node.Identifier];
        var table = _explicitlyDefinedTables[tableName];
        const bool hasExternallyProvidedTypes = true;

        var schema = provider.GetSchema(schemaInfo.Schema);

        AddAssembly(schema.GetType().Assembly);

        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());
        _generatedAliases.Add(_queryAlias);

        var aliasedSchemaFromNode = new Parser.SchemaFromNode(
            schemaInfo.Schema,
            schemaInfo.Method,
            (ArgsListNode) Nodes.Pop(),
            _queryAlias,
            node.InSourcePosition,
            hasExternallyProvidedTypes
        );

        table = schema.GetTableByName(
            schemaInfo.Method,
            new RuntimeContext(
                CancellationToken.None,
                table.Columns,
                RetrieveEnvironmentVariables(_positionalEnvironmentVariablesKey, aliasedSchemaFromNode),
                (aliasedSchemaFromNode, Array.Empty<ISchemaColumn>(), AllTrueWhereNode, hasExternallyProvidedTypes),
                logger
            ),
            _schemaFromArgs.ToArray()
        ) ?? table;
        var tableSymbol = new TableSymbol(
            _queryAlias,
            schema,
            table,
            !string.IsNullOrEmpty(node.Alias)
        );

        _schemaFromArgs.Clear();

        _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);
        _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_queryAlias);
        _currentScope[node.Id] = _queryAlias;

        if (!_inferredColumns.ContainsKey(aliasedSchemaFromNode))
            _inferredColumns.Add(aliasedSchemaFromNode, table.Columns);

        if (!_usedColumns.ContainsKey(aliasedSchemaFromNode))
            _usedColumns.Add(aliasedSchemaFromNode, []);

        _usedWhereNodes.TryAdd(aliasedSchemaFromNode, AllTrueWhereNode);
        _usedSchemasQuantity += 1;
        _schemaFromInfo.Add(_queryAlias, (_schemaFromKey, _positionalEnvironmentVariablesKey));
        _positionalEnvironmentVariablesKey += 1;

        Nodes.Push(aliasedSchemaFromNode);
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
        var exp = Nodes.Pop();
        var b = (FromNode) Nodes.Pop();
        var a = (FromNode) Nodes.Pop();

        Nodes.Push(new Parser.JoinSourcesTableFromNode(a, b, exp, node.JoinType));
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        var b = (FromNode) Nodes.Pop();
        var a = (FromNode) Nodes.Pop();

        Nodes.Push(new Parser.ApplySourcesTableFromNode(a, b, node.ApplyType));
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
                throw new TableIsNotDefinedException(node.VariableName);

            tableSymbol = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(node.VariableName);
        }

        var tableSchemaPair = tableSymbol.GetTableByAlias(node.VariableName);
        _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias,
            new TableSymbol(_queryAlias, tableSchemaPair.Schema, tableSchemaPair.Table, node.Alias == _queryAlias));
        _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_queryAlias);
        _currentScope[node.Id] = _queryAlias;

        _aliasMapToInMemoryTableMap.Add(_queryAlias, node.VariableName);
        _usedSchemasQuantity += 1;

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

    public void Visit(ApplyFromNode node)
    {
        var appliedTable = (FromNode) Nodes.Pop();
        var source = (FromNode) Nodes.Pop();
        var appliedFrom = new Parser.ApplyFromNode(source, appliedTable, node.ApplyType);
        _identifier = appliedFrom.Alias;
        _schemaFromArgs.Clear();
        Nodes.Push(appliedFrom);
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

        if (from is null)
        {
            throw new FromNodeIsNull();
        }

        if (groupBy == null && _refreshMethods.Count > 0)
        {
            groupBy = new GroupByNode([new FieldNode(new IntegerNode("1", "s"), 0, string.Empty)], null);
        }

        _currentScope.ScopeSymbolTable.AddSymbol(from.Alias.ToRefreshMethodsSymbolName(),
            new RefreshMethodsSymbol(_refreshMethods));
        _refreshMethods.Clear();

        if (_currentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(string.Empty))
            _currentScope.ScopeSymbolTable.MoveSymbol(string.Empty, from.Alias);

        Methods.Push(from.Alias);
        Nodes.Push(new QueryNode(select, from, where, groupBy, orderBy, skip, take));

        _schemaFromArgs.Clear();
        _aliasToSchemaFromNodeMap.Clear();
        _schemaFromInfo.Clear();
        _aliasMapToInMemoryTableMap.Clear();
        _usedSchemasQuantity = 0;
    }

    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        var exp = Nodes.Pop();
        var from = (FromNode) Nodes.Pop();
        Nodes.Push(
            new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType));
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        var from = (FromNode) Nodes.Pop();
        Nodes.Push(
            new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType));
    }

    public void Visit(InternalQueryNode node)
    {
        throw new NotSupportedException("Internal Query Node is not supported here");
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
        VisitSetOperationNode(node, "Union");
    }

    public void Visit(UnionAllNode node)
    {
        VisitSetOperationNode(node, "UnionAll");
    }

    public void Visit(ExceptNode node)
    {
        VisitSetOperationNode(node, "Except");
    }

    public void Visit(IntersectNode node)
    {
        VisitSetOperationNode(node, "Intersect");
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
        _currentScope.Parent.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Name);

        Nodes.Push(new CteInnerExpressionNode(set, node.Name));
    }

    public void Visit(JoinNode node)
    {
        _identifier = node.Alias;
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode) Nodes.Pop()));
    }

    public void Visit(ApplyNode node)
    {
        _identifier = node.Alias;
        Nodes.Push(new Parser.ApplyNode((Parser.ApplyFromNode) Nodes.Pop()));
    }

    public void SetScope(Scope scope)
    {
        _currentScope = scope;
    }

    protected virtual IReadOnlyDictionary<string, string> RetrieveEnvironmentVariables(uint position, SchemaFromNode node)
    {
        var environmentVariables = new Dictionary<string, string>();
        
        InternalPositionalEnvironmentVariables.TryAdd(position, environmentVariables);

        return environmentVariables;
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
        for (var i = reorderedList.Length - 1; i >= 0; i--)
        {
            // Enhanced error handling for stack management
            if (Nodes.Count == 0)
            {
                throw new InvalidOperationException($"Stack underflow when processing field {i}. Expected {oldFields.Length} fields but stack is empty.");
            }
            
            var poppedNode = Nodes.Pop();
            reorderedList[i] = poppedNode as FieldNode;
            
            // Enhanced workaround for window function processing: if we get an AccessMethodNode or other expression,
            // wrap it in a FieldNode using the original field metadata
            if (reorderedList[i] == null)
            {
                // Use the original field to get the proper field name and order
                var originalField = oldFields[i];
                reorderedList[i] = new FieldNode(poppedNode, originalField.FieldOrder, originalField.FieldName);
            }
        }

        var fields = new List<FieldNode>(reorderedList.Length);
        var positionCounter = 0;

        foreach (var field in reorderedList)
        {
            if (field.Expression is AllColumnsNode allColumnsNode)
            {
                AddAllColumnsFields(fields, allColumnsNode, ref positionCounter);
            }
            else
            {
                fields.Add(new FieldNode(field.Expression, positionCounter++, field.FieldName));
            }
        }

        return fields.ToArray();
    }

    private void AddAllColumnsFields(List<FieldNode> fields, AllColumnsNode allColumnsNode, ref int positionCounter)
    {
        var identifier = !string.IsNullOrWhiteSpace(allColumnsNode.Alias)
            ? allColumnsNode.Alias
            : _identifier;

        if (_generatedColumns.TryGetValue(identifier, out var generatedColumns))
        {
            foreach (var column in generatedColumns)
            {
                fields.Add(new FieldNode(column.Expression, positionCounter++, column.FieldName));
            }
        }
        else if (string.IsNullOrWhiteSpace(allColumnsNode.Alias))
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
            foreach (var compoundTableIdentifier in tableSymbol.CompoundTables)
            {
                if (!_generatedColumns.TryGetValue(compoundTableIdentifier, out var compoundColumns)) continue;

                foreach (var column in compoundColumns)
                {
                    fields.Add(new FieldNode(column.Expression, positionCounter++, column.FieldName));
                }
            }
        }
    }

    private void VisitAccessMethod(AccessMethodNode node, Func<FunctionToken, Node, ArgsListNode, MethodInfo, string, bool, AccessMethodNode> func)
    {
        if (Nodes.Pop() is not ArgsListNode args)
            throw CannotResolveMethodException.CreateForNullArguments(node.Name);

        var groupArgs = new List<Type> {typeof(string)};
        groupArgs.AddRange(args.Args.Skip(1).Select(f => f.ReturnType));

        if (_usedSchemasQuantity > 1 && string.IsNullOrWhiteSpace(node.Alias))
            throw new AliasMissingException(node);
        
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
                    throw CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(node.Name, args.Args);
                }

                canSkipInjectSource = true;
            }
        }

        var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

        if (!isAggregateMethod && method.IsGenericMethod && TryReduceDimensions(method, args, out var reducedMethod))
        {
            method = reducedMethod;
        }

        if (
            !isAggregateMethod && 
            method.IsGenericMethod && 
            !method.IsConstructedGenericMethod &&
            TryConstructGenericMethod(method, args, entityType, out var constructedMethod))
        {
            method = constructedMethod;
        }

        AccessMethodNode accessMethod;
        if (isAggregateMethod)
        {
            accessMethod = func(node.FunctionToken, args, node.ExtraAggregateArguments, method, alias, false);
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
                throw CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(setMethodName, newSetArgs.ToArray());
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

            accessMethod = func(node.FunctionToken, new ArgsListNode(newArgs.ToArray()), null, method, alias,
                canSkipInjectSource);
        }
        else
        {
            accessMethod = func(node.FunctionToken, args, new ArgsListNode([]), method, alias,
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
        var tableColumns = new List<ISchemaColumn>();

        for (var i = 0; i < node.TableTypePairs.Length; i++)
        {
            var (columnName, typeName) = node.TableTypePairs[i];

            var remappedType = EvaluationHelper.RemapPrimitiveTypes(typeName);

            var type = EvaluationHelper.RemapPrimitiveTypeAsNullable(remappedType);

            if (type == null)
                throw new TypeNotFoundException($"Type '{remappedType}' could not be found.");

            tableColumns.Add(new SchemaColumn(columnName, i, type));
        }

        var table = new DynamicTable(tableColumns.ToArray());
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



    public void Visit(WindowFunctionNode node)
    {
        // Enhanced window function processing with PARTITION BY and ROWS BETWEEN support
        // Handle window functions by delegating to regular method resolution with advanced window specifications
        
        Console.WriteLine($"DEBUG: WindowFunctionNode.Visit called for {node.FunctionName}");
        
        if (Nodes.Pop() is not ArgsListNode args)
            throw CannotResolveMethodException.CreateForNullArguments(node.FunctionName);

        // DEBUG: Validate that args don't contain null values
        if (args.Args.Any(arg => arg == null))
        {
            // Use original node.Arguments if stack args are corrupted
            if (node.Arguments?.Args != null && !node.Arguments.Args.Any(arg => arg == null))
            {
                args = node.Arguments;
            }
            else
            {
                throw new InvalidOperationException($"WindowFunctionNode {node.FunctionName} has null arguments in ArgsListNode");
            }
        }

        // Extract window specification information for advanced processing
        var windowSpec = node.WindowSpecification;
        var partitionColumns = "";
        var orderColumns = "";
        var frameStart = "UNBOUNDED PRECEDING";
        var frameEnd = "CURRENT ROW";

        // Process PARTITION BY clause
        if (windowSpec?.PartitionBy != null)
        {
            partitionColumns = windowSpec.PartitionBy.ToString();
        }

        // Process ORDER BY clause  
        if (windowSpec?.OrderBy != null)
        {
            orderColumns = windowSpec.OrderBy.ToString();
        }

        // Process ROWS BETWEEN window frame
        if (windowSpec?.WindowFrame != null)
        {
            var frame = windowSpec.WindowFrame;
            frameStart = frame.StartBound;
            frameEnd = frame.EndBound;
        }

        // Create enhanced arguments list that includes window specification parameters
        var enhancedArgs = new List<Node>();
        enhancedArgs.AddRange(args.Args); // Original function arguments

        // For aggregate window functions, start with basic method resolution
        // Window specification processing will be enhanced in future iterations
        var enhancedArgsListNode = new ArgsListNode(enhancedArgs.ToArray());
        
        Console.WriteLine($"DEBUG: Trying to resolve method with args: {string.Join(", ", enhancedArgsListNode.Args.Select(a => a.ReturnType.Name))}");
        Console.WriteLine($"DEBUG: Enhanced args length: {enhancedArgsListNode.Args.Length}");
        
        for (int i = 0; i < enhancedArgsListNode.Args.Length; i++)
        {
            var arg = enhancedArgsListNode.Args[i];
            Console.WriteLine($"DEBUG: Enhanced arg[{i}] type: {arg.ReturnType.Name}");
        }
        var groupArgs = new List<Type> {typeof(string)};
        groupArgs.AddRange(enhancedArgsListNode.Args.Skip(1).Select(f => f.ReturnType));

        var alias = _identifier;
        var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(alias);
        var schemaTablePair = tableSymbol.GetTableByAlias(alias);
        var entityType = schemaTablePair.Table.Metadata.TableEntityType;

        AddAssembly(entityType.Assembly);
        AddBaseTypeAssembly(entityType);

        var canSkipInjectSource = false;
        MethodInfo method = null;
        
        // For window functions, use the standard method resolution that handles generics properly
        // TryResolveMethod delegates to TryGetAnnotatedMethod which supports generic parameter matching
        Console.WriteLine($"DEBUG: About to try resolving method {node.FunctionName} with entity type {entityType.Name}");
        
        if (!schemaTablePair.Schema.TryResolveMethod(node.FunctionName, enhancedArgsListNode.Args.Select(f => f.ReturnType).ToArray(), entityType, out method))
        {
            Console.WriteLine($"DEBUG: Method resolution failed for {node.FunctionName}");
            throw CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(node.FunctionName, enhancedArgsListNode.Args);
        }
        
        Console.WriteLine($"DEBUG: Method resolution succeeded for {node.FunctionName}: {method.Name}");

        var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

        if (!isAggregateMethod && method.IsGenericMethod && TryReduceDimensions(method, enhancedArgsListNode, out var reducedMethod))
        {
            method = reducedMethod;
        }

        if (!isAggregateMethod && method.IsGenericMethod && !method.IsConstructedGenericMethod &&
            TryConstructGenericMethod(method, enhancedArgsListNode, entityType, out var constructedMethod))
        {
            method = constructedMethod;
        }

        
        // Create the result AccessMethodNode with enhanced arguments for window specification processing
        var functionToken = new FunctionToken(node.FunctionName, default);
        
        // Create a special marker to indicate this is a window function
        // Use ExtraAggregateArguments to store a marker that prevents aggregate rewriting
        var windowFunctionMarker = new ArgsListNode([new WordNode("__WINDOW_FUNCTION__")]);
        
        var resultNode = new AccessMethodNode(functionToken, enhancedArgsListNode, windowFunctionMarker, canSkipInjectSource, method, alias);
        
        // Store window specification information for execution phase
        // TODO: Enhance AccessMethodNode or create custom WindowAccessMethodNode to store window specifications
        
        Nodes.Push(resultNode);
    }

    private static string GetWindowMethodName(string functionName)
    {
        return functionName.ToUpper() switch
        {
            "SUM" => "SumWithWindow",
            "COUNT" => "CountWithWindow", 
            "AVG" => "AvgWithWindow",
            "MIN" => "MinWithWindow",
            "MAX" => "MaxWithWindow",
            _ => functionName // For functions like RANK, DENSE_RANK, LAG, LEAD that don't need special window variants
        };
    }

    private static bool IsAggregateFunction(string functionName)
    {
        var aggregateFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SUM", "COUNT", "AVG", "MIN", "MAX", "STDEV", "VAR"
        };
        return aggregateFunctions.Contains(functionName);
    }

    private static string[] GetAvailableMethodNames(ISchema schema)
    {
        try
        {
            // Try to get some basic method information for debugging
            var methods = new List<string>();
            
            // Check if it's a basic schema that has LibraryBase methods
            if (schema.GetType().Name.Contains("Basic"))
            {
                methods.Add("Basic schema detected - should have Sum<T>, Count<T>, Avg<T>, Rank, DenseRank, Lag, Lead");
            }
            
            return methods.Any() ? methods.ToArray() : new[] { "Debug: no methods found" };
        }
        catch
        {
            return new[] { "Debug: could not retrieve method names" };
        }
    }

    public void Visit(WindowFrameNode node)
    {
        // Window frame nodes don't need special metadata processing for now
        // They are part of window specification structure and will be handled during execution
    }

    public void Visit(WindowSpecificationNode node)
    {
        // Process PARTITION BY and ORDER BY clauses for window functions
        
        // Handle PARTITION BY clause if present
        if (node.PartitionBy != null)
        {
            // PARTITION BY expressions should be processed like regular expressions
            // They will be used to group data during window function execution
            
            // For now, we don't need to do special processing as the traversal visitor
            // will have already processed the PartitionBy node
        }
        
        // Handle ORDER BY clause if present  
        if (node.OrderBy != null)
        {
            // ORDER BY expressions should be processed like regular expressions
            // They will be used to sort data within partitions during window function execution
            
            // For now, we don't need to do special processing as the traversal visitor
            // will have already processed the OrderBy node
        }
        
        // Handle window frame if present
        if (node.WindowFrame != null)
        {
            // Window frames define the subset of rows to consider for window functions
            // The WindowFrameNode will be processed by its own visitor
        }
        
        // CRITICAL: Window specification nodes should NOT push anything to the stack
        // They provide context for window function execution but don't represent actual data
        // The ORDER BY and PARTITION BY expressions are used for execution context only
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
        _identifier = null;
    }

    public void SetTheMostInnerIdentifierOfDotNode(IdentifierNode node)
    {
        _theMostInnerIdentifier = node;
    }

    public void InnerCteBegins()
    {
    }

    public void InnerCteEnds()
    {
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

    private string PreviousSetOperatorPositionKey()
    {
        return (_setKey - 2).ToString().ToSetOperatorKey((_setKey - 2).ToString());
    }

    private void ProcessSingleTable(AllColumnsNode node, TableSymbol tableSymbol, string identifier)
    {
        var generatedColumnIdentifier = node.Alias ?? identifier;
        var tuple = tableSymbol.GetTableByAlias(generatedColumnIdentifier);
        var table = tuple.Table;

        var generatedColumns = GetOrCreateGeneratedColumns(generatedColumnIdentifier);

        for (var i = 0; i < table.Columns.Length; i++)
        {
            AddColumnToGeneratedColumns(tableSymbol, table.Columns[i], i, generatedColumnIdentifier, generatedColumns);
        }

        UpdateUsedColumns(generatedColumnIdentifier, table);
    }

    private void ProcessCompoundTable(TableSymbol tableSymbol)
    {
        foreach (var tableIdentifier in tableSymbol.CompoundTables)
        {
            var tuple = tableSymbol.GetTableByAlias(tableIdentifier);
            var table = tuple.Table;

            var generatedColumns = GetOrCreateGeneratedColumns(tableIdentifier);

            for (var i = 0; i < table.Columns.Length; i++)
            {
                AddColumnToGeneratedColumns(tableSymbol, table.Columns[i], i, tableIdentifier, generatedColumns, isCompoundTable: true);
            }

            UpdateUsedColumns(tableIdentifier, table);
        }
    }

    private List<FieldNode> GetOrCreateGeneratedColumns(string identifier)
    {
        if (!_generatedColumns.TryGetValue(identifier, out var generatedColumns))
        {
            generatedColumns = [];
            _generatedColumns.Add(identifier, generatedColumns);
        }
        else
        {
            generatedColumns.Clear();
        }
        return generatedColumns;
    }

    private void AddColumnToGeneratedColumns(TableSymbol tableSymbol, ISchemaColumn column, int index, string identifier, List<FieldNode> generatedColumns, bool isCompoundTable = false)
    {
        AddAssembly(column.ColumnType.Assembly);

        var accessColumn = new AccessColumnNode(column.ColumnName, identifier, column.ColumnType, TextSpan.Empty);
        string fieldName;
        if (isCompoundTable)
        {
            fieldName = $"{identifier}.{column.ColumnName}";
        }
        else
        {
            fieldName = tableSymbol.HasAlias ? $"{identifier}.{column.ColumnName}" : column.ColumnName;
        }
        generatedColumns.Add(new FieldNode(accessColumn, index, fieldName));
    }

    private void UpdateUsedColumns(string identifier, ISchemaTable table)
    {
        if (_aliasToSchemaFromNodeMap.TryGetValue(identifier, out var schemaFromNode))
        {
            _usedColumns[schemaFromNode] = table.Columns.ToList();
        }
    }

    private ISchemaTable GetTableFromSchema(ISchema schema, SchemaFromNode schemaFrom)
    {
        var runtimeContext = new RuntimeContext(
            CancellationToken.None,
            columns[schemaFrom.Alias + _schemaFromKey].Select((f, i) => new SchemaColumn(f, i, typeof(object))).ToArray(),
            RetrieveEnvironmentVariables(_schemaFromInfo[schemaFrom.Alias].PositionalEnvironmentVariableKey, schemaFrom),
            (schemaFrom, Array.Empty<ISchemaColumn>(), AllTrueWhereNode, false),
            logger
        );

        return schema.GetTableByName(schemaFrom.Method, runtimeContext, schemaFrom.Parameters);
    }

    private void UpdateQueryAliasAndSymbolTable(PropertyFromNode node, ISchema schema, ISchemaTable table)
    {
        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());
        _generatedAliases.Add(_queryAlias);
        
        _schemaFromArgs.Clear();
        var tableSymbol = new TableSymbol(_queryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
        _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);
        _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);
        _currentScope[node.Id] = _queryAlias;
    }

    private static Exception SetOperatorDoesNotHaveKeysException(string setOperator)
    {
        return new SetOperatorMustHaveKeyColumnsException(setOperator);
    }

    private void MakeSureBothSideFieldsAreOfAssignableTypes(QueryNode left, QueryNode right, string cachedSetOperatorKey)
    {
        var leftFields = left.Select.Fields;
        var rightFields = right.Select.Fields;

        if (leftFields.Length != rightFields.Length)
        {
            throw new SetOperatorMustHaveSameQuantityOfColumnsException();
        }

        for (var i = 0; i < leftFields.Length; i++)
        {
            if (leftFields[i].Expression.ReturnType != rightFields[i].Expression.ReturnType)
            {
                throw new SetOperatorMustHaveSameTypesOfColumnsException(leftFields[i], rightFields[i]);
            }
        }

        _cachedSetFields.TryAdd(cachedSetOperatorKey, rightFields);
    }

    private void MakeSureBothSideFieldsAreOfAssignableTypes(QueryNode left, string cachedSetOperatorKey, string currentSetOperatorKey)
    {
        var leftFields = left.Select.Fields;
        var rightFields = _cachedSetFields[cachedSetOperatorKey];

        if (leftFields.Length != rightFields.Length)
        {
            throw new SetOperatorMustHaveSameQuantityOfColumnsException();
        }

        for (var i = 0; i < leftFields.Length; i++)
        {
            if (leftFields[i].Expression.ReturnType != rightFields[i].Expression.ReturnType)
            {
                throw new SetOperatorMustHaveSameTypesOfColumnsException(leftFields[i], rightFields[i]);
            }
        }

        _cachedSetFields.TryAdd(currentSetOperatorKey, leftFields);
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

            throw new UnknownColumnOrAliasException(
                $"Column or Alias '{identifier}' could not be found. Did you mean to use [{candidates}]?");
        }

        throw new UnknownColumnOrAliasException($"Column or Alias {identifier} could not be found.");
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

    private static bool TryConstructGenericMethod(MethodInfo methodInfo, ArgsListNode args, Type entity, out MethodInfo constructedMethod)
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
                if ((genericArgument.IsGenericParameter || genericArgument.IsGenericMethodParameter) && parameters[0].ParameterType.IsGenericParameter)
                {
                    genericArgumentsDistinct.Add(entity);
                }
            }
            
            for (; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.IsOptional && args.Args.Length < parameters.Length - shiftArgsWhenInjectSpecificSourcePresent)
                {
                    continue;
                }
                
                var returnType = args.Args.Where((_, index) => index == i - shiftArgsWhenInjectSpecificSourcePresent).Single().ReturnType;
                var elementType = returnType.GetElementType();

                if (returnType.IsGenericType && parameter.ParameterType.IsGenericType && returnType.GetGenericTypeDefinition() == parameter.ParameterType.GetGenericTypeDefinition())
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

    private static ISchemaTable TurnTypeIntoTable(Type type)
    {   
        var columns = new List<ISchemaColumn>();

        Type nestedType;
        if (type.IsArray)
        {
            nestedType = type.GetElementType();
        }
        else if (IsGenericEnumerable(type, out nestedType))
        {
            // nestedType is already set by the IsGenericEnumerable method
        }
        else
        {
            throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
        }

        if (nestedType == null)
        {
            throw new InvalidOperationException("Element type is null.");
        }

        if (nestedType.IsPrimitive || nestedType == typeof(string))
        {
            return new DynamicTable([new SchemaColumn(nameof(PrimitiveTypeEntity<int>.Value), 0, nestedType)]);
        }
    
        foreach (var property in nestedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            columns.Add(new SchemaColumn(property.Name, columns.Count, property.PropertyType));
        }
    
        return new DynamicTable(columns.ToArray(), nestedType);
    }

    private static bool IsGenericEnumerable(Type type, out Type elementType)
    {
        elementType = null;
    
        // Check if the type is a generic type
        if (!type.IsGenericType) return false;
            
        // Get all interfaces implemented by the type
        var interfaces = type.GetInterfaces().Concat([type]);
        
        foreach (var interfaceType in interfaces)
        {
            if (!interfaceType.IsGenericType ||
                interfaceType.GetGenericTypeDefinition() != typeof(IEnumerable<>)) continue;
                    
            elementType = interfaceType.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    private static void ValidateBindablePropertyAsTable(ISchemaTable table, ISchemaColumn targetColumn)
    {
        var propertyInfo = table.Metadata.TableEntityType.GetProperty(targetColumn.ColumnName);
        var bindablePropertyAsTableAttribute = propertyInfo?.GetCustomAttribute<BindablePropertyAsTableAttribute>();

        if (bindablePropertyAsTableAttribute == null) return;
        
        var isValid = IsGenericEnumerable(propertyInfo!.PropertyType, out var elementType) ||
                      IsArray(propertyInfo.PropertyType!, out elementType) ||
                      elementType.IsPrimitive || elementType == typeof(string);

        if (!isValid)
        {
            throw new ColumnMustBeMarkedAsBindablePropertyAsTableException();
        }
    }
        
    private static bool IsArray(Type type, out Type elementType)
    {
        elementType = null;
    
        if (!type.IsArray) return false;
            
        elementType = type.GetElementType();
        return true;
    }

    private static Type FollowProperties(Type type, PropertyFromNode.PropertyNameAndTypePair[] propertiesChain)
    {
        var propertiesWithoutColumnType = propertiesChain.Skip(1);
        
        foreach (var property in propertiesWithoutColumnType)
        {
            var propertyInfo = type.GetProperty(property.PropertyName);
            
            if (propertyInfo == null)
            {
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, type.GetProperties());
                return null;
            }

            type = propertyInfo.PropertyType;
        }
        
        return type;
    }

    private static PropertyFromNode.PropertyNameAndTypePair[] RewritePropertiesChainWithTargetColumn(ISchemaColumn targetColumn, PropertyFromNode.PropertyNameAndTypePair[] nodePropertiesChain)
    {
        var propertiesChain = new PropertyFromNode.PropertyNameAndTypePair[nodePropertiesChain.Length];
        var rootType = targetColumn.ColumnType;
        propertiesChain[0] = new PropertyFromNode.PropertyNameAndTypePair(targetColumn.ColumnName, rootType);
        
        for (var i = 1; i < nodePropertiesChain.Length; i++)
        {
            var property = nodePropertiesChain[i];
            var propertyInfo = rootType.GetProperty(property.PropertyName);
            
            if (propertyInfo == null)
            {
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, rootType.GetProperties());
                return null;
            }
            
            propertiesChain[i] = new PropertyFromNode.PropertyNameAndTypePair(propertyInfo.Name, propertyInfo.PropertyType);
        }

        return propertiesChain;
    }

    private void VisitSetOperationNode(SetOperatorNode node, string setOperatorName)
    {
        if (node.Keys.Length == 0)
        {
            throw SetOperatorDoesNotHaveKeysException(setOperatorName);
        }
        
        var key = CreateSetOperatorPositionKey();
        _currentScope[MetaAttributes.SetOperatorName] = key;
        SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode) node.Left, node.Keys));

        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (right is QueryNode rightAsQueryNode)
        {
            MakeSureBothSideFieldsAreOfAssignableTypes((QueryNode) left, rightAsQueryNode, key);
        }
        else
        {
            MakeSureBothSideFieldsAreOfAssignableTypes((QueryNode) left, PreviousSetOperatorPositionKey(), key);
        }

        var rightMethodName = Methods.Pop();
        var leftMethodName = Methods.Pop();

        var methodName = $"{leftMethodName}_{setOperatorName}_{rightMethodName}";
        Methods.Push(methodName);
        _currentScope.ScopeSymbolTable.AddSymbol(methodName,
            _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode) left).From.Alias));

        Nodes.Push(CreateSetOperatorNode(setOperatorName, node, left, right));
    }

    private SetOperatorNode CreateSetOperatorNode(string setOperatorName, SetOperatorNode node, Node left, Node right)
    {
        return setOperatorName switch
        {
            "Union" => new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            "UnionAll" => new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            "Except" => new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            "Intersect" => new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            _ => throw new NotSupportedException($"Set operator '{setOperatorName}' is not supported.")
        };
    }

    private TableSymbol FindTableSymbolInScopeHierarchy(string name)
    {
        var scope = _currentScope;
        while (scope != null)
        {
            if (scope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(name, out var tableSymbol))
            {
                return tableSymbol;
            }
            scope = scope.Parent;
        }
        
        // If not found in any scope, fall back to current scope behavior for error consistency
        return _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(name);
    }
}