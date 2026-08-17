using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.Utils;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Typed boundary between the mutable semantic visitor state and the immutable
/// artifact consumed by rewrite, planning, and analysis clients.
/// </summary>
internal sealed class SemanticMetadataSnapshotBuilder
{
    public SemanticMetadataSnapshot Build(SemanticMetadataSnapshotInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new SemanticMetadataSnapshot
        {
            Root = input.Root,
            InferredColumns = MetadataSnapshotContractsFreezer.FreezeSchemaColumns(input.InferredColumns),
            InferredColumnsByAlias = MetadataSnapshotContractsFreezer.FreezeSchemaColumns(input.InferredColumnsByAlias),
            UsedColumns = MetadataSnapshotContractsFreezer.FreezeSchemaColumns(input.UsedColumns),
            UsedWhereNodes = SemanticMetadataSnapshotFreezer.FreezeDictionary(input.UsedWhereNodes),
            SourcePlanRequestsPerSchema = SemanticMetadataSnapshotFreezer.FreezeDictionary(input.SourcePlanRequestsPerSchema),
            SourceContractDiagnosticLocationsPerSchema = SemanticMetadataSnapshotFreezer.FreezeDictionary(
                input.SourceContractDiagnosticLocationsPerSchema),
            SourceContracts = MetadataSnapshotContractsFreezer.FreezeSourceContracts(input.SourceContracts),
            ScriptParameterDefinitions = SemanticMetadataSnapshotFreezer.FreezeList(input.ScriptParameterDefinitions),
            ScriptVariableDefinitions = SemanticMetadataSnapshotFreezer.FreezeList(input.ScriptVariableDefinitions),
            SourceRuntimeSettingsBySourceContextId = SemanticMetadataSnapshotFreezer.FreezeRuntimeSettings(
                input.SourceRuntimeSettingsBySourceContextId),
            SourceRuntimeSettingDescriptionsBySourceContextId = SemanticMetadataSnapshotFreezer.FreezeRuntimeSettingDescriptions(
                input.SourceRuntimeSettingDescriptionsBySourceContextId),
            HasDeclaredSourceRuntimeSettings = input.HasDeclaredSourceRuntimeSettings,
            HasSourceRuntimeSettingValues = input.HasSourceRuntimeSettingValues,
            Assemblies = SemanticMetadataSnapshotFreezer.FreezeList(input.Assemblies),
            SetOperatorFieldPositions = SemanticMetadataSnapshotFreezer.FreezeArrays(input.SetOperatorFieldPositions),
            SetOperatorFieldTypes = SemanticMetadataSnapshotFreezer.FreezeArrays(input.SetOperatorFieldTypes),
            ResultShape = SemanticMetadataSnapshotFreezer.BuildResultShape(input.ResultShape)
        };
    }
}

internal sealed record SemanticMetadataSnapshotInput
{
    public required RootNode Root { get; init; }

    public required IEnumerable<KeyValuePair<SchemaFromNode, IEnumerable<ISchemaColumn>>> InferredColumns { get; init; }

    public required IEnumerable<KeyValuePair<string, IEnumerable<ISchemaColumn>>> InferredColumnsByAlias { get; init; }

    public required IEnumerable<KeyValuePair<SchemaFromNode, IEnumerable<ISchemaColumn>>> UsedColumns { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, WhereNode> UsedWhereNodes { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsPerSchema { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, SourceContractDiagnosticLocationMap>
        SourceContractDiagnosticLocationsPerSchema { get; init; }

    public required IEnumerable<BoundSourceContract> SourceContracts { get; init; }

    public required IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions { get; init; }

    public required IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>
        SourceRuntimeSettingsBySourceContextId { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>
        SourceRuntimeSettingDescriptionsBySourceContextId { get; init; }

    public required bool HasDeclaredSourceRuntimeSettings { get; init; }

    public required bool HasSourceRuntimeSettingValues { get; init; }

    public required IReadOnlyList<Assembly> Assemblies { get; init; }

    public required IEnumerable<KeyValuePair<string, int[]>> SetOperatorFieldPositions { get; init; }

    public required IEnumerable<KeyValuePair<string, Type[]>> SetOperatorFieldTypes { get; init; }

    public required SemanticResultShapeSnapshotInput ResultShape { get; init; }
}

internal sealed record SemanticResultShapeSnapshotInput
{
    public required IReadOnlyList<string> GeneratedAliases { get; init; }

    public required IReadOnlyDictionary<string, List<FieldNode>> GeneratedColumns { get; init; }

    public required IReadOnlyDictionary<string, Node> SelectFieldAliases { get; init; }

    public IdentifierNode? TheMostInnerIdentifier { get; init; }
}
