using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Schema;
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
        EvaluatorCompilationOptions compilationOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var generatedSyntaxIdentity = CSharpClrGeneratedCodeCompatibility.CreateStructuralIdentity(
            items.RenderingArtifacts.Artifact,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var referencesByName = new List<string>(items.AdditionalReferenceTypes.Count);
        foreach (var type in items.AdditionalReferenceTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            referencesByName.Add(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
        }

        referencesByName.Sort((left, right) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return StringComparer.Ordinal.Compare(left, right);
        });
        cancellationToken.ThrowIfCancellationRequested();
        var references = string.Join("\n", referencesByName);
        cancellationToken.ThrowIfCancellationRequested();
        var runtimeContract = items.RenderingArtifacts.RuntimeContract?.ToString() ?? string.Empty;
        var outputType = items.OutputType?.AssemblyQualifiedName ?? string.Empty;
        var renderProfile = TargetRenderPurposeFactory.CreateProfile(items.CompilationPurpose, items.EmitPdb);
        var providerContract = CreateCanonicalProviderContractSignature(schemaProvider, cancellationToken);
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
                providerContract,
                runtimeContract,
                items.InterpreterSourceCode ?? string.Empty));
        cancellationToken.ThrowIfCancellationRequested();

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
            providerContract,
            items.InterpreterSourceCode ?? string.Empty);
    }

    private static string CreateCanonicalProviderContractSignature(
        ISchemaProvider schemaProvider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new StringBuilder();
        builder.Append(schemaProvider.GetType().AssemblyQualifiedName ?? schemaProvider.GetType().FullName);
        foreach (var field in GetOrderedInstanceFields(
                     schemaProvider.GetType(),
                     cancellationToken,
                     useAssemblyQualifiedDeclaringType: true))
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Append('|')
                .Append(field.DeclaringType?.AssemblyQualifiedName)
                .Append('.')
                .Append(field.Name)
                .Append(':');
            AppendCanonicalProviderValue(builder, field.GetValue(schemaProvider), 0, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return builder.ToString();
    }

    private static void AppendCanonicalProviderValue(
        StringBuilder builder,
        object? value,
        int depth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
                var entries = new List<(string key, string valueType)>();
                foreach (var entry in (IEnumerable)dictionary)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entries.Add(CreateCanonicalDictionaryEntry(entry));
                }

                entries.Sort((left, right) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var keyComparison = StringComparer.Ordinal.Compare(left.key, right.key);
                    return keyComparison != 0
                        ? keyComparison
                        : StringComparer.Ordinal.Compare(left.valueType, right.valueType);
                });
                foreach (var (key, valueType) in entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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

        foreach (var field in GetOrderedInstanceFields(
                     type,
                     cancellationToken,
                     useAssemblyQualifiedDeclaringType: true))
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Append('|')
                .Append(field.DeclaringType?.AssemblyQualifiedName)
                .Append('.')
                .Append(field.Name)
                .Append(':');
            AppendCanonicalProviderValue(builder, field.GetValue(value), depth + 1, cancellationToken);
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
