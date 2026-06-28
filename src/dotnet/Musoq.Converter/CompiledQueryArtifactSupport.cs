using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter;

internal static class CompiledQueryArtifactSupport
{
    public const string MetadataArtifactKind = "ArtifactKind";
    public const string MetadataAssemblyName = "AssemblyName";
    public const string MetadataScriptSha256 = "ScriptSha256";
    public const string MetadataSemanticShapeSha256 = "SemanticShapeSha256";
    public const string MetadataGeneratedCodeSha256 = "GeneratedCodeSha256";
    public const string ArtifactKindRuntimeV2Query = "RuntimeV2CompiledQuery";

    public static string CurrentEngineVersion { get; } = string.Join(
        ";",
        GetAssemblySignature(typeof(InstanceCreator)),
        GetAssemblySignature(typeof(CompiledQuery)),
        GetAssemblySignature(typeof(Parser.Parser)),
        GetAssemblySignature(typeof(ISchemaProvider)));

    public static IReadOnlyDictionary<string, string> CreateMetadata(
        string assemblyName,
        string script,
        BuildItems items,
        CSharpCompilation compilation)
    {
        var runnableTypeName = GetRunnableTypeName(assemblyName);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MetadataArtifactKind] = ArtifactKindRuntimeV2Query,
            [MetadataAssemblyName] = assemblyName,
            [MetadataScriptSha256] = ComputeHash(script),
            [MetadataSemanticShapeSha256] = ComputeSemanticShapeHash(items, runnableTypeName),
            [MetadataGeneratedCodeSha256] = ComputeGeneratedCodeHash(compilation)
        };
    }

    public static string GetRunnableTypeName(string assemblyName)
    {
        return $"{SanitizeNameForNamespace(assemblyName)}.CompiledQuery";
    }

    public static string ComputeSemanticShapeHash(BuildItems items, string runnableTypeName)
    {
        ArgumentNullException.ThrowIfNull(items);

        var builder = new StringBuilder();
        builder.Append("RunnableTypeName=").Append(runnableTypeName).AppendLine();
        builder.Append("QueryResultMode=").Append(items.QueryResultMode).AppendLine();
        AppendType(builder, "OutputType", items.OutputType);
        AppendScriptParameters(builder, items.ScriptParameterDefinitions);
        AppendScriptVariables(builder, items.ScriptVariableDefinitions);
        AppendColumns(builder, "UsedColumns", items.UsedColumns);
        AppendAliasColumns(builder, "PipelineInferredColumns", items.PipelineInferredColumns);
        AppendSourceIdentities(builder, items.SourcePlanRequestsPerSchema);

        return ComputeHash(builder.ToString());
    }

    public static string ComputeCompilationOptionsSignature(CompilationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new StringBuilder();
        AppendOption(builder, nameof(options.ParallelizationMode), options.ParallelizationMode);
        AppendOption(builder, nameof(options.UseHashJoin), options.UseHashJoin);
        AppendOption(builder, nameof(options.UseSortMergeJoin), options.UseSortMergeJoin);
        AppendOption(builder, nameof(options.UseCommonSubexpressionElimination), options.UseCommonSubexpressionElimination);
        AppendOption(builder, nameof(options.UseConstantFolding), options.UseConstantFolding);
        AppendOption(builder, nameof(options.UsePrimitiveTypeValidation), options.UsePrimitiveTypeValidation);
        AppendOption(builder, nameof(options.UseCteParallelization), options.UseCteParallelization);
        AppendOption(builder, nameof(options.UseCteSidecarIndexes), options.UseCteSidecarIndexes);
        AppendOption(builder, nameof(options.InstrumentationMode), options.InstrumentationMode);
        AppendOption(builder, nameof(options.MaxDegreeOfParallelismOverride), options.MaxDegreeOfParallelismOverride);
        AppendOption(builder, nameof(options.ForceTableResultMaterialization), options.ForceTableResultMaterialization);
        return ComputeHash(builder.ToString());
    }

    public static string ComputeGeneratedCodeHash(CSharpCompilation compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        var builder = new StringBuilder();
        var index = 0;
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var text = syntaxTree.GetText().ToString();
            builder
                .Append(CultureInfo.InvariantCulture, $"tree:{index}:")
                .Append(text.Length)
                .AppendLine()
                .Append(text)
                .AppendLine();
            index++;
        }

        return ComputeHash(builder.ToString());
    }

    public static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void AppendOption<T>(StringBuilder builder, string name, T value)
    {
        builder
            .Append(name)
            .Append('=')
            .Append(value?.ToString() ?? "<null>")
            .Append(';');
    }

    private static void AppendScriptParameters(StringBuilder builder, IReadOnlyList<ScriptParameterDefinition> parameters)
    {
        builder.Append("ScriptParameters=").Append(parameters.Count).AppendLine();
        foreach (var parameter in parameters.OrderBy(static parameter => parameter.Name, StringComparer.Ordinal))
        {
            builder.Append("Parameter:");
            builder.Append(parameter.Name).Append('|');
            AppendType(builder, "Type", parameter.ParameterType);
            builder.Append("HasDefault=").Append(parameter.HasDefaultValue).Append('|');
            builder.Append("DefaultType=").Append(parameter.DefaultValue?.GetType().AssemblyQualifiedName ?? "<null>");
            builder.AppendLine();
        }
    }

    private static void AppendScriptVariables(StringBuilder builder, IReadOnlyList<ScriptVariableDefinition> variables)
    {
        builder.Append("ScriptVariables=").Append(variables.Count).AppendLine();
        foreach (var variable in variables.OrderBy(static variable => variable.Name, StringComparer.Ordinal))
        {
            builder.Append("Variable:");
            builder.Append(variable.Name).Append('|');
            AppendType(builder, "Type", variable.VariableType);
            builder.Append("CanUseConst=").Append(variable.CanUseConstKeyword).Append('|');
            builder.Append("ValueType=").Append(variable.Value?.GetType().AssemblyQualifiedName ?? "<null>");
            builder.AppendLine();
        }
    }

    private static void AppendColumns(
        StringBuilder builder,
        string label,
        IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> columnsBySource)
    {
        builder.Append(label).Append('=').Append(columnsBySource.Count).AppendLine();
        foreach (var entry in columnsBySource
                     .OrderBy(static entry => entry.Key.Id, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Key.Schema, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Key.Method, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Key.Alias, StringComparer.Ordinal))
        {
            AppendSourceNode(builder, entry.Key);
            AppendColumnList(builder, entry.Value);
        }
    }

    private static void AppendAliasColumns(
        StringBuilder builder,
        string label,
        IReadOnlyDictionary<string, ISchemaColumn[]>? columnsByAlias)
    {
        if (columnsByAlias == null)
        {
            builder.Append(label).Append("=<null>").AppendLine();
            return;
        }

        builder.Append(label).Append('=').Append(columnsByAlias.Count).AppendLine();
        foreach (var entry in columnsByAlias.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            builder.Append("Alias=").Append(entry.Key).AppendLine();
            AppendColumnList(builder, entry.Value);
        }
    }

    private static void AppendSourceIdentities(
        StringBuilder builder,
        IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest> requestsBySource)
    {
        builder.Append("SourceIdentities=").Append(requestsBySource.Count).AppendLine();
        foreach (var entry in requestsBySource
                     .OrderBy(static entry => entry.Key.Id, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Key.Schema, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Key.Method, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Key.Alias, StringComparer.Ordinal))
        {
            AppendSourceNode(builder, entry.Key);
            var identity = entry.Value.Identity;
            builder
                .Append("Identity:")
                .Append(identity.SchemaName).Append('|')
                .Append(identity.MethodName).Append('|')
                .Append(identity.SourceContextId).Append('|')
                .Append(identity.Alias).AppendLine();
            AppendSourceColumnRefs(builder, "RequiredColumns", entry.Value.RequiredColumns);
            AppendOrderBy(builder, entry.Value.OrderBy);
            builder.Append("Skip=").Append(entry.Value.Skip?.ToString(CultureInfo.InvariantCulture) ?? "<null>").Append('|');
            builder.Append("Take=").Append(entry.Value.Take?.ToString(CultureInfo.InvariantCulture) ?? "<null>").Append('|');
            builder.Append("PredicateType=").Append(entry.Value.Predicate?.GetType().AssemblyQualifiedName ?? "<null>").AppendLine();
        }
    }

    private static void AppendSourceNode(StringBuilder builder, SchemaFromNode source)
    {
        builder
            .Append("Source:")
            .Append(source.Id).Append('|')
            .Append(source.Schema).Append('|')
            .Append(source.Method).Append('|')
            .Append(source.Alias).Append('|')
            .Append(source.QueryId.ToString(CultureInfo.InvariantCulture))
            .AppendLine();
    }

    private static void AppendColumnList(StringBuilder builder, IReadOnlyList<ISchemaColumn> columns)
    {
        builder.Append("Columns=").Append(columns.Count).AppendLine();
        foreach (var column in columns
                     .OrderBy(static column => column.ColumnIndex)
                     .ThenBy(static column => column.ColumnName, StringComparer.Ordinal))
        {
            builder
                .Append("Column:")
                .Append(column.ColumnIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(column.ColumnName).Append('|')
                .Append(column.IntendedTypeName ?? "<null>").Append('|');
            AppendType(builder, "ColumnType", column.ColumnType);
            AppendReadModifiers(builder, column.ReadModifiers);
            builder.AppendLine();
        }
    }

    private static void AppendSourceColumnRefs(
        StringBuilder builder,
        string label,
        IReadOnlyList<SourceColumnRef> columns)
    {
        builder.Append(label).Append('=').Append(columns.Count).AppendLine();
        foreach (var column in columns.OrderBy(static column => column.Name, StringComparer.Ordinal))
        {
            builder.Append("SourceColumn:").Append(column.Name).Append('|');
            AppendReadModifiers(builder, column.ReadModifiers);
            builder.AppendLine();
        }
    }

    private static void AppendOrderBy(StringBuilder builder, IReadOnlyList<OrderByExpression> orderBy)
    {
        builder.Append("OrderBy=").Append(orderBy.Count).AppendLine();
        foreach (var order in orderBy.OrderBy(static order => order.Column.Name, StringComparer.Ordinal))
        {
            builder.Append("OrderColumn:").Append(order.Column.Name).Append('|').Append(order.Direction).Append('|');
            AppendReadModifiers(builder, order.Column.ReadModifiers);
            builder.AppendLine();
        }
    }

    private static void AppendReadModifiers(StringBuilder builder, IReadOnlyDictionary<string, string> readModifiers)
    {
        builder.Append("ReadModifiers=").Append(readModifiers.Count).Append('[');
        foreach (var modifier in readModifiers.OrderBy(static modifier => modifier.Key, StringComparer.Ordinal))
        {
            builder
                .Append(modifier.Key)
                .Append('=')
                .Append(modifier.Value)
                .Append(';');
        }

        builder.Append(']');
    }

    private static void AppendType(StringBuilder builder, string label, Type? type)
    {
        builder
            .Append(label)
            .Append('=')
            .Append(type?.AssemblyQualifiedName ?? "<null>")
            .Append('|');
    }

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

    private static string GetAssemblySignature(Type type)
    {
        var assembly = type.Assembly;
        var assemblyName = assembly.GetName();
        var name = assemblyName.Name ?? assembly.FullName ?? type.FullName ?? type.Name;
        var version = assemblyName.Version?.ToString() ?? "0.0.0.0";
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "<none>";
        var mvid = assembly.ManifestModule.ModuleVersionId.ToString("D");
        return $"{name}/{version}/{informationalVersion}/{mvid}";
    }
}
