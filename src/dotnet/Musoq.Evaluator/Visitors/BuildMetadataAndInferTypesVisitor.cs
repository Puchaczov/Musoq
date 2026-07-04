using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.RuntimeSettings;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor : DefensiveVisitorBase, IAwareExpressionVisitor
{
    private enum ComputedFieldColumnTypeMode
    {
        PreferExpressionReturnType,
        InferFromColumns
    }

    private static readonly WhereNode AllTrueWhereNode =
        new(new EqualityNode(new IntegerNode("1", "s"), new IntegerNode("1", "s")));

    private readonly IReadOnlyDictionary<string, string[]> _columns;
    private readonly CompilationOptions _compilationOptions;

    private readonly ILogger<BuildMetadataAndInferTypesVisitor> _logger;
    private readonly TypeConversionNodeFactory _nodeFactory;
    private readonly ISchemaProvider _provider;
    private readonly SourceRuntimeSettingsResolutionService _sourceRuntimeSettingsResolutionService;

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
        var methodResolver1 = methodResolver ?? new LibraryMethodResolver();
        _nodeFactory = new TypeConversionNodeFactory(methodResolver1);
        _compilationOptions = compilationOptions ?? new CompilationOptions();
        SchemaRegistry = schemaRegistry;
        DiagnosticContext = diagnosticContext;
        _diagnosticReporter = new SemanticDiagnosticReporter(DiagnosticContext);
        _columnPropertyBindingService = new SemanticColumnPropertyBindingService(_sourceBinding, _resultShape);
        _expressionBindingService = new SemanticExpressionBindingService(_diagnosticReporter);
        _sourceBindingService = new SemanticSourceBindingService(_sourceBinding);
        _methodBindingService = new SemanticMethodBindingService(AddAssembly);
        _resultShapeBindingService = new SemanticResultShapeBindingService(_resultShape);
        _queryValidationService = new SemanticQueryValidationService(_diagnosticReporter, _compilationOptions);
        _sourceRuntimeSettingsResolutionService =
            new SourceRuntimeSettingsResolutionService(_compilationOptions, DiagnosticContext);
        _scriptParameters = new ScriptParameterMetadataBinder(TryReportScriptParameterError, AddAssembly);
        _scriptVariables = new ScriptVariableMetadataBinder(TryReportScriptParameterError, AddAssembly);
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

    private Stack<string> Methods => _methodResolution.Methods;

    protected Stack<Node> Nodes => _queryState.Nodes;

    public virtual IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId =>
        InternalSourceRuntimeSettingsBySourceContextId;

    public virtual IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId =>
        InternalSourceRuntimeSettingDescriptionsBySourceContextId;

    public List<Assembly> Assemblies => _methodResolution.Assemblies;

    public IDictionary<string, int[]> SetOperatorFieldPositions => _queryState.SetOperatorFieldPositions;

    public IDictionary<string, Type[]> SetOperatorFieldTypes => _queryState.SetOperatorFieldTypes;

    public SchemaRegistry? SchemaRegistry { get; }

    public IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> InferredColumns
    {
        get
        {
            var result = new Dictionary<SchemaFromNode, ISchemaColumn[]>();

            foreach (var aliasColumnsPair in _sourceBinding.InferredColumns)
                result.Add(aliasColumnsPair.Key, aliasColumnsPair.Value.ToArray());

            return result;
        }
    }

    public IReadOnlyDictionary<string, ISchemaColumn[]> InferredColumnsByAlias
    {
        get
        {
            var result = new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal);

            foreach (var pair in _sourceBinding.InferredColumnsByAlias)
                result.Add(pair.Key, pair.Value.ToArray());

            return result;
        }
    }

    public IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> UsedColumns
    {
        get
        {
            var result = new Dictionary<SchemaFromNode, ISchemaColumn[]>();

            foreach (var aliasColumnsPair in _sourceBinding.UsedColumns)
                result.Add(aliasColumnsPair.Key, aliasColumnsPair.Value.ToArray());

            return result;
        }
    }

    public IReadOnlyDictionary<SchemaFromNode, WhereNode> UsedWhereNodes
    {
        get
        {
            return _sourceBinding.UsedWhereNodes.ToDictionary(aliasColumnsPair => aliasColumnsPair.Key,
                aliasColumnsPair => aliasColumnsPair.Value);
        }
    }

    public IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsPerSchema
    {
        get
        {
            return _sourceBinding.SourcePlanRequestsPerSchema.ToDictionary(
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
        ArgumentNullException.ThrowIfNull(node);
        if (node.Type == DescForType.Query)
        {
            Nodes.Push(new DescNode(SafePop(Nodes, VisitorOperationNames.VisitDescNode)));
            return;
        }

        var fromNode = SafeCast<FromNode>(SafePop(Nodes, VisitorOperationNames.VisitDescNode),
            VisitorOperationNames.VisitDescNode);
        Nodes.Push(new DescNode(fromNode, node.Type, node.Column));
    }




    protected virtual IReadOnlyDictionary<string, string> RetrieveInitialSourceRuntimeSettings(string sourceContextId,
        SchemaFromNode node)
    {
        var sourceRuntimeSettings = new Dictionary<string, string>();

        InternalSourceRuntimeSettingsBySourceContextId.TryAdd(sourceContextId, sourceRuntimeSettings);

        return sourceRuntimeSettings;
    }

    private void AddAssembly(Assembly asm)
    {
        if (Assemblies.Contains(asm))
            return;

        Assemblies.Add(asm);
    }

}
