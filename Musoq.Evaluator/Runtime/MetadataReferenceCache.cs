using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

/// <summary>
///     Thread-safe cache for MetadataReference objects to avoid repeated file loading
///     and memory allocation when compiling multiple queries.
/// </summary>
public static class MetadataReferenceCache
{
    private static readonly ConcurrentDictionary<string, MetadataReference> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets the current number of cached references.
    /// </summary>
    public static int Count => Cache.Count;

    /// <summary>
    ///     Gets or creates a MetadataReference for the given assembly path.
    /// </summary>
    /// <param name="assemblyPath">The full path to the assembly file.</param>
    /// <returns>A cached or newly created MetadataReference.</returns>
    public static MetadataReference GetOrCreate(string assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath))
            throw new ArgumentNullException(nameof(assemblyPath));

        return Cache.GetOrAdd(assemblyPath, path => MetadataReference.CreateFromFile(path));
    }

    /// <summary>
    ///     Clears the cache. Useful for testing or when assemblies may have changed on disk.
    /// </summary>
    public static void Clear()
    {
        Cache.Clear();
    }
}