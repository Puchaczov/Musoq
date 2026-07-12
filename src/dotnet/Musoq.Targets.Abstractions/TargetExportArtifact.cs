using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetExportArtifact : ExecutableQueryArtifact
{
    public TargetExportArtifact(
        ExecutionTargetId targetId,
        IEnumerable<TargetExportSourceFile>? sourceFiles,
        IEnumerable<TargetExportBinaryBlob>? binaryBlobs,
        IEnumerable<TargetRuntimeEntrypoint>? entrypoints,
        TargetRuntimeServiceRequirements? runtimeServices,
        TargetHostAbiInventory? hostAbiInventory,
        IReadOnlyDictionary<string, string>? diagnosticsMetadata)
        : base(targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId.Value))
            throw new ArgumentException("Target export artifact must declare a target id.", nameof(targetId));

        SourceFiles = Freeze(sourceFiles);
        BinaryBlobs = Freeze(binaryBlobs);
        Entrypoints = Freeze(entrypoints);
        ValidateArtifactPaths(SourceFiles, BinaryBlobs);
        ValidateEntrypoints(Entrypoints);
        RuntimeServices = runtimeServices ?? TargetRuntimeServiceRequirements.Empty;
        HostAbiInventory = hostAbiInventory ?? TargetHostAbiInventory.Empty;
        HostAbiInventory.ValidateRuntimeServices(RuntimeServices);
        DiagnosticsMetadata = FreezeDictionary(diagnosticsMetadata);
    }

    public IReadOnlyList<TargetExportSourceFile> SourceFiles { get; }

    public IReadOnlyList<TargetExportBinaryBlob> BinaryBlobs { get; }

    public IReadOnlyList<TargetRuntimeEntrypoint> Entrypoints { get; }

    public TargetRuntimeServiceRequirements RuntimeServices { get; }

    public TargetHostAbiInventory HostAbiInventory { get; }

    public IReadOnlyDictionary<string, string> DiagnosticsMetadata { get; }

    public static TargetExportArtifact Create(
        ExecutionTargetId targetId,
        IEnumerable<TargetExportSourceFile>? sourceFiles = null,
        IEnumerable<TargetExportBinaryBlob>? binaryBlobs = null,
        IEnumerable<TargetRuntimeEntrypoint>? entrypoints = null,
        TargetRuntimeServiceRequirements? runtimeServices = null,
        TargetHostAbiInventory? hostAbiInventory = null,
        IReadOnlyDictionary<string, string>? diagnosticsMetadata = null)
    {
        return new TargetExportArtifact(
            targetId,
            sourceFiles,
            binaryBlobs,
            entrypoints,
            runtimeServices ?? TargetRuntimeServiceRequirements.Empty,
            hostAbiInventory,
            diagnosticsMetadata);
    }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }

    private static IReadOnlyDictionary<string, string> FreezeDictionary(
        IReadOnlyDictionary<string, string>? values)
    {
        return new ReadOnlyDictionary<string, string>(
            values is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(values, StringComparer.Ordinal));
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
            throw new ArgumentException($"Artifact path '{duplicate}' is used by both a source file and binary blob.");
    }

    private static void ValidateEntrypoints(IReadOnlyList<TargetRuntimeEntrypoint> entrypoints)
    {
        RequireUniqueEntrypoint(entrypoints, static entrypoint => entrypoint.Name, "name");
        RequireUniqueEntrypoint(entrypoints, static entrypoint => entrypoint.SymbolName, "symbol");
    }

    private static void RequireUniqueEntrypoint(
        IEnumerable<TargetRuntimeEntrypoint> entrypoints,
        Func<TargetRuntimeEntrypoint, string> selector,
        string label)
    {
        var duplicate = entrypoints
            .GroupBy(selector, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate != null)
            throw new ArgumentException($"Runtime entrypoint {label} '{duplicate.Key}' is duplicated.");
    }
}
