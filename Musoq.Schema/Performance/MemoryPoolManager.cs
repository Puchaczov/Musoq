using System;

namespace Musoq.Schema.Performance;

/// <summary>
/// Global manager for memory pool optimization
/// Provides centralized control and monitoring for Phase 3 memory management
/// </summary>
public static class MemoryPoolManager
{
    private static volatile bool _isEnabled = true;

    /// <summary>
    /// Enable or disable memory pooling globally
    /// </summary>
    public static bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            MemoryPool.IsEnabled = value;
        }
    }

    /// <summary>
    /// Get comprehensive memory pool statistics
    /// </summary>
    public static MemoryPoolStatistics GetStatistics()
    {
        return MemoryPool.GetStatistics();
    }

    /// <summary>
    /// Clear all pools and reset statistics
    /// </summary>
    public static void ClearPools()
    {
        MemoryPool.Clear();
    }

    /// <summary>
    /// Pre-warm the pools with initial capacity
    /// </summary>
    public static void PreWarmPools(int tableCapacity = 10, int resolverCapacity = 100)
    {
        if (!_isEnabled) return;

        // Pre-create and return objects to pool to establish initial capacity
        var tables = new PooledTable[tableCapacity];
        var resolvers = new PooledObjectResolver[resolverCapacity];

        // Create tables
        for (int i = 0; i < tableCapacity; i++)
        {
            tables[i] = new PooledTable($"warmup_{i}", Array.Empty<ISchemaColumn>());
        }

        // Create resolvers
        for (int i = 0; i < resolverCapacity; i++)
        {
            resolvers[i] = new PooledObjectResolver();
        }

        // Return them to pool
        for (int i = 0; i < tableCapacity; i++)
        {
            MemoryPool.ReturnTable(tables[i]);
        }

        for (int i = 0; i < resolverCapacity; i++)
        {
            MemoryPool.ReturnResolver(resolvers[i]);
        }
    }

    /// <summary>
    /// Get a summary of memory pool efficiency
    /// </summary>
    public static string GetEfficiencySummary()
    {
        var stats = GetStatistics();
        return $"Memory Pool Efficiency - {stats}";
    }

    /// <summary>
    /// Check if memory pooling is providing significant benefit
    /// </summary>
    public static bool IsEffective()
    {
        var stats = GetStatistics();
        
        // Consider effective if we have decent cache hit rates
        return stats.TableCacheEfficiency > 0.5 || stats.ResolverCacheEfficiency > 0.5;
    }
}