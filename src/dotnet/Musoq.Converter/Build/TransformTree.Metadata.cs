using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

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
        IReadOnlyList<Diagnostic> diagnostics)
    {
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

        return new SemanticBuildArtifacts
        {
            Phase = phase,
            TransformedQueryTree = rewrittenQueryTree,
            UsedColumns = metadata.UsedColumns.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToArray()),
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
            PipelineInferredColumns = CreateAliasKeyedInferredColumns(metadata),
            PipelineUsedColumns = CreateAliasKeyedUsedColumns(metadata),
            CteExecutionPlan = cteExecutionPlan
        };
    }

    private BuildMetadataAndInferTypesVisitor CreateMetadataVisitor(
        TransformPipelineContext context,
        IReadOnlyDictionary<string, string[]> columns)
    {
        return context.CreateBuildMetadataAndInferTypesVisitor?.Invoke(
            context.SchemaProvider, columns, context.CompilationOptions, context.SchemaRegistry, loggerResolver.ResolveLogger<BuildMetadataAndInferTypesVisitor>()) ??
               new BuildMetadataAndInferTypesVisitor(
                   context.SchemaProvider,
                   columns,
                   loggerResolver.ResolveLogger<BuildMetadataAndInferTypesVisitor>(),
                   context.DiagnosticContext,
                   context.CompilationOptions,
                   context.SchemaRegistry);
    }

    private static Dictionary<string, ISchemaColumn[]> CreateAliasKeyedInferredColumns(
        SemanticMetadataSnapshot metadata)
    {
        var aliasKeyedColumns = new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal);

        foreach (var inferredColumn in metadata.InferredColumns)
        {
            var alias = inferredColumn.Key.Alias;

            if (string.IsNullOrWhiteSpace(alias))
                continue;

            // Alias names can repeat across independent scopes (for example CTE branches).
            // Keep the latest inferred schema for that alias instead of throwing.
            aliasKeyedColumns[alias] = inferredColumn.Value.ToArray();
        }

        foreach (var aliasColumnsPair in metadata.InferredColumnsByAlias)
            aliasKeyedColumns[aliasColumnsPair.Key] = aliasColumnsPair.Value.ToArray();

        return aliasKeyedColumns;
    }

    private static Dictionary<string, IReadOnlySet<string>> CreateAliasKeyedUsedColumns(
        SemanticMetadataSnapshot metadata)
    {
        var aliasKeyedUsed = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

        foreach (var usedEntry in metadata.UsedColumns)
        {
            var alias = usedEntry.Key.Alias;

            if (string.IsNullOrWhiteSpace(alias))
                continue;

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in usedEntry.Value)
                names.Add(column.ColumnName);

            aliasKeyedUsed[alias] = names;
        }

        return aliasKeyedUsed;
    }
}
