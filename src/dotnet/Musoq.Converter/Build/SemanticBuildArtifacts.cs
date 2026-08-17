using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

/// <summary>
/// Typed view of the semantic stage output: the normalized query tree and the
/// metadata derived from binding methods and inferring types.
/// </summary>
internal sealed record SemanticBuildArtifacts
{
    public required SemanticPhaseArtifacts Phase { get; init; }

    public required RootNode TransformedQueryTree { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> UsedColumns { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, WhereNode> UsedWhereNodes { get; init; }

    public required IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsPerSchema { get; init; }

    public IReadOnlyDictionary<SchemaFromNode, SourceContractDiagnosticLocationMap> SourceContractDiagnosticLocationsPerSchema { get; init; } =
        new Dictionary<SchemaFromNode, SourceContractDiagnosticLocationMap>();

    public required IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions { get; init; }

    public required IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; init; }

    public required bool HasDeclaredSourceRuntimeSettings { get; init; }

    public required bool HasSourceRuntimeSettingValues { get; init; }

    public required SemanticScopeArtifact ScopeArtifact { get; init; }

    public IReadOnlyDictionary<string, ISchemaColumn[]>? PipelineInferredColumns { get; init; }

    public IReadOnlyDictionary<string, IReadOnlySet<string>>? PipelineUsedColumns { get; init; }

    public CteExecutionPlan? CteExecutionPlan { get; init; }
}
