using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

/// <summary>
///     Legacy static compatibility facade for a bounded metadata-reference cache.
///     New compilation paths should use an explicit <see cref="EvaluatorRuntimeEnvironment"/>.
/// </summary>
public static class MetadataReferenceCache
{
    internal static IMetadataReferenceCache Default => RuntimeLibraries.MetadataReferences;

    internal static IMetadataReferenceCache CreateScoped() =>
        new DefaultMetadataReferenceCache();

    /// <summary>
    ///     Gets the current number of cached references.
    /// </summary>
    public static int Count => RuntimeLibraries.WithEnvironment(static environment => environment.MetadataReferenceCache.Count);

    /// <summary>
    ///     Gets or creates a MetadataReference for the given assembly path.
    /// </summary>
    /// <param name="assemblyPath">The full path to the assembly file.</param>
    /// <returns>A cached or newly created MetadataReference.</returns>
    public static MetadataReference GetOrCreate(string assemblyPath)
    {
        return RuntimeLibraries.WithEnvironment(environment => environment.GetOrCreateMetadataReference(assemblyPath));
    }

    /// <summary>
    ///     Clears the cache. Useful for testing or when assemblies may have changed on disk.
    /// </summary>
    public static void Clear()
    {
        RuntimeLibraries.WithEnvironment(static environment => environment.MetadataReferenceCache.Clear());
    }
}
