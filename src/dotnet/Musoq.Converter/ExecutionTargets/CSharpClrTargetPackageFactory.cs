using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        int executionIrVersion = TargetContractVersions.ExecutionIr,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(binaryBlobs);
        ArgumentNullException.ThrowIfNull(entrypoints);

        var frozenBlobs = binaryBlobs.ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        var frozenEntrypoints = entrypoints.ToArray();
        RequireMetadata(metadata, generatedCodeSha256MetadataKey);
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var key in requiredMetadataKeys ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequireMetadata(metadata, key);
        }
        var expectedSemanticsVersion = semanticsContract.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!metadata.TryGetValue(CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion, out var semanticsVersion) ||
            !string.Equals(semanticsVersion, expectedSemanticsVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"CSharpClr package semantics metadata must be '{expectedSemanticsVersion}', but was '{semanticsVersion ?? "<missing>"}'.");
        }
        cancellationToken.ThrowIfCancellationRequested();

        var hasAssemblyBlob = false;
        foreach (var blob in frozenBlobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(blob.Name, assemblyBlobName, StringComparison.Ordinal) &&
                blob.Content.Length > 0)
            {
                hasAssemblyBlob = true;
                break;
            }
        }

        if (!hasAssemblyBlob)
        {
            throw new InvalidOperationException(
                $"CSharpClr package is missing required CLR assembly blob '{assemblyBlobName}'.");
        }

        RequireTableEntrypoint(frozenEntrypoints, "CSharpClr package", cancellationToken);

        var abiInventory = hostAbiInventory ?? TargetHostAbiInventory.Empty;
        var runtimeServices = abiInventory.CreateServiceRequirements(
            TargetRuntimeServiceFulfillmentKind.TargetProvided);
        cancellationToken.ThrowIfCancellationRequested();

        return TargetArtifactPackage.CreateValidated(
            ExecutionTargetIds.CSharpClr,
            artifactKind,
            executableArtifactKind,
            semanticsContract,
            metadata,
            sourceFiles: null,
            binaryBlobs: frozenBlobs,
            entrypoints: frozenEntrypoints,
            runtimeServices: runtimeServices,
            hostAbiInventory: abiInventory,
            executionIrVersion: executionIrVersion,
            packageFormatVersion: TargetContractVersions.PackageFormat,
            cancellationToken: cancellationToken);
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
        string packageLabel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var entrypoint in entrypoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entrypoint.Kind == TargetRuntimeEntrypointKind.TableQuery &&
                !string.IsNullOrWhiteSpace(entrypoint.SymbolName))
                return;
        }

        throw new InvalidOperationException(
            $"{packageLabel} must include a table-query entrypoint with a symbol name.");
    }
}
