using System;

namespace Musoq.Schema.Compilation;

/// <summary>
/// Global manager for the schema method compilation cache.
/// Provides centralized control over method compilation caching for performance optimization.
/// </summary>
public static class SchemaMethodCompilationCacheManager
{
    private static readonly Lazy<SchemaMethodCompilationCache> _instance = 
        new(() => new SchemaMethodCompilationCache());

    /// <summary>
    /// Gets or sets whether schema method compilation caching is enabled.
    /// When disabled, methods will use reflection as before.
    /// Default: true (enabled for maximum performance).
    /// </summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the global schema method compilation cache instance.
    /// </summary>
    public static SchemaMethodCompilationCache Instance => _instance.Value;

    /// <summary>
    /// Gets comprehensive statistics about cache performance.
    /// </summary>
    /// <returns>Cache statistics including hit rate, compilation times, etc.</returns>
    public static SchemaMethodCompilationCache.CacheStatistics GetStatistics()
    {
        return Instance.GetStatistics();
    }

    /// <summary>
    /// Clears all cached compiled methods.
    /// Useful for testing or when memory usage needs to be reduced.
    /// </summary>
    public static void ClearCache()
    {
        Instance.Clear();
    }

    /// <summary>
    /// Resets the cache manager to its initial state.
    /// Creates a new cache instance and resets all statistics.
    /// </summary>
    public static void Reset()
    {
        Instance.Clear();
        IsEnabled = true;
    }
}