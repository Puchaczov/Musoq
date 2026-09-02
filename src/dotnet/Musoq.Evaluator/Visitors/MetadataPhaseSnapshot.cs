using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.Utils;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Immutable handoff produced by semantic analysis. The visitor may continue to
/// be used while a build is assembled, but consumers observe copied collections.
/// </summary>
internal sealed record SemanticMetadataSnapshot
{
    public required RootNode Root { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, IReadOnlyList<ISchemaColumn>> InferredColumns { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<ISchemaColumn>> InferredColumnsByAlias { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, IReadOnlyList<ISchemaColumn>> UsedColumns { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, WhereNode> UsedWhereNodes { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsPerSchema { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, SourceContractDiagnosticLocationMap>
        SourceContractDiagnosticLocationsPerSchema { get; init; }

    public required IReadOnlyList<BoundSourceContract> SourceContracts { get; init; }

    public required IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions { get; init; }

    public required IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>
        SourceRuntimeSettingsBySourceContextId { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>
        SourceRuntimeSettingDescriptionsBySourceContextId { get; init; }

    public required bool HasDeclaredSourceRuntimeSettings { get; init; }

    public required bool HasSourceRuntimeSettingValues { get; init; }

    public required IReadOnlyList<Assembly> Assemblies { get; init; }

    public required IReadOnlyDictionary<string, EnumTypeDescriptor> QueryLocalEnumTypes { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<int>> SetOperatorFieldPositions { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<Type>> SetOperatorFieldTypes { get; init; }

    public required SemanticResultShapeSnapshot ResultShape { get; init; }

}

internal sealed record SemanticResultShapeSnapshot
{
    public required IReadOnlyList<string> GeneratedAliases { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<FieldNode>> GeneratedColumns { get; init; }

    public required IReadOnlyDictionary<string, Node> SelectFieldAliases { get; init; }

    public IdentifierNode? TheMostInnerIdentifier { get; init; }

}
