namespace Musoq.Converter;

/// <summary>
///     Result returned by a compiled artifact loader.
///     When no custom loader is supplied, the engine loads artifact bytes through a collectible assembly load context.
/// </summary>
public sealed class CompiledQueryArtifactLoadResult
{
    public CompiledQueryArtifactLoadResult(Type runnableType, IDisposable? lifetimeOwner = null)
    {
        RunnableType = runnableType ?? throw new ArgumentNullException(nameof(runnableType));
        LifetimeOwner = lifetimeOwner;
    }

    /// <summary>
    ///     Gets the runnable type loaded from the artifact.
    /// </summary>
    public Type RunnableType { get; }

    /// <summary>
    ///     Gets an optional owner disposed with the returned compiled query.
    /// </summary>
    public IDisposable? LifetimeOwner { get; }
}
