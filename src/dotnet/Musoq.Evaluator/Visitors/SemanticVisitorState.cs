using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly DiagnosticState _diagnostics = new();
    private readonly MethodResolutionState _methodResolution = new();
    private readonly SemanticQueryState _queryState = new();
    private readonly ResultShapeState _resultShape = new();
    private readonly SourceBindingState _sourceBinding = new();

    protected Dictionary<string, IReadOnlyDictionary<string, string>> InternalSourceRuntimeSettingsBySourceContextId =>
        _sourceBinding.InternalSourceRuntimeSettingsBySourceContextId;

    protected Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> InternalSourceRuntimeSettingDescriptionsBySourceContextId =>
        _sourceBinding.InternalSourceRuntimeSettingDescriptionsBySourceContextId;

    internal bool InsideWindowFunction
    {
        get => _queryState.InsideWindowFunction;
        set => _queryState.InsideWindowFunction = value;
    }

    private sealed partial record SourceBindingState
    {
        public Dictionary<string, string> AliasMapToInMemoryTableMap { get; } = new();

        public IDictionary<string, SchemaFromNode> AliasToSchemaFromNodeMap { get; } =
            new Dictionary<string, SchemaFromNode>();

        public Dictionary<string, ISchemaTable> ExplicitlyDefinedTables { get; } =
            new();

        public Dictionary<string, CoupledSourceDefinition> ExplicitlyCoupledSources { get; } =
            new();

        public Dictionary<SchemaFromNode, ISchemaColumn[]> InferredColumns { get; } =
            new();

        public Dictionary<string, ISchemaColumn[]> InferredColumnsByAlias { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsPerSchema { get; } =
            new();

        public Dictionary<string, (int SchemaFromKey, string SourceContextId)> SchemaFromInfo { get; } =
            new();

        public Dictionary<SchemaFromNode, List<ISchemaColumn>> UsedColumns { get; } =
            new();

        public Dictionary<SchemaFromNode, WhereNode> UsedWhereNodes { get; } =
            new();

        public HashSet<string> AllUsedSchemaNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, IReadOnlyDictionary<string, string>> InternalSourceRuntimeSettingsBySourceContextId { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> InternalSourceRuntimeSettingDescriptionsBySourceContextId { get; } =
            new(StringComparer.Ordinal);

        public Scope CurrentScope { get; set; } = new(null, 0);

        public string Identifier { get; set; } = string.Empty;

        public string QueryAlias { get; set; } = string.Empty;

        public int SchemaFromKey { get; set; }

        public int UsedSchemasQuantity { get; set; }
    }

    private sealed record MethodResolutionState
    {
        public List<AccessMethodNode> RefreshMethods { get; } = [];

        public Stack<string> Methods { get; } = new();

        public List<Assembly> Assemblies { get; } = new(8);
    }

    private sealed record DiagnosticState
    {
        public List<Type> NullSuspiciousTypes { get; } = [];

        public bool HasSeenNonParameterStatement { get; set; }
    }

    private sealed record SemanticQueryState
    {
        public Dictionary<string, FieldNode[]> CachedSetFields { get; } = new();

        public Stack<Node> Nodes { get; } = new();

        public IDictionary<string, int[]> SetOperatorFieldPositions { get; } = new Dictionary<string, int[]>();

        public IDictionary<string, Type[]> SetOperatorFieldTypes { get; } = new Dictionary<string, Type[]>();

        public QueryPart QueryPart { get; set; }

        public int SetKey { get; set; }

        public bool InsideWindowFunction { get; set; }
    }

    private sealed record ResultShapeState
    {
        public List<string> GeneratedAliases { get; } = [];

        public Dictionary<string, List<FieldNode>> GeneratedColumns { get; } = [];

        public Dictionary<string, Node> SelectFieldAliases { get; } = new(StringComparer.OrdinalIgnoreCase);

        public IdentifierNode? TheMostInnerIdentifier { get; set; }
    }
}
