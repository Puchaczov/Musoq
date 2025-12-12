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

namespace Musoq.Evaluator.Visitors;

public class BuildMetadataAndInferTypesVisitor : DefensiveVisitorBase, IAwareExpressionVisitor
{
    private readonly ISchemaProvider _provider;
    private readonly IReadOnlyDictionary<string, string[]> _columns;
    private readonly ILogger<BuildMetadataAndInferTypesVisitor> _logger;

    /// <summary>
    /// Public constructor for external use (e.g., from Musoq.Converter).
    /// </summary>
    public BuildMetadataAndInferTypesVisitor(
        ISchemaProvider _provider, 
        IReadOnlyDictionary<string, string[]> _columns, 
        ILogger<BuildMetadataAndInferTypesVisitor> _logger)
        : this(_provider, _columns, _logger, null)
    {
    }

    /// <summary>
    /// Internal constructor that allows dependency injection of ILibraryMethodResolver.
    /// Used for testing and advanced scenarios.
    /// </summary>
    internal BuildMetadataAndInferTypesVisitor(
        ISchemaProvider provider, 
        IReadOnlyDictionary<string, string[]> columns, 
        ILogger<BuildMetadataAndInferTypesVisitor> logger,
        ILibraryMethodResolver methodResolver)
    {
        _provider = provider;
        _columns = columns;
        _logger = logger;
        _methodResolver = methodResolver ?? new LibraryMethodResolver();
        _nodeFactory = new TypeConversionNodeFactory(_methodResolver);
    }
    
    private static readonly WhereNode AllTrueWhereNode =
        new(new EqualityNode(new IntegerNode("1", "s"), new IntegerNode("1", "s")));
    
    private readonly ILibraryMethodResolver _methodResolver;
    private readonly TypeConversionNodeFactory _nodeFactory;

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

    public List<Assembly> Assemblies { get; } = new(8);

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

    public virtual void Visit(Node node)
    {
    }

    /// <summary>
    /// Helper method to handle binary operators that use SafePopMultiple for error handling.
    /// </summary>
    private void VisitBinaryOperatorWithSafePop<T>(Func<Node, Node, T> nodeFactory, string operationName) where T : Node
    {
        var nodes = SafePopMultiple(Nodes, 2, operationName);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(nodeFactory(left, right));
    }

    /// <summary>
    /// Helper method to handle binary operators that use direct Pop operations.
    /// </summary>
    private void VisitBinaryOperatorWithDirectPop<T>(Func<Node, Node, T> nodeFactory) where T : Node
    {
        var right = SafePop(Nodes, "VisitBinaryOperatorWithDirectPop (right)");
        var left = SafePop(Nodes, "VisitBinaryOperatorWithDirectPop (left)");
        Nodes.Push(nodeFactory(left, right));
    }

    /// <summary>
    /// Visits a binary operator node and applies appropriate type conversions.
    /// Handles three conversion strategies:
    /// 1. Runtime operators for object types (delegates to runtime conversion methods)
    /// 2. DateTime string literal conversion (converts string literals to DateTime when comparing with DateTime _columns)
    /// 3. Numeric string/object conversion (converts strings to numbers when used with numeric literals)
    /// </summary>
    /// <typeparam name="T">Type of binary operator node to create.</typeparam>
    /// <param name="nodeFactory">Factory function to create the binary operator node.</param>
    /// <param name="isRelationalComparison">True for comparison operators (&gt;, &lt;, &gt;=, &lt;=).</param>
    /// <param name="isArithmeticOperation">True for arithmetic operators (+, -, *, /, %).</param>
    private void VisitBinaryOperatorWithTypeConversion<T>(Func<Node, Node, T> nodeFactory, bool isRelationalComparison = false, bool isArithmeticOperation = false) where T : Node
    {
        var right = SafePop(Nodes, "VisitBinaryOperatorWithTypeConversion (right)");
        var left = SafePop(Nodes, "VisitBinaryOperatorWithTypeConversion (left)");
        
        var leftIsObject = TypeConversionNodeFactory.IsObjectType(left.ReturnType);
        var rightIsObject = TypeConversionNodeFactory.IsObjectType(right.ReturnType);
        
        if (leftIsObject || rightIsObject)
        {
            var operatorMethodName = _nodeFactory.GetRuntimeOperatorMethodName(nodeFactory);
            if (operatorMethodName != null)
            {
                var wrappedNode = _nodeFactory.CreateRuntimeOperatorCall(operatorMethodName, left, right);
                Nodes.Push(wrappedNode);
                return;
            }
        }
        
        var transformedLeft = TransformStringToDateTimeIfNeeded(left, right);
        var transformedRight = TransformStringToDateTimeIfNeeded(right, left);
        
        transformedLeft = TransformToNumericTypeIfNeeded(transformedLeft, transformedRight, isRelationalComparison, isArithmeticOperation);
        transformedRight = TransformToNumericTypeIfNeeded(transformedRight, transformedLeft, isRelationalComparison, isArithmeticOperation);
        
        Nodes.Push(nodeFactory(transformedLeft, transformedRight));
    }

    private Node TransformStringToDateTimeIfNeeded(Node candidateNode, Node otherNode)
    {
        if (candidateNode is not WordNode stringNode || !TypeConversionNodeFactory.IsDateTimeType(otherNode.ReturnType))
            return candidateNode;

        return _nodeFactory.CreateDateTimeConversionNode(otherNode.ReturnType, stringNode.Value);
    }

    private Node TransformToNumericTypeIfNeeded(Node candidateNode, Node otherNode, bool isRelationalComparison, bool isArithmeticOperation)
    {
        var shouldTransform = (isRelationalComparison || isArithmeticOperation) 
            ? isArithmeticOperation ? TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType) : TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType)
            : TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType);

        if (!shouldTransform || !TypeConversionNodeFactory.IsNumericLiteralNode(otherNode, out var targetType))
            return candidateNode;

        return _nodeFactory.CreateNumericConversionNode(candidateNode, targetType, TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType), isRelationalComparison, isArithmeticOperation);
    }

    public virtual void Visit(DescNode node)
    {
        var fromNode = SafeCast<FromNode>(SafePop(Nodes, nameof(Visit) + nameof(DescNode)), nameof(Visit) + nameof(DescNode));
        Nodes.Push(new DescNode(fromNode, node.Type));
    }

    public virtual void Visit(StarNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new StarNode(left, right), isArithmeticOperation: true);
    }

    public virtual void Visit(FSlashNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new FSlashNode(left, right), isArithmeticOperation: true);
    }

    public virtual void Visit(ModuloNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new ModuloNode(left, right), isArithmeticOperation: true);
    }

    public virtual void Visit(AddNode node)
    {
        var right = SafePop(Nodes, "Visit(AddNode) right");
        var left = SafePop(Nodes, "Visit(AddNode) left");
        
        var leftIsStringLiteral = left is WordNode;
        var rightIsStringLiteral = right is WordNode;
        
        if (leftIsStringLiteral || rightIsStringLiteral)
        {
            Nodes.Push(left);
            Nodes.Push(right);
            VisitBinaryOperatorWithSafePop((l, r) => new AddNode(l, r), nameof(Visit) + nameof(AddNode));
        }
        else
        {
            Nodes.Push(left);
            Nodes.Push(right);
            VisitBinaryOperatorWithTypeConversion((l, r) => new AddNode(l, r), isArithmeticOperation: true);
        }
    }

    public virtual void Visit(HyphenNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new HyphenNode(left, right), isArithmeticOperation: true);
    }

    public virtual void Visit(AndNode node)
    {
        VisitBinaryOperatorWithSafePop((left, right) => new AndNode(left, right), nameof(Visit) + nameof(AndNode));
    }

    public virtual void Visit(OrNode node)
    {
        VisitBinaryOperatorWithSafePop((left, right) => new OrNode(left, right), nameof(Visit) + nameof(OrNode));
    }

    public virtual void Visit(ShortCircuitingNodeLeft node)
    {
        var childNode = SafePop(Nodes, nameof(Visit) + nameof(ShortCircuitingNodeLeft));
        Nodes.Push(new ShortCircuitingNodeLeft(childNode, node.UsedFor));
    }

    public virtual void Visit(ShortCircuitingNodeRight node)
    {
        var childNode = SafePop(Nodes, nameof(Visit) + nameof(ShortCircuitingNodeRight));
        Nodes.Push(new ShortCircuitingNodeRight(childNode, node.UsedFor));
    }

    public virtual void Visit(EqualityNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new EqualityNode(left, right), isRelationalComparison: false);
    }

    public virtual void Visit(GreaterOrEqualNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new GreaterOrEqualNode(left, right), isRelationalComparison: true);
    }

    public virtual void Visit(LessOrEqualNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new LessOrEqualNode(left, right), isRelationalComparison: true);
    }

    public virtual void Visit(GreaterNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new GreaterNode(left, right), isRelationalComparison: true);
    }

    public virtual void Visit(LessNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new LessNode(left, right), isRelationalComparison: true);
    }

    public virtual void Visit(DiffNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new DiffNode(left, right), isRelationalComparison: false);
    }

    public virtual void Visit(NotNode node)
    {
        var operand = SafePop(Nodes, nameof(Visit) + nameof(NotNode));
        Nodes.Push(new NotNode(operand));
    }

    public virtual void Visit(LikeNode node)
    {
        VisitBinaryOperatorWithDirectPop((left, right) => new LikeNode(left, right));
    }

    public virtual void Visit(RLikeNode node)
    {
        VisitBinaryOperatorWithDirectPop((left, right) => new RLikeNode(left, right));
    }

    public virtual void Visit(InNode node)
    {
        var right = SafePop(Nodes, nameof(Visit) + nameof(InNode) + " (right)");
        var left = SafePop(Nodes, nameof(Visit) + nameof(InNode) + " (left)");
        Nodes.Push(new InNode(left, (ArgsListNode) right));
    }

    public virtual void Visit(FieldNode node)
    {
        var expression = SafePop(Nodes, nameof(Visit) + nameof(FieldNode));
        Nodes.Push(new FieldNode(expression, node.FieldOrder, node.FieldName));
    }

    public virtual void Visit(FieldOrderedNode node)
    {
        var expression = SafePop(Nodes, nameof(Visit) + nameof(FieldOrderedNode));
        Nodes.Push(new FieldOrderedNode(expression, node.FieldOrder, node.FieldName, node.Order));
    }

    public virtual void Visit(SelectNode node)
    {
        var fields = CreateFields(node.Fields);

        Nodes.Push(new SelectNode(fields.ToArray()));
    }

    public virtual void Visit(GroupSelectNode node)
    {
        var fields = CreateFields(node.Fields);

        Nodes.Push(new GroupSelectNode(fields.ToArray()));
    }

    public virtual void Visit(StringNode node)
    {
        AddAssembly(typeof(string).Assembly);
        Nodes.Push(new StringNode(node.Value));
        _schemaFromArgs.Add(node.Value);
    }

    public virtual void Visit(DecimalNode node)
    {
        AddAssembly(typeof(decimal).Assembly);
        Nodes.Push(new DecimalNode(node.Value));
        _schemaFromArgs.Add(node.Value);
    }

    public virtual void Visit(IntegerNode node)
    {
        AddAssembly(typeof(int).Assembly);
        Nodes.Push(new IntegerNode(node.ObjValue));
        _schemaFromArgs.Add(node.ObjValue);
    }

    public virtual void Visit(HexIntegerNode node)
    {
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new HexIntegerNode(node.ObjValue));
        _schemaFromArgs.Add(node.ObjValue);
    }

    public virtual void Visit(BinaryIntegerNode node)
    {
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new BinaryIntegerNode(node.ObjValue));
        _schemaFromArgs.Add(node.ObjValue);
    }

    public virtual void Visit(OctalIntegerNode node)
    {
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new OctalIntegerNode(node.ObjValue));
        _schemaFromArgs.Add(node.ObjValue);
    }

    public virtual void Visit(BooleanNode node)
    {
        AddAssembly(typeof(bool).Assembly);
        Nodes.Push(new BooleanNode(node.Value));
        _schemaFromArgs.Add(node.Value);
    }

    public virtual void Visit(WordNode node)
    {
        AddAssembly(typeof(string).Assembly);
        Nodes.Push(new WordNode(node.Value));
        _schemaFromArgs.Add(node.Value);
    }

    public virtual void Visit(NullNode node)
    {
        Nodes.Push(new NullNode(node.ReturnType));
    }

    public virtual void Visit(ContainsNode node)
    {
        var right = SafePop(Nodes, nameof(Visit) + nameof(ContainsNode) + " (right)");
        var left = SafePop(Nodes, nameof(Visit) + nameof(ContainsNode) + " (left)");
        Nodes.Push(new ContainsNode(left, right as ArgsListNode));
    }

    public virtual void Visit(AccessMethodNode node)
    {
        VisitAccessMethod(node,
            (token, modifiedNode, exArgs, arg3, alias, canSkipInjectSource) =>
                new AccessMethodNode(token, modifiedNode as ArgsListNode, exArgs, canSkipInjectSource, arg3, alias));
    }

    public virtual void Visit(AccessRawIdentifierNode node)
    {
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public virtual void Visit(IsNullNode node)
    {
        var operand = SafePop(Nodes, nameof(Visit) + nameof(IsNullNode));
        Nodes.Push(new IsNullNode(operand, node.IsNegated));
    }

    public virtual void Visit(AccessRefreshAggregationScoreNode node)
    {
        VisitAccessMethod(node,
            (token, node1, exArgs, arg3, alias, _) =>
                new AccessRefreshAggregationScoreNode(token, node1 as ArgsListNode, exArgs, node.CanSkipInjectSource,
                    arg3, alias));
    }

    public virtual void Visit(AccessColumnNode node)
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

    public virtual void Visit(AllColumnsNode node)
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

    public virtual void Visit(IdentifierNode node)
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

    public virtual void Visit(AccessObjectArrayNode node)
    {
        if (node.IsColumnAccess)
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(
                string.IsNullOrEmpty(node.TableAlias) ? _identifier : node.TableAlias);
            
            if (tableSymbol == null)
            {
                throw new UnknownPropertyException($"Table {node.TableAlias ?? _identifier} could not be found.");
            }

            var column = tableSymbol.GetColumnByAliasAndName(
                string.IsNullOrEmpty(node.TableAlias) ? _identifier : node.TableAlias, 
                node.ObjectName);

            if (column == null)
            {
                throw new UnknownPropertyException($"Column {node.ObjectName} could not be found.");
            }

            Nodes.Push(node);
            return;
        }

        var parentNode = Nodes.Count > 0 ? Nodes.Peek() : null;
        var parentNodeType = parentNode?.ReturnType;
        
        bool hasValidParentContext = parentNode != null && parentNodeType != null &&
                                    !parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)) &&
                                    parentNodeType.Name != "RowSource" &&
                                    !BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(parentNodeType);
        
        if (!hasValidParentContext)
        {
            var currentTableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
            var column = currentTableSymbol?.GetColumnByAliasAndName(_identifier, node.ObjectName);
            if (column != null && BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(column.ColumnType))
            {
                var columnAccessNode = new AccessObjectArrayNode(node.Token, column.ColumnType);
                Nodes.Push(columnAccessNode);
                return;
            }
        }

        if (parentNodeType != null && parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
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

            if (isNotRoot && parentNodeType != null)
            {
                PropertyInfo propertyAccess;
                try
                {
                    propertyAccess = parentNodeType.GetProperty(node.Name);
                }
                catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
                {
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
                }

                isArray = propertyAccess?.PropertyType.IsArray == true;
                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(propertyAccess?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    throw new ObjectIsNotAnArrayException(
                        $"Object {parentNodeType.Name} property '{node.Name}' is not an array or indexable type.");
                }

                if (propertyAccess == null)
                {
                    throw new UnknownPropertyException($"Property '{node.Name}' not found on object {parentNodeType.Name}.");
                }

                Nodes.Push(new AccessObjectArrayNode(node.Token, propertyAccess));

                return;
            }

            if (parentNodeType != null)
            {
                PropertyInfo property;
                try
                {
                    property = parentNodeType.GetProperty(node.Name);
                }
                catch (Exception ex) when (ex is AmbiguousMatchException || ex is ArgumentException)
                {
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
                }

                isArray = property?.PropertyType.IsArray == true;
                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(property?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    throw new ObjectIsNotAnArrayException(
                        $"Object {node.Name} is not an array or indexable type.");
                }

                if (property == null)
                {
                    throw new UnknownPropertyException($"Property '{node.Name}' not found on object {parentNodeType.Name}.");
                }

                Nodes.Push(new AccessObjectArrayNode(node.Token, property));
            }
            else
            {
                throw new UnknownPropertyException($"Could not resolve array access for {node.ObjectName}[{node.Token.Index}]");
            }
        }
    }

    public virtual void Visit(AccessObjectKeyNode node)
    {
        if (node.DestinationKind == AccessObjectKeyNode.Destination.Variable)
        {
            throw new ConstructionNotYetSupported($"Construction ${node.ToString()} is not yet supported.");
        }

        var parentNode = SafePeek(Nodes, nameof(Visit) + nameof(AccessObjectKeyNode));
        if (parentNode?.ReturnType == null)
        {
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                nameof(Visit) + nameof(AccessObjectKeyNode),
                $"Parent node has no return type for key access '{node.Name}'"
            );
        }
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
                PropertyInfo propertyAccess = null;
                try
                {
                    propertyAccess = parentNodeType.GetProperty(node.Name);
                }
                catch (Exception ex) when (ex is AmbiguousMatchException || ex is ArgumentException)
                {
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
                }

                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(propertyAccess?.PropertyType);

                if (!isIndexer)
                {
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Object {parentNodeType.Name} property '{node.Name}' does not implement indexer.");
                }

                if (propertyAccess == null)
                {
                    throw new UnknownPropertyException($"Property '{node.Name}' not found on object {parentNodeType.Name}.");
                }

                Nodes.Push(new AccessObjectKeyNode(node.Token, propertyAccess));

                return;
            }

            PropertyInfo property = null;
            try
            {
                property = parentNodeType.GetProperty(node.Name);
            }
            catch (Exception ex) when (ex is AmbiguousMatchException || ex is ArgumentException)
            {
                throw new ObjectDoesNotImplementIndexerException(
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
            }

            isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(property?.PropertyType);

            if (!isIndexer)
            {
                throw new ObjectDoesNotImplementIndexerException(
                    $"Object {node.Name} does not implement indexer.");
            }

            if (property == null)
            {
                throw new UnknownPropertyException($"Property '{node.Name}' not found on object {parentNodeType.Name}.");
            }

            Nodes.Push(new AccessObjectKeyNode(node.Token, property));
        }
    }

    public virtual void Visit(PropertyValueNode node)
    {
        var parentNode = SafePeek(Nodes, nameof(Visit) + nameof(PropertyValueNode));
        if (parentNode?.ReturnType == null)
        {
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                nameof(Visit) + nameof(PropertyValueNode),
                $"Parent node has no return type for property access '{node.Name}'"
            );
        }
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

            Type type;
            try
            {
                var propertyInfo = parentNode.ReturnType.GetProperty(node.Name);
                type = propertyInfo?.PropertyType ?? 
                       (_theMostInnerIdentifier?.Name == node.Name ? typeof(object) : typeof(ExpandoObject));
            }
            catch (Exception ex) when (ex is AmbiguousMatchException || ex is ArgumentException)
            {
                type = _theMostInnerIdentifier?.Name == node.Name ? typeof(object) : typeof(ExpandoObject);
            }
            Nodes.Push(new PropertyValueNode(node.Name, new ExpandoObjectPropertyInfo(node.Name, type)));
        }
        else
        {
            PropertyInfo propertyInfo = null;
            try
            {
                propertyInfo = parentNodeType.GetProperty(node.Name);
            }
            catch (Exception ex) when (ex is AmbiguousMatchException || ex is ArgumentException)
            {
                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    nameof(Visit) + nameof(PropertyValueNode),
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
            }

            if (propertyInfo == null)
            {
                throw new UnknownPropertyException($"Property '{node.Name}' not found on object {parentNodeType.Name}");
            }

            Nodes.Push(new PropertyValueNode(node.Name, propertyInfo));
        }
    }

    public virtual void Visit(DotNode node)
    {
        var exp = SafePop(Nodes, nameof(Visit) + nameof(DotNode) + " (expression)");
        var root = SafePop(Nodes, nameof(Visit) + nameof(DotNode) + " (root)");

        if (root?.ReturnType == null)
        {
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                nameof(Visit) + nameof(DotNode),
                "Root node has no return type for dot access");
        }

        if (root is AccessColumnNode accessColumnNode && exp is AccessObjectArrayNode arrayNode2 && !arrayNode2.IsColumnAccess)
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(accessColumnNode.Alias);
            if (tableSymbol != null)
            {
                var column = tableSymbol.GetColumnByAliasAndName(accessColumnNode.Alias, arrayNode2.ObjectName);
                if (column != null && BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(column.ColumnType))
                {
                    var columnAccessArrayNode = new AccessObjectArrayNode(arrayNode2.Token, column.ColumnType, accessColumnNode.Alias);
                    Nodes.Push(columnAccessArrayNode);
                    return;
                }
            }
        }

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
        var chainPretend = SafePop(Nodes, nameof(Visit) + nameof(AccessCallChainNode));

        Nodes.Push(chainPretend is AccessColumnNode
            ? chainPretend
            : new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
    }

    public virtual void Visit(ArgsListNode node)
    {
        var args = new Node[node.Args.Length];

        for (var i = node.Args.Length - 1; i >= 0; --i)
            args[i] = SafePop(Nodes, nameof(Visit) + nameof(ArgsListNode) + $" (arg {i})");

        Nodes.Push(new ArgsListNode(args));
    }

    public virtual void Visit(WhereNode node)
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

    public virtual void Visit(GroupByNode node)
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
        var schema = _provider.GetSchema(node.Schema);
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
                node.QueryId.ToString(),
                CancellationToken.None,
                _columns[_queryAlias + _schemaFromKey].Select((f, i) => new SchemaColumn(f, i, typeof(object)))
                    .ToArray(),
                environmentVariables,
                (aliasedSchemaFromNode, [], AllTrueWhereNode, hasExternallyProvidedTypes),
                _logger
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

    public virtual void Visit(SchemaMethodFromNode node)
    {
        _usedSchemasQuantity += 1;
        Nodes.Push(new Parser.SchemaMethodFromNode(node.Alias, node.Schema, node.Method));
    }

    public virtual void Visit(PropertyFromNode node)
    {
        ISchemaTable table;
        ISchema schema;

        if (_aliasToSchemaFromNodeMap.TryGetValue(node.SourceAlias, out var schemaFrom))
        {
            schema = _provider.GetSchema(schemaFrom.Schema);
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

    public virtual void Visit(AccessMethodFromNode node)
    {
        ISchemaTable table;
        ISchema schema;

        if (_aliasToSchemaFromNodeMap.TryGetValue(node.SourceAlias, out var schemaFrom))
        {
            schema = _provider.GetSchema(schemaFrom.Schema);
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

    public virtual void Visit(AliasedFromNode node)
    {
        var schemaInfo = _explicitlyUsedAliases[node.Identifier];
        var tableName = _explicitlyCoupledTablesWithAliases[node.Identifier];
        var table = _explicitlyDefinedTables[tableName];
        const bool hasExternallyProvidedTypes = true;

        var schema = _provider.GetSchema(schemaInfo.Schema);

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
                node.InSourcePosition.ToString(),
                CancellationToken.None,
                table.Columns,
                RetrieveEnvironmentVariables(_positionalEnvironmentVariablesKey, aliasedSchemaFromNode),
                (aliasedSchemaFromNode, [], AllTrueWhereNode, hasExternallyProvidedTypes),
                _logger
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

    public virtual void Visit(JoinSourcesTableFromNode node)
    {
        var exp = Nodes.Pop();
        var b = (FromNode) Nodes.Pop();
        var a = (FromNode) Nodes.Pop();

        Nodes.Push(new Parser.JoinSourcesTableFromNode(a, b, exp, node.JoinType));
    }

    public virtual void Visit(ApplySourcesTableFromNode node)
    {
        var b = (FromNode) Nodes.Pop();
        var a = (FromNode) Nodes.Pop();

        Nodes.Push(new Parser.ApplySourcesTableFromNode(a, b, node.ApplyType));
    }

    public virtual void Visit(InMemoryTableFromNode node)
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

    public virtual void Visit(JoinFromNode node)
    {
        var expression = Nodes.Pop();
        var joinedTable = (FromNode) Nodes.Pop();
        var source = (FromNode) Nodes.Pop();
        var joinedFrom = new Parser.JoinFromNode(source, joinedTable, expression, node.JoinType);
        _identifier = joinedFrom.Alias;
        _schemaFromArgs.Clear();
        Nodes.Push(joinedFrom);
    }

    public virtual void Visit(ApplyFromNode node)
    {
        var appliedTable = (FromNode) Nodes.Pop();
        var source = (FromNode) Nodes.Pop();
        var appliedFrom = new Parser.ApplyFromNode(source, appliedTable, node.ApplyType);
        _identifier = appliedFrom.Alias;
        _schemaFromArgs.Clear();
        Nodes.Push(appliedFrom);
    }

    public virtual void Visit(ExpressionFromNode node)
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

    public virtual void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        var exp = Nodes.Pop();
        var from = (FromNode) Nodes.Pop();
        Nodes.Push(
            new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType));
    }

    public virtual void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        var from = (FromNode) Nodes.Pop();
        Nodes.Push(
            new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType));
    }

    public virtual void Visit(InternalQueryNode node)
    {
        throw new NotSupportedException("Internal Query Node is not supported here");
    }

    public virtual void Visit(RootNode node)
    {
        Nodes.Push(new RootNode(Nodes.Pop()));
    }

    public virtual void Visit(SingleSetNode node)
    {
    }

    public virtual void Visit(RefreshNode node)
    {
    }

    public virtual void Visit(UnionNode node)
    {
        VisitSetOperationNode(node, "Union");
    }

    public virtual void Visit(UnionAllNode node)
    {
        VisitSetOperationNode(node, "UnionAll");
    }

    public virtual void Visit(ExceptNode node)
    {
        VisitSetOperationNode(node, "Except");
    }

    public virtual void Visit(IntersectNode node)
    {
        VisitSetOperationNode(node, "Intersect");
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

    public virtual void Visit(JoinNode node)
    {
        _identifier = node.Alias;
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode) Nodes.Pop()));
    }

    public virtual void Visit(ApplyNode node)
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
            reorderedList[i] = Nodes.Pop() as FieldNode;
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
        var args = GetAndValidateArgs(node);
        var methodContext = ResolveMethodContext(node, args);
        var (method, canSkipInjectSource) = ResolveMethod(node, args, methodContext);
        
        method = ProcessGenericMethodIfNeeded(method, args, methodContext.EntityType);
        
        var accessMethod = CreateAccessMethod(node, args, method, methodContext, canSkipInjectSource, func);
        
        node.ChangeMethod(method);
        FinalizeMethodVisit(method, accessMethod);
    }

    /// <summary>
    /// Extracts and validates arguments from the node stack.
    /// </summary>
    private ArgsListNode GetAndValidateArgs(AccessMethodNode node)
    {
        var nodeFromStack = SafePop(Nodes, nameof(GetAndValidateArgs));
        if (nodeFromStack is not ArgsListNode args)
            throw CannotResolveMethodException.CreateForNullArguments(node.Name);
        return args;
    }

    /// <summary>
    /// Contains context information needed for method resolution.
    /// </summary>
    private record struct MethodResolutionContext(
        string Alias,
        TableSymbol TableSymbol,
        (ISchema Schema, ISchemaTable Table, string TableName) SchemaTablePair,
        Type EntityType);

    /// <summary>
    /// Resolves the method context including schema, table, and entity information.
    /// </summary>
    private MethodResolutionContext ResolveMethodContext(AccessMethodNode node, ArgsListNode args)
    {
        if (_usedSchemasQuantity > 1 && string.IsNullOrWhiteSpace(node.Alias))
            throw new AliasMissingException(node);
        
        var alias = !string.IsNullOrEmpty(node.Alias) ? node.Alias : _identifier;
        var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(alias);
        var schemaTablePair = tableSymbol.GetTableByAlias(alias);
        var entityType = schemaTablePair.Table.Metadata.TableEntityType;

        AddAssembly(entityType.Assembly);
        AddBaseTypeAssembly(entityType);

        return new MethodResolutionContext(alias, tableSymbol, schemaTablePair, entityType);
    }

    private (MethodInfo Method, bool CanSkipInjectSource) ResolveMethod(AccessMethodNode node, ArgsListNode args, MethodResolutionContext context)
    {
        var argCount = args.Args.Length;
        var argTypes = new Type[argCount];
        
        for (var i = 0; i < argCount; i++)
        {
            argTypes[i] = args.Args[i].ReturnType;
        }
        var groupArgCount = argCount > 0 ? argCount : 1;
        var groupArgTypes = new Type[groupArgCount];
        groupArgTypes[0] = typeof(string);
        for (var i = 1; i < argCount; i++)
        {
            groupArgTypes[i] = argTypes[i];
        }

        if (context.SchemaTablePair.Schema.TryResolveAggregationMethod(node.Name, groupArgTypes, context.EntityType, out var method))
        {
            return (method, false);
        }

        if (context.SchemaTablePair.Schema.TryResolveMethod(node.Name, argTypes, context.EntityType, out method))
        {
            return (method, false);
        }

        if (context.SchemaTablePair.Schema.TryResolveRawMethod(node.Name, argTypes, out method))
        {
            return (method, true);
        }

        throw CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(node.Name, args.Args);
    }

    /// <summary>
    /// Processes generic methods by reducing dimensions or constructing generic types if needed.
    /// </summary>
    private MethodInfo ProcessGenericMethodIfNeeded(MethodInfo method, ArgsListNode args, Type entityType)
    {
        var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

        if (!isAggregateMethod && method.IsGenericMethod && TryReduceDimensions(method, args, out var reducedMethod))
        {
            method = reducedMethod;
        }

        if (!isAggregateMethod && 
            method.IsGenericMethod && 
            !method.IsConstructedGenericMethod &&
            TryConstructGenericMethod(method, args, entityType, out var constructedMethod))
        {
            method = constructedMethod;
        }

        return method;
    }

    /// <summary>
    /// Creates the appropriate access method based on whether it's an aggregation method or not.
    /// </summary>
    private AccessMethodNode CreateAccessMethod(
        AccessMethodNode node, 
        ArgsListNode args, 
        MethodInfo method, 
        MethodResolutionContext context, 
        bool canSkipInjectSource,
        Func<FunctionToken, Node, ArgsListNode, MethodInfo, string, bool, AccessMethodNode> func)
    {
        var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

        if (isAggregateMethod)
        {
            return ProcessAggregateMethod(node, args, method, context, func);
        }
        
        return func(node.FunctionToken, args, new ArgsListNode([]), method, context.Alias, canSkipInjectSource);
    }

    /// <summary>
    /// Processes aggregate methods by creating both the method and its corresponding "Set" method.
    /// </summary>
    private AccessMethodNode ProcessAggregateMethod(
        AccessMethodNode node, 
        ArgsListNode args, 
        MethodInfo method, 
        MethodResolutionContext context,
        Func<FunctionToken, Node, ArgsListNode, MethodInfo, string, bool, AccessMethodNode> func)
    {
        var accessMethod = func(node.FunctionToken, args, node.ExtraAggregateArguments, method, context.Alias, false);
        var identifier = accessMethod.ToString();

        var newArgs = new List<Node> { new WordNode(identifier) };
        newArgs.AddRange(args.Args.Skip(1));

        var newSetArgs = new List<Node> { new WordNode(identifier) };
        newSetArgs.AddRange(args.Args);

        var setMethodName = $"Set{method.Name}";
        var argTypes = newSetArgs.Select(f => f.ReturnType).ToArray();

        if (!context.SchemaTablePair.Schema.TryResolveAggregationMethod(setMethodName, argTypes, context.EntityType, out var setMethod))
        {
            throw CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(setMethodName, newSetArgs.ToArray());
        }

        if (setMethod.IsGenericMethodDefinition)
        {
            (method, setMethod) = MakeGenericAggregationMethods(method, setMethod, newSetArgs);
        }

        var setMethodNode = func(new FunctionToken(setMethodName, TextSpan.Empty),
            new ArgsListNode(newSetArgs.ToArray()), null, setMethod, context.Alias, false);

        _refreshMethods.Add(setMethodNode);

        return func(node.FunctionToken, new ArgsListNode(newArgs.ToArray()), null, method, context.Alias, false);
    }

    /// <summary>
    /// Creates generic versions of aggregation methods when needed.
    /// </summary>
    private (MethodInfo Method, MethodInfo SetMethod) MakeGenericAggregationMethods(
        MethodInfo method, 
        MethodInfo setMethod, 
        List<Node> newSetArgs)
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
                    genericArgumentsDistinct.Add(newSetArgs.Where((arg, index) => index == i - 1).Single().ReturnType);
                }
            }
        }

        var genericArgumentsConcreteTypes = genericArgumentsDistinct.Distinct().ToArray();

        return (method.MakeGenericMethod(genericArgumentsConcreteTypes), 
                setMethod.MakeGenericMethod(genericArgumentsConcreteTypes));
    }

    /// <summary>
    /// Finalizes the method visit by adding required assemblies and pushing the result.
    /// </summary>
    private void FinalizeMethodVisit(MethodInfo method, AccessMethodNode accessMethod)
    {
        if (method.DeclaringType == null)
            throw new InvalidOperationException("Method must have a declaring type.");

        AddAssembly(method.DeclaringType.Assembly);
        AddAssembly(method.ReturnType.Assembly);

        Nodes.Push(accessMethod);
    }

    public virtual void Visit(OrderByNode node)
    {
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode) Nodes.Pop();

        Nodes.Push(new OrderByNode(fields));
    }

    public virtual void Visit(CreateTableNode node)
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

    public virtual void Visit(CoupleNode node)
    {
        _explicitlyCoupledTablesWithAliases.Add(node.MappedSchemaName, node.TableName);
        _explicitlyUsedAliases.Add(node.MappedSchemaName, node.SchemaMethodNode);
        Nodes.Push(new CoupleNode(node.SchemaMethodNode, node.TableName, node.MappedSchemaName));
    }

    public virtual void Visit(StatementsArrayNode node)
    {
        var statements = new StatementNode[node.Statements.Length];
        for (var i = 0; i < node.Statements.Length; ++i)
        {
            statements[node.Statements.Length - 1 - i] = (StatementNode) Nodes.Pop();
        }

        Nodes.Push(new StatementsArrayNode(statements));
    }

    public virtual void Visit(StatementNode node)
    {
        Nodes.Push(new StatementNode(Nodes.Pop()));
    }

    public virtual void Visit(CaseNode node)
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
                : new CaseNode(whenThenPairs.ToArray(), elseNode, BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(greatestCommonSubtype));

            Nodes.Push(caseNode);
        }
        else
        {
            var greatestCommonSubtype = FindGreatestCommonSubtype();
            var nullableGreatestCommonSubtype = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(greatestCommonSubtype);
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

    public virtual void Visit(WhenNode node)
    {
        var newNode = new WhenNode(Nodes.Pop());

        Nodes.Push(newNode);
    }

    public virtual void Visit(ThenNode node)
    {
        var newNode = new ThenNode(Nodes.Pop());

        _nullSuspiciousTypes.Add(newNode.ReturnType);

        Nodes.Push(newNode);
    }

    public virtual void Visit(ElseNode node)
    {
        var newNode = new ElseNode(Nodes.Pop());

        _nullSuspiciousTypes.Add(newNode.ReturnType);

        Nodes.Push(newNode);
    }

    public virtual void Visit(FieldLinkNode node)
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
        var types = _nullSuspiciousTypes.Where(type => type != NullNode.NullType.Instance).Select(BuildMetadataAndInferTypesVisitorUtilities.StripNullable)
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
                greatestCommonSubtype = currentType;
                continue;
            }

            if (currentType.IsAssignableTo(greatestCommonSubtype))
            {
                continue;
            }

            greatestCommonSubtype = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(greatestCommonSubtype, currentType);
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

        var positionCounter = 0;
        for (var i = 0; i < table.Columns.Length; i++)
        {
            if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(table.Columns[i].ColumnType))
            {
                AddColumnToGeneratedColumns(tableSymbol, table.Columns[i], positionCounter++, generatedColumnIdentifier, generatedColumns);
            }
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

            var positionCounter = 0;
            for (var i = 0; i < table.Columns.Length; i++)
            {
                if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(table.Columns[i].ColumnType))
                {
                    AddColumnToGeneratedColumns(tableSymbol, table.Columns[i], positionCounter++, tableIdentifier, generatedColumns, isCompoundTable: true);
                }
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
            schemaFrom.QueryId.ToString(),
            CancellationToken.None,
            _columns[schemaFrom.Alias + _schemaFromKey].Select((f, i) => new SchemaColumn(f, i, typeof(object))).ToArray(),
            RetrieveEnvironmentVariables(_schemaFromInfo[schemaFrom.Alias].PositionalEnvironmentVariableKey, schemaFrom),
            (schemaFrom, [], AllTrueWhereNode, false),
            _logger
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

    private static void PrepareAndThrowUnknownColumnExceptionMessage(string identifier, ISchemaColumn[] _columns)
    {
        var library = new TransitionLibrary();
        var candidates = new StringBuilder();

        var candidatesColumns = _columns.Where(
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
        var _columns = new List<ISchemaColumn>();

        Type nestedType;
        if (type.IsArray)
        {
            nestedType = type.GetElementType();
        }
        else if (IsGenericEnumerable(type, out nestedType))
        {
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
            _columns.Add(new SchemaColumn(property.Name, _columns.Count, property.PropertyType));
        }
    
        return new DynamicTable(_columns.ToArray(), nestedType);
    }

    private static bool IsGenericEnumerable(Type type, out Type elementType)
    {
        elementType = null;
    
        if (!type.IsGenericType) return false;
            
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
        SetOperatorFieldPositions.Add(key, BuildMetadataAndInferTypesVisitorUtilities.CreateSetOperatorPositionIndexes((QueryNode) node.Left, node.Keys));

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
        
        return _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(name);
    }
}
