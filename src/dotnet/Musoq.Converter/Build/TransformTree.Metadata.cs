using System.Collections.Generic;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static string SanitizeNameForNamespace(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Query.Compiled";

        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '.' && chars[i] != '_')
                chars[i] = '_';
        }

        if (char.IsDigit(chars[0]))
            return $"_{new string(chars)}";

        return new string(chars);
    }

    private static SemanticBuildArtifacts BuildSemanticArtifacts(
        RootNode transformedQueryTree,
        BuildMetadataAndInferTypesVisitor metadata,
        BuildMetadataAndInferTypesTraverseVisitor metadataTraverser,
        CteExecutionPlan? cteExecutionPlan)
    {
        return new SemanticBuildArtifacts
        {
            TransformedQueryTree = transformedQueryTree,
            UsedColumns = metadata.UsedColumns,
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
            PipelineScope = metadataTraverser.Scope,
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
        BuildMetadataAndInferTypesVisitor metadata)
    {
        var aliasKeyedColumns = new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal);

        foreach (var inferredColumn in metadata.InferredColumns)
        {
            var alias = inferredColumn.Key.Alias;

            if (string.IsNullOrWhiteSpace(alias))
                continue;

            // Alias names can repeat across independent scopes (for example CTE branches).
            // Keep the latest inferred schema for that alias instead of throwing.
            aliasKeyedColumns[alias] = inferredColumn.Value;
        }

        foreach (var aliasColumnsPair in metadata.InferredColumnsByAlias)
            aliasKeyedColumns[aliasColumnsPair.Key] = aliasColumnsPair.Value;

        return aliasKeyedColumns;
    }

    private static Dictionary<string, IReadOnlySet<string>> CreateAliasKeyedUsedColumns(
        BuildMetadataAndInferTypesVisitor metadata)
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
