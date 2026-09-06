using System.Threading;

namespace Musoq.Converter;

/// <summary>
///     Loads a runnable type and optional lifetime owner for a compiled query artifact.
///     The engine disposes the lifetime owner when the returned compiled query is disposed.
/// </summary>
public delegate CompiledQueryArtifactLoadResult CompiledQueryArtifactLoader(ICompiledQueryArtifact artifact);

/// <summary>
/// Loads a runnable type and optional lifetime owner while receiving the current cooperative cancellation token.
/// </summary>
public delegate CompiledQueryArtifactLoadResult CompiledQueryArtifactLoaderWithCancellation(
    ICompiledQueryArtifact artifact,
    CancellationToken cancellationToken);
