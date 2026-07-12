using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Abstractions;

internal abstract record TargetFinalizationResult
{
    protected TargetFinalizationResult(
        ExecutionTargetId targetId,
        bool success,
        IReadOnlyList<TargetDiagnostic>? diagnostics,
        ExecutableQueryArtifact? artifact)
    {
        if (success && artifact is null)
            throw new ArgumentException("Successful target finalization must produce an executable artifact.", nameof(artifact));

        if (!success && artifact is not null)
            throw new ArgumentException("Failed target finalization cannot expose an executable artifact.", nameof(artifact));

        if (artifact is not null && artifact.TargetId != targetId)
        {
            throw new ArgumentException(
                $"Target finalization result '{targetId}' cannot contain an executable artifact for target '{artifact.TargetId}'.",
                nameof(artifact));
        }

        var frozenDiagnostics = Freeze(diagnostics);
        if (success && frozenDiagnostics.Any(static diagnostic =>
                diagnostic.Severity == TargetDiagnosticSeverity.Error))
        {
            throw new ArgumentException(
                "Successful target finalization cannot contain error diagnostics.",
                nameof(diagnostics));
        }

        TargetId = targetId;
        Success = success;
        Diagnostics = frozenDiagnostics;
        Artifact = artifact;
    }

    public ExecutionTargetId TargetId { get; }

    public bool Success { get; }

    public IReadOnlyList<TargetDiagnostic> Diagnostics { get; }

    public ExecutableQueryArtifact? Artifact { get; }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}
