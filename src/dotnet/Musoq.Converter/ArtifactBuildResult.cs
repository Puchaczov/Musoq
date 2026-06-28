using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter;

/// <summary>
///     Represents the outcome of compiling a query into a portable runtime-v2 artifact.
/// </summary>
public sealed class ArtifactBuildResult
{
    private ArtifactBuildResult(
        ICompiledQueryArtifact? artifact,
        IReadOnlyList<Diagnostic> diagnostics,
        Exception? caughtException)
    {
        Artifact = artifact;
        Diagnostics = diagnostics;
        CaughtException = caughtException;
        Errors = diagnostics.Where(static d => d.IsError).ToList();
        Warnings = diagnostics.Where(static d => d.IsWarning).ToList();
    }

    [MemberNotNullWhen(true, nameof(Artifact))]
    public bool Succeeded => Artifact != null && Errors.Count == 0;

    public ICompiledQueryArtifact? Artifact { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public IReadOnlyList<Diagnostic> Errors { get; }

    public IReadOnlyList<Diagnostic> Warnings { get; }

    public Exception? CaughtException { get; }

    internal static ArtifactBuildResult Success(
        ICompiledQueryArtifact artifact,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        return new ArtifactBuildResult(artifact, diagnostics, caughtException: null);
    }

    internal static ArtifactBuildResult Failure(
        IReadOnlyList<Diagnostic> diagnostics,
        Exception? caughtException = null)
    {
        return new ArtifactBuildResult(null, diagnostics, caughtException);
    }
}
