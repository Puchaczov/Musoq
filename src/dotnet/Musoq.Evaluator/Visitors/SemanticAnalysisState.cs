using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticAnalysisState
{
    private readonly Stack<string> _methods = new();
    private readonly Stack<Node> _nodes = new();

    public SemanticAnalysisState()
    {
        Traversal = new SemanticTraversalFrame(_nodes, _methods);
    }

    public DiagnosticState Diagnostics { get; } = new();

    public MethodResolutionState MethodResolution { get; } = new();

    public SemanticQueryState Query { get; } = new();

    public ResultShapeState ResultShape { get; } = new();

    public SourceBindingState SourceBinding { get; } = new();

    public SemanticTraversalFrame Traversal { get; }
}

internal sealed partial record SourceBindingState
{
    public Dictionary<string, string> AliasMapToInMemoryTableMap { get; } = new();

    public IDictionary<string, SchemaFromNode> AliasToSchemaFromNodeMap { get; } =
        new Dictionary<string, SchemaFromNode>();

    public Dictionary<string, ISchemaTable> ExplicitlyDefinedTables { get; } = new();

    public Dictionary<string, CoupledSourceDefinition> ExplicitlyCoupledSources { get; } = new();

    public Dictionary<SchemaFromNode, ISchemaColumn[]> InferredColumns { get; } = new();

    public Dictionary<string, ISchemaColumn[]> InferredColumnsByAlias { get; } =
        new(StringComparer.Ordinal);

    public Dictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsPerSchema { get; } = new();

    public Dictionary<string, (int SchemaFromKey, string SourceContextId)> SchemaFromInfo { get; } = new();

    public Dictionary<SchemaFromNode, List<ISchemaColumn>> UsedColumns { get; } = new();

    public Dictionary<SchemaFromNode, WhereNode> UsedWhereNodes { get; } = new();

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

internal sealed record MethodResolutionState
{
    public List<AccessMethodNode> RefreshMethods { get; } = [];

    public List<Assembly> Assemblies { get; } = new(8);
}

internal sealed record DiagnosticState
{
    public List<Type> NullSuspiciousTypes { get; } = [];

    public bool HasSeenNonParameterStatement { get; set; }
}

internal sealed record SemanticQueryState
{
    public Dictionary<string, FieldNode[]> CachedSetFields { get; } = new();

    public IDictionary<string, int[]> SetOperatorFieldPositions { get; } = new Dictionary<string, int[]>();

    public IDictionary<string, Type[]> SetOperatorFieldTypes { get; } = new Dictionary<string, Type[]>();

    public QueryPart QueryPart { get; set; }

    public int SetKey { get; set; }

    public bool InsideWindowFunction { get; set; }
}

internal sealed record ResultShapeState
{
    public List<string> GeneratedAliases { get; } = [];

    public Dictionary<string, List<FieldNode>> GeneratedColumns { get; } = [];

    public Dictionary<string, Node> SelectFieldAliases { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IdentifierNode? TheMostInnerIdentifier { get; set; }
}
