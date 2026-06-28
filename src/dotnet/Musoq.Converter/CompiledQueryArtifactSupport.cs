using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator;

namespace Musoq.Converter;

internal static class CompiledQueryArtifactSupport
{
    public const string MetadataArtifactKind = "ArtifactKind";
    public const string MetadataAssemblyName = "AssemblyName";
    public const string MetadataScriptSha256 = "ScriptSha256";
    public const string MetadataGeneratedCodeSha256 = "GeneratedCodeSha256";
    public const string ArtifactKindRuntimeV2Query = "RuntimeV2CompiledQuery";

    public static string CurrentEngineVersion { get; } = string.Join(
        ";",
        GetAssemblySignature(typeof(InstanceCreator)),
        GetAssemblySignature(typeof(CompiledQuery)),
        GetAssemblySignature(typeof(Parser.Parser)));

    public static IReadOnlyDictionary<string, string> CreateMetadata(
        string assemblyName,
        string script,
        CSharpCompilation compilation)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MetadataArtifactKind] = ArtifactKindRuntimeV2Query,
            [MetadataAssemblyName] = assemblyName,
            [MetadataScriptSha256] = ComputeHash(script),
            [MetadataGeneratedCodeSha256] = ComputeGeneratedCodeHash(compilation)
        };
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

    private static string GetAssemblySignature(Type type)
    {
        var assemblyName = type.Assembly.GetName();
        var name = assemblyName.Name ?? type.Assembly.FullName ?? type.FullName ?? type.Name;
        var version = assemblyName.Version?.ToString() ?? "0.0.0.0";
        return $"{name}/{version}";
    }
}
