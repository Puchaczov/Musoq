using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.Optimization;

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
        return CreateMetadata(
            context,
            runnableTypeName,
            executableArtifactKind,
            generatedCodeSha256,
            CancellationToken.None);
    }

    public static IReadOnlyDictionary<string, string> CreateMetadata(
        TargetArtifactPackagingContext context,
        string runnableTypeName,
        string executableArtifactKind,
        string generatedCodeSha256,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MetadataArtifactKind] = ArtifactKindRuntimeV2Query,
            [MetadataAssemblyName] = context.PackageName,
            [MetadataRuntimeV2ContractSignature] = RuntimeV2Contract.ContractSignature,
            [MetadataExecutionSemanticsVersion] = context.SemanticsContract.Version.ToString(CultureInfo.InvariantCulture),
            [MetadataExecutionTarget] = context.TargetId.ToString(),
            [MetadataExecutableArtifactKind] = executableArtifactKind,
            [MetadataScriptSha256] = ComputeHash(context.Script),
            [MetadataSemanticShapeSha256] = ComputeSemanticShapeHash(
                context.SemanticFacts,
                runnableTypeName,
                cancellationToken),
            [MetadataGeneratedCodeSha256] = generatedCodeSha256
        };

        cancellationToken.ThrowIfCancellationRequested();
        return metadata;
    }

    public static CompiledQueryArtifact CreateCompiledArtifactFromPackage(
        TargetArtifactPackage package,
        string engineVersion,
        string artifactFormatVersion,
        string compilationOptionsSignature)
    {
        return CreateCompiledArtifactFromPackage(
            package,
            engineVersion,
            artifactFormatVersion,
            compilationOptionsSignature,
            CancellationToken.None);
    }

    public static CompiledQueryArtifact CreateCompiledArtifactFromPackage(
        TargetArtifactPackage package,
        string engineVersion,
        string artifactFormatVersion,
        string compilationOptionsSignature,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);
        cancellationToken.ThrowIfCancellationRequested();

        if (package.TargetId != ExecutionTargetIds.CSharpClr ||
            !string.Equals(package.ArtifactKind, ArtifactKindRuntimeV2Query, StringComparison.Ordinal) ||
            !string.Equals(package.ExecutableArtifactKind, ExecutableArtifactKindClrAssembly, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Public compiled query artifacts currently support only '{ExecutionTargetIds.CSharpClr}' reusable CLR assembly packages. Package target is '{package.TargetId}' and executable kind is '{package.ExecutableArtifactKind}'.");
        }

        var assemblyBytes = RequireBlobContent(package, CSharpClrAssemblyBlobName, cancellationToken);
        var symbolsBytes = TryGetBlobContent(package, CSharpClrSymbolsBlobName, cancellationToken);
        var runnableTypeName = GetRunnableTypeName(package, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

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
        return ComputeSemanticShapeHash(facts, runnableTypeName, CancellationToken.None);
    }

    public static string ComputeSemanticShapeHash(
        TargetArtifactSemanticFacts facts,
        string runnableTypeName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(facts);
        cancellationToken.ThrowIfCancellationRequested();

        var builder = new StringBuilder();
        builder.Append("RuntimeV2ContractSignature=").Append(RuntimeV2Contract.ContractSignature).AppendLine();
        builder.Append("RunnableTypeName=").Append(runnableTypeName).AppendLine();
        builder.Append("QueryResultMode=").Append(facts.QueryResultMode).AppendLine();
        AppendTypeName(builder, "OutputType", facts.PortableOutputTypeName);
        AppendScriptParameters(builder, facts.PortableScriptParameters, cancellationToken);
        AppendScriptVariables(builder, facts.PortableScriptVariables, cancellationToken);
        AppendColumns(builder, "UsedColumns", facts.PortableUsedColumns, cancellationToken);
        AppendAliasColumns(builder, "PipelineInferredColumns", facts.PortablePipelineInferredColumns, cancellationToken);
        AppendSourceIdentities(builder, facts.PortableSourcePlanSignatures, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        return ComputeHash(builder.ToString());
    }

    public static string ComputeCompilationOptionsSignature(CompilationOptions options)
    {
        return CompilationOptionsFingerprint.Compute(options);
    }

    public static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string GetRunnableTypeName(TargetArtifactPackage package)
    {
        return GetRunnableTypeName(package, CancellationToken.None);
    }

    private static string GetRunnableTypeName(
        TargetArtifactPackage package,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TargetRuntimeEntrypoint? entrypoint = null;
        foreach (var candidate in package.Entrypoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidate.Kind == TargetRuntimeEntrypointKind.TableQuery)
            {
                entrypoint = candidate;
                break;
            }
        }

        if (entrypoint is null || string.IsNullOrWhiteSpace(entrypoint.SymbolName))
            throw new InvalidOperationException(
                $"C# CLR compiled artifact package is missing a '{TargetRuntimeEntrypointKind.TableQuery}' runnable entrypoint.");

        return entrypoint.SymbolName;
    }

    private static byte[] RequireBlobContent(
        TargetArtifactPackage package,
        string blobName)
    {
        return RequireBlobContent(package, blobName, CancellationToken.None);
    }

    private static byte[] RequireBlobContent(
        TargetArtifactPackage package,
        string blobName,
        CancellationToken cancellationToken)
    {
        return TryGetBlobContent(package, blobName, cancellationToken) is { Length: > 0 } content
            ? content
            : throw new InvalidOperationException(
                $"C# CLR compiled artifact package is missing required binary blob '{blobName}'.");
    }

    private static byte[]? TryGetBlobContent(
        TargetArtifactPackage package,
        string blobName)
    {
        return TryGetBlobContent(package, blobName, CancellationToken.None);
    }

    private static byte[]? TryGetBlobContent(
        TargetArtifactPackage package,
        string blobName,
        CancellationToken cancellationToken)
    {
        foreach (var blob in package.BinaryBlobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(blob.Name, blobName, StringComparison.Ordinal))
                return blob.Content;
        }

        return null;
    }

    private static void AppendScriptParameters(
        StringBuilder builder,
        IReadOnlyList<TargetArtifactScriptParameterFact> parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        builder.Append("ScriptParameters=").Append(parameters.Count).AppendLine();
        foreach (var parameter in parameters.OrderBy(static parameter => parameter.Name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        IReadOnlyList<TargetArtifactScriptVariableFact> variables,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        builder.Append("ScriptVariables=").Append(variables.Count).AppendLine();
        foreach (var variable in variables.OrderBy(static variable => variable.Name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        IReadOnlyList<TargetArtifactSourceColumnsFact> columnsBySource,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        builder.Append(label).Append('=').Append(columnsBySource.Count).AppendLine();
        foreach (var entry in columnsBySource
                     .OrderBy(static entry => entry.Source.Id, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Schema, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Method, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Alias, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendSourceFact(builder, entry.Source, cancellationToken);
            AppendColumnList(builder, entry.Columns, cancellationToken);
        }
    }

    private static void AppendAliasColumns(
        StringBuilder builder,
        string label,
        IReadOnlyList<TargetArtifactAliasColumnsFact>? columnsByAlias,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (columnsByAlias == null)
        {
            builder.Append(label).Append("=<null>").AppendLine();
            return;
        }

        builder.Append(label).Append('=').Append(columnsByAlias.Count).AppendLine();
        foreach (var entry in columnsByAlias.OrderBy(static entry => entry.Alias, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Append("Alias=").Append(entry.Alias).AppendLine();
            AppendColumnList(builder, entry.Columns, cancellationToken);
        }
    }

    private static void AppendSourceIdentities(
        StringBuilder builder,
        IReadOnlyList<TargetArtifactSourcePlanFact> requestsBySource,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        builder.Append("SourceIdentities=").Append(requestsBySource.Count).AppendLine();
        foreach (var entry in requestsBySource
                     .OrderBy(static entry => entry.Source.Id, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Schema, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Method, StringComparer.Ordinal)
                     .ThenBy(static entry => entry.Source.Alias, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendSourceFact(builder, entry.Source, cancellationToken);
            builder
                .Append("Identity:")
                .Append(entry.IdentitySchemaName).Append('|')
                .Append(entry.IdentityMethodName).Append('|')
                .Append(entry.IdentitySourceContextId).Append('|')
                .Append(entry.IdentityAlias).AppendLine();
            AppendSourceColumnRefs(builder, "RequiredColumns", entry.RequiredColumns, cancellationToken);
            AppendOrderBy(builder, entry.OrderBy, cancellationToken);
            builder.Append("Skip=").Append(entry.Skip).Append('|');
            builder.Append("Take=").Append(entry.Take).Append('|');
            builder.Append("PredicateType=").Append(entry.PredicateTypeName);
            if (entry.RequestedComputedProjectionFingerprints.Count > 0)
            {
                builder.Append("|Computed=")
                    .Append(string.Join(",", entry.RequestedComputedProjectionFingerprints));
            }

            if (entry.Replayability != RowStreamReplayability.Unknown)
                builder.Append("|Replayability=").Append(entry.Replayability);

            builder.AppendLine();
        }
    }

    private static void AppendSourceFact(
        StringBuilder builder,
        TargetArtifactSourceFact source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
        IReadOnlyList<TargetArtifactColumnFact> columns,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        builder.Append("Columns=").Append(columns.Count).AppendLine();
        foreach (var column in columns
                     .OrderBy(static column => column.ColumnIndex)
                     .ThenBy(static column => column.ColumnName, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder
                .Append("Column:")
                .Append(column.ColumnIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(column.ColumnName).Append('|')
                .Append(column.IntendedTypeName ?? "<null>").Append('|');
            AppendTypeName(builder, "ColumnType", column.ColumnTypeName);
            AppendTypeName(builder, "SourceReadType", column.SourceReadTypeName);
            builder.Append("EnumTypeFingerprint=").Append(column.EnumTypeFingerprint).Append('|');
            builder.Append("Stability=").Append(column.Stability).Append('|');
            AppendReadModifiers(builder, column.ReadModifiers, cancellationToken);
            builder.AppendLine();
        }
    }

    private static void AppendSourceColumnRefs(
        StringBuilder builder,
        string label,
        IReadOnlyList<TargetArtifactSourceColumnRefFact> columns,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        builder.Append(label).Append('=').Append(columns.Count).AppendLine();
        foreach (var column in columns.OrderBy(static column => column.Name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Append("SourceColumn:").Append(column.Name).Append('|');
            AppendReadModifiers(builder, column.ReadModifiers, cancellationToken);
            builder.AppendLine();
        }
    }

    private static void AppendOrderBy(
        StringBuilder builder,
        IReadOnlyList<TargetArtifactOrderByFact> orderBy,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        builder.Append("OrderBy=").Append(orderBy.Count).AppendLine();
        foreach (var order in orderBy.OrderBy(static order => order.Column.Name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Append("OrderColumn:").Append(order.Column.Name).Append('|').Append(order.Direction).Append('|');
            AppendReadModifiers(builder, order.Column.ReadModifiers, cancellationToken);
            builder.AppendLine();
        }
    }

    private static void AppendReadModifiers(
        StringBuilder builder,
        IReadOnlyDictionary<string, string> readModifiers,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        builder.Append("ReadModifiers=").Append(readModifiers.Count).Append('[');
        foreach (var modifier in readModifiers.OrderBy(static modifier => modifier.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
