using System;

namespace Musoq.Converter;

/// <summary>
///     Loads a runnable type for a compiled query artifact.
///     Hosts can use this delegate to load artifact bytes through a custom AssemblyLoadContext.
/// </summary>
public delegate Type CompiledQueryArtifactTypeLoader(ICompiledQueryArtifact artifact);
