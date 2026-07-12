using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        int packageFormatVersion = TargetContractVersions.PackageFormat)
    {
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
        Metadata = FreezeDictionary(metadata);
        SourceFiles = Freeze(sourceFiles);
        BinaryBlobs = Freeze(binaryBlobs);
        Entrypoints = Freeze(entrypoints);
        ValidateArtifactPaths(SourceFiles, BinaryBlobs);
        ValidateEntrypoints(Entrypoints);
        RuntimeServices = runtimeServices ?? TargetRuntimeServiceRequirements.Empty;
        HostAbiInventory = hostAbiInventory ?? TargetHostAbiInventory.Empty;
        HostAbiInventory.ValidateRuntimeServices(RuntimeServices);
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
            packageFormatVersion);
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
        ArgumentNullException.ThrowIfNull(exportArtifact);

        if (exportArtifact.TargetId != targetId)
        {
            throw new InvalidOperationException(
                $"Export artifact target '{exportArtifact.TargetId}' does not match package target '{targetId}'.");
        }

        if (exportArtifact.SourceFiles.Count == 0 && exportArtifact.BinaryBlobs.Count == 0)
            throw new InvalidOperationException("Portable export package must include at least one source file or binary blob.");

        RequireQueryEntrypoint(exportArtifact.Entrypoints, "Portable export package");

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
            packageFormatVersion);
    }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }

    private static IReadOnlyDictionary<string, string> FreezeDictionary(
        IReadOnlyDictionary<string, string>? metadata)
    {
        return new ReadOnlyDictionary<string, string>(
            metadata is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(metadata, StringComparer.Ordinal));
    }

    private static string RequireText(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;
    }

    private static void RequireQueryEntrypoint(
        IReadOnlyList<TargetRuntimeEntrypoint> entrypoints,
        string packageLabel)
    {
        if (!entrypoints.Any(static entrypoint =>
                entrypoint.Kind is TargetRuntimeEntrypointKind.TableQuery or TargetRuntimeEntrypointKind.TypedQuery &&
                !string.IsNullOrWhiteSpace(entrypoint.SymbolName)))
        {
            throw new InvalidOperationException(
                $"{packageLabel} must include a table-query or typed-query entrypoint with a symbol name.");
        }
    }

    private static void ValidateArtifactPaths(
        IReadOnlyList<TargetExportSourceFile> sourceFiles,
        IReadOnlyList<TargetExportBinaryBlob> binaryBlobs)
    {
        TargetArtifactPath.RequireUnique(sourceFiles, static file => file.Path, "Source file");
        TargetArtifactPath.RequireUnique(binaryBlobs, static blob => blob.Name, "Binary blob");
        var duplicate = sourceFiles.Select(static file => file.Path)
            .Intersect(binaryBlobs.Select(static blob => blob.Name), StringComparer.Ordinal)
            .FirstOrDefault();
        if (duplicate != null)
            throw new ArgumentException($"Package path '{duplicate}' is used by both a source file and binary blob.");
    }

    private static void ValidateEntrypoints(IReadOnlyList<TargetRuntimeEntrypoint> entrypoints)
    {
        RequireUnique(entrypoints, static entrypoint => entrypoint.Name, "name");
        RequireUnique(entrypoints, static entrypoint => entrypoint.SymbolName, "symbol");
    }

    private static void RequireUnique(
        IEnumerable<TargetRuntimeEntrypoint> entrypoints,
        Func<TargetRuntimeEntrypoint, string> selector,
        string label)
    {
        var duplicate = entrypoints.GroupBy(selector, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate != null)
            throw new ArgumentException($"Runtime entrypoint {label} '{duplicate.Key}' is duplicated.");
    }
}
