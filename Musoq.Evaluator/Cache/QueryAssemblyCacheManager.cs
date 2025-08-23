using System;

namespace Musoq.Evaluator.Cache;

/// <summary>
/// Global singleton manager for the query assembly cache.
/// Provides a centralized cache instance for the entire application.
/// </summary>
public static class QueryAssemblyCacheManager
{
    private static readonly Lazy<QueryAssemblyCache> _instance = new(() => new QueryAssemblyCache());
    
    /// <summary>
    /// Gets the global query assembly cache instance
    /// </summary>
    public static QueryAssemblyCache Instance => _instance.Value;
    
    /// <summary>
    /// Enables or disables the cache globally. When disabled, compilation will bypass the cache.
    /// </summary>
    public static bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Resets the cache instance (useful for testing)
    /// </summary>
    public static void Reset()
    {
        if (_instance.IsValueCreated)
        {
            _instance.Value.Clear();
        }
    }
}