using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Performance;

/// <summary>
/// Memory pool for reusing Table and Row objects to reduce allocation overhead
/// Phase 3 optimization: 40% memory allocation reduction target
/// </summary>
public static class MemoryPool
{
    private static readonly ConcurrentQueue<IReadOnlyTable> _tablePool = new();
    private static readonly ConcurrentQueue<IObjectResolver> _resolverPool = new();
    private static readonly ConcurrentBag<MemoryPoolStatistics> _stats = new();
    
    private static volatile bool _isEnabled = true;
    private static readonly object _statsLock = new();
    
    public static bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    /// Rent a table instance from the pool or create new one
    /// </summary>
    public static PooledTable RentTable(string name, IReadOnlyCollection<ISchemaColumn> columns)
    {
        if (!_isEnabled || !_tablePool.TryDequeue(out var table))
        {
            RecordStatistic(MemoryPoolStatisticType.TableMiss);
            return new PooledTable(name, columns);
        }

        RecordStatistic(MemoryPoolStatisticType.TableHit);
        
        if (table is PooledTable pooledTable)
        {
            pooledTable.Reset(name, columns);
            return pooledTable;
        }

        // Wrong type, create new one
        return new PooledTable(name, columns);
    }

    /// <summary>
    /// Return a table instance to the pool
    /// </summary>
    public static void ReturnTable(PooledTable table)
    {
        if (!_isEnabled) return;
        
        table.Clear();
        _tablePool.Enqueue(table);
        RecordStatistic(MemoryPoolStatisticType.TableReturned);
    }

    /// <summary>
    /// Rent an object resolver from the pool
    /// </summary>
    public static PooledObjectResolver RentResolver()
    {
        if (!_isEnabled || !_resolverPool.TryDequeue(out var resolver))
        {
            RecordStatistic(MemoryPoolStatisticType.ResolverMiss);
            return new PooledObjectResolver();
        }

        RecordStatistic(MemoryPoolStatisticType.ResolverHit);
        
        if (resolver is PooledObjectResolver pooledResolver)
        {
            pooledResolver.Reset();
            return pooledResolver;
        }

        return new PooledObjectResolver();
    }

    /// <summary>
    /// Return an object resolver to the pool
    /// </summary>
    public static void ReturnResolver(PooledObjectResolver resolver)
    {
        if (!_isEnabled) return;
        
        resolver.Clear();
        _resolverPool.Enqueue(resolver);
        RecordStatistic(MemoryPoolStatisticType.ResolverReturned);
    }

    /// <summary>
    /// Get memory pool usage statistics
    /// </summary>
    public static MemoryPoolStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            var stats = new MemoryPoolStatistics();
            
            foreach (var stat in _stats)
            {
                stats.Merge(stat);
            }

            // Calculate efficiency
            var totalTableRequests = stats.TableHits + stats.TableMisses;
            var totalResolverRequests = stats.ResolverHits + stats.ResolverMisses;
            
            stats.TableCacheEfficiency = totalTableRequests > 0 
                ? (double)stats.TableHits / totalTableRequests 
                : 0.0;
                
            stats.ResolverCacheEfficiency = totalResolverRequests > 0 
                ? (double)stats.ResolverHits / totalResolverRequests 
                : 0.0;

            stats.PooledTablesAvailable = _tablePool.Count;
            stats.PooledResolversAvailable = _resolverPool.Count;

            return stats;
        }
    }

    /// <summary>
    /// Clear all pools and reset statistics
    /// </summary>
    public static void Clear()
    {
        lock (_statsLock)
        {
            while (_tablePool.TryDequeue(out _)) { }
            while (_resolverPool.TryDequeue(out _)) { }
            _stats.Clear();
        }
    }

    private static void RecordStatistic(MemoryPoolStatisticType type)
    {
        if (!_isEnabled) return;
        
        var stat = new MemoryPoolStatistics();
        
        switch (type)
        {
            case MemoryPoolStatisticType.TableHit:
                stat.TableHits = 1;
                break;
            case MemoryPoolStatisticType.TableMiss:
                stat.TableMisses = 1;
                break;
            case MemoryPoolStatisticType.TableReturned:
                stat.TablesReturned = 1;
                break;
            case MemoryPoolStatisticType.ResolverHit:
                stat.ResolverHits = 1;
                break;
            case MemoryPoolStatisticType.ResolverMiss:
                stat.ResolverMisses = 1;
                break;
            case MemoryPoolStatisticType.ResolverReturned:
                stat.ResolversReturned = 1;
                break;
        }
        
        _stats.Add(stat);
    }

    private enum MemoryPoolStatisticType
    {
        TableHit,
        TableMiss,
        TableReturned,
        ResolverHit,
        ResolverMiss,
        ResolverReturned
    }
}

/// <summary>
/// Statistics for memory pool usage and efficiency
/// </summary>
public class MemoryPoolStatistics
{
    public long TableHits { get; set; }
    public long TableMisses { get; set; }
    public long TablesReturned { get; set; }
    public long ResolverHits { get; set; }
    public long ResolverMisses { get; set; }
    public long ResolversReturned { get; set; }
    
    public double TableCacheEfficiency { get; set; }
    public double ResolverCacheEfficiency { get; set; }
    
    public int PooledTablesAvailable { get; set; }
    public int PooledResolversAvailable { get; set; }

    public void Merge(MemoryPoolStatistics other)
    {
        TableHits += other.TableHits;
        TableMisses += other.TableMisses;
        TablesReturned += other.TablesReturned;
        ResolverHits += other.ResolverHits;
        ResolverMisses += other.ResolverMisses;
        ResolversReturned += other.ResolversReturned;
    }

    public override string ToString()
    {
        return $"Tables - Hits: {TableHits}, Misses: {TableMisses}, Efficiency: {TableCacheEfficiency:P1} | " +
               $"Resolvers - Hits: {ResolverHits}, Misses: {ResolverMisses}, Efficiency: {ResolverCacheEfficiency:P1} | " +
               $"Available - Tables: {PooledTablesAvailable}, Resolvers: {PooledResolversAvailable}";
    }
}