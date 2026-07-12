using System;
using System.Collections.Generic;

namespace Musoq.Targets.TestPortable;

internal sealed class PortableSubsetRenderedQueryFinalizer : IRenderedQueryFinalizer
{
    public ExecutionTargetId TargetId => PortableSubsetTarget.TargetId;

    public TargetFinalizationResult Finalize(RenderedQueryArtifact artifact, TargetFinalizationOptions options)
    {
        if (artifact is not PortableSubsetRenderedArtifact portable)
        {
            throw new InvalidOperationException(
                $"Portable subset finalizer expected '{nameof(PortableSubsetRenderedArtifact)}', but received '{artifact.GetType().Name}'.");
        }

        var manifest = portable.Program.CreateManifest();
        var runtimeServices = portable.HostAbiInventory.CreateServiceRequirements(
            TargetRuntimeServiceFulfillmentKind.HostImport);
        var export = TargetExportArtifact.Create(
            TargetId,
            sourceFiles:
            [
                new TargetExportSourceFile("program.musoq-portable", "musoq-portable-subset", manifest)
            ],
            entrypoints:
            [
                new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TableQuery, "run")
            ],
            runtimeServices: runtimeServices,
            hostAbiInventory: portable.HostAbiInventory,
            diagnosticsMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["language"] = "musoq-portable-subset",
                ["manifestFormat"] = "1"
            });

        return new PortableSubsetFinalizationResult(export);
    }
}

internal sealed record PortableSubsetFinalizationResult(ExecutableQueryArtifact Executable)
    : TargetFinalizationResult(
        PortableSubsetTarget.TargetId,
        true,
        [],
        Executable);

internal sealed class PortableSubsetRenderedQueryInspector : IRenderedQueryInspector
{
    public ExecutionTargetId TargetId => PortableSubsetTarget.TargetId;

    public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact)
    {
        if (artifact is not PortableSubsetRenderedArtifact portable)
        {
            throw new InvalidOperationException(
                $"Portable subset inspector expected '{nameof(PortableSubsetRenderedArtifact)}', but received '{artifact.GetType().Name}'.");
        }

        return new RenderedQueryInspection(
            TargetId,
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["language"] = "musoq-portable-subset",
                ["inspectionText"] = portable.Program.CreateManifest()
            });
    }
}
