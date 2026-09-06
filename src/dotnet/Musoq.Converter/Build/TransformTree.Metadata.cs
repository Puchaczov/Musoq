using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static SemanticBuildArtifacts BuildSemanticArtifacts(
        RootNode parsedQueryTree,
        RootNode normalizedQueryTree,
        RootNode metadataQueryTree,
        RootNode rewrittenQueryTree,
        SemanticMetadataSnapshot metadata,
        SemanticScopeArtifact scopeArtifact,
        CteExecutionPlan? cteExecutionPlan,
        IEnumerable<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var phase = new SemanticPhaseArtifacts
        {
            ParsedQuery = parsedQueryTree,
            NormalizedQuery = normalizedQueryTree,
            MetadataQuery = metadataQueryTree,
            RewrittenQuery = rewrittenQueryTree,
            Metadata = metadata,
            Scope = scopeArtifact,
            Diagnostics = diagnostics.ToArray()
        };

        var usedColumns = new Dictionary<SchemaFromNode, ISchemaColumn[]>();
        foreach (var pair in metadata.UsedColumns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            usedColumns[pair.Key] = pair.Value.ToArray();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var pipelineInferredColumns = CreateAliasKeyedInferredColumns(metadata, cancellationToken);
        var pipelineUsedColumns = CreateAliasKeyedUsedColumns(metadata, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return new SemanticBuildArtifacts
        {
            Phase = phase,
            TransformedQueryTree = rewrittenQueryTree,
            UsedColumns = usedColumns,
            UsedWhereNodes = metadata.UsedWhereNodes,
            SourcePlanRequestsPerSchema = metadata.SourcePlanRequestsPerSchema,
            SourceContractDiagnosticLocationsPerSchema = metadata.SourceContractDiagnosticLocationsPerSchema,
            ScriptParameterDefinitions = metadata.ScriptParameterDefinitions,
            ScriptVariableDefinitions = metadata.ScriptVariableDefinitions,
            SourceRuntimeSettingsBySourceContextId = metadata.SourceRuntimeSettingsBySourceContextId,
            SourceRuntimeSettingDescriptionsBySourceContextId =
                metadata.SourceRuntimeSettingDescriptionsBySourceContextId,
            HasDeclaredSourceRuntimeSettings = metadata.HasDeclaredSourceRuntimeSettings,
            HasSourceRuntimeSettingValues = metadata.HasSourceRuntimeSettingValues,
            ScopeArtifact = scopeArtifact,
            PipelineInferredColumns = pipelineInferredColumns,
            PipelineUsedColumns = pipelineUsedColumns,
            CteExecutionPlan = cteExecutionPlan
        };
    }

    private static Dictionary<string, ISchemaColumn[]> CreateAliasKeyedInferredColumns(
        SemanticMetadataSnapshot metadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var aliasKeyedColumns = new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal);

        foreach (var inferredColumn in metadata.InferredColumns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var alias = inferredColumn.Key.Alias;

            if (string.IsNullOrWhiteSpace(alias))
                continue;

            // Alias names can repeat across independent scopes (for example CTE branches).
            // Keep the latest inferred schema for that alias instead of throwing.
            aliasKeyedColumns[alias] = inferredColumn.Value.ToArray();
        }

        foreach (var aliasColumnsPair in metadata.InferredColumnsByAlias)
        {
            cancellationToken.ThrowIfCancellationRequested();
            aliasKeyedColumns[aliasColumnsPair.Key] = aliasColumnsPair.Value.ToArray();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return aliasKeyedColumns;
    }

    private static Dictionary<string, IReadOnlySet<string>> CreateAliasKeyedUsedColumns(
        SemanticMetadataSnapshot metadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var aliasKeyedUsed = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

        foreach (var usedEntry in metadata.UsedColumns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var alias = usedEntry.Key.Alias;

            if (string.IsNullOrWhiteSpace(alias))
                continue;

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in usedEntry.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();
                names.Add(column.ColumnName);
            }

            aliasKeyedUsed[alias] = names;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return aliasKeyedUsed;
    }
}
