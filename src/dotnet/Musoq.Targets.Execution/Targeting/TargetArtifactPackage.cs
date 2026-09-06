using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Musoq.Targets.Execution;

internal sealed record TargetArtifactPackage
{
    private TargetArtifactPackage(
        ExecutionTargetId targetId,
        string artifactKind,
        string executableArtifactKind,
        ExecutionSemanticsContract semanticsContract,
        IReadOnlyDictionary<string, string>? metadata = null,
        IEnumerable<TargetExportSourceFile>? sourceFiles = null,
        IEnumerable<TargetExportBinaryBlob>? binaryBlobs = null,
        IEnumerable<TargetRuntimeEntrypoint>? entrypoints = null,
        TargetRuntimeServiceRequirements? runtimeServices = null,
        TargetHostAbiInventory? hostAbiInventory = null,
        int executionIrVersion = TargetContractVersions.ExecutionIr,
        int packageFormatVersion = TargetContractVersions.PackageFormat,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (executionIrVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(executionIrVersion));
        if (packageFormatVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(packageFormatVersion));

        TargetId = targetId;
        ArtifactKind = RequireText(artifactKind, nameof(artifactKind));
        ExecutableArtifactKind = RequireText(executableArtifactKind, nameof(executableArtifactKind));
        SemanticsContract = semanticsContract ?? throw new ArgumentNullException(nameof(semanticsContract));
        ExecutionIrVersion = executionIrVersion;
        PackageFormatVersion = packageFormatVersion;
        Metadata = FreezeDictionary(metadata, cancellationToken);
        SourceFiles = Freeze(sourceFiles, cancellationToken);
        BinaryBlobs = Freeze(binaryBlobs, cancellationToken);
        Entrypoints = Freeze(entrypoints, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateArtifactPaths(SourceFiles, BinaryBlobs, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEntrypoints(Entrypoints, cancellationToken);
        RuntimeServices = runtimeServices ?? TargetRuntimeServiceRequirements.Empty;
        HostAbiInventory = hostAbiInventory ?? TargetHostAbiInventory.Empty;
        HostAbiInventory.ValidateRuntimeServices(RuntimeServices);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public ExecutionTargetId TargetId { get; }

    public string ArtifactKind { get; }

    public string ExecutableArtifactKind { get; }

    public ExecutionSemanticsContract SemanticsContract { get; }

    public int ExecutionIrVersion { get; }

    public int HostAbiVersion => HostAbiInventory.ContractVersion;

    public int PackageFormatVersion { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public IReadOnlyList<TargetExportSourceFile> SourceFiles { get; }

    public IReadOnlyList<TargetExportBinaryBlob> BinaryBlobs { get; }

    public IReadOnlyList<TargetRuntimeEntrypoint> Entrypoints { get; }

    public TargetRuntimeServiceRequirements RuntimeServices { get; }

    public TargetHostAbiInventory HostAbiInventory { get; }

    public static TargetArtifactPackage CreateValidated(
        ExecutionTargetId targetId,
        string artifactKind,
        string executableArtifactKind,
        ExecutionSemanticsContract semanticsContract,
        IReadOnlyDictionary<string, string>? metadata = null,
        IEnumerable<TargetExportSourceFile>? sourceFiles = null,
        IEnumerable<TargetExportBinaryBlob>? binaryBlobs = null,
        IEnumerable<TargetRuntimeEntrypoint>? entrypoints = null,
        TargetRuntimeServiceRequirements? runtimeServices = null,
        TargetHostAbiInventory? hostAbiInventory = null,
        int executionIrVersion = TargetContractVersions.ExecutionIr,
        int packageFormatVersion = TargetContractVersions.PackageFormat)
    {
        return CreateValidated(
            targetId,
            artifactKind,
            executableArtifactKind,
            semanticsContract,
            metadata,
            sourceFiles,
            binaryBlobs,
            entrypoints,
            runtimeServices,
            hostAbiInventory,
            executionIrVersion,
            packageFormatVersion,
            CancellationToken.None);
    }

    public static TargetArtifactPackage CreateValidated(
        ExecutionTargetId targetId,
        string artifactKind,
        string executableArtifactKind,
        ExecutionSemanticsContract semanticsContract,
        IReadOnlyDictionary<string, string>? metadata,
        IEnumerable<TargetExportSourceFile>? sourceFiles,
        IEnumerable<TargetExportBinaryBlob>? binaryBlobs,
        IEnumerable<TargetRuntimeEntrypoint>? entrypoints,
        TargetRuntimeServiceRequirements? runtimeServices,
        TargetHostAbiInventory? hostAbiInventory,
        int executionIrVersion,
        int packageFormatVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(targetId.Value))
            throw new ArgumentException("Target artifact package must declare a target id.", nameof(targetId));

        return new TargetArtifactPackage(
            targetId,
            artifactKind,
            executableArtifactKind,
            semanticsContract,
            metadata,
            sourceFiles,
            binaryBlobs,
            entrypoints,
            runtimeServices,
            hostAbiInventory,
            executionIrVersion,
            packageFormatVersion,
            cancellationToken);
    }

    public static TargetArtifactPackage CreatePortableExportPackage(
        ExecutionTargetId targetId,
        string artifactKind,
        TargetExportArtifact exportArtifact,
        ExecutionSemanticsContract semanticsContract,
        IReadOnlyDictionary<string, string>? metadata = null,
        int executionIrVersion = TargetContractVersions.ExecutionIr,
        int packageFormatVersion = TargetContractVersions.PackageFormat)
    {
        return CreatePortableExportPackage(
            targetId,
            artifactKind,
            exportArtifact,
            semanticsContract,
            metadata,
            executionIrVersion,
            packageFormatVersion,
            CancellationToken.None);
    }

    public static TargetArtifactPackage CreatePortableExportPackage(
        ExecutionTargetId targetId,
        string artifactKind,
        TargetExportArtifact exportArtifact,
        ExecutionSemanticsContract semanticsContract,
        IReadOnlyDictionary<string, string>? metadata,
        int executionIrVersion,
        int packageFormatVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(exportArtifact);

        if (exportArtifact.TargetId != targetId)
        {
            throw new InvalidOperationException(
                $"Export artifact target '{exportArtifact.TargetId}' does not match package target '{targetId}'.");
        }

        if (exportArtifact.SourceFiles.Count == 0 && exportArtifact.BinaryBlobs.Count == 0)
            throw new InvalidOperationException("Portable export package must include at least one source file or binary blob.");

        RequireQueryEntrypoint(exportArtifact.Entrypoints, "Portable export package", cancellationToken);

        return CreateValidated(
            targetId,
            artifactKind,
            nameof(TargetExportArtifact),
            semanticsContract,
            metadata ?? exportArtifact.DiagnosticsMetadata,
            exportArtifact.SourceFiles,
            exportArtifact.BinaryBlobs,
            exportArtifact.Entrypoints,
            exportArtifact.RuntimeServices,
            exportArtifact.HostAbiInventory,
            executionIrVersion,
            packageFormatVersion,
            cancellationToken);
    }

    private static IReadOnlyList<T> Freeze<T>(
        IEnumerable<T>? values,
        CancellationToken cancellationToken)
    {
        if (values is null)
            return Array.AsReadOnly(Array.Empty<T>());

        var frozen = new List<T>();
        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            frozen.Add(value);
        }

        return Array.AsReadOnly(frozen.ToArray());
    }

    private static IReadOnlyDictionary<string, string> FreezeDictionary(
        IReadOnlyDictionary<string, string>? metadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var frozen = new Dictionary<string, string>(StringComparer.Ordinal);
        if (metadata is not null)
        {
            foreach (var entry in metadata)
            {
                cancellationToken.ThrowIfCancellationRequested();
                frozen.Add(entry.Key, entry.Value);
            }
        }

        return new ReadOnlyDictionary<string, string>(
            frozen);
    }

    private static string RequireText(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;
    }

    private static void RequireQueryEntrypoint(
        IReadOnlyList<TargetRuntimeEntrypoint> entrypoints,
        string packageLabel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var entrypoint in entrypoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entrypoint.Kind is TargetRuntimeEntrypointKind.TableQuery or TargetRuntimeEntrypointKind.TypedQuery &&
                !string.IsNullOrWhiteSpace(entrypoint.SymbolName))
                return;
        }

        throw new InvalidOperationException(
            $"{packageLabel} must include a table-query or typed-query entrypoint with a symbol name.");
    }

    private static void ValidateArtifactPaths(
        IReadOnlyList<TargetExportSourceFile> sourceFiles,
        IReadOnlyList<TargetExportBinaryBlob> binaryBlobs,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TargetArtifactPath.RequireUnique(sourceFiles, static file => file.Path, "Source file");
        cancellationToken.ThrowIfCancellationRequested();
        TargetArtifactPath.RequireUnique(binaryBlobs, static blob => blob.Name, "Binary blob");
        cancellationToken.ThrowIfCancellationRequested();
        var duplicate = sourceFiles.Select(static file => file.Path)
            .Intersect(binaryBlobs.Select(static blob => blob.Name), StringComparer.Ordinal)
            .FirstOrDefault();
        if (duplicate != null)
            throw new ArgumentException($"Package path '{duplicate}' is used by both a source file and binary blob.");
    }

    private static void ValidateEntrypoints(
        IReadOnlyList<TargetRuntimeEntrypoint> entrypoints,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireUnique(entrypoints, static entrypoint => entrypoint.Name, "name", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        RequireUnique(entrypoints, static entrypoint => entrypoint.SymbolName, "symbol", cancellationToken);
    }

    private static void RequireUnique(
        IEnumerable<TargetRuntimeEntrypoint> entrypoints,
        Func<TargetRuntimeEntrypoint, string> selector,
        string label,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var duplicate = entrypoints.GroupBy(selector, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        cancellationToken.ThrowIfCancellationRequested();
        if (duplicate != null)
            throw new ArgumentException($"Runtime entrypoint {label} '{duplicate.Key}' is duplicated.");
    }
}
