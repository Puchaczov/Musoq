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
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.Api;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using InterpretFromNode = Musoq.Parser.Nodes.From.InterpretFromNode;
using JoinFromNode = Musoq.Parser.Nodes.From.JoinFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;
using NotSupportedException = System.NotSupportedException;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.Visitors;

public class BuildMetadataAndInferTypesVisitor : DefensiveVisitorBase, IAwareExpressionVisitor
{
    private static readonly WhereNode AllTrueWhereNode =
        new(new EqualityNode(new IntegerNode("1", "s"), new IntegerNode("1", "s")));

    private readonly IDictionary<string, string> _aliasMapToInMemoryTableMap =
        new Dictionary<string, string>();

    private readonly IDictionary<string, SchemaFromNode> _aliasToSchemaFromNodeMap =
        new Dictionary<string, SchemaFromNode>();

    private readonly IDictionary<string, FieldNode[]> _cachedSetFields =
        new Dictionary<string, FieldNode[]>();

    private readonly IReadOnlyDictionary<string, string[]> _columns;
    private readonly CompilationOptions _compilationOptions;

    private readonly IDictionary<string, string> _explicitlyCoupledTablesWithAliases =
        new Dictionary<string, string>();

    private readonly IDictionary<string, ISchemaTable> _explicitlyDefinedTables =
        new Dictionary<string, ISchemaTable>();

    private readonly IDictionary<string, SchemaMethodFromNode> _explicitlyUsedAliases =
        new Dictionary<string, SchemaMethodFromNode>();

    private readonly List<string> _generatedAliases = [];

    private readonly Dictionary<string, List<FieldNode>> _generatedColumns = [];

    private readonly List<FieldNode> _groupByFields = [];

    private readonly IDictionary<SchemaFromNode, ISchemaColumn[]> _inferredColumns =
        new Dictionary<SchemaFromNode, ISchemaColumn[]>();

    private readonly ILogger<BuildMetadataAndInferTypesVisitor> _logger;

    private readonly ILibraryMethodResolver _methodResolver;
    private readonly TypeConversionNodeFactory _nodeFactory;
    private readonly List<Type> _nullSuspiciousTypes = [];
    private readonly ISchemaProvider _provider;

    private readonly IDictionary<SchemaFromNode, QueryHints> _queryHintsPerSchema =
        new Dictionary<SchemaFromNode, QueryHints>();

    private readonly List<AccessMethodNode> _refreshMethods = [];
    private readonly List<object> _schemaFromArgs = [];

    private readonly IDictionary<string, (int SchemaFromKey, uint PositionalEnvironmentVariableKey)> _schemaFromInfo =
        new Dictionary<string, (int, uint)>();

    private readonly SchemaRegistry? _schemaRegistry;

    private readonly IDictionary<SchemaFromNode, List<ISchemaColumn>> _usedColumns =
        new Dictionary<SchemaFromNode, List<ISchemaColumn>>();

    private readonly IDictionary<SchemaFromNode, WhereNode> _usedWhereNodes =
        new Dictionary<SchemaFromNode, WhereNode>();

    protected readonly Dictionary<uint, IReadOnlyDictionary<string, string>> InternalPositionalEnvironmentVariables =
        new();

    private Scope _currentScope;
    private string _identifier;
    private uint _positionalEnvironmentVariablesKey;
    private string _queryAlias;

    private QueryPart _queryPart;
    private int _schemaFromKey;

    private int _setKey;
    private IdentifierNode _theMostInnerIdentifier;

    private int _usedSchemasQuantity;

    /// <summary>
    ///     Public constructor for external use (e.g., from Musoq.Converter).
    /// </summary>
    public BuildMetadataAndInferTypesVisitor(
        ISchemaProvider provider,
        IReadOnlyDictionary<string, string[]> columns,
        ILogger<BuildMetadataAndInferTypesVisitor> logger,
        CompilationOptions? compilationOptions = null,
        SchemaRegistry? schemaRegistry = null)
        : this(provider, columns, logger, null, compilationOptions, schemaRegistry, null)
    {
    }

    /// <summary>
    ///     Public constructor for LSP/diagnostic use with error collection.
    /// </summary>
    public BuildMetadataAndInferTypesVisitor(
        ISchemaProvider provider,
        IReadOnlyDictionary<string, string[]> columns,
        ILogger<BuildMetadataAndInferTypesVisitor> logger,
        DiagnosticContext diagnosticContext,
        CompilationOptions? compilationOptions = null,
        SchemaRegistry? schemaRegistry = null)
        : this(provider, columns, logger, null, compilationOptions, schemaRegistry, diagnosticContext)
    {
    }

    internal BuildMetadataAndInferTypesVisitor(
        ISchemaProvider provider,
        IReadOnlyDictionary<string, string[]> columns,
        ILogger<BuildMetadataAndInferTypesVisitor> logger,
        ILibraryMethodResolver? methodResolver,
        CompilationOptions? compilationOptions = null,
        SchemaRegistry? schemaRegistry = null,
        DiagnosticContext? diagnosticContext = null)
    {
        _provider = provider;
        _columns = columns;
        _logger = logger;
        _methodResolver = methodResolver ?? new LibraryMethodResolver();
        _nodeFactory = new TypeConversionNodeFactory(_methodResolver);
        _compilationOptions = compilationOptions ?? new CompilationOptions();
        _schemaRegistry = schemaRegistry;
        DiagnosticContext = diagnosticContext;
    }

    /// <summary>
    ///     Gets whether diagnostics are being collected instead of throwing exceptions.
    /// </summary>
    protected bool IsCollectingDiagnostics => DiagnosticContext != null;

    /// <summary>
    ///     Gets the diagnostic context if available.
    /// </summary>
    protected DiagnosticContext? DiagnosticContext { get; }

    protected override string VisitorName => nameof(BuildMetadataAndInferTypesVisitor);

    private Stack<string> Methods { get; } = new();

    protected Stack<Node> Nodes { get; } = new();

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
                result.Add(aliasColumnsPair.Key, aliasColumnsPair.Value.ToArray());

            return result;
        }
    }

    public IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> UsedColumns
    {
        get
        {
            var result = new Dictionary<SchemaFromNode, ISchemaColumn[]>();

            foreach (var aliasColumnsPair in _usedColumns)
                result.Add(aliasColumnsPair.Key, aliasColumnsPair.Value.ToArray());

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

    public IReadOnlyDictionary<SchemaFromNode, QueryHints> QueryHintsPerSchema
    {
        get
        {
            return _queryHintsPerSchema.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);
        }
    }

    public RootNode Root => (RootNode)Nodes.Peek();

    public override void Visit(Node node)
    {
    }

    public override void Visit(DescNode node)
    {
        var fromNode = SafeCast<FromNode>(SafePop(Nodes, VisitorOperationNames.VisitDescNode),
            VisitorOperationNames.VisitDescNode);
        Nodes.Push(new DescNode(fromNode, node.Type, node.Column));
    }

    public override void Visit(StarNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new StarNode(left, right), isArithmeticOperation: true);
    }

    public override void Visit(FSlashNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new FSlashNode(left, right),
            isArithmeticOperation: true);
    }

    public override void Visit(ModuloNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new ModuloNode(left, right),
            isArithmeticOperation: true);
    }

    public override void Visit(AddNode node)
    {
        var right = SafePop(Nodes, "Visit(AddNode) right");
        var left = SafePop(Nodes, "Visit(AddNode) left");

        var leftIsStringLiteral = left is WordNode;
        var rightIsStringLiteral = right is WordNode;

        if (leftIsStringLiteral || rightIsStringLiteral)
        {
            Nodes.Push(left);
            Nodes.Push(right);
            VisitBinaryOperatorWithSafePop((l, r) => new AddNode(l, r), VisitorOperationNames.VisitAddNode);
        }
        else
        {
            Nodes.Push(left);
            Nodes.Push(right);
            VisitBinaryOperatorWithTypeConversion((l, r) => new AddNode(l, r), isArithmeticOperation: true);
        }
    }

    public override void Visit(HyphenNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new HyphenNode(left, right),
            isArithmeticOperation: true);
    }

    public override void Visit(AndNode node)
    {
        VisitBinaryOperatorWithSafePop((left, right) => new AndNode(left, right), VisitorOperationNames.VisitAndNode);
    }

    public override void Visit(OrNode node)
    {
        VisitBinaryOperatorWithSafePop((left, right) => new OrNode(left, right), VisitorOperationNames.VisitOrNode);
    }

    public override void Visit(BitwiseAndNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseAndNode(left, right),
            isArithmeticOperation: true);
    }

    public override void Visit(BitwiseOrNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseOrNode(left, right),
            isArithmeticOperation: true);
    }

    public override void Visit(BitwiseXorNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseXorNode(left, right),
            isArithmeticOperation: true);
    }

    public override void Visit(LeftShiftNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new LeftShiftNode(left, right),
            isArithmeticOperation: true);
    }

    public override void Visit(RightShiftNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new RightShiftNode(left, right),
            isArithmeticOperation: true);
    }

    public override void Visit(ShortCircuitingNodeLeft node)
    {
        var childNode = SafePop(Nodes, VisitorOperationNames.VisitShortCircuitingNodeLeft);
        Nodes.Push(new ShortCircuitingNodeLeft(childNode, node.UsedFor));
    }

    public override void Visit(ShortCircuitingNodeRight node)
    {
        var childNode = SafePop(Nodes, VisitorOperationNames.VisitShortCircuitingNodeRight);
        Nodes.Push(new ShortCircuitingNodeRight(childNode, node.UsedFor));
    }

    public override void Visit(EqualityNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new EqualityNode(left, right));
    }

    public override void Visit(GreaterOrEqualNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new GreaterOrEqualNode(left, right), true);
    }

    public override void Visit(LessOrEqualNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new LessOrEqualNode(left, right), true);
    }

    public override void Visit(GreaterNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new GreaterNode(left, right), true);
    }

    public override void Visit(LessNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new LessNode(left, right), true);
    }

    public override void Visit(DiffNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new DiffNode(left, right));
    }

    public override void Visit(NotNode node)
    {
        var operand = SafePop(Nodes, VisitorOperationNames.VisitNotNode);
        Nodes.Push(new NotNode(operand));
    }

    public override void Visit(LikeNode node)
    {
        VisitBinaryOperatorWithDirectPop((left, right) => new LikeNode(left, right));
    }

    public override void Visit(RLikeNode node)
    {
        VisitBinaryOperatorWithDirectPop((left, right) => new RLikeNode(left, right));
    }

    public override void Visit(InNode node)
    {
        var right = SafePop(Nodes, VisitorOperationNames.VisitInNodeRight);
        var left = SafePop(Nodes, VisitorOperationNames.VisitInNodeLeft);
        Nodes.Push(new InNode(left, (ArgsListNode)right));
    }

    public override void Visit(FieldNode node)
    {
        var expression = SafePop(Nodes, VisitorOperationNames.VisitFieldNode);
        Nodes.Push(new FieldNode(expression, node.FieldOrder, node.FieldName));
    }

    public override void Visit(FieldOrderedNode node)
    {
        var expression = SafePop(Nodes, VisitorOperationNames.VisitFieldOrderedNode);
        Nodes.Push(new FieldOrderedNode(expression, node.FieldOrder, node.FieldName, node.Order));
    }

    public override void Visit(SelectNode node)
    {
        var fields = CreateFields(node.Fields);

        Nodes.Push(new SelectNode(fields.ToArray(), node.IsDistinct));
    }

    public override void Visit(GroupSelectNode node)
    {
        var fields = CreateFields(node.Fields);

        Nodes.Push(new GroupSelectNode(fields.ToArray()));
    }

    public override void Visit(StringNode node)
    {
        AddAssembly(typeof(string).Assembly);
        Nodes.Push(new StringNode(node.Value));
        _schemaFromArgs.Add(node.Value);
    }

    public override void Visit(DecimalNode node)
    {
        AddAssembly(typeof(decimal).Assembly);
        Nodes.Push(new DecimalNode(node.Value));
        _schemaFromArgs.Add(node.Value);
    }

    public override void Visit(IntegerNode node)
    {
        AddAssembly(typeof(int).Assembly);
        Nodes.Push(new IntegerNode(node.ObjValue));
        _schemaFromArgs.Add(node.ObjValue);
    }

    public override void Visit(HexIntegerNode node)
    {
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new HexIntegerNode(node.ObjValue));
        _schemaFromArgs.Add(node.ObjValue);
    }

    public override void Visit(BinaryIntegerNode node)
    {
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new BinaryIntegerNode(node.ObjValue));
        _schemaFromArgs.Add(node.ObjValue);
    }

    public override void Visit(OctalIntegerNode node)
    {
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new OctalIntegerNode(node.ObjValue));
        _schemaFromArgs.Add(node.ObjValue);
    }

    public override void Visit(BooleanNode node)
    {
        AddAssembly(typeof(bool).Assembly);
        Nodes.Push(new BooleanNode(node.Value));
        _schemaFromArgs.Add(node.Value);
    }

    public override void Visit(WordNode node)
    {
        AddAssembly(typeof(string).Assembly);
        Nodes.Push(new WordNode(node.Value));
        _schemaFromArgs.Add(node.Value);
    }

    public override void Visit(NullNode node)
    {
        Nodes.Push(new NullNode(node.ReturnType));
    }

    public override void Visit(ContainsNode node)
    {
        var right = SafePop(Nodes, VisitorOperationNames.VisitContainsNodeRight);
        var left = SafePop(Nodes, VisitorOperationNames.VisitContainsNodeLeft);
        Nodes.Push(new ContainsNode(left, right as ArgsListNode));
    }

    public override void Visit(AccessMethodNode node)
    {
        VisitAccessMethod(node,
            (token, modifiedNode, exArgs, arg3, alias, canSkipInjectSource) =>
                new AccessMethodNode(token, modifiedNode as ArgsListNode, exArgs, canSkipInjectSource, arg3, alias));
    }

    public override void Visit(InterpretCallNode node)
    {
        var dataSource = SafePop(Nodes, VisitorOperationNames.VisitInterpretCallNode);


        Nodes.Push(new InterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(ParseCallNode node)
    {
        var dataSource = SafePop(Nodes, VisitorOperationNames.VisitParseCallNode);


        Nodes.Push(new ParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(TryInterpretCallNode node)
    {
        var dataSource = SafePop(Nodes, VisitorOperationNames.VisitTryInterpretCallNode);


        Nodes.Push(new TryInterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(TryParseCallNode node)
    {
        var dataSource = SafePop(Nodes, VisitorOperationNames.VisitTryParseCallNode);


        Nodes.Push(new TryParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(PartialInterpretCallNode node)
    {
        var dataSource = SafePop(Nodes, VisitorOperationNames.VisitPartialInterpretCallNode);


        Nodes.Push(new PartialInterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(InterpretAtCallNode node)
    {
        var offset = SafePop(Nodes, VisitorOperationNames.VisitInterpretAtCallNodeOffset);
        var dataSource = SafePop(Nodes, VisitorOperationNames.VisitInterpretAtCallNodeDataSource);


        Nodes.Push(new InterpretAtCallNode(dataSource, offset, node.SchemaName, node.ReturnType));
    }

    public override void Visit(AccessRawIdentifierNode node)
    {
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public override void Visit(IsNullNode node)
    {
        var operand = SafePop(Nodes, VisitorOperationNames.VisitIsNullNode);
        Nodes.Push(new IsNullNode(operand, node.IsNegated));
    }

    public override void Visit(AccessRefreshAggregationScoreNode node)
    {
        VisitAccessMethod(node,
            (token, node1, exArgs, arg3, alias, _) =>
                new AccessRefreshAggregationScoreNode(token, node1 as ArgsListNode, exArgs, node.CanSkipInjectSource,
                    arg3, alias));
    }

    public override void Visit(AccessColumnNode node)
    {
        try
        {
            var hasProcessedQueryId = _currentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
            var identifier = (hasProcessedQueryId
                ? _currentScope[MetaAttributes.ProcessedQueryId]
                : _identifier) ?? node.Alias;

            if (string.IsNullOrEmpty(identifier))
                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    VisitorOperationNames.VisitAccessColumnNode,
                    "No valid identifier found for column access",
                    "Ensure the query has proper FROM clause and table aliases are correctly specified."
                );

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);
            if (tableSymbol == null)
                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    VisitorOperationNames.VisitAccessColumnNode,
                    $"Table symbol not found for identifier '{identifier}'",
                    "Verify that the table or alias is properly defined in the query."
                );

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
                if (TryReportOrThrowUnknownColumn(node.Name, tuple.Table.Columns, node))
                    return;
                return;
            }

            AddAssembly(column.ColumnType.Assembly);
            node.ChangeReturnType(column.ColumnType);

            var usedColumns = _usedColumns
                .Where(c => c.Key.Alias == tuple.TableName && c.Key.QueryId == _schemaFromKey)
                .Select(f => f.Value)
                .FirstOrDefault();

            if (usedColumns is not null)
                if (usedColumns.All(c => c.ColumnName != column.ColumnName))
                    usedColumns.Add(column);

            var accessColumn = new AccessColumnNode(column.ColumnName, tuple.TableName, column.ColumnType, node.Span,
                column.IntendedTypeName);
            Nodes.Push(accessColumn);
        }
        catch (Exception ex) when (ex is not VisitorException)
        {
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitAccessColumnNode,
                $"Failed to process column access for '{node.Name}': {ex.Message}",
                "Check that the column exists in the specified table and that table aliases are correct."
            );
        }
    }

    public override void Visit(AllColumnsNode node)
    {
        var identifier = _identifier;
        var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);

        if (!string.IsNullOrWhiteSpace(node.Alias) ||
            (!tableSymbol.IsCompoundTable && string.IsNullOrWhiteSpace(node.Alias)))
            ProcessSingleTable(node, tableSymbol, identifier);
        else if (tableSymbol.IsCompoundTable) ProcessCompoundTable(tableSymbol);

        Nodes.Push(node);
    }

    public override void Visit(IdentifierNode node)
    {
        if (node.Name != _identifier && _queryPart != QueryPart.From)
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
            var column = tableSymbol.GetColumnByAliasAndName(_identifier, node.Name);

            if (column == null)
                if (TryReportOrThrowUnknownColumn(node.Name, tableSymbol.GetColumns(), node))
                    return;

            Visit(new AccessColumnNode(node.Name, string.Empty, column?.ColumnType, TextSpan.Empty,
                column?.IntendedTypeName));
            return;
        }

        Nodes.Push(new IdentifierNode(node.Name));
    }

    public override void Visit(AccessObjectArrayNode node)
    {
        if (node.IsColumnAccess)
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(
                string.IsNullOrEmpty(node.TableAlias) ? _identifier : node.TableAlias);

            if (tableSymbol == null)
            {
                if (TryReportUnknownProperty(node.TableAlias ?? _identifier, null, node))
                    return;
                throw new UnknownPropertyException($"Table {node.TableAlias ?? _identifier} could not be found.");
            }

            var column = tableSymbol.GetColumnByAliasAndName(
                string.IsNullOrEmpty(node.TableAlias) ? _identifier : node.TableAlias,
                node.ObjectName);

            if (column == null)
            {
                if (TryReportUnknownProperty(node.ObjectName, null, node))
                    return;
                throw new UnknownPropertyException($"Column {node.ObjectName} could not be found.");
            }

            Nodes.Push(node);
            return;
        }

        var parentNode = Nodes.Count > 0 ? Nodes.Peek() : null;
        var parentNodeType = parentNode?.ReturnType;

        var hasValidParentContext = parentNode != null && parentNodeType != null &&
                                    !parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)) &&
                                    parentNodeType.Name != "RowSource" &&
                                    !BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(parentNodeType);

        if (!hasValidParentContext)
        {
            var currentTableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
            var column = currentTableSymbol?.GetColumnByAliasAndName(_identifier, node.ObjectName);
            if (column != null && BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(column.ColumnType))
            {
                var elementIntendedTypeName = GetArrayElementIntendedTypeName(column.IntendedTypeName);
                var columnAccessNode =
                    new AccessObjectArrayNode(node.Token, column.ColumnType, null, elementIntendedTypeName);
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
                    if (TryReportObjectNotArray(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                            node))
                        return;
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
                }

                isArray = propertyAccess?.PropertyType.IsArray == true;
                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(propertyAccess?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    if (TryReportObjectNotArray(
                            $"Object {parentNodeType.Name} property '{node.Name}' is not an array or indexable type.",
                            node))
                        return;
                    throw new ObjectIsNotAnArrayException(
                        $"Object {parentNodeType.Name} property '{node.Name}' is not an array or indexable type.");
                }

                if (propertyAccess == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    throw new UnknownPropertyException(
                        $"Property '{node.Name}' not found on object {parentNodeType.Name}.");
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
                    if (TryReportObjectNotArray(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                            node))
                        return;
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
                }

                isArray = property?.PropertyType.IsArray == true;
                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(property?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    if (TryReportObjectNotArray($"Object {node.Name} is not an array or indexable type.", node))
                        return;
                    throw new ObjectIsNotAnArrayException(
                        $"Object {node.Name} is not an array or indexable type.");
                }

                if (property == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    throw new UnknownPropertyException(
                        $"Property '{node.Name}' not found on object {parentNodeType.Name}.");
                }

                Nodes.Push(new AccessObjectArrayNode(node.Token, property));
            }
            else
            {
                if (TryReportUnknownProperty(node.ObjectName, null, node))
                    return;
                throw new UnknownPropertyException(
                    $"Could not resolve array access for {node.ObjectName}[{node.Token.Index}]");
            }
        }
    }

    public override void Visit(AccessObjectKeyNode node)
    {
        if (node.DestinationKind == AccessObjectKeyNode.Destination.Variable)
        {
            if (TryReportConstructionNotSupported($"Construction ${node.ToString()} is not yet supported.", node))
                return;
            throw new ConstructionNotYetSupported($"Construction ${node.ToString()} is not yet supported.");
        }

        var parentNode = SafePeek(Nodes, VisitorOperationNames.VisitAccessObjectKeyNode);
        if (parentNode?.ReturnType == null)
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitAccessObjectKeyNode,
                $"Parent node has no return type for key access '{node.Name}'"
            );
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
                    if (TryReportNoIndexer(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                            node))
                        return;
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
                }

                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(propertyAccess?.PropertyType);

                if (!isIndexer)
                {
                    if (TryReportNoIndexer(
                            $"Object {parentNodeType.Name} property '{node.Name}' does not implement indexer.", node))
                        return;
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Object {parentNodeType.Name} property '{node.Name}' does not implement indexer.");
                }

                if (propertyAccess == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    throw new UnknownPropertyException(
                        $"Property '{node.Name}' not found on object {parentNodeType.Name}.");
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
                if (TryReportNoIndexer(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}", node))
                    return;
                throw new ObjectDoesNotImplementIndexerException(
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
            }

            isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(property?.PropertyType);

            if (!isIndexer)
            {
                if (TryReportNoIndexer($"Object {node.Name} does not implement indexer.", node))
                    return;
                throw new ObjectDoesNotImplementIndexerException(
                    $"Object {node.Name} does not implement indexer.");
            }

            if (property == null)
            {
                if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                    return;
                throw new UnknownPropertyException(
                    $"Property '{node.Name}' not found on object {parentNodeType.Name}.");
            }

            Nodes.Push(new AccessObjectKeyNode(node.Token, property));
        }
    }

    public override void Visit(PropertyValueNode node)
    {
        var parentNode = SafePeek(Nodes, VisitorOperationNames.VisitPropertyValueNode);
        if (parentNode?.ReturnType == null)
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitPropertyValueNode,
                $"Parent node has no return type for property access '{node.Name}'"
            );
        var parentNodeType = parentNode.ReturnType;


        if (parentNodeType == typeof(object) || parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
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
                    VisitorOperationNames.VisitPropertyValueNode,
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}");
            }

            if (propertyInfo == null)
            {
                if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                    return;
                throw new UnknownPropertyException($"Property '{node.Name}' not found on object {parentNodeType.Name}");
            }

            Nodes.Push(new PropertyValueNode(node.Name, propertyInfo));
        }
    }

    public override void Visit(DotNode node)
    {
        var exp = SafePop(Nodes, VisitorOperationNames.VisitDotNodeExpression);
        var root = SafePop(Nodes, VisitorOperationNames.VisitDotNodeRoot);

        if (root?.ReturnType == null)
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitDotNode,
                "Root node has no return type for dot access");


        if (root is AccessColumnNode accessColumnNode && exp is AccessObjectArrayNode arrayNode2 &&
            !arrayNode2.IsColumnAccess)
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(accessColumnNode.Alias);
            if (tableSymbol != null)
            {
                var column = tableSymbol.GetColumnByAliasAndName(accessColumnNode.Alias, arrayNode2.ObjectName);
                if (column != null && BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(column.ColumnType))
                {
                    var elementIntendedTypeName = GetArrayElementIntendedTypeName(column.IntendedTypeName);
                    var columnAccessArrayNode =
                        new AccessObjectArrayNode(arrayNode2.Token, column.ColumnType, accessColumnNode.Alias,
                            elementIntendedTypeName);
                    Nodes.Push(columnAccessArrayNode);
                    return;
                }
            }
        }


        if (root is IdentifierNode identifierRoot && exp is AccessObjectArrayNode arrayNode3 &&
            !arrayNode3.IsColumnAccess)
            if (_currentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(identifierRoot.Name))
            {
                var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifierRoot.Name);
                if (tableSymbol != null)
                {
                    var column = tableSymbol.GetColumnByAliasAndName(identifierRoot.Name, arrayNode3.ObjectName);
                    if (column != null && BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(column.ColumnType))
                    {
                        var elementIntendedTypeName = GetArrayElementIntendedTypeName(column.IntendedTypeName);
                        var columnAccessArrayNode =
                            new AccessObjectArrayNode(arrayNode3.Token, column.ColumnType, identifierRoot.Name,
                                elementIntendedTypeName);
                        Nodes.Push(columnAccessArrayNode);
                        return;
                    }
                }
            }

        DotNode newNode;


        var isNestedSchemaReference = root.ReturnType == typeof(object) &&
                                      ((root is AccessColumnNode { IntendedTypeName: not null } accessColRootCheck &&
                                        !string.IsNullOrEmpty(accessColRootCheck.IntendedTypeName)) ||
                                       (root is AccessObjectArrayNode { IntendedTypeName: not null } arrayRootCheck &&
                                        !string.IsNullOrEmpty(arrayRootCheck.IntendedTypeName)) ||
                                       (root is DotNode { IntendedTypeName: not null } dotRootCheck &&
                                        !string.IsNullOrEmpty(dotRootCheck.IntendedTypeName)));


        var rootIntendedTypeName = root switch
        {
            AccessColumnNode accessColRoot => accessColRoot.IntendedTypeName,
            AccessObjectArrayNode arrayRoot => arrayRoot.IntendedTypeName,
            DotNode dotRoot => dotRoot.IntendedTypeName,
            _ => null
        };


        if (root.ReturnType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
        }

        else if (isNestedSchemaReference)
        {
            var expressionNode = exp;
            string? childIntendedTypeName = null;

            if (exp is IdentifierNode identNode)
            {
                var propertyType = typeof(object);


                if (_schemaRegistry != null && !string.IsNullOrEmpty(rootIntendedTypeName))
                {
                    var schemaName = rootIntendedTypeName.Split('.').Last();
                    if (_schemaRegistry.TryGetSchema(schemaName, out var refSchema) &&
                        refSchema?.Node is BinarySchemaNode binaryNode)
                    {
                        var allFields = GetAllBinarySchemaFields(binaryNode);
                        var field = allFields.FirstOrDefault(f => f.Name == identNode.Name);
                        if (field is FieldDefinitionNode fieldDef)
                            (propertyType, childIntendedTypeName) =
                                ResolveTypeAnnotationClrTypeWithIntendedName(fieldDef.TypeAnnotation);
                    }
                }

                expressionNode = new PropertyValueNode(identNode.Name,
                    new ExpandoObjectPropertyInfo(identNode.Name, propertyType));
            }


            newNode = new DotNode(root, expressionNode, node.IsTheMostInner, string.Empty, expressionNode.ReturnType);


            if (!string.IsNullOrEmpty(childIntendedTypeName))
            {
            }
        }

        else if (root.ReturnType == typeof(object))
        {
            var expressionNode = exp;

            if (exp is IdentifierNode identNode)
                expressionNode = new PropertyValueNode(identNode.Name,
                    new ExpandoObjectPropertyInfo(identNode.Name, typeof(object)));

            newNode = new DotNode(root, expressionNode, node.IsTheMostInner, string.Empty, expressionNode.ReturnType);
        }
        else
        {
            if (exp is AccessObjectArrayNode arrayNode)
            {
                var propertyName = arrayNode.ObjectName;
                var property = root.ReturnType.GetProperty(propertyName);

                if (property == null)
                {
                    if (TryReportUnknownPropertyWithSuggestions(propertyName, root.ReturnType.GetProperties(), node))
                        return;
                    PrepareAndThrowUnknownPropertyExceptionMessage(propertyName,
                        root.ReturnType.GetProperties());
                }

                newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
            }
            else if (exp is IdentifierNode identifierNode)
            {
                var hasProperty = root.ReturnType.GetProperty(identifierNode.Name) != null;

                if (!hasProperty)
                {
                    if (TryReportUnknownPropertyWithSuggestions(identifierNode.Name, root.ReturnType.GetProperties(),
                            node))
                        return;
                    PrepareAndThrowUnknownPropertyExceptionMessage(identifierNode.Name,
                        root.ReturnType.GetProperties());
                }

                newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
            }
            else
            {
                throw new NotSupportedException(
                    $"Unsupported expression type in DotNode: {exp?.GetType().Name ?? "null"}");
            }
        }

        Nodes.Push(newNode);
    }

    public override void Visit(AccessCallChainNode node)
    {
        var chainPretend = SafePop(Nodes, VisitorOperationNames.VisitAccessCallChainNode);

        Nodes.Push(chainPretend is AccessColumnNode
            ? chainPretend
            : new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
    }

    public override void Visit(ArgsListNode node)
    {
        var args = new Node[node.Args.Length];

        for (var i = node.Args.Length - 1; i >= 0; --i)
            args[i] = SafePop(Nodes, VisitorOperationNames.VisitArgsListNode);

        Nodes.Push(new ArgsListNode(args));
    }

    public override void Visit(WhereNode node)
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
            _usedWhereNodes[aliasSchemaPair.Schema] = rewrittenWhereNode;

        Nodes.Push(rewrittenWhereNode);
    }

    public override void Visit(GroupByNode node)
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

    public override void Visit(HavingNode node)
    {
        Nodes.Push(new HavingNode(Nodes.Pop()));
    }

    public override void Visit(SkipNode node)
    {
        Nodes.Push(new SkipNode((IntegerNode)node.Expression));
    }

    public override void Visit(TakeNode node)
    {
        Nodes.Push(new TakeNode((IntegerNode)node.Expression));
    }

    public override void Visit(SchemaFromNode node)
    {
        var schema = _provider.GetSchema(node.Schema);
        const bool hasExternallyProvidedTypes = false;

        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());

        if (HasAlreadyUsedAlias(_queryAlias))
        {
            if (TryReportDuplicateAlias(node, _queryAlias, node))
                return;
            throw new AliasAlreadyUsedException(node, _queryAlias);
        }

        _generatedAliases.Add(_queryAlias);

        var aliasedSchemaFromNode = new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(),
            _queryAlias, node.QueryId, hasExternallyProvidedTypes);

        var environmentVariables =
            RetrieveEnvironmentVariables(_positionalEnvironmentVariablesKey, aliasedSchemaFromNode);
        var isDesc = _currentScope.Name == "Desc";
        var table = !isDesc
            ? schema.GetTableByName(
                node.Method,
                new RuntimeContext(
                    node.QueryId.ToString(),
                    CancellationToken.None,
                    GetColumnsForAlias(_queryAlias, _schemaFromKey),
                    environmentVariables,
                    new QuerySourceInfo(aliasedSchemaFromNode, [], AllTrueWhereNode, hasExternallyProvidedTypes,
                        QueryHints.Empty),
                    _logger
                ),
                _schemaFromArgs.ToArray())
            : new DynamicTable([]);

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

    public override void Visit(SchemaMethodFromNode node)
    {
        _usedSchemasQuantity += 1;
        Nodes.Push(new Parser.SchemaMethodFromNode(node.Alias, node.Schema, node.Method));
    }

    public override void Visit(PropertyFromNode node)
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
            if (TryReportOrThrowUnknownColumn(node.FirstProperty.PropertyName, table.Columns, node))
                return;
            return;
        }

        if (ValidateBindablePropertyAsTableWithDiagnostics(table, targetColumn, node))
            return;

        AddAssembly(targetColumn.ColumnType.Assembly);

        var followedType = FollowPropertiesWithDiagnostics(targetColumn.ColumnType, node.PropertiesChain, node);
        if (followedType == null)
            return;

        var nestedTable = TurnTypeIntoTableWithIntendedTypeName(
            followedType,
            targetColumn.IntendedTypeName,
            node);
        if (nestedTable == null)
            return;
        table = nestedTable;

        UpdateQueryAliasAndSymbolTable(node, schema, table);

        var rewrittenChain =
            RewritePropertiesChainWithTargetColumnWithDiagnostics(targetColumn, node.PropertiesChain, node);
        if (rewrittenChain == null)
            return;

        Nodes.Push(
            new Parser.PropertyFromNode(
                node.Alias,
                node.SourceAlias,
                rewrittenChain
            )
        );
    }

    public override void Visit(AccessMethodFromNode node)
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

        var accessMethodNode = (AccessMethodNode)Nodes.Pop();
        var convertedTable = TurnTypeIntoTableWithDiagnostics(accessMethodNode.ReturnType, node);
        if (convertedTable == null)
            return;
        table = convertedTable;
        var tableSymbol = new TableSymbol(_queryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
        _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);
        _currentScope[node.Id] = _queryAlias;
        _aliasMapToInMemoryTableMap.Add(_queryAlias, node.SourceAlias);
        _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);

        Nodes.Push(new Parser.AccessMethodFromNode(node.Alias, node.SourceAlias, accessMethodNode,
            accessMethodNode.ReturnType));
    }

    public override void Visit(AliasedFromNode node)
    {
        if (IsInterpretFunction(node.Identifier))
        {
            _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());
            _generatedAliases.Add(_queryAlias);

            _logger?.LogDebug(
                "DEBUG Visit(AliasedFromNode): Processing Interpret function '{Identifier}' with alias '{Alias}' -> _queryAlias='{QueryAlias}'",
                node.Identifier, node.Alias, _queryAlias);

            var args = (ArgsListNode)Nodes.Pop();


            var schemaName = ExtractSchemaNameFromArgs(args, node.Identifier);


            var interpretTable = CreateInterpretTable(schemaName);


            Type? returnType = null;
            if (schemaName != null && _schemaRegistry != null &&
                _schemaRegistry.TryGetSchema(schemaName, out var schemaRegistration))
                returnType = schemaRegistration?.GeneratedType;

            var interpretTableSymbol = new TableSymbol(
                _queryAlias,
                new TransitionSchema(_queryAlias, interpretTable),
                interpretTable,
                !string.IsNullOrEmpty(node.Alias)
            );

            _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, interpretTableSymbol);
            _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_queryAlias);
            _currentScope[node.Id] = _queryAlias;
            _aliasMapToInMemoryTableMap.Add(_queryAlias, _queryAlias);

            _logger?.LogDebug(
                "DEBUG Visit(AliasedFromNode): Registered TableSymbol '{QueryAlias}' with {ColumnCount} columns in scope '{ScopeName}'",
                _queryAlias, interpretTable?.Columns?.Count() ?? 0, _currentScope.Name);

            Nodes.Push(new AliasedFromNode(node.Identifier, args, _queryAlias, returnType ?? node.ReturnType,
                node.InSourcePosition));
            return;
        }

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
            (ArgsListNode)Nodes.Pop(),
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
                new QuerySourceInfo(aliasedSchemaFromNode, [], AllTrueWhereNode, hasExternallyProvidedTypes,
                    QueryHints.Empty),
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

    public override void Visit(JoinSourcesTableFromNode node)
    {
        var exp = Nodes.Pop();
        var b = (FromNode)Nodes.Pop();
        var a = (FromNode)Nodes.Pop();

        Nodes.Push(new Parser.JoinSourcesTableFromNode(a, b, exp, node.JoinType));
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        var b = (FromNode)Nodes.Pop();
        var a = (FromNode)Nodes.Pop();

        Nodes.Push(new Parser.ApplySourcesTableFromNode(a, b, node.ApplyType));
    }

    public override void Visit(InMemoryTableFromNode node)
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
            {
                if (TryReportTableNotDefined(node.VariableName, node))
                    return;
                throw new TableIsNotDefinedException(node.VariableName);
            }

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

    public override void Visit(JoinFromNode node)
    {
        var expression = Nodes.Pop();
        var joinedTable = (FromNode)Nodes.Pop();
        var source = (FromNode)Nodes.Pop();
        var joinedFrom = new Parser.JoinFromNode(source, joinedTable, expression, node.JoinType);
        _identifier = joinedFrom.Alias;
        _schemaFromArgs.Clear();
        Nodes.Push(joinedFrom);
    }

    public override void Visit(ApplyFromNode node)
    {
        var appliedTable = (FromNode)Nodes.Pop();
        var source = (FromNode)Nodes.Pop();
        var appliedFrom = new Parser.ApplyFromNode(source, appliedTable, node.ApplyType);
        _identifier = appliedFrom.Alias;
        _schemaFromArgs.Clear();
        Nodes.Push(appliedFrom);
    }

    public override void Visit(ExpressionFromNode node)
    {
        var from = (FromNode)Nodes.Pop();
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

    public override void Visit(InterpretFromNode node)
    {
        var interpretCall = Nodes.Pop();
        _identifier = node.Alias;


        string? schemaName = null;
        if (interpretCall is InterpretCallNode icn)
            schemaName = icn.SchemaName;
        else if (interpretCall is TryInterpretCallNode ticn)
            schemaName = ticn.SchemaName;
        else if (interpretCall is PartialInterpretCallNode picn)
            schemaName = picn.SchemaName;
        else if (interpretCall is ParseCallNode pcn)
            schemaName = pcn.SchemaName;
        else if (interpretCall is InterpretAtCallNode iacn)
            schemaName = iacn.SchemaName;
        else if (interpretCall is ArgsListNode argsNode) schemaName = ExtractSchemaNameFromArgs(argsNode);


        if (schemaName != null && _schemaRegistry != null)
        {
            var interpretTable = CreateInterpretTable(schemaName);

            Type? returnType = null;
            if (_schemaRegistry.TryGetSchema(schemaName, out var schemaRegistration))
                returnType = schemaRegistration?.GeneratedType;

            var interpretTableSymbol = new TableSymbol(
                node.Alias,
                new TransitionSchema(node.Alias, interpretTable),
                interpretTable,
                !string.IsNullOrEmpty(node.Alias)
            );

            _currentScope.ScopeSymbolTable.AddSymbol(node.Alias, interpretTableSymbol);
            _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);

            var newInterpretFromNode = new Parser.InterpretFromNode(node.Alias, interpretCall, node.ApplyType,
                returnType ?? node.ReturnType);
            _currentScope[newInterpretFromNode.Id] = node.Alias;
            Nodes.Push(newInterpretFromNode);
        }
        else
        {
            var newInterpretFromNode =
                new Parser.InterpretFromNode(node.Alias, interpretCall, node.ApplyType, node.ReturnType);
            _currentScope[newInterpretFromNode.Id] = node.Alias;
            Nodes.Push(newInterpretFromNode);
        }

        if (node.ReturnType != null) AddAssembly(node.ReturnType.Assembly);
    }

    public override void Visit(CreateTransformationTableNode node)
    {
        var fields = CreateFields(node.Fields);

        Nodes.Push(new CreateTransformationTableNode(node.Name, node.Keys, fields, node.ForGrouping));
    }

    public override void Visit(RenameTableNode node)
    {
        Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
    }

    public override void Visit(TranslatedSetTreeNode node)
    {
    }

    public override void Visit(IntoNode node)
    {
        Nodes.Push(new IntoNode(node.Name));
    }

    public override void Visit(QueryScope node)
    {
    }

    public override void Visit(ShouldBePresentInTheTable node)
    {
        Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
    }

    public override void Visit(TranslatedSetOperatorNode node)
    {
    }

    public override void Visit(QueryNode node)
    {
        var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var take = node.Take != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
        var select = Nodes.Pop() as SelectNode;
        var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;
        var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
        var from = Nodes.Pop() as FromNode;

        if (from is null) throw new FromNodeIsNull();

        if (groupBy == null && _refreshMethods.Count > 0)
            groupBy = new GroupByNode([new FieldNode(new IntegerNode("1", "s"), 0, string.Empty)], null);

        _currentScope.ScopeSymbolTable.AddSymbol(from.Alias.ToRefreshMethodsSymbolName(),
            new RefreshMethodsSymbol(_refreshMethods));
        _refreshMethods.Clear();

        if (_currentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(string.Empty))
            _currentScope.ScopeSymbolTable.MoveSymbol(string.Empty, from.Alias);

        Methods.Push(from.Alias);

        var queryNode = new QueryNode(select, from, where, groupBy, orderBy, skip, take);


        ValidateSelectFieldsArePrimitive(queryNode.Select.Fields, "SELECT");

        if (where != null)
            ValidateExpressionIsPrimitive(where.Expression, "WHERE");

        if (groupBy != null)
        {
            foreach (var field in groupBy.Fields)
                ValidateExpressionIsPrimitive(field.Expression, "GROUP BY");

            if (groupBy.Having != null)
                ValidateExpressionIsPrimitive(groupBy.Having.Expression, "HAVING");
        }

        if (orderBy != null)
            foreach (var field in orderBy.Fields)
                ValidateExpressionIsPrimitive(field.Expression, "ORDER BY");

        if (skip != null)
            ValidateExpressionIsPrimitive(skip.Expression, "SKIP");

        if (take != null)
            ValidateExpressionIsPrimitive(take.Expression, "TAKE");


        long? skipValue = skip?.Expression is IntegerNode skipInt ? Convert.ToInt64(skipInt.ObjValue) : null;
        long? takeValue = take?.Expression is IntegerNode takeInt ? Convert.ToInt64(takeInt.ObjValue) : null;
        var isDistinct = select?.IsDistinct ?? false;

        // Determine if we can safely pass optimization hints to the data source.
        // Hints can ONLY be passed when ALL of these conditions are met:
        // 1. Single-table query (no JOINs/APPLYs) - multi-table queries need all data for joining
        // 2. No ORDER BY clause - sorting happens after data retrieval, so Skip/Take must operate on sorted results
        // 3. No GROUP BY clause (explicit or implicit) - grouping happens after data retrieval, Skip/Take must operate on grouped results
        //    Note: DISTINCT creates an implicit GROUP BY
        var isSingleTableQuery = _aliasToSchemaFromNodeMap.Count == 1;
        var hasOrderBy = orderBy != null;
        var hasGroupBy = groupBy != null;
        var canPassHints = isSingleTableQuery && !hasOrderBy && !hasGroupBy;

        var hints = canPassHints
            ? QueryHints.Create(skipValue, takeValue, isDistinct)
            : QueryHints.Empty;

        foreach (var schemaFromNode in _aliasToSchemaFromNodeMap.Values)
            _queryHintsPerSchema[schemaFromNode] = hints;

        Nodes.Push(queryNode);

        _schemaFromArgs.Clear();
        _aliasToSchemaFromNodeMap.Clear();
        _schemaFromInfo.Clear();
        _aliasMapToInMemoryTableMap.Clear();
        _usedSchemasQuantity = 0;
    }

    public override void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        var exp = Nodes.Pop();
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(
            new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType));
    }

    public override void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(
            new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType));
    }

    public override void Visit(InternalQueryNode node)
    {
        throw new NotSupportedException("Internal Query Node is not supported here");
    }

    public override void Visit(RootNode node)
    {
        Nodes.Push(new RootNode(Nodes.Pop()));
    }

    public override void Visit(SingleSetNode node)
    {
    }

    public override void Visit(RefreshNode node)
    {
    }

    public override void Visit(UnionNode node)
    {
        VisitSetOperationNode(node, "Union");
    }

    public override void Visit(UnionAllNode node)
    {
        VisitSetOperationNode(node, "UnionAll");
    }

    public override void Visit(ExceptNode node)
    {
        VisitSetOperationNode(node, "Except");
    }

    public override void Visit(IntersectNode node)
    {
        VisitSetOperationNode(node, "Intersect");
    }

    public override void Visit(PutTrueNode node)
    {
        Nodes.Push(new PutTrueNode());
    }

    public override void Visit(MultiStatementNode node)
    {
        var items = new Node[node.Nodes.Length];

        for (var i = node.Nodes.Length - 1; i >= 0; --i)
            items[i] = Nodes.Pop();

        Nodes.Push(new MultiStatementNode(items, node.ReturnType));
    }

    public override void Visit(CteExpressionNode node)
    {
        var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

        var set = Nodes.Pop();

        for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
            sets[i] = (CteInnerExpressionNode)Nodes.Pop();

        Nodes.Push(new CteExpressionNode(sets, set));
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        var set = Nodes.Pop();

        var collector = new GetSelectFieldsVisitor();
        var traverser = new GetSelectFieldsTraverseVisitor(collector);

        set.Accept(traverser);

        var table = new VariableTable(collector.CollectedFieldNames);
        _currentScope.Parent.ScopeSymbolTable.AddSymbol(node.Name,
            new TableSymbol(node.Name, new TransitionSchema(node.Name, table), table, false));
        _currentScope.Parent.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Name);

        if (_compilationOptions.UsePrimitiveTypeValidation)
            foreach (var fieldInfo in collector.CollectedFieldNames)
                if (!BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(fieldInfo.ColumnType))
                {
                    var fieldNode = new FieldNode(new IntegerNode("0", "s"), fieldInfo.ColumnIndex,
                        fieldInfo.ColumnName);
                    if (TryReportInvalidExpressionType(fieldNode, fieldInfo.ColumnType, $"CTE '{node.Name}'",
                            fieldNode))
                        continue;
                    throw new InvalidQueryExpressionTypeException(
                        fieldNode,
                        fieldInfo.ColumnType,
                        $"CTE '{node.Name}'");
                }

        Nodes.Push(new CteInnerExpressionNode(set, node.Name));
    }

    public override void Visit(JoinNode node)
    {
        _identifier = node.Alias;
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode)Nodes.Pop()));
    }

    public override void Visit(ApplyNode node)
    {
        _identifier = node.Alias;
        Nodes.Push(new Parser.ApplyNode((Parser.ApplyFromNode)Nodes.Pop()));
    }

    public void SetScope(Scope scope)
    {
        _currentScope = scope;
    }

    public override void Visit(OrderByNode node)
    {
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode)Nodes.Pop();

        Nodes.Push(new OrderByNode(fields));
    }

    public override void Visit(CreateTableNode node)
    {
        var tableColumns = new List<ISchemaColumn>();

        for (var i = 0; i < node.TableTypePairs.Length; i++)
        {
            var (columnName, typeName) = node.TableTypePairs[i];

            var remappedType = EvaluationHelper.RemapPrimitiveTypes(typeName);

            var type = EvaluationHelper.RemapPrimitiveTypeAsNullable(remappedType);

            if (type == null)
            {
                if (TryReportTypeNotFound(remappedType, node))
                    continue;
                throw new TypeNotFoundException($"Type '{remappedType}' could not be found.");
            }

            tableColumns.Add(new SchemaColumn(columnName, i, type));
        }

        var table = new DynamicTable(tableColumns.ToArray());
        _explicitlyDefinedTables.Add(node.Name, table);

        Nodes.Push(new CreateTableNode(node.Name, node.TableTypePairs));
    }

    public override void Visit(CoupleNode node)
    {
        _explicitlyCoupledTablesWithAliases.Add(node.MappedSchemaName, node.TableName);
        _explicitlyUsedAliases.Add(node.MappedSchemaName, node.SchemaMethodNode);
        Nodes.Push(new CoupleNode(node.SchemaMethodNode, node.TableName, node.MappedSchemaName));
    }

    public override void Visit(StatementsArrayNode node)
    {
        var statements = new StatementNode[node.Statements.Length];
        for (var i = 0; i < node.Statements.Length; ++i)
            statements[node.Statements.Length - 1 - i] = (StatementNode)Nodes.Pop();

        Nodes.Push(new StatementsArrayNode(statements));
    }

    public override void Visit(StatementNode node)
    {
        Nodes.Push(new StatementNode(Nodes.Pop()));
    }

    public override void Visit(CaseNode node)
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
                : new CaseNode(whenThenPairs.ToArray(), elseNode,
                    BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(greatestCommonSubtype));

            Nodes.Push(caseNode);
        }
        else
        {
            var greatestCommonSubtype = FindGreatestCommonSubtype();
            var nullableGreatestCommonSubtype =
                BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(greatestCommonSubtype);
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

    public override void Visit(WhenNode node)
    {
        var newNode = new WhenNode(Nodes.Pop());

        Nodes.Push(newNode);
    }

    public override void Visit(ThenNode node)
    {
        var newNode = new ThenNode(Nodes.Pop());

        _nullSuspiciousTypes.Add(newNode.ReturnType);

        Nodes.Push(newNode);
    }

    public override void Visit(ElseNode node)
    {
        var newNode = new ElseNode(Nodes.Pop());

        _nullSuspiciousTypes.Add(newNode.ReturnType);

        Nodes.Push(newNode);
    }

    public override void Visit(FieldLinkNode node)
    {
        var index = node.Index - 1;

        if (_groupByFields.Count <= index)
        {
            if (TryReportFieldLinkOutOfRange(index, _groupByFields.Count, node))
                return;
            throw new FieldLinkIndexOutOfRangeException(index, _groupByFields.Count);
        }

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

    private void VisitBinaryOperatorWithSafePop<T>(Func<Node, Node, T> nodeFactory, string operationName) where T : Node
    {
        var nodes = SafePopMultiple(Nodes, 2, operationName);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(nodeFactory(left, right));
    }

    private void VisitBinaryOperatorWithDirectPop<T>(Func<Node, Node, T> nodeFactory) where T : Node
    {
        var right = SafePop(Nodes, "VisitBinaryOperatorWithDirectPop (right)");
        var left = SafePop(Nodes, "VisitBinaryOperatorWithDirectPop (left)");
        Nodes.Push(nodeFactory(left, right));
    }

    private void VisitBinaryOperatorWithTypeConversion<T>(Func<Node, Node, T> nodeFactory,
        bool isRelationalComparison = false, bool isArithmeticOperation = false) where T : Node
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

        transformedLeft = TransformToNumericTypeIfNeeded(transformedLeft, transformedRight, isRelationalComparison,
            isArithmeticOperation);
        transformedRight = TransformToNumericTypeIfNeeded(transformedRight, transformedLeft, isRelationalComparison,
            isArithmeticOperation);

        Nodes.Push(nodeFactory(transformedLeft, transformedRight));
    }

    private Node TransformStringToDateTimeIfNeeded(Node candidateNode, Node otherNode)
    {
        if (candidateNode is not WordNode stringNode || !TypeConversionNodeFactory.IsDateTimeType(otherNode.ReturnType))
            return candidateNode;

        return _nodeFactory.CreateDateTimeConversionNode(otherNode.ReturnType, stringNode.Value);
    }

    private Node TransformToNumericTypeIfNeeded(Node candidateNode, Node otherNode, bool isRelationalComparison,
        bool isArithmeticOperation)
    {
        var shouldTransform = isRelationalComparison || isArithmeticOperation
            ? isArithmeticOperation
                ? TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType)
                : TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType)
            : TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType);

        if (!shouldTransform || !TypeConversionNodeFactory.IsNumericLiteralNode(otherNode, out var targetType))
            return candidateNode;

        return _nodeFactory.CreateNumericConversionNode(candidateNode, targetType,
            TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType), isRelationalComparison,
            isArithmeticOperation);
    }

    private bool HasAlreadyUsedAlias(string queryAlias)
    {
        var scope = _currentScope;

        while (scope != null)
        {
            if (scope.ScopeSymbolTable.TryGetSymbol<AliasesSymbol>(MetaAttributes.Aliases, out var symbol) &&
                symbol.ContainsAlias(queryAlias))
                return true;

            scope = scope.Parent;
        }

        return false;
    }

    protected virtual IReadOnlyDictionary<string, string> RetrieveEnvironmentVariables(uint position,
        SchemaFromNode node)
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
        for (var i = reorderedList.Length - 1; i >= 0; i--) reorderedList[i] = Nodes.Pop() as FieldNode;

        var fields = new List<FieldNode>(reorderedList.Length);
        var positionCounter = 0;

        foreach (var field in reorderedList)
            if (field.Expression is AllColumnsNode allColumnsNode)
                AddAllColumnsFields(fields, allColumnsNode, ref positionCounter);
            else
                fields.Add(new FieldNode(field.Expression, positionCounter++, field.FieldName));

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
                fields.Add(new FieldNode(column.Expression, positionCounter++, column.FieldName));
        }
        else if (string.IsNullOrWhiteSpace(allColumnsNode.Alias))
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
            foreach (var compoundTableIdentifier in tableSymbol.CompoundTables)
            {
                if (!_generatedColumns.TryGetValue(compoundTableIdentifier, out var compoundColumns)) continue;

                foreach (var column in compoundColumns)
                    fields.Add(new FieldNode(column.Expression, positionCounter++, column.FieldName));
            }
        }
    }

    private void VisitAccessMethod(AccessMethodNode node,
        Func<FunctionToken, Node, ArgsListNode, MethodInfo, string, bool, AccessMethodNode> func)
    {
        var args = GetAndValidateArgs(node);
        var methodContext = ResolveMethodContext(node, args);
        var (method, canSkipInjectSource) = ResolveMethod(node, args, methodContext);

        method = ProcessGenericMethodIfNeeded(method, args, methodContext.EntityType);

        var accessMethod = CreateAccessMethod(node, args, method, methodContext, canSkipInjectSource, func);

        node.ChangeMethod(method);
        FinalizeMethodVisit(method, accessMethod);
    }

    private ArgsListNode GetAndValidateArgs(AccessMethodNode node)
    {
        var nodeFromStack = SafePop(Nodes, nameof(GetAndValidateArgs));
        if (nodeFromStack is not ArgsListNode args)
            throw CannotResolveMethodException.CreateForNullArguments(node.Name);
        return args;
    }

    private MethodResolutionContext ResolveMethodContext(AccessMethodNode node, ArgsListNode args)
    {
        if (_usedSchemasQuantity > 1 && string.IsNullOrWhiteSpace(node.Alias))
        {
            if (TryReportMissingAlias(node))
                return default;
            throw new AliasMissingException(node);
        }

        var alias = !string.IsNullOrEmpty(node.Alias) ? node.Alias : _identifier;
        var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(alias);
        var schemaTablePair = tableSymbol.GetTableByAlias(alias);
        var entityType = schemaTablePair.Table.Metadata.TableEntityType;

        AddAssembly(entityType.Assembly);
        AddBaseTypeAssembly(entityType);

        return new MethodResolutionContext(alias, tableSymbol, schemaTablePair, entityType);
    }

    private (MethodInfo Method, bool CanSkipInjectSource) ResolveMethod(AccessMethodNode node, ArgsListNode args,
        MethodResolutionContext context)
    {
        var argCount = args.Args.Length;
        var argTypes = new Type[argCount];

        for (var i = 0; i < argCount; i++) argTypes[i] = args.Args[i].ReturnType;
        var groupArgCount = argCount > 0 ? argCount : 1;
        var groupArgTypes = new Type[groupArgCount];
        groupArgTypes[0] = typeof(string);
        for (var i = 1; i < argCount; i++) groupArgTypes[i] = argTypes[i];

        if (context.SchemaTablePair.Schema.TryResolveAggregationMethod(node.Name, groupArgTypes, context.EntityType,
                out var method)) return (method, false);

        if (context.SchemaTablePair.Schema.TryResolveMethod(node.Name, argTypes, context.EntityType, out method))
            return (method, false);

        if (context.SchemaTablePair.Schema.TryResolveRawMethod(node.Name, argTypes, out method)) return (method, true);

        throw CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(node.Name, args.Args);
    }

    private MethodInfo ProcessGenericMethodIfNeeded(MethodInfo method, ArgsListNode args, Type entityType)
    {
        var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

        if (!isAggregateMethod && method.IsGenericMethod && TryReduceDimensions(method, args, out var reducedMethod))
            method = reducedMethod;

        if (!isAggregateMethod &&
            method.IsGenericMethod &&
            !method.IsConstructedGenericMethod &&
            TryConstructGenericMethod(method, args, entityType, out var constructedMethod))
            method = constructedMethod;

        return method;
    }

    private AccessMethodNode CreateAccessMethod(
        AccessMethodNode node,
        ArgsListNode args,
        MethodInfo method,
        MethodResolutionContext context,
        bool canSkipInjectSource,
        Func<FunctionToken, Node, ArgsListNode, MethodInfo, string, bool, AccessMethodNode> func)
    {
        var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

        if (isAggregateMethod) return ProcessAggregateMethod(node, args, method, context, func);

        return func(node.FunctionToken, args, new ArgsListNode([]), method, context.Alias, canSkipInjectSource);
    }

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

        if (!context.SchemaTablePair.Schema.TryResolveAggregationMethod(setMethodName, argTypes, context.EntityType,
                out var setMethod))
            throw CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(setMethodName,
                newSetArgs.ToArray());

        if (setMethod.IsGenericMethodDefinition)
            (method, setMethod) = MakeGenericAggregationMethods(method, setMethod, newSetArgs);

        var setMethodNode = func(new FunctionToken(setMethodName, TextSpan.Empty),
            new ArgsListNode(newSetArgs.ToArray()), null, setMethod, context.Alias, false);

        _refreshMethods.Add(setMethodNode);

        return func(node.FunctionToken, new ArgsListNode(newArgs.ToArray()), null, method, context.Alias, false);
    }

    private (MethodInfo Method, MethodInfo SetMethod) MakeGenericAggregationMethods(
        MethodInfo method,
        MethodInfo setMethod,
        List<Node> newSetArgs)
    {
        var setParams = setMethod.GetParameters();
        var genericArguments = setMethod.GetGenericArguments();
        var genericArgumentsDistinct = new List<Type>();

        foreach (var genericArgument in genericArguments)
            for (var i = 0; i < setParams.Length; i++)
            {
                var setParam = setParams[i];

                if (setParam.ParameterType == genericArgument)
                    genericArgumentsDistinct.Add(newSetArgs.Where((arg, index) => index == i - 1).Single().ReturnType);
            }

        var genericArgumentsConcreteTypes = genericArgumentsDistinct.Distinct().ToArray();

        return (method.MakeGenericMethod(genericArgumentsConcreteTypes),
            setMethod.MakeGenericMethod(genericArgumentsConcreteTypes));
    }

    private void FinalizeMethodVisit(MethodInfo method, AccessMethodNode accessMethod)
    {
        if (method.DeclaringType == null)
            throw new InvalidOperationException("Method must have a declaring type.");

        AddAssembly(method.DeclaringType.Assembly);
        AddAssembly(method.ReturnType.Assembly);

        Nodes.Push(accessMethod);
    }

    private Type FindGreatestCommonSubtype()
    {
        var types = _nullSuspiciousTypes.Where(type => type != NullNode.NullType.Instance)
            .Select(BuildMetadataAndInferTypesVisitorUtilities.StripNullable)
            .Distinct().ToArray();

        if (types.Length == 0) return null;

        var greatestCommonSubtype = types[0];

        foreach (var currentType in types.Skip(1))
        {
            if (greatestCommonSubtype.IsAssignableTo(currentType))
            {
                greatestCommonSubtype = currentType;
                continue;
            }

            if (currentType.IsAssignableTo(greatestCommonSubtype)) continue;

            greatestCommonSubtype =
                BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(greatestCommonSubtype, currentType);
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
            if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(table.Columns[i]
                    .ColumnType))
                AddColumnToGeneratedColumns(tableSymbol, table.Columns[i], positionCounter++, generatedColumnIdentifier,
                    generatedColumns);

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
                if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(table.Columns[i]
                        .ColumnType))
                    AddColumnToGeneratedColumns(tableSymbol, table.Columns[i], positionCounter++, tableIdentifier,
                        generatedColumns, true);

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

    private void AddColumnToGeneratedColumns(TableSymbol tableSymbol, ISchemaColumn column, int index,
        string identifier, List<FieldNode> generatedColumns, bool isCompoundTable = false)
    {
        AddAssembly(column.ColumnType.Assembly);

        var accessColumn = new AccessColumnNode(column.ColumnName, identifier, column.ColumnType, TextSpan.Empty,
            column.IntendedTypeName);
        string fieldName;
        if (isCompoundTable)
            fieldName = $"{identifier}.{column.ColumnName}";
        else
            fieldName = tableSymbol.HasAlias ? $"{identifier}.{column.ColumnName}" : column.ColumnName;
        generatedColumns.Add(new FieldNode(accessColumn, index, fieldName));
    }

    private void UpdateUsedColumns(string identifier, ISchemaTable table)
    {
        if (_aliasToSchemaFromNodeMap.TryGetValue(identifier, out var schemaFromNode))
            _usedColumns[schemaFromNode] = table.Columns.ToList();
    }

    private ISchemaTable GetTableFromSchema(ISchema schema, SchemaFromNode schemaFrom)
    {
        var runtimeContext = new RuntimeContext(
            schemaFrom.QueryId.ToString(),
            CancellationToken.None,
            GetColumnsForAlias(schemaFrom.Alias, _schemaFromKey),
            RetrieveEnvironmentVariables(GetPositionalEnvVarKeyForAlias(schemaFrom.Alias),
                schemaFrom),
            new QuerySourceInfo(schemaFrom, [], AllTrueWhereNode, false, QueryHints.Empty),
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

    private void ValidateSelectFieldsArePrimitive(FieldNode[] fields, string context)
    {
        if (!_compilationOptions.UsePrimitiveTypeValidation) return;

        foreach (var field in fields)
        {
            var returnType = field.Expression.ReturnType;
            if (!BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(returnType))
            {
                if (TryReportInvalidExpressionType(field, returnType, context, field.Expression))
                    continue;
                throw new InvalidQueryExpressionTypeException(field, returnType, context);
            }
        }
    }

    private void ValidateExpressionIsPrimitive(Node expression, string context)
    {
        if (!_compilationOptions.UsePrimitiveTypeValidation) return;

        var returnType = expression.ReturnType;
        if (!BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(returnType))
        {
            if (TryReportInvalidExpressionType(expression.ToString(), returnType, context, expression))
                return;
            throw new InvalidQueryExpressionTypeException(expression.ToString(), returnType, context);
        }
    }

    private void MakeSureBothSideFieldsAreOfAssignableTypes(QueryNode left, QueryNode right,
        string cachedSetOperatorKey)
    {
        var leftFields = left.Select.Fields;
        var rightFields = right.Select.Fields;

        ValidateSelectFieldsArePrimitive(leftFields, "SET operator (left side)");
        ValidateSelectFieldsArePrimitive(rightFields, "SET operator (right side)");

        if (leftFields.Length != rightFields.Length)
        {
            if (TryReportSetOperatorColumnCount(right))
                return;
            throw new SetOperatorMustHaveSameQuantityOfColumnsException();
        }

        for (var i = 0; i < leftFields.Length; i++)
            if (leftFields[i].Expression.ReturnType != rightFields[i].Expression.ReturnType)
            {
                if (TryReportSetOperatorColumnTypes(leftFields[i], rightFields[i], rightFields[i].Expression))
                    continue;
                throw new SetOperatorMustHaveSameTypesOfColumnsException(leftFields[i], rightFields[i]);
            }

        _cachedSetFields.TryAdd(cachedSetOperatorKey, rightFields);
    }

    private void MakeSureBothSideFieldsAreOfAssignableTypes(QueryNode left, string cachedSetOperatorKey,
        string currentSetOperatorKey)
    {
        var leftFields = left.Select.Fields;
        var rightFields = _cachedSetFields[cachedSetOperatorKey];

        ValidateSelectFieldsArePrimitive(leftFields, "SET operator");

        if (leftFields.Length != rightFields.Length)
        {
            if (TryReportSetOperatorColumnCount(left))
                return;
            throw new SetOperatorMustHaveSameQuantityOfColumnsException();
        }

        for (var i = 0; i < leftFields.Length; i++)
            if (leftFields[i].Expression.ReturnType != rightFields[i].Expression.ReturnType)
            {
                if (TryReportSetOperatorColumnTypes(leftFields[i], rightFields[i], leftFields[i].Expression))
                    continue;
                throw new SetOperatorMustHaveSameTypesOfColumnsException(leftFields[i], rightFields[i]);
            }

        _cachedSetFields.TryAdd(currentSetOperatorKey, leftFields);
    }

    private static void PrepareAndThrowUnknownColumnExceptionMessage(string identifier, ISchemaColumn[] _columns)
    {
        var library = new TransitionLibrary();
        var candidates = new StringBuilder();

        var candidatesColumns = _columns.Where(col =>
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

    /// <summary>
    ///     Reports or throws an unknown column exception. If diagnostic context is available,
    ///     reports the error and returns true (to allow continuation). Otherwise throws.
    /// </summary>
    /// <param name="identifier">The column identifier that was not found.</param>
    /// <param name="columns">Available columns for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (continue execution), false if thrown.</returns>
    protected bool TryReportOrThrowUnknownColumn(string identifier, ISchemaColumn[] columns, Node node)
    {
        if (DiagnosticContext != null)
        {
            var availableColumns = columns.Select(c => c.ColumnName);
            DiagnosticContext.ReportUnknownColumn(identifier, availableColumns, node);
            return true;
        }

        PrepareAndThrowUnknownColumnExceptionMessage(identifier, columns);
        return false;
    }

    private static void PrepareAndThrowUnknownPropertyExceptionMessage(string identifier, PropertyInfo[] properties)
    {
        var library = new TransitionLibrary();
        var candidates = new StringBuilder();

        var candidatesProperties = properties.Where(prop =>
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

    private static bool IsInterpretFunction(string functionName)
    {
        return functionName.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("TryParse", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractSchemaNameFromArgs(ArgsListNode args, string? functionName = null)
    {
        var schemaArgIndex = functionName?.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) == true ? 2 : 1;

        if (args.Args.Length <= schemaArgIndex)
            throw new InvalidOperationException(
                $"DEBUG ExtractSchemaNameFromArgs: args.Length={args.Args.Length}, expected > {schemaArgIndex} for function {functionName ?? "unknown"}");

        var schemaArg = args.Args[schemaArgIndex];

        if (schemaArg is StringNode stringNode)
            return stringNode.Value;


        if (schemaArg is WordNode wordNode)
            return wordNode.Value;

        throw new InvalidOperationException(
            $"DEBUG ExtractSchemaNameFromArgs: args[{schemaArgIndex}] type is {schemaArg?.GetType().Name ?? "null"}, value: {schemaArg}");
    }

    private ISchemaTable CreateInterpretTable(string? schemaName)
    {
        if (schemaName == null || _schemaRegistry == null)
        {
            var msg =
                $"DEBUG: schemaName={schemaName ?? "null"}, registry={(_schemaRegistry != null ? $"present with {_schemaRegistry.Schemas.Count()} schemas" : "null")}";
            throw new InvalidOperationException(msg);
        }

        var schema = _schemaRegistry.GetSchema(schemaName);
        if (schema == null)
        {
            var schemaNames = string.Join(", ", _schemaRegistry.Schemas.Select(s => s.Name));
            throw new InvalidOperationException($"DEBUG: schema '{schemaName}' not found. Available: [{schemaNames}]");
        }


        var columns = new List<ISchemaColumn>();
        var columnIndex = 0;


        if (schema.Node is BinarySchemaNode binaryNode)
        {
            var allFields = GetAllBinarySchemaFields(binaryNode);

            foreach (var field in allFields)
            {
                if (field.Name.StartsWith('_'))
                    continue;


                if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode })
                    continue;


                Type columnType;
                string? intendedTypeName = null;
                var isConditional = false;
                if (field is FieldDefinitionNode parsedField)
                {
                    (columnType, intendedTypeName) =
                        ResolveTypeAnnotationClrTypeWithIntendedName(parsedField.TypeAnnotation);
                    isConditional = parsedField.IsConditional;
                }
                else if (field is ComputedFieldNode computedField)
                {
                    var exprType = computedField.Expression.ReturnType;


                    if (exprType == null || exprType == typeof(void))
                        columnType = InferComputedFieldType(computedField.Expression, columns);
                    else
                        columnType = exprType;
                }
                else
                {
                    columnType = typeof(object);
                }


                if (isConditional && columnType.IsValueType && Nullable.GetUnderlyingType(columnType) == null)
                    columnType = typeof(Nullable<>).MakeGenericType(columnType);

                columns.Add(new SchemaColumn(field.Name, columnIndex++, columnType, intendedTypeName));
            }
        }
        else if (schema.Node is TextSchemaNode textSchemaNode)
        {
            foreach (var field in textSchemaNode.Fields)
            {
                if (field.Name.StartsWith('_'))
                    continue;


                columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(string)));
            }
        }

        if (columns.Count == 0)
            return CreateEmptyTable();

        return new DynamicTable(columns.ToArray());
    }

    private List<SchemaFieldNode> GetAllBinarySchemaFields(
        BinarySchemaNode binaryNode)
    {
        var allFields = new List<SchemaFieldNode>();


        if (!string.IsNullOrEmpty(binaryNode.Extends))
        {
            var parentSchema = _schemaRegistry.GetSchema(binaryNode.Extends);
            if (parentSchema?.Node is BinarySchemaNode parentBinaryNode)
                allFields.AddRange(GetAllBinarySchemaFields(parentBinaryNode));
        }


        allFields.AddRange(binaryNode.Fields);

        return allFields;
    }

    private Type ResolveTypeAnnotationClrType(TypeAnnotationNode typeAnnotation)
    {
        var (type, _) = ResolveTypeAnnotationClrTypeWithIntendedName(typeAnnotation);
        return type;
    }

    private (Type ClrType, string? IntendedTypeName) ResolveTypeAnnotationClrTypeWithIntendedName(
        TypeAnnotationNode typeAnnotation)
    {
        if (typeAnnotation is SchemaReferenceTypeNode schemaRef)
        {
            if (_schemaRegistry != null && _schemaRegistry.TryGetSchema(schemaRef.SchemaName, out var refSchema))
            {
                if (refSchema?.GeneratedType != null)
                    return (refSchema.GeneratedType, null);


                return (typeof(object), refSchema?.GeneratedTypeName);
            }

            return (typeof(object), null);
        }


        if (typeAnnotation is StringTypeNode stringType &&
            !string.IsNullOrEmpty(stringType.AsTextSchemaName))
        {
            if (_schemaRegistry != null &&
                _schemaRegistry.TryGetSchema(stringType.AsTextSchemaName, out var textSchema))
            {
                if (textSchema?.GeneratedType != null)
                    return (textSchema.GeneratedType, null);

                return (typeof(object), textSchema?.GeneratedTypeName);
            }

            return (typeof(object), null);
        }


        if (typeAnnotation is ArrayTypeNode arrayType)
        {
            var (elementType, elementIntendedTypeName) =
                ResolveTypeAnnotationClrTypeWithIntendedName(arrayType.ElementType);
            var arrayClrType = elementType.MakeArrayType();
            var arrayIntendedTypeName = elementIntendedTypeName != null ? $"{elementIntendedTypeName}[]" : null;
            return (arrayClrType, arrayIntendedTypeName);
        }


        return (typeAnnotation.ClrType, null);
    }

    private static ISchemaTable CreateEmptyTable()
    {
        return new DynamicTable([]);
    }

    private static Type InferComputedFieldType(Node expression, List<ISchemaColumn> contextColumns)
    {
        if (expression is EqualityNode or DiffNode or GreaterNode or GreaterOrEqualNode
            or LessNode or LessOrEqualNode or AndNode or OrNode)
            return typeof(bool);


        if (expression is WordNode)
            return typeof(string);


        if (expression is AccessMethodNode methodNode)
            if (methodNode.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                return typeof(string);

        if (expression is BinaryNode binaryNode)
        {
            var leftType = InferOperandType(binaryNode.Left, contextColumns);
            var rightType = InferOperandType(binaryNode.Right, contextColumns);


            if (expression is AddNode && (leftType == typeof(string) || rightType == typeof(string)))
                return typeof(string);

            if (IsNumericType(leftType) && IsNumericType(rightType)) return GetWiderNumericType(leftType, rightType);


            return typeof(int);
        }

        return typeof(object);
    }

    private static Type InferOperandType(Node operand, List<ISchemaColumn> contextColumns)
    {
        if (operand is BinaryNode binaryOp) return InferComputedFieldType(binaryOp, contextColumns);

        if (operand is IdentifierNode identifier)
        {
            var column = contextColumns.FirstOrDefault(c =>
                c.ColumnName.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase));
            return column?.ColumnType ?? typeof(object);
        }

        if (operand is IntegerNode) return typeof(int);


        if (operand is WordNode) return typeof(string);


        if (operand is AccessMethodNode methodNode)
            if (methodNode.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                return typeof(string);

        return typeof(object);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double);
    }

    private static Type GetWiderNumericType(Type left, Type right)
    {
        var typeOrder = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double)
        };

        var leftIndex = Array.IndexOf(typeOrder, left);
        var rightIndex = Array.IndexOf(typeOrder, right);

        if (leftIndex < 0 && rightIndex < 0) return typeof(int);
        if (leftIndex < 0) return right;
        if (rightIndex < 0) return left;

        return leftIndex > rightIndex ? left : right;
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

    private static bool TryConstructGenericMethod(MethodInfo methodInfo, ArgsListNode args, Type entity,
        out MethodInfo constructedMethod)
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
                if ((genericArgument.IsGenericParameter || genericArgument.IsGenericMethodParameter) &&
                    parameters[0].ParameterType.IsGenericParameter) genericArgumentsDistinct.Add(entity);
            }

            for (; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.IsOptional &&
                    args.Args.Length < parameters.Length - shiftArgsWhenInjectSpecificSourcePresent) continue;

                var returnType = args.Args.Where((_, index) => index == i - shiftArgsWhenInjectSpecificSourcePresent)
                    .Single().ReturnType;
                var elementType = returnType.GetElementType();

                if (returnType.IsGenericType && parameter.ParameterType.IsGenericType &&
                    returnType.GetGenericTypeDefinition() == parameter.ParameterType.GetGenericTypeDefinition())
                {
                    genericArgumentsDistinct.Add(returnType.GetGenericArguments()[0]);
                    continue;
                }

                if (parameter.ParameterType.IsGenericType &&
                    parameter.ParameterType.IsAssignableTo(typeof(IEnumerable<>).MakeGenericType(genericArgument)) &&
                    elementType is not null)
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

                    if (firstAssignableInterface is null) continue;

                    var elementTypeOfFirstAssignableInterface = firstAssignableInterface.type.GetElementType() ??
                                                                firstAssignableInterface.type.GetGenericArguments()[0];

                    genericArgumentsDistinct.Add(elementTypeOfFirstAssignableInterface);
                }

                if (parameter.ParameterType == genericArgument) genericArgumentsDistinct.Add(returnType);
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

        if (nestedType == null) throw new InvalidOperationException("Element type is null.");

        if (nestedType.IsPrimitive || nestedType == typeof(string))
            return new DynamicTable([new SchemaColumn(nameof(PrimitiveTypeEntity<int>.Value), 0, nestedType)]);

        foreach (var property in nestedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            _columns.Add(new SchemaColumn(property.Name, _columns.Count, property.PropertyType));

        return new DynamicTable(_columns.ToArray(), nestedType);
    }

    private ISchemaTable? TurnTypeIntoTableWithDiagnostics(Type type, Node? node)
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
            if (TryReportColumnMustBeArray(node))
                return null;
            throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
        }

        if (nestedType == null) throw new InvalidOperationException("Element type is null.");

        if (nestedType.IsPrimitive || nestedType == typeof(string))
            return new DynamicTable([new SchemaColumn(nameof(PrimitiveTypeEntity<int>.Value), 0, nestedType)]);

        foreach (var property in nestedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            _columns.Add(new SchemaColumn(property.Name, _columns.Count, property.PropertyType));

        return new DynamicTable(_columns.ToArray(), nestedType);
    }

    private ISchemaTable? TurnTypeIntoTableWithIntendedTypeName(Type type, string? intendedTypeName, Node? node)
    {
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
            if (TryReportColumnMustBeArray(node))
                return null;
            throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
        }

        if (nestedType == null) throw new InvalidOperationException("Element type is null.");

        if (nestedType.IsPrimitive || nestedType == typeof(string))
            return new DynamicTable([new SchemaColumn(nameof(PrimitiveTypeEntity<int>.Value), 0, nestedType)]);


        if (nestedType == typeof(object) && !string.IsNullOrEmpty(intendedTypeName) && _schemaRegistry != null)
        {
            var elementIntendedTypeName = intendedTypeName.EndsWith("[]")
                ? intendedTypeName.Substring(0, intendedTypeName.Length - 2)
                : intendedTypeName;
            var schemaName = elementIntendedTypeName.Split('.').Last();

            if (_schemaRegistry.TryGetSchema(schemaName, out var schemaRegistration) &&
                schemaRegistration?.Node is BinarySchemaNode binaryNode)
            {
                var columns = new List<ISchemaColumn>();
                var allFields = GetAllBinarySchemaFields(binaryNode);
                var columnIndex = 0;

                foreach (var field in allFields)
                    if (field is FieldDefinitionNode fieldDef)
                    {
                        var (columnType, childIntendedTypeName) =
                            ResolveTypeAnnotationClrTypeWithIntendedName(fieldDef.TypeAnnotation);
                        columns.Add(new SchemaColumn(field.Name, columnIndex++, columnType, childIntendedTypeName));
                    }
                    else if (field is ComputedFieldNode computedField)
                    {
                        var columnType = InferComputedFieldType(computedField.Expression, columns);
                        columns.Add(new SchemaColumn(field.Name, columnIndex++, columnType));
                    }

                return new DynamicTable(columns.ToArray());
            }
        }


        var _columns = new List<ISchemaColumn>();
        foreach (var property in nestedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            _columns.Add(new SchemaColumn(property.Name, _columns.Count, property.PropertyType));

        return new DynamicTable(_columns.ToArray(), nestedType);
    }

    private static string GetArrayElementIntendedTypeName(string arrayIntendedTypeName)
    {
        if (string.IsNullOrEmpty(arrayIntendedTypeName))
            return null;

        if (arrayIntendedTypeName.EndsWith("[]"))
            return arrayIntendedTypeName.Substring(0, arrayIntendedTypeName.Length - 2);

        return arrayIntendedTypeName;
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

        if (!isValid) throw new ColumnMustBeMarkedAsBindablePropertyAsTableException();
    }

    private bool ValidateBindablePropertyAsTableWithDiagnostics(ISchemaTable table, ISchemaColumn targetColumn,
        Node? node)
    {
        var propertyInfo = table.Metadata.TableEntityType.GetProperty(targetColumn.ColumnName);
        var bindablePropertyAsTableAttribute = propertyInfo?.GetCustomAttribute<BindablePropertyAsTableAttribute>();

        if (bindablePropertyAsTableAttribute == null) return false;

        var isValid = IsGenericEnumerable(propertyInfo!.PropertyType, out var elementType) ||
                      IsArray(propertyInfo.PropertyType!, out elementType) ||
                      elementType.IsPrimitive || elementType == typeof(string);

        if (!isValid)
        {
            if (TryReportColumnNotBindable(targetColumn.ColumnName, node))
                return true;
            throw new ColumnMustBeMarkedAsBindablePropertyAsTableException();
        }

        return false;
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

    private Type? FollowPropertiesWithDiagnostics(Type type, PropertyFromNode.PropertyNameAndTypePair[] propertiesChain,
        Node? node)
    {
        var propertiesWithoutColumnType = propertiesChain.Skip(1);

        foreach (var property in propertiesWithoutColumnType)
        {
            var propertyInfo = type.GetProperty(property.PropertyName);

            if (propertyInfo == null)
            {
                if (TryReportUnknownPropertyWithSuggestions(property.PropertyName, type.GetProperties(), node))
                    return null;
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, type.GetProperties());
                return null;
            }

            type = propertyInfo.PropertyType;
        }

        return type;
    }

    private static PropertyFromNode.PropertyNameAndTypePair[] RewritePropertiesChainWithTargetColumn(
        ISchemaColumn targetColumn, PropertyFromNode.PropertyNameAndTypePair[] nodePropertiesChain)
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

            propertiesChain[i] =
                new PropertyFromNode.PropertyNameAndTypePair(propertyInfo.Name, propertyInfo.PropertyType);
        }

        return propertiesChain;
    }

    private PropertyFromNode.PropertyNameAndTypePair[]? RewritePropertiesChainWithTargetColumnWithDiagnostics(
        ISchemaColumn targetColumn, PropertyFromNode.PropertyNameAndTypePair[] nodePropertiesChain, Node? node)
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
                if (TryReportUnknownPropertyWithSuggestions(property.PropertyName, rootType.GetProperties(), node))
                    return null;
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, rootType.GetProperties());
                return null;
            }

            propertiesChain[i] =
                new PropertyFromNode.PropertyNameAndTypePair(propertyInfo.Name, propertyInfo.PropertyType);
        }

        return propertiesChain;
    }

    private void VisitSetOperationNode(SetOperatorNode node, string setOperatorName)
    {
        if (node.Keys.Length == 0)
        {
            if (TryReportSetOperatorMissingKeys(setOperatorName, node))
                return;
            throw SetOperatorDoesNotHaveKeysException(setOperatorName);
        }

        var key = CreateSetOperatorPositionKey();
        _currentScope[MetaAttributes.SetOperatorName] = key;
        SetOperatorFieldPositions.Add(key,
            BuildMetadataAndInferTypesVisitorUtilities.CreateSetOperatorPositionIndexes((QueryNode)node.Left,
                node.Keys));

        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (right is QueryNode rightAsQueryNode)
            MakeSureBothSideFieldsAreOfAssignableTypes((QueryNode)left, rightAsQueryNode, key);
        else
            MakeSureBothSideFieldsAreOfAssignableTypes((QueryNode)left, PreviousSetOperatorPositionKey(), key);

        var rightMethodName = Methods.Pop();
        var leftMethodName = Methods.Pop();

        var methodName = $"{leftMethodName}_{setOperatorName}_{rightMethodName}";
        Methods.Push(methodName);
        _currentScope.ScopeSymbolTable.AddSymbol(methodName,
            _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode)left).From.Alias));

        Nodes.Push(CreateSetOperatorNode(setOperatorName, node, left, right));
    }

    private SetOperatorNode CreateSetOperatorNode(string setOperatorName, SetOperatorNode node, Node left, Node right)
    {
        return setOperatorName switch
        {
            "Union" => new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            "UnionAll" => new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
                node.IsTheLastOne),
            "Except" => new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            "Intersect" => new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
                node.IsTheLastOne),
            _ => throw new NotSupportedException($"Set operator '{setOperatorName}' is not supported.")
        };
    }

    private TableSymbol FindTableSymbolInScopeHierarchy(string name)
    {
        var scope = _currentScope;
        while (scope != null)
        {
            if (scope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(name, out var tableSymbol)) return tableSymbol;
            scope = scope.Parent;
        }

        return _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(name);
    }

    /// <summary>
    ///     Contains context information needed for method resolution.
    /// </summary>
    private record struct MethodResolutionContext(
        string Alias,
        TableSymbol TableSymbol,
        (ISchema Schema, ISchemaTable Table, string TableName) SchemaTablePair,
        Type EntityType);

    #region Diagnostic Helpers

    /// <summary>
    ///     Reports an unknown column error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="columnName">The column name that was not found.</param>
    /// <param name="availableColumns">Available column names for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportUnknownColumn(string columnName, IEnumerable<string> availableColumns, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportUnknownColumn(columnName, availableColumns, node);
            return true;
        }


        var availableList = availableColumns.ToArray();
        if (availableList.Length > 0)
        {
            var library = new TransitionLibrary();
            var candidates = availableList
                .Where(col => library.Soundex(col) == library.Soundex(columnName) ||
                              library.LevenshteinDistance(col, columnName) < 3)
                .ToArray();

            if (candidates.Length > 0)
                throw new UnknownColumnOrAliasException(
                    $"Column or Alias '{columnName}' could not be found. Did you mean to use [{string.Join(", ", candidates)}]?");
        }

        throw new UnknownColumnOrAliasException($"Column or Alias '{columnName}' could not be found.");
    }

    /// <summary>
    ///     Reports an unknown property error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="propertyName">The property name that was not found.</param>
    /// <param name="objectType">The type of object on which the property was not found.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportUnknownProperty(string propertyName, Type? objectType, Node? node)
    {
        if (DiagnosticContext != null)
        {
            var availableProperties = objectType?.GetProperties().Select(p => p.Name) ?? [];
            DiagnosticContext.ReportUnknownColumn(propertyName, availableProperties, node);
            return true;
        }

        throw new UnknownPropertyException(
            objectType != null
                ? $"Property '{propertyName}' not found on object {objectType.Name}."
                : $"Property '{propertyName}' could not be found.");
    }

    /// <summary>
    ///     Reports a type-related error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="typeName">The type name that was not found or is invalid.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportTypeNotFound(string typeName, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3005_TypeMismatch,
                $"Type '{typeName}' could not be found or resolved.",
                node);
            return true;
        }

        throw new TypeNotFoundException($"Type '{typeName}' could not be found.");
    }

    /// <summary>
    ///     Reports an ambiguous column reference. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="columnName">The ambiguous column name.</param>
    /// <param name="alias1">First possible source alias.</param>
    /// <param name="alias2">Second possible source alias.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportAmbiguousColumn(string columnName, string alias1, string alias2, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportAmbiguousColumn(columnName, alias1, alias2, node);
            return true;
        }

        throw new AmbiguousColumnException(columnName, alias1, alias2);
    }

    /// <summary>
    ///     Reports a general semantic error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <typeparam name="TException">The type of exception to throw if not collecting diagnostics.</typeparam>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), never returns false (throws instead).</returns>
    protected bool TryReportSemanticError<TException>(DiagnosticCode code, string message, Node? node)
        where TException : Exception
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(code, message, node);
            return true;
        }

        throw (TException)Activator.CreateInstance(typeof(TException), message)!;
    }

    /// <summary>
    ///     Reports a semantic error using an existing exception. If diagnostics are enabled, records the error and returns
    ///     true.
    ///     Otherwise rethrows the exception.
    /// </summary>
    /// <param name="exception">The exception to report or throw.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), never returns false (throws instead).</returns>
    protected bool TryReportException(Exception exception, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportException(exception, node?.Span);
            return true;
        }

        throw exception;
    }

    /// <summary>
    ///     Reports an object-not-array error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportObjectNotArray(string message, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(DiagnosticCode.MQ3017_ObjectNotArray, message, node);
            return true;
        }

        throw new ObjectIsNotAnArrayException(message);
    }

    /// <summary>
    ///     Reports an no-indexer error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportNoIndexer(string message, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(DiagnosticCode.MQ3018_NoIndexer, message, node);
            return true;
        }

        throw new ObjectDoesNotImplementIndexerException(message);
    }

    /// <summary>
    ///     Reports a set operator column count error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportSetOperatorColumnCount(Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3019_SetOperatorColumnCount,
                "Set operator must have the same quantity of columns in both queries",
                node);
            return true;
        }

        throw new SetOperatorMustHaveSameQuantityOfColumnsException();
    }

    /// <summary>
    ///     Reports a set operator column type error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="left">The left field node.</param>
    /// <param name="right">The right field node.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportSetOperatorColumnTypes(FieldNode left, FieldNode right, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3020_SetOperatorColumnTypes,
                $"Set operator must have the same types of columns in both queries. Left column expression is {left} and right column expression is {right}",
                node);
            return true;
        }

        throw new SetOperatorMustHaveSameTypesOfColumnsException(left, right);
    }

    /// <summary>
    ///     Reports a duplicate alias error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="schemaNode">The schema from node.</param>
    /// <param name="alias">The duplicate alias.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportDuplicateAlias(SchemaFromNode schemaNode, string alias, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3021_DuplicateAlias,
                $"Alias '{alias}' is already used in query. Please use a different alias.",
                node);
            return true;
        }

        throw new AliasAlreadyUsedException(schemaNode, alias);
    }

    /// <summary>
    ///     Reports a missing alias error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="methodNode">The access method node.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportMissingAlias(AccessMethodNode methodNode)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3022_MissingAlias,
                $"Alias must be provided for method call when more than one schema is used. Problem occurred in method {methodNode}",
                methodNode);
            return true;
        }

        throw new AliasMissingException(methodNode);
    }

    /// <summary>
    ///     Reports a table not defined error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="tableName">The undefined table name.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportTableNotDefined(string tableName, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3023_TableNotDefined,
                $"Table '{tableName}' is not defined in query",
                node);
            return true;
        }

        throw new TableIsNotDefinedException(tableName);
    }

    /// <summary>
    ///     Reports a column must be array error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportColumnMustBeArray(Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3025_ColumnMustBeArray,
                "Column must be an array or implement IEnumerable<T> interface",
                node);
            return true;
        }

        throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
    }

    /// <summary>
    ///     Reports an invalid expression type error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="field">The field with invalid type.</param>
    /// <param name="invalidType">The invalid type.</param>
    /// <param name="context">The context where the error occurred.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportInvalidExpressionType(FieldNode field, Type? invalidType, string context, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3027_InvalidExpressionType,
                $"Query output column '{field.FieldName}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. Only primitive types are allowed in query outputs.",
                node);
            return true;
        }

        throw new InvalidQueryExpressionTypeException(field, invalidType, context);
    }

    /// <summary>
    ///     Reports an invalid expression type error for expressions. If diagnostics are enabled, records the error and returns
    ///     true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="expressionDescription">Description of the expression.</param>
    /// <param name="invalidType">The invalid type.</param>
    /// <param name="context">The context where the error occurred.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportInvalidExpressionType(string expressionDescription, Type? invalidType, string context,
        Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3027_InvalidExpressionType,
                $"Expression '{expressionDescription}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. Only primitive types are allowed in query expressions.",
                node);
            return true;
        }

        throw new InvalidQueryExpressionTypeException(expressionDescription, invalidType, context);
    }

    /// <summary>
    ///     Reports a field link index out of range error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="index">The invalid index.</param>
    /// <param name="maxCount">The maximum allowed count.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportFieldLinkOutOfRange(int index, int maxCount, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3024_GroupByIndexOutOfRange,
                $"Field link index {index} is out of range. Maximum allowed is {maxCount - 1}.",
                node);
            return true;
        }

        throw new FieldLinkIndexOutOfRangeException(index, maxCount);
    }

    /// <summary>
    ///     Reports a column must be marked as bindable property error. If diagnostics are enabled, records the error and
    ///     returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportColumnNotBindable(string columnName, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3026_ColumnNotBindable,
                $"Column '{columnName}' must be marked with BindablePropertyAsTable attribute to be used in this context.",
                node);
            return true;
        }

        throw new ColumnMustBeMarkedAsBindablePropertyAsTableException();
    }

    /// <summary>
    ///     Reports an unknown column or alias error with suggestions. If diagnostics are enabled, records the error and
    ///     returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="identifier">The unknown identifier.</param>
    /// <param name="columns">Available columns for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportUnknownColumnWithSuggestions(string identifier, ISchemaColumn[] columns, Node? node)
    {
        if (DiagnosticContext != null)
        {
            var library = new TransitionLibrary();
            var candidatesColumns = columns.Where(col =>
                library.Soundex(col.ColumnName) == library.Soundex(identifier) ||
                library.LevenshteinDistance(col.ColumnName, identifier) < 3).ToArray();

            var message = candidatesColumns.Length > 0
                ? $"Column or Alias '{identifier}' could not be found. Did you mean to use [{string.Join(", ", candidatesColumns.Select(c => c.ColumnName))}]?"
                : $"Column or Alias '{identifier}' could not be found.";

            DiagnosticContext.ReportError(DiagnosticCode.MQ3001_UnknownColumn, message, node);
            return true;
        }


        return false;
    }

    /// <summary>
    ///     Reports an unknown property error with suggestions. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="identifier">The unknown identifier.</param>
    /// <param name="properties">Available properties for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportUnknownPropertyWithSuggestions(string identifier, PropertyInfo[] properties, Node? node)
    {
        if (DiagnosticContext != null)
        {
            var library = new TransitionLibrary();
            var candidatesProperties = properties.Where(prop =>
                library.Soundex(prop.Name) == library.Soundex(identifier) ||
                library.LevenshteinDistance(prop.Name, identifier) < 3).ToArray();

            var message = candidatesProperties.Length > 0
                ? $"Property '{identifier}' could not be found. Did you mean to use [{string.Join(", ", candidatesProperties.Select(p => p.Name))}]?"
                : $"Property '{identifier}' could not be found.";

            DiagnosticContext.ReportError(DiagnosticCode.MQ3028_UnknownProperty, message, node);
            return true;
        }


        return false;
    }

    /// <summary>
    ///     Reports a construction not yet supported error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="description">Description of the unsupported construction.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportConstructionNotSupported(string description, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3030_ConstructionNotSupported,
                description,
                node);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Reports a set operator missing keys error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="setOperator">The name of the set operator (UNION, EXCEPT, INTERSECT).</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportSetOperatorMissingKeys(string setOperator, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3031_SetOperatorMissingKeys,
                $"{setOperator} operator must have keys. Set operators require key columns to determine how to combine rows.",
                node);
            return true;
        }

        return false;
    }

    private ISchemaColumn[] GetColumnsForAlias(string alias, int schemaFromKey)
    {
        var key = alias + schemaFromKey;
        if (_columns.TryGetValue(key, out var columnNames))
            return columnNames.Select((f, i) => new SchemaColumn(f, i, typeof(object))).ToArray();


        return [];
    }

    private uint GetPositionalEnvVarKeyForAlias(string alias)
    {
        if (_schemaFromInfo.TryGetValue(alias, out var info)) return info.PositionalEnvironmentVariableKey;


        return 0;
    }

    #endregion
}
