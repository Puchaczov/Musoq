namespace Musoq.Converter;

/// <summary>
///     Loads a runnable type for a compiled query artifact.
///     Hosts can use this delegate to load artifact bytes through a custom AssemblyLoadContext.
///     This legacy delegate does not provide a lifetime owner; prefer <see cref="CompiledQueryArtifactLoader" />
///     for collectible or otherwise owned loading strategies.
/// </summary>
public delegate Type CompiledQueryArtifactTypeLoader(ICompiledQueryArtifact artifact);
