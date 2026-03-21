#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
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
using Musoq.Parser.Exceptions;
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
using static Musoq.Evaluator.Visitors.BinaryOperatorTypeRules;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
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

    private readonly IDictionary<SchemaFromNode, List<ISchemaColumn>> _usedColumns =
        new Dictionary<SchemaFromNode, List<ISchemaColumn>>();

    private readonly IDictionary<SchemaFromNode, WhereNode> _usedWhereNodes =
        new Dictionary<SchemaFromNode, WhereNode>();

    private readonly HashSet<string> _allUsedSchemaNames = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Node> _selectFieldAliases = new(StringComparer.OrdinalIgnoreCase);

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

    internal bool InsideWindowFunction { get; set; }

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
        SchemaRegistry = schemaRegistry;
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

    public SchemaRegistry? SchemaRegistry { get; }

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
        VisitBinaryOperatorWithTypeConversion((left, right) => new StarNode(left, right), node,
            BinaryOperatorKind.Multiply, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(FSlashNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new FSlashNode(left, right), node,
            BinaryOperatorKind.Divide, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(ModuloNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new ModuloNode(left, right), node,
            BinaryOperatorKind.Modulo, BinaryOperationContext.ArithmeticOperation);
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
            VisitBinaryOperatorWithTypeConversion((l, r) => new AddNode(l, r), node, BinaryOperatorKind.Add,
                BinaryOperationContext.ArithmeticOperation);
        }
    }

    public override void Visit(HyphenNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new HyphenNode(left, right), node,
            BinaryOperatorKind.Subtract, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(AndNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitAndNode);
        var left = nodes[0];
        var right = nodes[1];

        ValidateBooleanOperand(left, "AND", node);
        ValidateBooleanOperand(right, "AND", node);

        Nodes.Push(new AndNode(left, right));
    }

    public override void Visit(OrNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitOrNode);
        var left = nodes[0];
        var right = nodes[1];

        ValidateBooleanOperand(left, "OR", node);
        ValidateBooleanOperand(right, "OR", node);

        Nodes.Push(new OrNode(left, right));
    }

    public override void Visit(BitwiseAndNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseAndNode(left, right), node,
            BinaryOperatorKind.BitwiseAnd, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(BitwiseOrNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseOrNode(left, right), node,
            BinaryOperatorKind.BitwiseOr, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(BitwiseXorNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseXorNode(left, right), node,
            BinaryOperatorKind.BitwiseXor, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(LeftShiftNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new LeftShiftNode(left, right), node,
            BinaryOperatorKind.LeftShift, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(RightShiftNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new RightShiftNode(left, right), node,
            BinaryOperatorKind.RightShift, BinaryOperationContext.ArithmeticOperation);
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
        VisitBinaryOperatorWithTypeConversion((left, right) => new EqualityNode(left, right), node,
            BinaryOperatorKind.Equality);
    }

    public override void Visit(GreaterOrEqualNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new GreaterOrEqualNode(left, right), node,
            BinaryOperatorKind.Relational, BinaryOperationContext.RelationalComparison);
    }

    public override void Visit(LessOrEqualNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new LessOrEqualNode(left, right), node,
            BinaryOperatorKind.Relational, BinaryOperationContext.RelationalComparison);
    }

    public override void Visit(GreaterNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new GreaterNode(left, right), node,
            BinaryOperatorKind.Relational, BinaryOperationContext.RelationalComparison);
    }

    public override void Visit(LessNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new LessNode(left, right), node,
            BinaryOperatorKind.Relational, BinaryOperationContext.RelationalComparison);
    }

    public override void Visit(DiffNode node)
    {
        VisitBinaryOperatorWithTypeConversion((left, right) => new DiffNode(left, right), node,
            BinaryOperatorKind.Inequality);
    }

    public override void Visit(NotNode node)
    {
        var operand = SafePop(Nodes, VisitorOperationNames.VisitNotNode);
        ValidateBooleanOperand(operand, "NOT", node);
        Nodes.Push(new NotNode(operand));
    }

    public override void Visit(LikeNode node)
    {
        var right = SafePop(Nodes, "Visit(LikeNode) right");
        var left = SafePop(Nodes, "Visit(LikeNode) left");

        ValidatePatternOperand(left, "LIKE", node);
        ValidatePatternOperand(right, "LIKE", node);

        Nodes.Push(new LikeNode(left, right));
    }

    public override void Visit(RLikeNode node)
    {
        var right = SafePop(Nodes, "Visit(RLikeNode) right");
        var left = SafePop(Nodes, "Visit(RLikeNode) left");

        ValidatePatternOperand(left, "RLIKE", node);
        ValidatePatternOperand(right, "RLIKE", node);

        Nodes.Push(new RLikeNode(left, right));
    }

    public override void Visit(InNode node)
    {
        var right = SafePop(Nodes, VisitorOperationNames.VisitInNodeRight);
        var left = SafePop(Nodes, VisitorOperationNames.VisitInNodeLeft);
        Nodes.Push(new InNode(left, (ArgsListNode)right));
    }

    public override void Visit(BetweenNode node)
    {
        var max = SafePop(Nodes, "Visit(BetweenNode).Max");
        var min = SafePop(Nodes, "Visit(BetweenNode).Min");
        var expression = SafePop(Nodes, "Visit(BetweenNode).Expression");
        Nodes.Push(new BetweenNode(expression, min, max));
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

        CollectSelectFieldAliases(fields);

        Nodes.Push(new SelectNode(fields.ToArray(), node.IsDistinct));
    }

    public override void Visit(GroupSelectNode node)
    {
        var fields = CreateFields(node.Fields);

        CollectSelectFieldAliases(fields);

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
                new AccessMethodNode(token, modifiedNode as ArgsListNode, exArgs, canSkipInjectSource, arg3, alias,
                    default, node.IsDistinct));
    }

    public override void Visit(WindowFunctionNode node)
    {
        var spec = node.WindowSpecification != null
            ? SafePop(Nodes, "Visit(WindowFunctionNode).WindowSpec") as WindowSpecificationNode
            : null;

        var funcArgCount = node.FunctionCall.Arguments?.Args.Length ?? 0;
        var funcArgs = new Node[funcArgCount];
        for (var i = funcArgCount - 1; i >= 0; i--)
            funcArgs[i] = SafePop(Nodes, "Visit(WindowFunctionNode).FuncArg");

        var (returnType, resolvedFactory) = InferWindowFunctionReturnType(node.FunctionCall.Name, funcArgs);
        var argsListNode = new ArgsListNode(funcArgs);

        var functionCall = new AccessMethodNode(
            node.FunctionCall.FunctionToken,
            argsListNode,
            null,
            false,
            resolvedFactory,
            node.FunctionCall.Alias,
            default,
            node.FunctionCall.IsDistinct);

        WindowFunctionNode result;
        if (node.IsNamedWindowReference)
            result = new WindowFunctionNode(functionCall, node.WindowName);
        else
            result = new WindowFunctionNode(functionCall, spec);

        result.SetReturnType(returnType);
        Nodes.Push(result);
    }

    public override void Visit(WindowSpecificationNode node)
    {
        var orderByFields = new FieldOrderedNode[node.OrderByFields.Length];
        for (var i = node.OrderByFields.Length - 1; i >= 0; i--)
            orderByFields[i] = (FieldOrderedNode)SafePop(Nodes, "Visit(WindowSpecificationNode).OrderBy");

        var partitionFields = new FieldNode[node.PartitionFields.Length];
        for (var i = node.PartitionFields.Length - 1; i >= 0; i--)
            partitionFields[i] = (FieldNode)SafePop(Nodes, "Visit(WindowSpecificationNode).Partition");

        Nodes.Push(new WindowSpecificationNode(partitionFields, orderByFields));
    }

    public override void Visit(WindowDefinitionNode node)
    {
        var spec = (WindowSpecificationNode)SafePop(Nodes, "Visit(WindowDefinitionNode).Spec");
        Nodes.Push(new WindowDefinitionNode(node.Name, spec));
    }

    public override void Visit(WindowNode node)
    {
        var definitions = new WindowDefinitionNode[node.Definitions.Length];
        for (var i = node.Definitions.Length - 1; i >= 0; i--)
            definitions[i] = (WindowDefinitionNode)SafePop(Nodes, "Visit(WindowNode).Definition");

        Nodes.Push(new WindowNode(definitions));
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

            if (!string.IsNullOrEmpty(node.Alias) && !tableSymbol.ContainsAlias(node.Alias))
            {
                if (TryReportUnknownAlias(node.Alias, tableSymbol.CompoundTables, node))
                    return;

                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    VisitorOperationNames.VisitAccessColumnNode,
                    $"Unknown alias '{node.Alias}'",
                    "Verify that the alias is defined in the FROM or JOIN clause.");
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
                TryReportOrThrowUnknownColumn(node.Name, tuple.Table.Columns, node);
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
            throw new VisitorException(
                VisitorName,
                VisitorOperationNames.VisitAccessColumnNode,
                $"Failed to process column access for '{node.Name}': {ex.Message}. " +
                "Check that the column exists in the specified table and that table aliases are correct.",
                ex
            );
        }
    }

    public override void Visit(AllColumnsNode node)
    {
        var identifier = _identifier;
        var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);

        Node[] inferredReplaceExpressions = null;
        if (node.ReplaceItems is { Length: > 0 })
        {
            inferredReplaceExpressions = new Node[node.ReplaceItems.Length];
            for (var i = node.ReplaceItems.Length - 1; i >= 0; i--)
                inferredReplaceExpressions[i] = Nodes.Pop();
        }

        if (!string.IsNullOrWhiteSpace(node.Alias) ||
            (!tableSymbol.IsCompoundTable && string.IsNullOrWhiteSpace(node.Alias)))
            ProcessSingleTable(node, tableSymbol, identifier, inferredReplaceExpressions);
        else if (tableSymbol.IsCompoundTable) ProcessCompoundTable(node, tableSymbol, inferredReplaceExpressions);

        Nodes.Push(node);
    }

    public override void Visit(IdentifierNode node)
    {
        if (node.Name != _identifier && _queryPart != QueryPart.From)
        {
            if (_queryPart == QueryPart.OrderBy && _selectFieldAliases.TryGetValue(node.Name, out var aliasExpression))
            {
                Nodes.Push(aliasExpression);
                return;
            }

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
            var column = tableSymbol.GetColumnByAliasAndName(_identifier, node.Name);

            if (column == null)
            {
                if (tableSymbol.IsCompoundTable &&
                    tableSymbol.TryGetColumns(node.Name, out var aliasColumns) &&
                    aliasColumns is { Length: 1 })
                {
                    var singleCol = aliasColumns[0];
                    Visit(new AccessColumnNode(singleCol.ColumnName, node.Name, singleCol.ColumnType,
                        TextSpan.Empty, singleCol.IntendedTypeName));
                    return;
                }

                if (TryReportOrThrowUnknownColumn(node.Name, tableSymbol.GetColumns(), node))
                    return;
            }

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
                var span = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new UnknownPropertyException(node.TableAlias ?? _identifier, "unknown", span);
            }

            var column = tableSymbol.GetColumnByAliasAndName(
                string.IsNullOrEmpty(node.TableAlias) ? _identifier : node.TableAlias,
                node.ObjectName);

            if (column == null)
            {
                if (TryReportUnknownProperty(node.ObjectName, null, node))
                    return;
                var span = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new UnknownPropertyException(node.ObjectName, "unknown", span);
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
            var typeHintingAttributes = GetCachedTypeHintAttributes(parentNodeType);

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
                    var nodeSpan = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                        nodeSpan);
                }

                isArray = propertyAccess?.PropertyType.IsArray == true;
                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(propertyAccess?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    if (TryReportObjectNotArray(
                            $"Object {parentNodeType.Name} property '{node.Name}' is not an array or indexable type.",
                            node))
                        return;
                    var notArraySpan = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new ObjectIsNotAnArrayException(
                        $"Object {parentNodeType.Name} property '{node.Name}' is not an array or indexable type.",
                        notArraySpan);
                }

                if (propertyAccess == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    var propSpan = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new UnknownPropertyException(
                        node.Name, parentNodeType.Name, propSpan);
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
                    var exSpan = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                        exSpan);
                }

                isArray = property?.PropertyType.IsArray == true;
                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(property?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    if (TryReportObjectNotArray($"Object {node.Name} is not an array or indexable type.", node))
                        return;
                    var naSpan = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new ObjectIsNotAnArrayException(
                        $"Object {node.Name} is not an array or indexable type.", naSpan);
                }

                if (property == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    var propSpan = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new UnknownPropertyException(
                        node.Name, parentNodeType.Name, propSpan);
                }

                Nodes.Push(new AccessObjectArrayNode(node.Token, property));
            }
            else
            {
                if (TryReportUnknownProperty(node.ObjectName, null, node))
                    return;
                var objSpan = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new UnknownPropertyException(
                    node.ObjectName, "unknown", objSpan);
            }
        }
    }

    public override void Visit(AccessObjectKeyNode node)
    {
        if (node.DestinationKind == AccessObjectKeyNode.Destination.Variable)
        {
            if (TryReportConstructionNotSupported($"Construction ${node.ToString()} is not yet supported.", node))
                return;
            var keySpan = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new ConstructionNotYetSupported($"Construction ${node.ToString()} is not yet supported.", keySpan);
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
            var typeHintingAttributes = GetCachedTypeHintAttributes(parentNodeType);

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
                    var exSpan1 = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                        exSpan1);
                }

                isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(propertyAccess?.PropertyType);

                if (!isIndexer)
                {
                    if (TryReportNoIndexer(
                            $"Object {parentNodeType.Name} property '{node.Name}' does not implement indexer.", node))
                        return;
                    var niSpan1 = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Object {parentNodeType.Name} property '{node.Name}' does not implement indexer.", niSpan1);
                }

                if (propertyAccess == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    var propSpan1 = node.HasSpan ? node.Span : TextSpan.Empty;
                    throw new UnknownPropertyException(
                        node.Name, parentNodeType.Name, propSpan1);
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
                var exSpan2 = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new ObjectDoesNotImplementIndexerException(
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}", exSpan2);
            }

            isIndexer = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(property?.PropertyType);

            if (!isIndexer)
            {
                if (TryReportNoIndexer($"Object {node.Name} does not implement indexer.", node))
                    return;
                var niSpan2 = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new ObjectDoesNotImplementIndexerException(
                    $"Object {node.Name} does not implement indexer.", niSpan2);
            }

            if (property == null)
            {
                if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                    return;
                var propSpan2 = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new UnknownPropertyException(
                    node.Name, parentNodeType.Name, propSpan2);
            }

            Nodes.Push(new AccessObjectKeyNode(node.Token, property));
        }
    }

    public override void Visit(PropertyValueNode node)
    {
        var parentNode = SafePeek(Nodes, VisitorOperationNames.VisitPropertyValueNode);
        if (parentNode?.ReturnType == null || parentNode.ReturnType == typeof(void))
        {
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new UnknownColumnOrAliasException(
                node.Name,
                "while resolving property access",
                span);
        }

        var parentNodeType = parentNode.ReturnType;

        if (parentNodeType is NullNode.NullType)
        {
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new UnknownPropertyException(node.Name, "Null", span);
        }


        if (parentNodeType == typeof(object) || parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            var typeHintingAttributes = GetCachedTypeHintAttributes(parentNodeType);

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
                throw new VisitorException(
                    VisitorName,
                    VisitorOperationNames.VisitPropertyValueNode,
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                    ex);
            }

            if (propertyInfo == null)
            {
                if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                    return;
                var span = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new UnknownPropertyException(node.Name, parentNodeType.Name, span);
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


                if (SchemaRegistry != null && !string.IsNullOrEmpty(rootIntendedTypeName))
                {
                    var schemaName = rootIntendedTypeName.Split('.').Last();
                    if (SchemaRegistry.TryGetSchema(schemaName, out var refSchema) &&
                        refSchema?.Node is BinarySchemaNode binaryNode)
                    {
                        var allFields = GetAllBinarySchemaFields(binaryNode);
                        var field = allFields.FirstOrDefault(f =>
                            string.Equals(f.Name, identNode.Name, StringComparison.OrdinalIgnoreCase));
                        if (field is FieldDefinitionNode fieldDef)
                            (propertyType, childIntendedTypeName) =
                                ResolveTypeAnnotationClrTypeWithIntendedName(fieldDef.TypeAnnotation);
                    }
                    else if (schemaName.StartsWith("Inline_"))
                    {
                        var inlineFieldName = schemaName.Substring("Inline_".Length);
                        var inlineFields = FindInlineSchemaFields(inlineFieldName);
                        if (inlineFields != null)
                        {
                            var field = inlineFields.FirstOrDefault(f =>
                                string.Equals(f.Name, identNode.Name, StringComparison.OrdinalIgnoreCase));
                            if (field is FieldDefinitionNode fieldDef)
                            {
                                (propertyType, childIntendedTypeName) =
                                    ResolveTypeAnnotationClrTypeWithIntendedName(fieldDef.TypeAnnotation);
                                if (fieldDef.TypeAnnotation is InlineSchemaTypeNode)
                                    childIntendedTypeName = $"Musoq.Generated.Interpreters.Inline_{fieldDef.Name}";
                            }
                        }
                    }
                }

                expressionNode = new PropertyValueNode(identNode.Name,
                    new ExpandoObjectPropertyInfo(identNode.Name, propertyType));
            }


            newNode = new DotNode(root, expressionNode, node.IsTheMostInner, string.Empty, expressionNode.ReturnType,
                childIntendedTypeName);
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
                    var span = node.HasSpan ? node.Span : TextSpan.Empty;
                    PrepareAndThrowUnknownPropertyExceptionMessage(propertyName,
                        root.ReturnType.GetProperties(), span);
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
                    var span = node.HasSpan ? node.Span : TextSpan.Empty;
                    PrepareAndThrowUnknownPropertyExceptionMessage(identifierNode.Name,
                        root.ReturnType.GetProperties(), span);
                }

                newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
            }
            else
            {
                var dotSpan = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new NotSupportedException(
                    $"Unsupported expression type in property access at position {dotSpan.Start}: {exp?.GetType().Name ?? "null"}. Check the query syntax near this location.");
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
        ISchema schema;
        try
        {
            schema = _provider.GetSchema(node.Schema);
        }
        catch (Exception ex)
        {
            if (ex is IDiagnosticException)
                throw;

            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new UnknownInterpretationSchemaException(
                node.Schema,
                $"Unknown schema '{node.Schema}'.",
                span);
        }

        const bool hasExternallyProvidedTypes = false;

        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());

        if (HasAlreadyUsedAlias(_queryAlias))
        {
            if (TryReportDuplicateAlias(node, _queryAlias, node))
                return;
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new AliasAlreadyUsedException(_queryAlias, span);
        }

        _generatedAliases.Add(_queryAlias);

        var aliasedSchemaFromNode = new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(),
            _queryAlias, node.QueryId, hasExternallyProvidedTypes);
        if (node.HasSpan)
            aliasedSchemaFromNode.WithSpan(node.Span);

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
        _allUsedSchemaNames.Add(aliasedSchemaFromNode.Schema);

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
            {
                PushErrorRecoveryState(node, schema);
                return;
            }

            return;
        }

        if (ValidateBindablePropertyAsTableWithDiagnostics(table, targetColumn, node))
        {
            PushErrorRecoveryState(node, schema);
            return;
        }

        AddAssembly(targetColumn.ColumnType.Assembly);


        if (node.PropertiesChain.Length > 1
            && targetColumn.ColumnType == typeof(object)
            && !string.IsNullOrEmpty(targetColumn.IntendedTypeName)
            && SchemaRegistry != null)
        {
            var resolved = ResolveSchemaPropertyChain(
                targetColumn.IntendedTypeName,
                node.PropertiesChain.Skip(1).ToArray());

            if (resolved != null)
            {
                var resolvedNestedTable = TurnTypeIntoTableWithIntendedTypeName(
                    resolved.Value.ClrType,
                    resolved.Value.IntendedTypeName,
                    node);
                if (resolvedNestedTable == null)
                {
                    PushErrorRecoveryState(node, schema);
                    return;
                }

                table = resolvedNestedTable;

                UpdateQueryAliasAndSymbolTable(node, schema, table);


                var resolvedChain = new PropertyFromNode.PropertyNameAndTypePair[node.PropertiesChain.Length];
                resolvedChain[0] = new PropertyFromNode.PropertyNameAndTypePair(
                    targetColumn.ColumnName, targetColumn.ColumnType, targetColumn.IntendedTypeName);
                for (var i = 1; i < node.PropertiesChain.Length; i++)
                {
                    var propType = i == node.PropertiesChain.Length - 1
                        ? resolved.Value.ClrType
                        : typeof(object);
                    resolvedChain[i] = new PropertyFromNode.PropertyNameAndTypePair(
                        node.PropertiesChain[i].PropertyName, propType);
                }

                Nodes.Push(
                    new Parser.PropertyFromNode(
                        node.Alias,
                        node.SourceAlias,
                        resolvedChain
                    )
                );
                return;
            }
        }

        var followedType = FollowPropertiesWithDiagnostics(targetColumn.ColumnType, node.PropertiesChain, node);
        if (followedType == null)
        {
            PushErrorRecoveryState(node, schema);
            return;
        }

        var nestedTable = TurnTypeIntoTableWithIntendedTypeName(
            followedType,
            targetColumn.IntendedTypeName,
            node);
        if (nestedTable == null)
        {
            PushErrorRecoveryState(node, schema);
            return;
        }

        table = nestedTable;

        UpdateQueryAliasAndSymbolTable(node, schema, table);

        var rewrittenChain =
            RewritePropertiesChainWithTargetColumnWithDiagnostics(targetColumn, node.PropertiesChain, node);
        if (rewrittenChain == null)
        {
            PushErrorRecoveryState(node, schema);
            return;
        }

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
                "Visit(AliasedFromNode): Processing Interpret function '{Identifier}' with alias '{Alias}' -> _queryAlias='{QueryAlias}'",
                node.Identifier, node.Alias, _queryAlias);

            var args = (ArgsListNode)Nodes.Pop();


            var schemaName = ExtractSchemaNameFromArgs(args, node.Identifier);


            var interpretTable = CreateInterpretTable(schemaName);


            Type? returnType = null;
            if (schemaName != null && SchemaRegistry != null &&
                SchemaRegistry.TryGetSchema(schemaName, out var schemaRegistration))
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
                "Visit(AliasedFromNode): Registered TableSymbol '{QueryAlias}' with {ColumnCount} columns in scope '{ScopeName}'",
                _queryAlias, interpretTable?.Columns?.Count() ?? 0, _currentScope.Name);

            Nodes.Push(new AliasedFromNode(node.Identifier, args, _queryAlias, returnType ?? node.ReturnType,
                node.InSourcePosition));
            return;
        }

        if (!_explicitlyUsedAliases.ContainsKey(node.Identifier) && TryResolveAsStandaloneFunction(node))
            return;

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
        _aliasToSchemaFromNodeMap.Add(_queryAlias, aliasedSchemaFromNode);
        _allUsedSchemaNames.Add(aliasedSchemaFromNode.Schema);

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
                var span = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new TableIsNotDefinedException(node.VariableName, span);
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

        if (node.JoinType is JoinType.AsOf or JoinType.AsOfLeft)
            ValidateAsOfJoinCondition(expression, source, joinedTable);

        var joinedFrom = new Parser.JoinFromNode(source, joinedTable, expression, node.JoinType);
        _identifier = joinedFrom.Alias;
        _schemaFromArgs.Clear();
        Nodes.Push(joinedFrom);
    }

    private void ValidateAsOfJoinCondition(Node expression, FromNode source, FromNode joinedTable)
    {
        if (ContainsOrNode(expression))
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                "ASOF JOIN ON clause does not support OR.",
                DiagnosticCode.MQ3038_AsOfJoinOrNotSupported,
                expression.Span);

        var (inequalities, equalityCount) = CollectConditions(expression);

        if (inequalities.Count == 0)
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                "ASOF JOIN requires at least one inequality condition (>=, >, <=, <).",
                DiagnosticCode.MQ3036_AsOfJoinMissingInequality,
                expression.Span);

        if (inequalities.Count > 1)
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                $"ASOF JOIN supports exactly one inequality condition. Found {inequalities.Count}.",
                DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities,
                expression.Span);

        var inequality = inequalities[0];
        var leftAliases = CollectFromNodeAliases(source);
        var rightAliases = CollectFromNodeAliases(joinedTable);
        ValidateInequalityReferencesBothSides(inequality, leftAliases, rightAliases);
        ValidateInequalityColumnIsOrderable(inequality);
    }

    private static bool ContainsOrNode(Node node)
    {
        if (node is OrNode)
            return true;

        if (node is AndNode and)
            return ContainsOrNode(and.Left) || ContainsOrNode(and.Right);

        return false;
    }

    private static (List<BinaryNode> Inequalities, int EqualityCount) CollectConditions(Node node)
    {
        var inequalities = new List<BinaryNode>();
        var equalityCount = 0;
        var stack = new Stack<Node>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is AndNode and)
            {
                stack.Push(and.Right);
                stack.Push(and.Left);
                continue;
            }

            if (current is BinaryNode binary)
            {
                if (IsInequalityNode(binary))
                    inequalities.Add(binary);
                else
                    equalityCount++;
            }
        }

        return (inequalities, equalityCount);
    }

    private static bool IsInequalityNode(BinaryNode node)
    {
        return node is GreaterNode or LessNode or GreaterOrEqualNode or LessOrEqualNode;
    }

    private void ValidateInequalityReferencesBothSides(BinaryNode inequality, HashSet<string> leftAliases, HashSet<string> rightAliases)
    {
        var columnAliases = ExtractColumnAliases(inequality.Left);
        columnAliases.UnionWith(ExtractColumnAliases(inequality.Right));

        var referencesLeft = columnAliases.Overlaps(leftAliases);
        var referencesRight = columnAliases.Overlaps(rightAliases);

        if (!referencesLeft || !referencesRight)
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                "ASOF JOIN inequality must reference columns from both sides.",
                DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
                inequality.Span);
    }

    private static HashSet<string> CollectFromNodeAliases(FromNode node)
    {
        var aliases = new HashSet<string>();
        CollectFromNodeAliasesRecursive(node, aliases);
        return aliases;
    }

    private static void CollectFromNodeAliasesRecursive(FromNode node, HashSet<string> aliases)
    {
        if (node == null) return;

        aliases.Add(node.Alias);

        if (node is JoinFromNode joinNode)
        {
            CollectFromNodeAliasesRecursive(joinNode.Source, aliases);
            CollectFromNodeAliasesRecursive(joinNode.With, aliases);
        }
    }

    private static HashSet<string> ExtractColumnAliases(Node node)
    {
        var aliases = new HashSet<string>();
        var stack = new Stack<Node>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is AccessColumnNode col)
            {
                aliases.Add(col.Alias);
                continue;
            }

            if (current is BinaryNode binary)
            {
                stack.Push(binary.Left);
                stack.Push(binary.Right);
            }
        }

        return aliases;
    }

    private static void ValidateInequalityColumnIsOrderable(BinaryNode inequality)
    {
        ThrowIfNotOrderable(inequality.Left.ReturnType);
        ThrowIfNotOrderable(inequality.Right.ReturnType);
    }

    private static void ThrowIfNotOrderable(Type columnType)
    {
        var underlying = Nullable.GetUnderlyingType(columnType) ?? columnType;

        if (!IsOrderableType(underlying))
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                $"ASOF JOIN inequality column type '{underlying.Name}' is not orderable.",
                DiagnosticCode.MQ3040_AsOfJoinInequalityColumnNotOrderable,
                TextSpan.Empty);
    }

    private static bool IsOrderableType(Type type)
    {
        return typeof(IComparable).IsAssignableFrom(type) ||
               type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(decimal);
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


        if (schemaName != null && SchemaRegistry != null)
        {
            var interpretTable = CreateInterpretTable(schemaName);

            Type? returnType = null;
            if (SchemaRegistry.TryGetSchema(schemaName, out var schemaRegistration))
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
        var window = node.Window != null ? Nodes.Pop() as WindowNode : null;
        var take = node.Take != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
        var select = Nodes.Pop() as SelectNode;
        var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;
        var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
        var from = Nodes.Pop() as FromNode;

        if (from is null)
        {
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new FromNodeIsNull(span);
        }

        if (groupBy == null && _refreshMethods.Count > 0)
            groupBy = new GroupByNode([new FieldNode(new IntegerNode("1", "s"), 0, string.Empty)], null);

        _currentScope.ScopeSymbolTable.AddSymbol(from.Alias.ToRefreshMethodsSymbolName(),
            new RefreshMethodsSymbol(_refreshMethods));
        _refreshMethods.Clear();

        if (_currentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(string.Empty))
            _currentScope.ScopeSymbolTable.MoveSymbol(string.Empty, from.Alias);

        Methods.Push(from.Alias);

        var queryNode = new QueryNode(select, from, where, groupBy, orderBy, skip, take, window);


        ValidateSelectFieldsArePrimitive(queryNode.Select.Fields, "SELECT");

        if (where != null)
        {
            ValidateExpressionIsPrimitive(where.Expression, "WHERE");
            ValidateExpressionIsBoolean(where.Expression, "WHERE");
        }

        if (groupBy != null)
        {
            foreach (var field in groupBy.Fields)
                ValidateExpressionIsPrimitive(field.Expression, "GROUP BY");

            if (groupBy.Having != null)
            {
                ValidateExpressionIsPrimitive(groupBy.Having.Expression, "HAVING");
                ValidateExpressionIsBoolean(groupBy.Having.Expression, "HAVING");
            }
        }

        if (orderBy != null)
            foreach (var field in orderBy.Fields)
            {
                ValidateOrderByExpression(field);
                ValidateExpressionIsPrimitive(field.Expression, "ORDER BY");
            }

        if (skip != null)
            ValidateExpressionIsPrimitive(skip.Expression, "SKIP");

        if (take != null)
            ValidateExpressionIsPrimitive(take.Expression, "TAKE");


        if (groupBy != null && select != null)
            ValidateGroupBySemantics(select, groupBy);

        long? skipValue = skip?.Expression is IntegerNode skipInt ? Convert.ToInt64(skipInt.ObjValue) : null;
        long? takeValue = take?.Expression is IntegerNode takeInt ? Convert.ToInt64(takeInt.ObjValue) : null;
        var isDistinct = select?.IsDistinct ?? false;


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
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        throw new NotSupportedException(
            $"Internal Query Node is not supported in this context at position {span.Start}");
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
                var span = node.HasSpan ? node.Span : TextSpan.Empty;
                throw new TypeNotFoundException(remappedType, string.Empty, span);
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
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new FieldLinkIndexOutOfRangeException(index, _groupByFields.Count, span);
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

    public bool IsCurrentContextColumn(string name)
    {
        if (string.IsNullOrEmpty(_identifier)) return false;

        if (!_currentScope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(_identifier, out var tableSymbol))
            return false;

        return tableSymbol.GetColumnByAliasAndName(_identifier, name) != null;
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

    private void VisitBinaryOperatorWithTypeConversion<T>(Func<Node, Node, T> nodeFactory, Node errorContextNode,
        BinaryOperatorKind operatorKind, BinaryOperationContext operationContext = BinaryOperationContext.Standard) where T : Node
    {
        var right = SafePop(Nodes, "VisitBinaryOperatorWithTypeConversion (right)");
        var left = SafePop(Nodes, "VisitBinaryOperatorWithTypeConversion (left)");


        if (operationContext == BinaryOperationContext.ArithmeticOperation)
        {
            var leftIsNull = left.ReturnType is NullNode.NullType;
            var rightIsNull = right.ReturnType is NullNode.NullType;

            if (leftIsNull && rightIsNull)
            {
                Nodes.Push(new NullNode(typeof(object)));
                return;
            }

            if (leftIsNull || rightIsNull)
            {
                var nonNullType = leftIsNull ? right.ReturnType : left.ReturnType;
                var baseType = BuildMetadataAndInferTypesVisitorUtilities.StripNullable(nonNullType);

                var newLeft = leftIsNull ? new NullNode(baseType, left.Span) : left;
                var newRight = rightIsNull ? new NullNode(baseType, right.Span) : right;

                var nullBranchResult = nodeFactory(newLeft, newRight);
                if (errorContextNode.HasSpan)
                    nullBranchResult.WithSpan(errorContextNode.Span);
                Nodes.Push(nullBranchResult);
                return;
            }
        }

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

        transformedLeft = TransformToNumericTypeIfNeeded(transformedLeft, transformedRight, operationContext);
        transformedRight = TransformToNumericTypeIfNeeded(transformedRight, transformedLeft, operationContext);

        ValidateBinaryOperatorOperands(transformedLeft, transformedRight, operatorKind, errorContextNode);

        var result = nodeFactory(transformedLeft, transformedRight);
        if (errorContextNode.HasSpan)
            result.WithSpan(errorContextNode.Span);
        Nodes.Push(result);
    }

    private Node TransformStringToDateTimeIfNeeded(Node candidateNode, Node otherNode)
    {
        if (candidateNode is not WordNode stringNode || !TypeConversionNodeFactory.IsDateTimeType(otherNode.ReturnType))
            return candidateNode;

        return _nodeFactory.CreateDateTimeConversionNode(otherNode.ReturnType, stringNode.Value);
    }

    private Node TransformToNumericTypeIfNeeded(Node candidateNode, Node otherNode,
        BinaryOperationContext operationContext)
    {
        var shouldTransform = operationContext switch
        {
            BinaryOperationContext.ArithmeticOperation => TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType),
            BinaryOperationContext.RelationalComparison => TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType),
            _ => TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType)
        };

        if (!shouldTransform || !TypeConversionNodeFactory.IsNumericLiteralNode(otherNode, out var targetType))
            return candidateNode;

        return _nodeFactory.CreateNumericConversionNode(candidateNode, targetType,
            TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType), operationContext);
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

    private void CollectSelectFieldAliases(FieldNode[] fields)
    {
        _selectFieldAliases.Clear();

        foreach (var field in fields)
        {
            if (field.Expression is AllColumnsNode)
                continue;

            var expressionText = field.Expression.ToString();
            var alias = field.FieldName;

            if (string.IsNullOrEmpty(alias))
                continue;

            if (string.Equals(alias, expressionText, StringComparison.Ordinal))
                continue;

            _selectFieldAliases.TryAdd(alias, field.Expression);
        }
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
        var identifier = GetCurrentMethodResolutionIdentifier();

        if (_usedSchemasQuantity > 1 && string.IsNullOrWhiteSpace(node.Alias))
        {
            if (TryInferAggregateMethodContext(identifier, node, args, out var inferredContext))
                return inferredContext;

            if (TryReportAmbiguousAggregateOwnerFromGetterCandidates(identifier, node, args))
                return default;

            if (TryInferNonAggregateMethodContext(identifier, node, args, out var nonAggContext))
                return nonAggContext;

            if (TryReportMissingAlias(node))
                return default;
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new AliasMissingException(AliasMissingException.CreateMethodCallMessage(node.ToString()), span);
        }

        var alias = !string.IsNullOrEmpty(node.Alias) ? node.Alias : identifier;
        return CreateMethodResolutionContext(alias);
    }

    private bool TryInferAggregateMethodContext(string identifier, AccessMethodNode node, ArgsListNode args,
        out MethodResolutionContext context)
    {
        context = default;

        var tableSymbol = FindTableSymbolInScopeHierarchy(identifier);
        if (!tableSymbol.IsCompoundTable)
            return false;

        AggregateResolutionSignature? signature = null;
        MethodResolutionContext? resolvedContext = null;
        var ambiguousAliases = new List<string>();

        foreach (var alias in tableSymbol.CompoundTables)
        {
            var candidateContext = CreateMethodResolutionContext(alias, false);

            if (!TryResolveAggregateSignature(node, args, candidateContext, out var candidateSignature))
                continue;

            if (signature == null)
            {
                signature = candidateSignature;
                resolvedContext = candidateContext;
                continue;
            }

            if (!signature.Value.Equals(candidateSignature))
            {
                if (ambiguousAliases.Count == 0 && resolvedContext != null)
                    ambiguousAliases.Add(resolvedContext.Value.Alias);

                ambiguousAliases.Add(candidateContext.Alias);
            }
        }

        if (ambiguousAliases.Count > 0)
        {
            ReportAmbiguousAggregateOwner(node, ambiguousAliases);
            return false;
        }

        if (resolvedContext == null)
            return false;

        RegisterMethodContextAssemblies(resolvedContext.Value.EntityType);
        context = resolvedContext.Value;
        return true;
    }

    private bool TryReportAmbiguousAggregateOwnerFromGetterCandidates(string identifier, AccessMethodNode node,
        ArgsListNode args)
    {
        var tableSymbol = FindTableSymbolInScopeHierarchy(identifier);
        if (!tableSymbol.IsCompoundTable)
            return false;

        var groupArgTypes = GetGroupArgumentTypes(args);
        var methodName = node.IsDistinct ? $"{node.Name}Distinct" : node.Name;
        var candidateAliases = new List<string>();
        Type? schemaType = null;
        MethodInfo resolvedMethod = null;

        foreach (var alias in tableSymbol.CompoundTables)
        {
            var candidateContext = CreateMethodResolutionContext(alias, false);
            if (!candidateContext.SchemaTablePair.Schema.TryResolveAggregationMethod(methodName, groupArgTypes,
                    candidateContext.EntityType, out var candidateMethod))
                continue;

            if (schemaType == null)
            {
                schemaType = candidateContext.SchemaTablePair.Schema.GetType();
                resolvedMethod = candidateMethod;
                candidateAliases.Add(alias);
                continue;
            }

            if (schemaType != candidateContext.SchemaTablePair.Schema.GetType() ||
                !AreSameMethod(resolvedMethod, candidateMethod))
            {
                candidateAliases.Add(alias);
            }
        }

        if (candidateAliases.Count <= 1)
            return false;

        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportAmbiguousAggregateOwner(node.ToString(), candidateAliases, node);
            return true;
        }

        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        throw new AmbiguousAggregateOwnerException(node.ToString(), candidateAliases, span);
    }

    private string GetCurrentMethodResolutionIdentifier()
    {
        return _currentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId)
            ? _currentScope[MetaAttributes.ProcessedQueryId]
            : _identifier;
    }

    private bool TryInferNonAggregateMethodContext(string identifier, AccessMethodNode node, ArgsListNode args,
        out MethodResolutionContext context)
    {
        context = default;

        var tableSymbol = FindTableSymbolInScopeHierarchy(identifier);
        if (!tableSymbol.IsCompoundTable)
            return false;

        var argTypes = GetArgumentTypes(args);
        var methodName = node.Name;

        MethodInfo? firstMethod = null;
        MethodResolutionContext? resolvedContext = null;
        var ambiguousAliases = new List<string>();
        var allSameMethod = true;

        foreach (var alias in tableSymbol.CompoundTables)
        {
            var candidateContext = CreateMethodResolutionContext(alias, false);
            var schema = candidateContext.SchemaTablePair.Schema;

            if (!schema.TryResolveMethod(methodName, argTypes, candidateContext.EntityType, out var candidateMethod) &&
                !schema.TryResolveRawMethod(methodName, argTypes, out candidateMethod))
                continue;

            if (firstMethod == null)
            {
                firstMethod = candidateMethod;
                resolvedContext = candidateContext;
                ambiguousAliases.Add(alias);
                continue;
            }

            ambiguousAliases.Add(alias);

            if (!AreSameMethod(firstMethod, candidateMethod))
                allSameMethod = false;
        }

        if (resolvedContext == null)
            return false;

        if (ambiguousAliases.Count > 1 && !allSameMethod)
        {
            ReportAmbiguousMethodOwner(node, ambiguousAliases);
            return true;
        }

        RegisterMethodContextAssemblies(resolvedContext.Value.EntityType);
        context = resolvedContext.Value;
        return true;
    }

    private void ReportAmbiguousMethodOwner(AccessMethodNode methodNode, IReadOnlyCollection<string> candidateAliases)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportAmbiguousMethodOwner(methodNode.ToString(), candidateAliases, methodNode);
            return;
        }

        var span = methodNode.HasSpan ? methodNode.Span : TextSpan.Empty;
        throw new AmbiguousMethodOwnerException(methodNode.ToString(), candidateAliases, span);
    }

    private void ReportAmbiguousAggregateOwner(AccessMethodNode methodNode, IReadOnlyCollection<string> candidateAliases)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportAmbiguousAggregateOwner(methodNode.ToString(), candidateAliases, methodNode);
            return;
        }

        var span = methodNode.HasSpan ? methodNode.Span : TextSpan.Empty;
        throw new AmbiguousAggregateOwnerException(methodNode.ToString(), candidateAliases, span);
    }

    private MethodResolutionContext CreateMethodResolutionContext(string alias, bool registerAssemblies = true)
    {
        var tableSymbol = FindTableSymbolInScopeHierarchy(alias);
        var schemaTablePair = tableSymbol.GetTableByAlias(alias);
        var entityType = schemaTablePair.Table.Metadata.TableEntityType;

        if (registerAssemblies)
            RegisterMethodContextAssemblies(entityType);

        return new MethodResolutionContext(alias, tableSymbol, schemaTablePair, entityType);
    }

    private void RegisterMethodContextAssemblies(Type entityType)
    {
        AddAssembly(entityType.Assembly);
        AddBaseTypeAssembly(entityType);
    }

    private bool TryResolveAggregateSignature(AccessMethodNode node, ArgsListNode args, MethodResolutionContext context,
        out AggregateResolutionSignature signature)
    {
        signature = default;

        var argTypes = GetArgumentTypes(args);
        var groupArgTypes = GetGroupArgumentTypes(args);

        var methodName = node.Name;
        if (node.IsDistinct)
        {
            methodName = $"{methodName}Distinct";
            if (!context.SchemaTablePair.Schema.TryResolveAggregationMethod(methodName, groupArgTypes,
                    context.EntityType, out var distinctMethod))
                return false;

            if (!TryResolveSetAggregationMethod(node, argTypes, distinctMethod, context, out var setDistinctMethod))
                return false;

            signature = new AggregateResolutionSignature(context.SchemaTablePair.Schema.GetType(), distinctMethod,
                setDistinctMethod);
            return true;
        }

        if (!context.SchemaTablePair.Schema.TryResolveAggregationMethod(methodName, groupArgTypes, context.EntityType,
                out var method))
            return false;

        if (!TryResolveSetAggregationMethod(node, argTypes, method, context, out var setMethod))
            return false;

        signature = new AggregateResolutionSignature(context.SchemaTablePair.Schema.GetType(), method, setMethod);
        return true;
    }

    private bool TryResolveSetAggregationMethod(AccessMethodNode node, Type[] argTypes, MethodInfo method,
        MethodResolutionContext context, out MethodInfo setMethod)
    {
        var setMethodName = node.IsDistinct ? "SetDistinctAggregate" : $"Set{method.Name}";
        var setArgTypes = new Type[argTypes.Length + 1];
        setArgTypes[0] = typeof(string);

        for (var i = 0; i < argTypes.Length; i++)
            setArgTypes[i + 1] = argTypes[i];

        return context.SchemaTablePair.Schema.TryResolveAggregationMethod(setMethodName, setArgTypes,
            context.EntityType, out setMethod);
    }

    private (MethodInfo Method, bool CanSkipInjectSource) ResolveMethod(AccessMethodNode node, ArgsListNode args,
        MethodResolutionContext context)
    {
        var argTypes = GetArgumentTypes(args);
        var groupArgTypes = GetGroupArgumentTypes(args);


        var methodName = node.Name;
        if (node.IsDistinct)
        {
            var distinctMethodName = $"{node.Name}Distinct";
            if (context.SchemaTablePair.Schema.TryResolveAggregationMethod(distinctMethodName, groupArgTypes,
                    context.EntityType,
                    out var distinctMethod)) return (distinctMethod, false);


            throw CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(
                $"{node.Name}(DISTINCT ...)", args.Args);
        }

        if (context.SchemaTablePair.Schema.TryResolveAggregationMethod(methodName, groupArgTypes, context.EntityType,
                out var method)) return (method, false);

        if (context.SchemaTablePair.Schema.TryResolveMethod(methodName, argTypes, context.EntityType, out method))
            return (method, false);

        if (context.SchemaTablePair.Schema.TryResolveRawMethod(methodName, argTypes, out method)) return (method, true);

        if (IsInterpretOrParseFunction(methodName))
            throw new CannotResolveMethodException(
                $"'{methodName}' can only be used inside CROSS APPLY or OUTER APPLY, not in SELECT or WHERE. " +
                $"Example: ... CROSS APPLY {methodName}(source, 'SchemaName') alias",
                DiagnosticCode.MQ3033_InterpretFunctionOutsideApply,
                node.Span);

        throw CreateMethodResolutionExceptionWithSuggestion(methodName, args.Args, context);
    }

    private static CannotResolveMethodException CreateMethodResolutionExceptionWithSuggestion(
        string methodName, Node[] args, MethodResolutionContext context)
    {
        var allMethods = context.SchemaTablePair.Schema.GetAllLibraryMethods();
        var availableNames = allMethods.Keys;
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion(methodName, availableNames, maxDistance: 2);

        if (!string.IsNullOrWhiteSpace(suggestion))
        {
            var types = args.Length > 0
                ? string.Join(", ", args.Select(f => f.ReturnType?.ToString() ?? "null"))
                : string.Empty;

            var message = string.IsNullOrEmpty(types)
                ? $"Method {methodName} cannot be resolved. Did you mean '{suggestion}'?"
                : $"Method {methodName} with argument types {types} cannot be resolved. Did you mean '{suggestion}'?";

            return new CannotResolveMethodException(message);
        }

        return CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(methodName, args);
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


        var setMethodName = node.IsDistinct ? "SetDistinctAggregate" : $"Set{method.Name}";
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

    private void ProcessSingleTable(AllColumnsNode node, TableSymbol tableSymbol, string identifier, Node[] inferredReplaceExpressions)
    {
        var generatedColumnIdentifier = node.Alias ?? identifier;
        (ISchema Schema, ISchemaTable Table, string TableName) tuple;
        try
        {
            tuple = tableSymbol.GetTableByAlias(generatedColumnIdentifier);
        }
        catch (KeyNotFoundException)
        {
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new UnknownColumnOrAliasException(
                generatedColumnIdentifier,
                "in wildcard projection",
                span);
        }

        var table = tuple.Table;

        var eligibleColumns = new List<ISchemaColumn>();
        for (var i = 0; i < table.Columns.Length; i++)
            if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(table.Columns[i].ColumnType))
                eligibleColumns.Add(table.Columns[i]);

        var filteredColumns = node.HasModifiers
            ? ApplyStarModifiers(node, eligibleColumns, inferredReplaceExpressions)
            : null;

        var generatedColumns = GetOrCreateGeneratedColumns(generatedColumnIdentifier);

        if (filteredColumns != null)
        {
            var positionCounter = 0;
            foreach (var entry in filteredColumns)
            {
                if (entry.ReplacementExpression != null)
                {
                    AddAssembly(entry.ReplacementExpression.ReturnType.Assembly);
                    var fieldName = tableSymbol.HasAlias
                        ? $"{generatedColumnIdentifier}.{entry.Column.ColumnName}"
                        : entry.Column.ColumnName;
                    generatedColumns.Add(new FieldNode(entry.ReplacementExpression, positionCounter++, fieldName));
                }
                else
                {
                    AddColumnToGeneratedColumns(tableSymbol, entry.Column, positionCounter++,
                        generatedColumnIdentifier, generatedColumns);
                }
            }
        }
        else
        {
            var positionCounter = 0;
            foreach (var column in eligibleColumns)
                AddColumnToGeneratedColumns(tableSymbol, column, positionCounter++, generatedColumnIdentifier,
                    generatedColumns);
        }

        UpdateUsedColumns(generatedColumnIdentifier, table);
    }

    private void ProcessCompoundTable(AllColumnsNode node, TableSymbol tableSymbol, Node[] inferredReplaceExpressions)
    {
        if (!node.HasModifiers)
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

            return;
        }

        var allEligible = new List<(string TableIdentifier, ISchemaColumn Column)>();
        var tablesByIdentifier =
            new Dictionary<string, (ISchemaTable Table, TableSymbol Symbol)>(StringComparer.OrdinalIgnoreCase);

        foreach (var tableIdentifier in tableSymbol.CompoundTables)
        {
            var tuple = tableSymbol.GetTableByAlias(tableIdentifier);
            var table = tuple.Table;
            tablesByIdentifier[tableIdentifier] = (table, tableSymbol);

            for (var i = 0; i < table.Columns.Length; i++)
                if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(table.Columns[i]
                        .ColumnType))
                    allEligible.Add((tableIdentifier, table.Columns[i]));
        }

        var eligibleAsColumns = allEligible.Select(e => e.Column).ToList();
        var filtered = ApplyStarModifiers(node, eligibleAsColumns, inferredReplaceExpressions);

        var survivingSet = new HashSet<(string TableId, string ColumnName)>(
            filtered.Select((entry, idx) => (allEligible[FindOriginalIndex(allEligible, entry.Column)].TableIdentifier,
                entry.Column.ColumnName)));

        foreach (var tableIdentifier in tableSymbol.CompoundTables)
        {
            var (table, _) = tablesByIdentifier[tableIdentifier];
            var generatedColumns = GetOrCreateGeneratedColumns(tableIdentifier);

            var positionCounter = 0;
            for (var i = 0; i < table.Columns.Length; i++)
            {
                var column = table.Columns[i];
                if (!BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(column.ColumnType))
                    continue;

                if (!survivingSet.Contains((tableIdentifier, column.ColumnName)))
                    continue;

                var replaceEntry = filtered.FirstOrDefault(e =>
                    e.Column == column && e.ReplacementExpression != null);

                if (replaceEntry.ReplacementExpression != null)
                {
                    AddAssembly(replaceEntry.ReplacementExpression.ReturnType.Assembly);
                    var fieldName = $"{tableIdentifier}.{column.ColumnName}";
                    generatedColumns.Add(new FieldNode(replaceEntry.ReplacementExpression, positionCounter++,
                        fieldName));
                }
                else
                {
                    AddColumnToGeneratedColumns(tableSymbol, column, positionCounter++, tableIdentifier,
                        generatedColumns, true);
                }
            }

            UpdateUsedColumns(tableIdentifier, table);
        }
    }

    private static int FindOriginalIndex(
        List<(string TableIdentifier, ISchemaColumn Column)> allEligible,
        ISchemaColumn column)
    {
        for (var i = 0; i < allEligible.Count; i++)
            if (allEligible[i].Column == column)
                return i;

        return -1;
    }

    private List<(ISchemaColumn Column, Node ReplacementExpression)> ApplyStarModifiers(
        AllColumnsNode node,
        List<ISchemaColumn> eligibleColumns,
        Node[] inferredReplaceExpressions)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;

        var surviving = ApplyLikeFilter(node, eligibleColumns, span);
        surviving = ApplyExcludeFilter(node, surviving, span);
        return ApplyReplaceSubstitution(node, surviving, eligibleColumns, inferredReplaceExpressions, span);
    }

    private static List<ISchemaColumn> ApplyLikeFilter(
        AllColumnsNode node,
        List<ISchemaColumn> columns,
        TextSpan span)
    {
        if (node.LikePattern == null)
            return new List<ISchemaColumn>(columns);

        var matcher = CreateLikeColumnMatcher(node.LikePattern);
        var filtered = node.IsNotLike
            ? columns.Where(c => !matcher(c.ColumnName)).ToList()
            : columns.Where(c => matcher(c.ColumnName)).ToList();

        if (filtered.Count == 0)
        {
            var direction = node.IsNotLike ? "NOT LIKE" : "LIKE";
            throw new StarModifierValidationException(
                $"Star modifier {direction} '{node.LikePattern}' matched no columns.",
                DiagnosticCode.MQ3045_StarLikeMatchedNoColumns,
                span);
        }

        return filtered;
    }

    private static List<ISchemaColumn> ApplyExcludeFilter(
        AllColumnsNode node,
        List<ISchemaColumn> columns,
        TextSpan span)
    {
        if (node.ExcludeColumns is not { Length: > 0 })
            return columns;

        var columnNames = new HashSet<string>(columns.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var excl in node.ExcludeColumns)
        {
            if (!seen.Add(excl))
                throw new StarModifierValidationException(
                    $"Duplicate column '{excl}' in EXCLUDE list.",
                    DiagnosticCode.MQ3046_StarExcludeDuplicateColumn,
                    span);

            if (!columnNames.Contains(excl))
                throw new StarModifierValidationException(
                    $"EXCLUDE references non-existent column '{excl}'.",
                    DiagnosticCode.MQ3041_StarExcludeColumnNotFound,
                    span);
        }

        var excludeSet = new HashSet<string>(node.ExcludeColumns, StringComparer.OrdinalIgnoreCase);
        var surviving = columns.Where(c => !excludeSet.Contains(c.ColumnName)).ToList();

        if (surviving.Count == 0)
            throw new StarModifierValidationException(
                "EXCLUDE would remove all columns from the star expansion.",
                DiagnosticCode.MQ3043_StarExcludeRemovesAllColumns,
                span);

        return surviving;
    }

    private static List<(ISchemaColumn Column, Node ReplacementExpression)> ApplyReplaceSubstitution(
        AllColumnsNode node,
        List<ISchemaColumn> surviving,
        List<ISchemaColumn> eligibleColumns,
        Node[] inferredReplaceExpressions,
        TextSpan span)
    {
        var result = surviving.Select(c => (Column: c, ReplacementExpression: (Node)null)).ToList();

        if (node.ReplaceItems is not { Length: > 0 })
            return result;

        var survivingNames = new HashSet<string>(surviving.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);
        var excludeSet = node.ExcludeColumns != null
            ? new HashSet<string>(node.ExcludeColumns, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < node.ReplaceItems.Length; i++)
        {
            var targetColumn = node.ReplaceItems[i].ColumnName;

            if (!seen.Add(targetColumn))
                throw new StarModifierValidationException(
                    $"Duplicate column '{targetColumn}' in REPLACE list.",
                    DiagnosticCode.MQ3047_StarReplaceDuplicateColumn,
                    span);

            if (excludeSet.Contains(targetColumn))
                throw new StarModifierValidationException(
                    $"Column '{targetColumn}' appears in both EXCLUDE and REPLACE.",
                    DiagnosticCode.MQ3044_StarColumnInBothExcludeAndReplace,
                    span);

            if (!survivingNames.Contains(targetColumn))
            {
                var eligibleNames = new HashSet<string>(eligibleColumns.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);
                var wasRemoved = eligibleNames.Contains(targetColumn);
                var code = wasRemoved
                    ? DiagnosticCode.MQ3048_StarReplaceTargetsRemovedColumn
                    : DiagnosticCode.MQ3042_StarReplaceColumnNotFound;
                var reason = wasRemoved
                    ? "was removed by LIKE filter or EXCLUDE"
                    : "does not exist in the table";
                throw new StarModifierValidationException(
                    $"REPLACE targets column '{targetColumn}' which {reason}.",
                    code,
                    span);
            }

            var replaceExpr = inferredReplaceExpressions[i];
            var idx = result.FindIndex(e =>
                string.Equals(e.Column.ColumnName, targetColumn, StringComparison.OrdinalIgnoreCase));
            result[idx] = (result[idx].Column, replaceExpr);
        }

        return result;
    }

    private static Func<string, bool> CreateLikeColumnMatcher(string pattern)
    {
        var sb = new System.Text.StringBuilder("^");
        foreach (var ch in pattern)
        {
            switch (ch)
            {
                case '%':
                    sb.Append(".*");
                    break;
                case '_':
                    sb.Append('.');
                    break;
                default:
                    sb.Append(System.Text.RegularExpressions.Regex.Escape(ch.ToString()));
                    break;
            }
        }
        sb.Append('$');

        var regex = new System.Text.RegularExpressions.Regex(
            sb.ToString(),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Compiled);
        return input => regex.IsMatch(input);
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

    private (Type ReturnType, MethodInfo? ResolvedFactory) InferWindowFunctionReturnType(string functionName, Node[] args)
    {
        var normalizedName = functionName.ToLowerInvariant().Replace("_", "");

        MethodInfo? resolvedFactory = null;
        var isBuiltInOffset = normalizedName is "lag" or "lead";

        if (!isBuiltInOffset)
            TryResolveWindowFunctionFactory(functionName, out resolvedFactory);

        Type returnType;
        if (resolvedFactory != null)
        {
            returnType = ExtractWindowFunctionResultType(resolvedFactory) ?? typeof(object);
        }
        else
        {
            returnType = normalizedName switch
            {
                "lag" or "lead" => MakeNullableIfValueType(
                    args.Length > 0 ? args[0].ReturnType ?? typeof(object) : typeof(object)),
                _ => typeof(object)
            };
        }

        return (returnType, resolvedFactory);
    }

    private bool TryResolveWindowFunctionFactory(string functionName, out MethodInfo? factoryMethod)
    {
        foreach (var schemaFrom in _aliasToSchemaFromNodeMap.Values)
        {
            var schema = _provider.GetSchema(schemaFrom.Schema);
            if (schema.TryResolveWindowFunction(functionName, out var resolved))
            {
                factoryMethod = resolved;
                return true;
            }
        }

        foreach (var schemaName in _allUsedSchemaNames)
        {
            var schema = _provider.GetSchema(schemaName);
            if (schema.TryResolveWindowFunction(functionName, out var resolved))
            {
                factoryMethod = resolved;
                return true;
            }
        }

        factoryMethod = null;
        return false;
    }

    private static Type? ExtractWindowFunctionResultType(MethodInfo factoryMethod)
    {
        var returnType = factoryMethod.ReturnType;
        var windowFunctionInterface = returnType.GetInterfaces()
            .Concat([returnType])
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWindowFunction<,>));

        return windowFunctionInterface?.GetGenericArguments()[1];
    }

    private static Type MakeNullableIfValueType(Type type)
    {
        if (type.IsValueType)
            return typeof(Nullable<>).MakeGenericType(type);

        return type;
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

    private void PushErrorRecoveryState(PropertyFromNode node, ISchema schema)
    {
        var emptyTable = new DynamicTable([], typeof(object));
        UpdateQueryAliasAndSymbolTable(node, schema, emptyTable);
        Nodes.Push(new Parser.PropertyFromNode(node.Alias, node.SourceAlias, node.PropertiesChain));
    }

    private void ValidateGroupBySemantics(SelectNode select, GroupByNode groupBy)
    {
        var groupByExpressionStrings = new HashSet<string>(
            groupBy.Fields.Select(f => f.Expression.ToString()),
            StringComparer.OrdinalIgnoreCase);

        var groupByColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in groupBy.Fields) CollectColumnNames(field.Expression, groupByColumnNames);

        foreach (var field in select.Fields)
        {
            if (IsConstantExpression(field.Expression))
                continue;

            if (groupByExpressionStrings.Contains(field.Expression.ToString()))
                continue;

            if (ContainsAggregateFunction(field.Expression))
                continue;

            var nonGroupedColumns = new List<string>();
            FindNonGroupedColumns(field.Expression, groupByExpressionStrings, groupByColumnNames, nonGroupedColumns);

            if (nonGroupedColumns.Count <= 0)
                continue;

            var columnName = nonGroupedColumns[0];
            var groupByNames = groupBy.Fields
                .Select(f => f.Expression.ToString())
                .ToArray();

            if (TryReportNonAggregatedColumnInSelect(columnName, groupByNames, field.Expression))
                continue;

            throw new NonAggregatedColumnInSelectException(columnName, groupByNames,
                field.Expression.HasSpan ? field.Expression.Span : TextSpan.Empty);
        }
    }

    private bool TryReportNonAggregatedColumnInSelect(string columnName, string[] groupByColumns, Node? node)
    {
        if (DiagnosticContext != null)
        {
            var groupByList = groupByColumns.Length > 0
                ? string.Join(", ", groupByColumns)
                : "(none)";
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3012_NonAggregateInSelect,
                $"Column '{columnName}' must appear in the GROUP BY clause or be used in an aggregate function. " +
                $"Current GROUP BY columns: {groupByList}.",
                node);
            return true;
        }

        return false;
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

    private void ValidateExpressionIsBoolean(Node expression, string context)
    {
        var expressionType = NormalizeOperandType(expression.ReturnType);
        if (CanSkipStaticTypeValidation(expressionType))
            return;

        if (expressionType == typeof(bool))
            return;

        if (TryReportTypeMismatch(
                $"{context} clause requires a boolean expression, but got '{expressionType.Name}'.",
                expression))
            return;

        throw new TypeMismatchException(typeof(bool), expressionType,
            expression.HasSpan ? expression.Span : TextSpan.Empty);
    }

    private void ValidateOrderByExpression(FieldOrderedNode field)
    {
        if (field.Expression is not IntegerNode integerNode)
            return;

        if (!string.IsNullOrEmpty(field.FieldName) &&
            !string.Equals(field.FieldName, integerNode.ToString(), StringComparison.Ordinal))
            return;

        const string message = "ORDER BY column position is not supported. Use a column name or alias instead of a numeric position.";

        if (TryReportSemanticError<NotSupportedException>(DiagnosticCode.MQ2030_UnsupportedSyntax, message, field))
            return;

        throw new NotSupportedException(message);
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

        ReconcileFieldTypesForSetOperation(leftFields, rightFields, rightFields[0].Expression);

        _cachedSetFields.TryAdd(cachedSetOperatorKey, ResolveFieldsForCache(leftFields, rightFields));
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

        ReconcileFieldTypesForSetOperation(leftFields, rightFields, leftFields[0].Expression);

        _cachedSetFields.TryAdd(currentSetOperatorKey, ResolveFieldsForCache(leftFields, rightFields));
    }

    private void ReconcileFieldTypesForSetOperation(FieldNode[] leftFields, FieldNode[] rightFields,
        Node errorContextNode)
    {
        for (var i = 0; i < leftFields.Length; i++)
        {
            var leftType = leftFields[i].Expression.ReturnType;
            var rightType = rightFields[i].Expression.ReturnType;

            if (leftType == rightType)
                continue;

            var leftIsNull = leftType is NullNode.NullType;
            var rightIsNull = rightType is NullNode.NullType;

            if (leftIsNull && rightIsNull)
                continue;

            if (leftIsNull)
            {
                leftFields[i] = new FieldNode(
                    new NullNode(rightType, leftFields[i].Expression.Span),
                    leftFields[i].FieldOrder,
                    leftFields[i].FieldName,
                    leftFields[i].Span);
                continue;
            }

            if (rightIsNull)
            {
                rightFields[i] = new FieldNode(
                    new NullNode(leftType, rightFields[i].Expression.Span),
                    rightFields[i].FieldOrder,
                    rightFields[i].FieldName,
                    rightFields[i].Span);
                continue;
            }

            if (TryReportSetOperatorColumnTypes(leftFields[i], rightFields[i], errorContextNode))
                continue;
            throw new SetOperatorMustHaveSameTypesOfColumnsException(leftFields[i], rightFields[i]);
        }
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
            var dialectMessage = GetDialectColumnHint(identifier);
            if (dialectMessage != null)
            {
                DiagnosticContext.ReportError(DiagnosticCode.MQ3001_UnknownColumn, dialectMessage, node);
                return true;
            }

            var availableColumns = columns.Select(c => c.ColumnName);
            DiagnosticContext.ReportUnknownColumn(identifier, availableColumns, node);
            return true;
        }

        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        PrepareAndThrowUnknownColumnExceptionMessage(identifier, columns, span);
        return false;
    }

    /// <summary>
    ///     Reports an unknown alias error if diagnostic context is available.
    /// </summary>
    /// <param name="alias">The alias that was not found.</param>
    /// <param name="availableAliases">Available aliases for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (continue execution), false otherwise.</returns>
    protected bool TryReportUnknownAlias(string alias, string[] availableAliases, Node node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportUnknownAlias(alias, availableAliases, node);
            return true;
        }

        return false;
    }

    private bool TryResolveAsStandaloneFunction(AliasedFromNode node)
    {
        var sourceAlias = _queryAlias;

        if (string.IsNullOrEmpty(sourceAlias))
            return false;

        ISchema schema;
        ISchemaTable sourceTable;

        if (_aliasToSchemaFromNodeMap.TryGetValue(sourceAlias, out var schemaFrom))
        {
            schema = _provider.GetSchema(schemaFrom.Schema);
            var sourceSymbol = FindTableSymbolInScopeHierarchy(sourceAlias);
            sourceTable = sourceSymbol.FullTable;
        }
        else if (_aliasMapToInMemoryTableMap.TryGetValue(sourceAlias, out var inMemoryName))
        {
            var sourceSymbol = FindTableSymbolInScopeHierarchy(inMemoryName);
            sourceTable = sourceSymbol.FullTable;
            schema = new TransitionSchema(inMemoryName, sourceTable);
        }
        else
        {
            return false;
        }

        var entityType = sourceTable.Metadata.TableEntityType;
        var args = (ArgsListNode)Nodes.Pop();
        var argTypes = args.Args.Select(a => a.ReturnType).ToArray();

        if (!schema.TryResolveMethod(node.Identifier, argTypes, entityType, out var method) &&
            !schema.TryResolveRawMethod(node.Identifier, argTypes, out method))
            return false;

        var returnType = method.ReturnType;
        var convertedTable = TurnTypeIntoTableWithDiagnostics(returnType, node);

        if (convertedTable == null)
            return false;

        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString());
        _generatedAliases.Add(_queryAlias);

        var functionToken = new FunctionToken(node.Identifier, TextSpan.Empty);
        var canSkipInjectSource = schema.TryResolveRawMethod(node.Identifier, argTypes, out _);
        var accessMethodNode = new AccessMethodNode(functionToken, args, null, canSkipInjectSource, method,
            sourceAlias);

        var tableSymbol = new TableSymbol(_queryAlias, schema, convertedTable, !string.IsNullOrEmpty(node.Alias));
        _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);
        _currentScope[node.Id] = _queryAlias;
        _aliasMapToInMemoryTableMap.Add(_queryAlias, sourceAlias);
        _currentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);

        AddAssembly(method.DeclaringType!.Assembly);

        Nodes.Push(new Parser.AccessMethodFromNode(node.Alias, sourceAlias, accessMethodNode, returnType));
        return true;
    }

    private ISchemaTable CreateInterpretTable(string? schemaName)
    {
        if (schemaName == null || SchemaRegistry == null)
            throw new InvalidOperationException(
                $"Cannot create interpret table: schema name is '{schemaName ?? "null"}' and schema registry is {(SchemaRegistry != null ? "present" : "null")}.");

        var schema = SchemaRegistry.GetSchema(schemaName);
        if (schema == null)
        {
            var schemaNames = string.Join(", ", SchemaRegistry.Schemas.Select(s => s.Name));
            throw new InvalidOperationException($"Interpretation schema '{schemaName}' not found. Available: [{schemaNames}].");
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


                    if (parsedField.TypeAnnotation is InlineSchemaTypeNode)
                        intendedTypeName = $"Musoq.Generated.Interpreters.Inline_{parsedField.Name}";
                }
                else if (field is ComputedFieldNode computedField)
                {
                    var exprType = computedField.Expression.ReturnType;


                    if (exprType == null || exprType == typeof(void))
                        columnType = InferComputedFieldType(computedField.Expression, columns);
                    else
                        columnType = exprType;

                    isConditional = ReferencesConditionalField(computedField.Expression, allFields);
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

                if (field.FieldType == TextFieldType.Pattern && field.CaptureGroups.Length > 0)
                {
                    columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(object),
                        $"Musoq.Generated.Interpreters.{schema.Name}.CaptureResult_{field.Name}"));
                    continue;
                }

                if (field.FieldType == TextFieldType.Repeat)
                {
                    var elementSchemaName = field.PrimaryValue ?? "object";
                    columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(object[]),
                        $"Musoq.Generated.Interpreters.{elementSchemaName}[]"));
                    continue;
                }

                if (field.FieldType == TextFieldType.Switch)
                {
                    columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(ExpandoObject)));
                    continue;
                }

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
        if (string.IsNullOrEmpty(binaryNode.Extends))
            return binaryNode.Fields.ToList();

        var allFields = new List<SchemaFieldNode>();

        if (!string.IsNullOrEmpty(binaryNode.Extends))
        {
            var parentSchema = SchemaRegistry.GetSchema(binaryNode.Extends);
            if (parentSchema?.Node is BinarySchemaNode parentBinaryNode)
                allFields.AddRange(GetAllBinarySchemaFields(parentBinaryNode));
        }


        var overriddenParentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var childField in binaryNode.Fields)
            for (var i = 0; i < allFields.Count; i++)
                if (string.Equals(allFields[i].Name, childField.Name, StringComparison.OrdinalIgnoreCase))
                {
                    allFields[i] = childField;
                    overriddenParentNames.Add(childField.Name);
                    break;
                }


        foreach (var childField in binaryNode.Fields)
            if (!overriddenParentNames.Contains(childField.Name))
                allFields.Add(childField);

        return allFields;
    }

    private SchemaFieldNode[]? FindInlineSchemaFields(string fieldName)
    {
        if (SchemaRegistry == null) return null;

        foreach (var registration in SchemaRegistry.Schemas)
            if (registration.Node is BinarySchemaNode binaryNode)
            {
                var allFields = GetAllBinarySchemaFields(binaryNode);
                foreach (var field in allFields)
                    if (field is FieldDefinitionNode { TypeAnnotation: InlineSchemaTypeNode inlineSchema }
                        && string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                        return inlineSchema.Fields;
            }

        return null;
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
            if (SchemaRegistry != null && SchemaRegistry.TryGetSchema(schemaRef.SchemaName, out var refSchema))
            {
                if (refSchema?.GeneratedType != null)
                    return (refSchema.GeneratedType, null);


                var typeName = refSchema?.GeneratedTypeName;
                if (typeName != null && schemaRef.IsGenericInstantiation)
                    typeName = $"{typeName}<{string.Join(",", schemaRef.TypeArguments)}>";

                return (typeof(object), typeName);
            }

            return (typeof(object), null);
        }


        if (typeAnnotation is StringTypeNode stringType &&
            !string.IsNullOrEmpty(stringType.AsTextSchemaName))
        {
            if (SchemaRegistry != null &&
                SchemaRegistry.TryGetSchema(stringType.AsTextSchemaName, out var textSchema))
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

        if (typeAnnotation is InlineSchemaTypeNode) return (typeof(object), null);

        if (typeAnnotation is RepeatUntilTypeNode repeatUntilType)
        {
            var (elementType, elementIntendedTypeName) =
                ResolveTypeAnnotationClrTypeWithIntendedName(repeatUntilType.ElementType);
            var arrayClrType = elementType.MakeArrayType();
            var arrayIntendedTypeName = elementIntendedTypeName != null ? $"{elementIntendedTypeName}[]" : null;
            return (arrayClrType, arrayIntendedTypeName);
        }

        return (typeAnnotation.ClrType, null);
    }

    private (Type ClrType, string? IntendedTypeName)? ResolveSchemaPropertyChain(
        string intendedTypeName,
        PropertyFromNode.PropertyNameAndTypePair[] remainingProperties)
    {
        if (SchemaRegistry == null || remainingProperties.Length == 0)
            return null;


        var lastDot = intendedTypeName.LastIndexOf('.');
        var simpleName = lastDot >= 0 ? intendedTypeName.Substring(lastDot + 1) : intendedTypeName;

        string baseSchemaName;
        string[] typeArgs;

        var angleBracket = simpleName.IndexOf('<');
        if (angleBracket >= 0)
        {
            baseSchemaName = simpleName.Substring(0, angleBracket);
            var argsStr = simpleName.Substring(angleBracket + 1, simpleName.Length - angleBracket - 2);
            typeArgs = argsStr.Split(',').Select(a => a.Trim()).ToArray();
        }
        else
        {
            baseSchemaName = simpleName;
            typeArgs = Array.Empty<string>();
        }

        if (!SchemaRegistry.TryGetSchema(baseSchemaName, out var schemaRegistration))
            return null;

        if (schemaRegistration?.Node is not BinarySchemaNode binaryNode)
            return null;


        var typeParamMap = new Dictionary<string, string>();
        if (binaryNode.TypeParameters != null)
            for (var i = 0; i < binaryNode.TypeParameters.Length && i < typeArgs.Length; i++)
                typeParamMap[binaryNode.TypeParameters[i]] = typeArgs[i];


        var propertyName = remainingProperties[0].PropertyName;
        var allFields = GetAllBinarySchemaFields(binaryNode);
        var field = allFields.OfType<FieldDefinitionNode>().FirstOrDefault(f => f.Name == propertyName);
        if (field == null) return null;

        return ResolveTypeAnnotationWithSubstitution(field.TypeAnnotation, typeParamMap);
    }

    private (Type ClrType, string? IntendedTypeName) ResolveTypeAnnotationWithSubstitution(
        TypeAnnotationNode typeAnnotation, Dictionary<string, string> typeParamMap)
    {
        if (typeAnnotation is SchemaReferenceTypeNode schemaRef)
        {
            var resolvedName = typeParamMap.TryGetValue(schemaRef.SchemaName, out var substitutedName)
                ? substitutedName
                : schemaRef.SchemaName;

            if (SchemaRegistry != null && SchemaRegistry.TryGetSchema(resolvedName, out var refSchema))
                return (typeof(object), refSchema?.GeneratedTypeName);

            return (typeof(object), null);
        }

        if (typeAnnotation is ArrayTypeNode arrayType)
        {
            var (elemType, elemIntended) = ResolveTypeAnnotationWithSubstitution(arrayType.ElementType, typeParamMap);
            return (elemType.MakeArrayType(), elemIntended != null ? $"{elemIntended}[]" : null);
        }


        return ResolveTypeAnnotationClrTypeWithIntendedName(typeAnnotation);
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


        if (nestedType == typeof(object) && !string.IsNullOrEmpty(intendedTypeName) && SchemaRegistry != null)
        {
            var elementIntendedTypeName = intendedTypeName.EndsWith("[]")
                ? intendedTypeName.Substring(0, intendedTypeName.Length - 2)
                : intendedTypeName;
            var schemaName = elementIntendedTypeName.Split('.').Last();

            if (SchemaRegistry.TryGetSchema(schemaName, out var schemaRegistration))
            {
                if (schemaRegistration?.Node is BinarySchemaNode binaryNode)
                {
                    var columns = new List<ISchemaColumn>();
                    var allFields = GetAllBinarySchemaFields(binaryNode);
                    var columnIndex = 0;

                    foreach (var field in allFields)
                    {
                        if (field.Name.StartsWith('_'))
                            continue;

                        if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode })
                            continue;

                        if (field is FieldDefinitionNode fieldDef)
                        {
                            var (columnType, childIntendedTypeName) =
                                ResolveTypeAnnotationClrTypeWithIntendedName(fieldDef.TypeAnnotation);

                            if (fieldDef.IsConditional && columnType.IsValueType &&
                                Nullable.GetUnderlyingType(columnType) == null)
                                columnType = typeof(Nullable<>).MakeGenericType(columnType);

                            columns.Add(new SchemaColumn(field.Name, columnIndex++, columnType, childIntendedTypeName));
                        }
                        else if (field is ComputedFieldNode computedField)
                        {
                            var columnType = InferComputedFieldType(computedField.Expression, columns);

                            if (ReferencesConditionalField(computedField.Expression, allFields) &&
                                columnType.IsValueType && Nullable.GetUnderlyingType(columnType) == null)
                                columnType = typeof(Nullable<>).MakeGenericType(columnType);

                            columns.Add(new SchemaColumn(field.Name, columnIndex++, columnType));
                        }
                    }

                    return new DynamicTable(columns.ToArray());
                }

                if (schemaRegistration?.Node is TextSchemaNode textNode)
                {
                    var columns = new List<ISchemaColumn>();
                    var columnIndex = 0;

                    foreach (var field in textNode.Fields)
                    {
                        if (field.Name.StartsWith('_')) continue;
                        columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(string)));
                    }

                    return new DynamicTable(columns.ToArray());
                }
            }
        }


        var _columns = new List<ISchemaColumn>();
        foreach (var property in nestedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            _columns.Add(new SchemaColumn(property.Name, _columns.Count, property.PropertyType));

        return new DynamicTable(_columns.ToArray(), nestedType);
    }

    private bool ValidateBindablePropertyAsTableWithDiagnostics(ISchemaTable table, ISchemaColumn targetColumn,
        Node? node)
    {
        var propertyInfo = table.Metadata.TableEntityType.GetProperty(targetColumn.ColumnName);
        var bindablePropertyAsTableAttribute = propertyInfo?.GetCustomAttribute<BindablePropertyAsTableAttribute>();

        if (bindablePropertyAsTableAttribute == null) return false;

        var isValid = IsGenericEnumerable(propertyInfo!.PropertyType, out var elementType) ||
                      IsArray(propertyInfo.PropertyType!, out elementType) ||
                      (elementType != null && (elementType.IsPrimitive || elementType == typeof(string)));

        if (!isValid)
        {
            if (TryReportColumnNotBindable(targetColumn.ColumnName, node))
                return true;
            throw new ColumnMustBeMarkedAsBindablePropertyAsTableException();
        }

        return false;
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
                var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, type.GetProperties(), span);
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
                var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, rootType.GetProperties(), span);
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

        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (left is not QueryNode leftQuery)
            throw new InvalidOperationException($"Expected left side of {setOperatorName} to be a query node.");

        if (!ValidateSetOperatorKeys(leftQuery, node.Keys, node))
        {
            Nodes.Push(left);
            Nodes.Push(right);
            Nodes.Push(CreateSetOperatorNode(setOperatorName, node, left, right));
            return;
        }

        var key = CreateSetOperatorPositionKey();
        _currentScope[MetaAttributes.SetOperatorName] = key;
        SetOperatorFieldPositions.Add(key,
            BuildMetadataAndInferTypesVisitorUtilities.CreateSetOperatorPositionIndexes(leftQuery, node.Keys));

        if (right is QueryNode rightAsQueryNode)
            MakeSureBothSideFieldsAreOfAssignableTypes(leftQuery, rightAsQueryNode, key);
        else
            MakeSureBothSideFieldsAreOfAssignableTypes(leftQuery, PreviousSetOperatorPositionKey(), key);

        var rightMethodName = Methods.Pop();
        var leftMethodName = Methods.Pop();

        var methodName = $"{leftMethodName}_{setOperatorName}_{rightMethodName}";
        Methods.Push(methodName);
        _currentScope.ScopeSymbolTable.AddSymbol(methodName,
            _currentScope.Child[0].ScopeSymbolTable.GetSymbol(leftQuery.From.Alias));

        Nodes.Push(CreateSetOperatorNode(setOperatorName, node, left, right));
    }

    private bool ValidateSetOperatorKeys(QueryNode query, IReadOnlyCollection<string> keys, Node node)
    {
        var availableFieldNames = query.Select.Fields
            .SelectMany(field => new[] { field.FieldName, field.Expression.ToString() })
            .Where(fieldName => !string.IsNullOrWhiteSpace(fieldName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingKey = keys.FirstOrDefault(key =>
            !BuildMetadataAndInferTypesVisitorUtilities.TryGetSetOperatorFieldPosition(query, key, out _));
        if (missingKey == null)
            return true;

        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportUnknownColumn(missingKey, availableFieldNames, node);
            return false;
        }

        throw new InvalidOperationException($"Unknown column '{missingKey}'.");
    }

    private void ValidateBooleanOperand(Node operand, string operatorName, Node errorContextNode)
    {
        var operandType = NormalizeOperandType(operand.ReturnType);
        if (CanSkipStaticTypeValidation(operandType) || operandType == typeof(bool))
            return;

        ThrowOrReportInvalidOperandTypes(typeof(bool), operandType, errorContextNode,
            $"Operator {operatorName} requires boolean operands, but got '{operandType.Name}'.");
    }

    private void ValidatePatternOperand(Node operand, string operatorName, Node errorContextNode)
    {
        var operandType = NormalizeOperandType(operand.ReturnType);
        if (CanSkipStaticTypeValidation(operandType) || operandType == typeof(string))
            return;

        var message =
            $"Operator {operatorName} requires string operands, but got '{operandType.Name}'.";

        if (TryReportTypeMismatch(message, errorContextNode))
            return;

        throw new TypeMismatchException(typeof(string), operandType,
            errorContextNode.HasSpan ? errorContextNode.Span : TextSpan.Empty);
    }

    private void ValidateBinaryOperatorOperands(Node left, Node right, BinaryOperatorKind operatorKind, Node errorContextNode)
    {
        var leftType = NormalizeOperandType(left.ReturnType);
        var rightType = NormalizeOperandType(right.ReturnType);

        if (CanSkipStaticTypeValidation(leftType) || CanSkipStaticTypeValidation(rightType))
            return;

        var isValid = operatorKind switch
        {
            BinaryOperatorKind.Add => CanApplyAddition(leftType, rightType),
            BinaryOperatorKind.Subtract => CanApplySubtraction(leftType, rightType),
            BinaryOperatorKind.Multiply => CanApplyNumericOperator(leftType, rightType),
            BinaryOperatorKind.Divide => CanApplyNumericOperator(leftType, rightType),
            BinaryOperatorKind.Modulo => CanApplyNumericOperator(leftType, rightType),
            BinaryOperatorKind.BitwiseAnd => CanApplyBitwiseOperator(leftType, rightType),
            BinaryOperatorKind.BitwiseOr => CanApplyBitwiseOperator(leftType, rightType),
            BinaryOperatorKind.BitwiseXor => CanApplyBitwiseOperator(leftType, rightType),
            BinaryOperatorKind.LeftShift => CanApplyShiftOperator(leftType, rightType),
            BinaryOperatorKind.RightShift => CanApplyShiftOperator(leftType, rightType),
            BinaryOperatorKind.Equality => CanApplyEqualityOperator(leftType, rightType),
            BinaryOperatorKind.Inequality => CanApplyEqualityOperator(leftType, rightType),
            BinaryOperatorKind.Relational => CanApplyRelationalOperator(leftType, rightType),
            _ => true
        };

        if (isValid)
            return;

        if (operatorKind is BinaryOperatorKind.Equality or BinaryOperatorKind.Inequality or BinaryOperatorKind.Relational)
        {
            var message =
                $"Type mismatch: cannot compare '{leftType.Name}' with '{rightType.Name}'.";
            if (TryReportTypeMismatch(message, errorContextNode))
                return;

            throw new TypeMismatchException(leftType, rightType,
                errorContextNode.HasSpan ? errorContextNode.Span : TextSpan.Empty);
        }

        ThrowOrReportInvalidOperandTypes(leftType, rightType, errorContextNode);
    }

    private bool TryReportTypeMismatch(string message, Node node)
    {
        if (DiagnosticContext == null)
            return false;

        DiagnosticContext.ReportError(DiagnosticCode.MQ3005_TypeMismatch, message, node);
        return true;
    }

    private void ThrowOrReportInvalidOperandTypes(Type leftType, Type rightType, Node errorContextNode,
        string? message = null)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3007_InvalidOperandTypes,
                message ?? $"Invalid operand types for operator: '{leftType.Name}' and '{rightType.Name}'.",
                errorContextNode);
            return;
        }

        throw new InvalidOperandTypesException(leftType, rightType);
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

    private readonly record struct AggregateResolutionSignature(Type SchemaType, MethodInfo Method, MethodInfo SetMethod)
    {
        public bool Equals(AggregateResolutionSignature other)
        {
            return SchemaType == other.SchemaType && AreSameMethod(Method, other.Method) &&
                   AreSameMethod(SetMethod, other.SetMethod);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SchemaType, Method.Module, Method.MetadataToken, SetMethod.Module,
                SetMethod.MetadataToken);
        }

        private static bool AreSameMethod(MethodInfo left, MethodInfo right)
        {
            return left.Module.Equals(right.Module) && left.MetadataToken == right.MetadataToken;
        }
    }

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


        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
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
                    columnName,
                    $"Did you mean to use [{string.Join(", ", candidates)}]?",
                    span);
        }

        throw new UnknownColumnOrAliasException(columnName, string.Empty, span);
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
            DiagnosticContext.ReportUnknownProperty(propertyName, availableProperties, node);
            return true;
        }

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new UnknownPropertyException(
            propertyName,
            objectType?.Name ?? "unknown",
            span);
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

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new TypeNotFoundException(typeName, string.Empty, span);
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

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new AmbiguousColumnException(columnName, alias1, alias2, span);
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

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new ObjectIsNotAnArrayException(message, span);
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

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new ObjectDoesNotImplementIndexerException(message, span);
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

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new AliasAlreadyUsedException(alias, span);
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
                AliasMissingException.CreateMethodCallMessage(methodNode.ToString()),
                methodNode);
            return true;
        }

        var span = methodNode.HasSpan ? methodNode.Span : TextSpan.Empty;
        throw new AliasMissingException(AliasMissingException.CreateMethodCallMessage(methodNode.ToString()), span);
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

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new TableIsNotDefinedException(tableName, span);
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

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new FieldLinkIndexOutOfRangeException(index, maxCount, span);
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

        var span = node?.HasSpan == true ? node.Span : TextSpan.Empty;
        throw new ColumnMustBeMarkedAsBindablePropertyAsTableException(columnName, span);
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
                ? $"Unknown property '{identifier}'. Did you mean to use [{string.Join(", ", candidatesProperties.Select(p => p.Name))}]?"
                : $"Unknown property '{identifier}'.";

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
                SetOperatorMustHaveKeyColumnsException.CreateMessage(setOperator),
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
