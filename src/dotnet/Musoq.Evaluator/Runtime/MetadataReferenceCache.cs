using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

/// <summary>
///     Thread-safe cache for MetadataReference objects to avoid repeated file loading
///     and memory allocation when compiling multiple queries.
/// </summary>
public static class MetadataReferenceCache
{
    internal static IMetadataReferenceCache Default { get; } = new DefaultMetadataReferenceCache();

    /// <summary>
    ///     Gets the current number of cached references.
    /// </summary>
    public static int Count => Default.Count;

    /// <summary>
    ///     Gets or creates a MetadataReference for the given assembly path.
    /// </summary>
    /// <param name="assemblyPath">The full path to the assembly file.</param>
    /// <returns>A cached or newly created MetadataReference.</returns>
    public static MetadataReference GetOrCreate(string assemblyPath)
    {
        return Default.GetOrCreate(assemblyPath);
    }

    /// <summary>
    ///     Clears the cache. Useful for testing or when assemblies may have changed on disk.
    /// </summary>
    public static void Clear()
    {
        Default.Clear();
    }
}
