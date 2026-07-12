using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution;

internal sealed record TargetRenderResult
{
    private TargetRenderResult(
        ExecutionTargetId targetId,
        RenderedQueryArtifact? artifact,
        IEnumerable<TargetDiagnostic>? diagnostics)
    {
        var frozenDiagnostics = Array.AsReadOnly((diagnostics ?? []).ToArray());
        if (artifact is null && !frozenDiagnostics.Any(static diagnostic =>
                diagnostic.Severity == TargetDiagnosticSeverity.Error))
        {
            throw new ArgumentException("Failed target rendering must contain an error diagnostic.", nameof(diagnostics));
        }

        if (artifact is not null && artifact.TargetId != targetId)
            throw new ArgumentException("Rendered artifact target must match the render result target.", nameof(artifact));

        if (artifact is not null && frozenDiagnostics.Any(static diagnostic =>
                diagnostic.Severity == TargetDiagnosticSeverity.Error))
        {
            throw new ArgumentException("Successful target rendering cannot contain error diagnostics.", nameof(diagnostics));
        }

        TargetId = targetId;
        Artifact = artifact;
        Diagnostics = frozenDiagnostics;
    }

    public ExecutionTargetId TargetId { get; }

    public bool Success => Artifact is not null;

    public RenderedQueryArtifact? Artifact { get; }

    public IReadOnlyList<TargetDiagnostic> Diagnostics { get; }

    public static TargetRenderResult Succeeded(
        RenderedQueryArtifact artifact,
        IEnumerable<TargetDiagnostic>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        return new TargetRenderResult(artifact.TargetId, artifact, diagnostics);
    }

    public static TargetRenderResult Failed(
        ExecutionTargetId targetId,
        IEnumerable<TargetDiagnostic> diagnostics) =>
        new(targetId, null, diagnostics);
}
