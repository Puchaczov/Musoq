using System.Collections.Generic;
using System.Linq;

namespace Musoq.Converter.Build;

internal static class CSharpClrTargetPackageFactory
{
    public static TargetArtifactPackage CreateClrAssemblyPackage(
        string artifactKind,
        string executableArtifactKind,
        ExecutionSemanticsContract semanticsContract,
        IReadOnlyDictionary<string, string> metadata,
        IEnumerable<TargetExportBinaryBlob> binaryBlobs,
        IEnumerable<TargetRuntimeEntrypoint> entrypoints,
        TargetHostAbiInventory? hostAbiInventory,
        string assemblyBlobName,
        string generatedCodeSha256MetadataKey,
        IEnumerable<string>? requiredMetadataKeys = null,
        int executionIrVersion = TargetContractVersions.ExecutionIr)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(binaryBlobs);
        ArgumentNullException.ThrowIfNull(entrypoints);

        var frozenBlobs = binaryBlobs.ToArray();
        var frozenEntrypoints = entrypoints.ToArray();
        RequireMetadata(metadata, generatedCodeSha256MetadataKey);
        foreach (var key in requiredMetadataKeys ?? [])
            RequireMetadata(metadata, key);
        var expectedSemanticsVersion = semanticsContract.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!metadata.TryGetValue(CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion, out var semanticsVersion) ||
            !string.Equals(semanticsVersion, expectedSemanticsVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"CSharpClr package semantics metadata must be '{expectedSemanticsVersion}', but was '{semanticsVersion ?? "<missing>"}'.");
        }

        if (!frozenBlobs.Any(blob =>
                string.Equals(blob.Name, assemblyBlobName, StringComparison.Ordinal) &&
                blob.Content.Length > 0))
        {
            throw new InvalidOperationException(
                $"CSharpClr package is missing required CLR assembly blob '{assemblyBlobName}'.");
        }

        RequireTableEntrypoint(frozenEntrypoints, "CSharpClr package");

        var abiInventory = hostAbiInventory ?? TargetHostAbiInventory.Empty;
        var runtimeServices = abiInventory.CreateServiceRequirements(
            TargetRuntimeServiceFulfillmentKind.TargetProvided);

        return TargetArtifactPackage.CreateValidated(
            ExecutionTargetIds.CSharpClr,
            artifactKind,
            executableArtifactKind,
            semanticsContract,
            metadata,
            binaryBlobs: frozenBlobs,
            entrypoints: frozenEntrypoints,
            runtimeServices: runtimeServices,
            hostAbiInventory: abiInventory,
            executionIrVersion: executionIrVersion);
    }

    private static void RequireMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be null or whitespace.", nameof(key));

        if (!metadata.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Package metadata is missing required value '{key}'.");
    }

    private static void RequireTableEntrypoint(
        IReadOnlyList<TargetRuntimeEntrypoint> entrypoints,
        string packageLabel)
    {
        if (!entrypoints.Any(static entrypoint =>
                entrypoint.Kind == TargetRuntimeEntrypointKind.TableQuery &&
                !string.IsNullOrWhiteSpace(entrypoint.SymbolName)))
        {
            throw new InvalidOperationException(
                $"{packageLabel} must include a table-query entrypoint with a symbol name.");
        }
    }
}
