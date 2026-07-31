using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Targets.Abstractions;
using Musoq.Targets.Execution;
using EvaluatorCompilationOptions = Musoq.Evaluator.CompilationOptions;

namespace Musoq.Converter;

/// <summary>
/// Full identity of a reusable generated execution artifact. The generated
/// syntax is represented by an immutable token descriptor. Namespace/type
/// identity is normalized, ordinary trivia is ignored, and structured trivia
/// remains part of the identity; all semantic and runtime contract fields
/// remain exact.
/// </summary>
internal sealed record CanonicalExecutionArtifactContract(
    CSharpGeneratedSyntaxIdentity GeneratedSyntaxIdentity,
    string SemanticContractFingerprint,
    string RuntimeContractFingerprint,
    string ExecutionSemanticsFingerprint,
    string ExecutionTarget,
    string RenderProfile,
    int RenderProfileVersion,
    string ResultMode,
    string OutputType,
    string CompilationOptionsFingerprint,
    string OrderedReferenceIdentities,
    string ProviderContractFingerprint,
    string InterpreterState)
{
    public string NormalizedGeneratedSyntax => GeneratedSyntaxIdentity.Descriptor;
}

public static partial class InstanceCreator
{
    private static CanonicalExecutionArtifactContract CreateCanonicalExecutionArtifactContract(
        BuildItems items,
        ISchemaProvider schemaProvider,
        EvaluatorCompilationOptions compilationOptions)
    {
        var generatedSyntaxIdentity = CSharpClrGeneratedCodeCompatibility.CreateStructuralIdentity(
            items.RenderingArtifacts.Artifact);
        var references = string.Join(
            "\n",
            items.AdditionalReferenceTypes
                .Select(static type => type.AssemblyQualifiedName ?? type.FullName ?? type.Name)
                .OrderBy(static name => name, StringComparer.Ordinal));
        var runtimeContract = items.RenderingArtifacts.RuntimeContract?.ToString() ?? string.Empty;
        var outputType = items.OutputType?.AssemblyQualifiedName ?? string.Empty;
        var renderProfile = TargetRenderPurposeFactory.CreateProfile(items.CompilationPurpose, items.EmitPdb);
        var canonicalSemanticContractFingerprint = CompiledQueryArtifactSupport.ComputeHash(
            string.Join(
                "\n",
                generatedSyntaxIdentity.Hash,
                ExecutionSemanticsContract.Version1.Fingerprint,
                items.ExecutionTarget.ToString(),
                renderProfile.ToString(),
                TargetRenderProfileContract.Version.ToString(),
                items.QueryResultMode.ToString(),
                outputType,
                CompilationOptionsFingerprint.Compute(compilationOptions),
                references,
                CreateCanonicalProviderContractSignature(schemaProvider),
                runtimeContract,
                items.InterpreterSourceCode ?? string.Empty));

        return new CanonicalExecutionArtifactContract(
            generatedSyntaxIdentity,
            canonicalSemanticContractFingerprint,
            runtimeContract,
            ExecutionSemanticsContract.Version1.Fingerprint,
            items.ExecutionTarget.ToString(),
            renderProfile.ToString(),
            TargetRenderProfileContract.Version,
            items.QueryResultMode.ToString(),
            outputType,
            CompilationOptionsFingerprint.Compute(compilationOptions),
            references,
            CreateCanonicalProviderContractSignature(schemaProvider),
            items.InterpreterSourceCode ?? string.Empty);
    }

    private static string CreateCanonicalProviderContractSignature(ISchemaProvider schemaProvider)
    {
        var builder = new StringBuilder();
        builder.Append(schemaProvider.GetType().AssemblyQualifiedName ?? schemaProvider.GetType().FullName);
        foreach (var field in GetInstanceFields(schemaProvider.GetType())
                     .OrderBy(static field => field.DeclaringType?.AssemblyQualifiedName, StringComparer.Ordinal)
                     .ThenBy(static field => field.Name, StringComparer.Ordinal))
        {
            builder.Append('|')
                .Append(field.DeclaringType?.AssemblyQualifiedName)
                .Append('.')
                .Append(field.Name)
                .Append(':');
            AppendCanonicalProviderValue(builder, field.GetValue(schemaProvider), 0);
        }

        return builder.ToString();
    }

    private static void AppendCanonicalProviderValue(StringBuilder builder, object? value, int depth)
    {
        if (value is null)
        {
            builder.Append("<null>");
            return;
        }

        var type = value.GetType();
        builder.Append(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
        if (depth >= 2)
        {
            builder.Append("<opaque>");
            return;
        }

        switch (value)
        {
            case string:
                builder.Append("<runtime-string>");
                return;
            case Type representedType:
                builder.Append(':').Append(representedType.AssemblyQualifiedName ?? representedType.FullName);
                return;
            case Enum enumValue:
                builder.Append(':').Append(enumValue);
                return;
            case bool boolean:
                builder.Append(':').Append(boolean);
                return;
            case IDictionary dictionary:
                var entries = dictionary
                    .Cast<object>()
                    .Select(CreateCanonicalDictionaryEntry)
                    .OrderBy(static entry => entry.key, StringComparer.Ordinal)
                    .ThenBy(static entry => entry.valueType, StringComparer.Ordinal);
                foreach (var (key, valueType) in entries)
                {
                    builder.Append("[key=");
                    builder.Append(key);
                    builder.Append(",value-type=")
                        .Append(valueType)
                        .Append(']');
                }

                return;
            case IEnumerable:
                builder.Append("<runtime-sequence>");
                return;
        }

        foreach (var field in GetInstanceFields(type)
                     .OrderBy(static field => field.DeclaringType?.AssemblyQualifiedName, StringComparer.Ordinal)
                     .ThenBy(static field => field.Name, StringComparer.Ordinal))
        {
            builder.Append('|')
                .Append(field.DeclaringType?.AssemblyQualifiedName)
                .Append('.')
                .Append(field.Name)
                .Append(':');
            AppendCanonicalProviderValue(builder, field.GetValue(value), depth + 1);
        }
    }

    private static (string key, string valueType) CreateCanonicalDictionaryEntry(object entry)
    {
        object? key;
        object? value;
        if (entry is DictionaryEntry dictionaryEntry)
        {
            key = dictionaryEntry.Key;
            value = dictionaryEntry.Value;
        }
        else
        {
            var entryType = entry.GetType();
            key = entryType.GetProperty("Key")?.GetValue(entry);
            value = entryType.GetProperty("Value")?.GetValue(entry);
        }

        return (
            key is string stringKey
                ? stringKey
                : key?.GetType().AssemblyQualifiedName ?? "<null>",
            value?.GetType().AssemblyQualifiedName ?? "<null>");
    }

}
