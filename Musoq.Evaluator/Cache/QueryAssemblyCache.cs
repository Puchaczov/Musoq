using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Musoq.Schema;

namespace Musoq.Evaluator.Cache;

/// <summary>
/// LRU-based cache for compiled query assemblies to improve performance by avoiding recompilation
/// of identical queries. This addresses the 40-60% compilation overhead reduction opportunity
/// identified in the performance analysis.
/// </summary>
public class QueryAssemblyCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ReaderWriterLockSlim _accessOrderLock = new();
    private readonly LinkedList<string> _accessOrder = new();
    private readonly int _maxSize;
    private readonly TimeSpan _maxAge;

    public QueryAssemblyCache(int maxSize = 100, TimeSpan? maxAge = null)
    {
        _maxSize = maxSize;
        _maxAge = maxAge ?? TimeSpan.FromHours(1); // Default 1 hour cache age
    }

    /// <summary>
    /// Gets a cached compiled query or compiles a new one if not found
    /// </summary>
    public CompiledQuery GetOrCompile(string querySignature, Func<CompiledQuery> compiler)
    {
        // First, try to get from cache
        if (_cache.TryGetValue(querySignature, out var entry))
        {
            // Check if entry is still valid
            if (DateTime.UtcNow - entry.CreatedAt <= _maxAge)
            {
                UpdateAccessOrder(querySignature);
                return entry.CompiledQuery;
            }
            else
            {
                // Entry expired, remove it
                RemoveEntry(querySignature);
            }
        }

        // Not in cache or expired, compile new query
        var compiledQuery = compiler();
        
        // Add to cache
        AddToCache(querySignature, compiledQuery);
        
        return compiledQuery;
    }

    /// <summary>
    /// Generates a cache signature for a query based on the query text and schema provider
    /// </summary>
    public static string GenerateQuerySignature(string queryText, ISchemaProvider schemaProvider)
    {
        var schemaSignature = GenerateSchemaSignature(schemaProvider);
        var combinedInput = $"{queryText}|{schemaSignature}";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedInput));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Gets cache statistics for monitoring and debugging
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        _accessOrderLock.EnterReadLock();
        try
        {
            var now = DateTime.UtcNow;
            var validEntries = _cache.Values.Count(e => now - e.CreatedAt <= _maxAge);
            
            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                ValidEntries = validEntries,
                ExpiredEntries = _cache.Count - validEntries,
                MaxSize = _maxSize,
                CacheEfficiency = _cache.Count > 0 ? (double)validEntries / _cache.Count : 0.0
            };
        }
        finally
        {
            _accessOrderLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clears all cached entries
    /// </summary>
    public void Clear()
    {
        _accessOrderLock.EnterWriteLock();
        try
        {
            _cache.Clear();
            _accessOrder.Clear();
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }
    }

    private static string GenerateSchemaSignature(ISchemaProvider schemaProvider)
    {
        // Generate a signature based on the schema provider type
        // Since we can't enumerate all schemas, we use the provider type as the signature
        var providerType = schemaProvider.GetType().FullName ?? "Unknown";
        return providerType;
    }

    private void AddToCache(string querySignature, CompiledQuery compiledQuery)
    {
        var entry = new CacheEntry
        {
            CompiledQuery = compiledQuery,
            CreatedAt = DateTime.UtcNow
        };

        _cache[querySignature] = entry;

        _accessOrderLock.EnterWriteLock();
        try
        {
            // Add to front of access order
            _accessOrder.AddFirst(querySignature);
            
            // Evict if necessary
            while (_accessOrder.Count > _maxSize)
            {
                var lru = _accessOrder.Last?.Value;
                if (lru != null)
                {
                    _cache.TryRemove(lru, out _);
                    _accessOrder.RemoveLast();
                }
            }
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }
    }

    private void UpdateAccessOrder(string querySignature)
    {
        _accessOrderLock.EnterWriteLock();
        try
        {
            // Move to front of access order
            _accessOrder.Remove(querySignature);
            _accessOrder.AddFirst(querySignature);
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }
    }

    private void RemoveEntry(string querySignature)
    {
        _cache.TryRemove(querySignature, out _);
        
        _accessOrderLock.EnterWriteLock();
        try
        {
            _accessOrder.Remove(querySignature);
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }
    }

    private class CacheEntry
    {
        public CompiledQuery CompiledQuery { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}

/// <summary>
/// Statistics about the query assembly cache performance
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public int MaxSize { get; set; }
    public double CacheEfficiency { get; set; }
}