using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

internal static class CompiledQueryArtifactSupport
{
    public const string MetadataArtifactKind = "ArtifactKind";
    public const string MetadataAssemblyName = "AssemblyName";
    public const string MetadataScriptSha256 = "ScriptSha256";
    public const string MetadataSemanticShapeSha256 = "SemanticShapeSha256";
    public const string MetadataGeneratedCodeSha256 = "GeneratedCodeSha256";
    public const string MetadataRuntimeV2ContractSignature = "RuntimeV2ContractSignature";
    public const string MetadataExecutionSemanticsVersion = "ExecutionSemanticsVersion";
    public const string MetadataExecutionTarget = "ExecutionTarget";
    public const string MetadataExecutableArtifactKind = "ExecutableArtifactKind";
    public const string ArtifactKindRuntimeV2Query = "RuntimeV2CompiledQuery";
    public const string ExecutableArtifactKindClrAssembly = "ClrAssembly";
    public const string CSharpClrAssemblyBlobName = "query.dll";
    public const string CSharpClrSymbolsBlobName = "query.pdb";
    public const string CSharpClrAssemblyContentType = "application/vnd.musoq.csharp-clr-assembly";
    public const string CSharpClrSymbolsContentType = "application/vnd.musoq.csharp-clr-symbols";

    public static string CurrentEngineVersion { get; } = string.Join(
        ";",
        GetAssemblySignature(typeof(InstanceCreator)),
        GetAssemblySignature(typeof(CompiledQuery)),
        GetAssemblySignature(typeof(Parser.Parser)),
        GetAssemblySignature(typeof(ISchemaProvider)));

    public static IReadOnlyDictionary<string, string> CreateMetadata(
        TargetArtifactPackagingContext context,
        string runnableTypeName,
        string executableArtifactKind,
        string generatedCodeSha256)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MetadataArtifactKind] = ArtifactKindRuntimeV2Query,
            [MetadataAssemblyName] = context.PackageName,
            [MetadataRuntimeV2ContractSignature] = RuntimeV2Contract.ContractSignature,
            [MetadataExecutionSemanticsVersion] = context.SemanticsContract.Version.ToString(CultureInfo.InvariantCulture),
            [MetadataExecutionTarget] = context.TargetId.ToString(),
            [MetadataExecutableArtifactKind] = executableArtifactKind,
            [MetadataScriptSha256] = ComputeHash(context.Script),
            [MetadataSemanticShapeSha256] = ComputeSemanticShapeHash(context.SemanticFacts, runnableTypeName),
            [MetadataGeneratedCodeSha256] = generatedCodeSha256
        };
    }

    public static CompiledQueryArtifact CreateCompiledArtifactFromPackage(
        TargetArtifactPackage package,
        string engineVersion,
        string artifactFormatVersion,
        string compilationOptionsSignature)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (package.TargetId != ExecutionTargetIds.CSharpClr ||
            !string.Equals(package.ArtifactKind, ArtifactKindRuntimeV2Query, StringComparison.Ordinal) ||
            !string.Equals(package.ExecutableArtifactKind, ExecutableArtifactKindClrAssembly, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Public compiled query artifacts currently support only '{ExecutionTargetIds.CSharpClr}' reusable CLR assembly packages. Package target is '{package.TargetId}' and executable kind is '{package.ExecutableArtifactKind}'.");
        }

        var assemblyBytes = RequireBlobContent(package, CSharpClrAssemblyBlobName);
        var symbolsBytes = TryGetBlobContent(package, CSharpClrSymbolsBlobName);
        var runnableTypeName = GetRunnableTypeName(package);

        return new CompiledQueryArtifact(
            assemblyBytes,
            symbolsBytes,
            runnableTypeName,
            engineVersion,
            artifactFormatVersion,
            compilationOptionsSignature,
            package.Metadata);
    }

    public static string GetRunnableTypeName(string assemblyName)
    {
        return $"{SanitizeNameForNamespace(assemblyName)}.CompiledQuery";
    }

    public static string ComputeSemanticShapeHash(TargetArtifactSemanticFacts facts, string runnableTypeName)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var builder = new StringBuilder();
        builder.Append("RuntimeV2ContractSignature=").Append(RuntimeV2Contract.ContractSignature).AppendLine();
        builder.Append("RunnableTypeName=").Append(runnableTypeName).AppendLine();
        builder.Append("QueryResultMode=").Append(facts.QueryResultMode).AppendLine();
        AppendTypeName(builder, "OutputType", facts.PortableOutputTypeName);
        AppendScriptParameters(builder, facts.PortableScriptParameters);
        AppendScriptVariables(builder, facts.PortableScriptVariables);
        AppendColumns(builder, "UsedColumns", facts.PortableUsedColumns);
        AppendAliasColumns(builder, "PipelineInferredColumns", facts.PortablePipelineInferredColumns);
        AppendSourceIdentities(builder, facts.PortableSourcePlanSignatures);

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

    public static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string GetRunnableTypeName(TargetArtifactPackage package)
    {
        var entrypoint = package.Entrypoints.FirstOrDefault(static entrypoint =>
            entrypoint.Kind == TargetRuntimeEntrypointKind.TableQuery);
        if (entrypoint is null || string.IsNullOrWhiteSpace(entrypoint.SymbolName))
            throw new InvalidOperationException(
                $"C# CLR compiled artifact package is missing a '{TargetRuntimeEntrypointKind.TableQuery}' runnable entrypoint.");

        return entrypoint.SymbolName;
    }

    private static byte[] RequireBlobContent(
        TargetArtifactPackage package,
        string blobName)
    {
        return TryGetBlobContent(package, blobName) is { Length: > 0 } content
            ? content
            : throw new InvalidOperationException(
                $"C# CLR compiled artifact package is missing required binary blob '{blobName}'.");
    }

    private static byte[]? TryGetBlobContent(
        TargetArtifactPackage package,
        string blobName)
    {
        return package.BinaryBlobs
            .FirstOrDefault(blob => string.Equals(blob.Name, blobName, StringComparison.Ordinal))
            ?.Content;
    }

    private static void AppendOption<T>(StringBuilder builder, string name, T value)
    {
        builder
            .Append(name)
            .Append('=')
            .Append(value?.ToString() ?? "<null>")
            .Append(';');
    }

    private static void AppendScriptParameters(
        StringBuilder builder,
        IReadOnlyList<TargetArtifactScriptParameterFact> parameters)
    {
        builder.Append("ScriptParameters=").Append(parameters.Count).AppendLine();
        foreach (var parameter in parameters.OrderBy(static parameter => parameter.Name, StringComparer.Ordinal))
        {
            builder.Append("Parameter:");
            builder.Append(parameter.Name).Append('|');
            AppendTypeName(builder, "Type", parameter.TypeName);
            builder.Append("HasDefault=").Append(parameter.HasDefaultValue).Append('|');
            builder.Append("DefaultType=").Append(parameter.DefaultValueTypeName);
            builder.AppendLine();
        }
    }

    private static void AppendScriptVariables(
        StringBuilder builder,
        IReadOnlyList<TargetArtifactScriptVariableFact> variables)
    {
        builder.Append("ScriptVariables=").Append(variables.Count).AppendLine();
        foreach (var variable in variables.OrderBy(static variable => variable.Name, StringComparer.Ordinal))
        {
            builder.Append("Variable:");
            builder.Append(variable.Name).Append('|');
            AppendTypeName(builder, "Type", variable.TypeName);
            builder.Append("CanUseConst=").Append(variable.CanUseConstKeyword).Append('|');
            builder.Append("ValueType=").Append(variable.ValueTypeName);
            builder.AppendLine();
        }
    }

    private static void AppendColumns(
        StringBuilder builder,
        string label,
        IReadOnlyList<TargetArtifactSourceColumnsFact> columnsBySource)
    {
        builder.Append(label).Append('=').Append(columnsBySource.Count).AppendLine();
        foreach (var entry in columnsBySource
                     .OrderBy(static entry => entry.Source.Id, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Schema, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Method, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Alias, StringComparer.Ordinal))
        {
            AppendSourceFact(builder, entry.Source);
            AppendColumnList(builder, entry.Columns);
        }
    }

    private static void AppendAliasColumns(
        StringBuilder builder,
        string label,
        IReadOnlyList<TargetArtifactAliasColumnsFact>? columnsByAlias)
    {
        if (columnsByAlias == null)
        {
            builder.Append(label).Append("=<null>").AppendLine();
            return;
        }

        builder.Append(label).Append('=').Append(columnsByAlias.Count).AppendLine();
        foreach (var entry in columnsByAlias.OrderBy(static entry => entry.Alias, StringComparer.Ordinal))
        {
            builder.Append("Alias=").Append(entry.Alias).AppendLine();
            AppendColumnList(builder, entry.Columns);
        }
    }

    private static void AppendSourceIdentities(
        StringBuilder builder,
        IReadOnlyList<TargetArtifactSourcePlanFact> requestsBySource)
    {
        builder.Append("SourceIdentities=").Append(requestsBySource.Count).AppendLine();
        foreach (var entry in requestsBySource
                     .OrderBy(static entry => entry.Source.Id, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Schema, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Method, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Alias, StringComparer.Ordinal))
        {
            AppendSourceFact(builder, entry.Source);
            builder
                .Append("Identity:")
                .Append(entry.IdentitySchemaName).Append('|')
                .Append(entry.IdentityMethodName).Append('|')
                .Append(entry.IdentitySourceContextId).Append('|')
                .Append(entry.IdentityAlias).AppendLine();
            AppendSourceColumnRefs(builder, "RequiredColumns", entry.RequiredColumns);
            AppendOrderBy(builder, entry.OrderBy);
            builder.Append("Skip=").Append(entry.Skip).Append('|');
            builder.Append("Take=").Append(entry.Take).Append('|');
            builder.Append("PredicateType=").Append(entry.PredicateTypeName).AppendLine();
        }
    }

    private static void AppendSourceFact(StringBuilder builder, TargetArtifactSourceFact source)
    {
        builder
            .Append("Source:")
            .Append(source.Id).Append('|')
            .Append(source.Schema).Append('|')
            .Append(source.Method).Append('|')
            .Append(source.Alias).Append('|')
            .Append(source.QueryId)
            .AppendLine();
    }

    private static void AppendColumnList(
        StringBuilder builder,
        IReadOnlyList<TargetArtifactColumnFact> columns)
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
            AppendTypeName(builder, "ColumnType", column.ColumnTypeName);
            AppendReadModifiers(builder, column.ReadModifiers);
            builder.AppendLine();
        }
    }

    private static void AppendSourceColumnRefs(
        StringBuilder builder,
        string label,
        IReadOnlyList<TargetArtifactSourceColumnRefFact> columns)
    {
        builder.Append(label).Append('=').Append(columns.Count).AppendLine();
        foreach (var column in columns.OrderBy(static column => column.Name, StringComparer.Ordinal))
        {
            builder.Append("SourceColumn:").Append(column.Name).Append('|');
            AppendReadModifiers(builder, column.ReadModifiers);
            builder.AppendLine();
        }
    }

    private static void AppendOrderBy(StringBuilder builder, IReadOnlyList<TargetArtifactOrderByFact> orderBy)
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

    private static void AppendTypeName(StringBuilder builder, string label, string? typeName)
    {
        builder
            .Append(label)
            .Append('=')
            .Append(typeName ?? "<null>")
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
