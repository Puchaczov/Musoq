using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Musoq.Schema.Compilation;

/// <summary>
/// High-performance cache for compiled method invocations using expression trees.
/// Replaces reflection-based method calls with compiled delegates for significant performance improvements.
/// </summary>
public class SchemaMethodCompilationCache
{
    private readonly ConcurrentDictionary<string, CompiledMethodInfo> _cache;
    private readonly LinkedList<string> _accessOrder;
    private readonly ReaderWriterLockSlim _accessOrderLock;
    private readonly int _maxEntries;
    private readonly TimeSpan _expirationTime;
    
    /// <summary>
    /// Represents a compiled method with its delegate and metadata.
    /// </summary>
    public class CompiledMethodInfo
    {
        public Delegate CompiledDelegate { get; init; }
        public MethodInfo OriginalMethod { get; init; }
        public Type[] ParameterTypes { get; init; }
        public Type ReturnType { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastAccessedAt { get; set; }
        public long AccessCount { get; set; }
        
        public bool IsExpired(TimeSpan expirationTime) => 
            DateTime.UtcNow - LastAccessedAt > expirationTime;
    }
    
    /// <summary>
    /// Cache statistics for monitoring performance and effectiveness.
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; init; }
        public long TotalRequests { get; init; }
        public long CacheHits { get; init; }
        public long CacheMisses { get; init; }
        public long ExpiredEntries { get; init; }
        public long EvictedEntries { get; init; }
        public double CacheEfficiency => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0.0;
        public TimeSpan AverageCompilationTime { get; init; }
    }

    private long _totalRequests;
    private long _cacheHits;
    private long _cacheMisses;
    private long _expiredEntries;
    private long _evictedEntries;
    private readonly List<TimeSpan> _compilationTimes;
    private readonly object _statsLock = new();

    /// <summary>
    /// Initializes a new instance of the SchemaMethodCompilationCache.
    /// </summary>
    /// <param name="maxEntries">Maximum number of cached compiled methods (default: 500)</param>
    /// <param name="expirationTime">Time after which unused methods expire (default: 2 hours)</param>
    public SchemaMethodCompilationCache(int maxEntries = 500, TimeSpan? expirationTime = null)
    {
        if (maxEntries <= 0)
            throw new ArgumentException("Max entries must be positive", nameof(maxEntries));
            
        _maxEntries = maxEntries;
        _expirationTime = expirationTime ?? TimeSpan.FromHours(2);
        _cache = new ConcurrentDictionary<string, CompiledMethodInfo>();
        _accessOrder = new LinkedList<string>();
        _accessOrderLock = new ReaderWriterLockSlim();
        _compilationTimes = new List<TimeSpan>();
    }

    /// <summary>
    /// Gets or compiles a method delegate for the specified method and parameters.
    /// </summary>
    /// <param name="method">The method to compile</param>
    /// <param name="parameterTypes">The parameter types for the method call</param>
    /// <returns>A compiled delegate for the method invocation</returns>
    public Delegate GetOrCompileMethod(MethodInfo method, Type[] parameterTypes)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        if (parameterTypes == null)
            throw new ArgumentNullException(nameof(parameterTypes));

        Interlocked.Increment(ref _totalRequests);
        
        var cacheKey = GenerateCacheKey(method, parameterTypes);
        
        if (_cache.TryGetValue(cacheKey, out var cachedMethod))
        {
            // Check if expired
            if (cachedMethod.IsExpired(_expirationTime))
            {
                _cache.TryRemove(cacheKey, out _);
                RemoveFromAccessOrder(cacheKey);
                Interlocked.Increment(ref _expiredEntries);
            }
            else
            {
                // Update access statistics
                cachedMethod.LastAccessedAt = DateTime.UtcNow;
                cachedMethod.AccessCount++;
                UpdateAccessOrder(cacheKey);
                Interlocked.Increment(ref _cacheHits);
                return cachedMethod.CompiledDelegate;
            }
        }

        // Cache miss - compile the method
        Interlocked.Increment(ref _cacheMisses);
        return CompileAndCacheMethod(method, parameterTypes, cacheKey);
    }

    /// <summary>
    /// Compiles a method into a delegate and caches it.
    /// </summary>
    private Delegate CompileAndCacheMethod(MethodInfo method, Type[] parameterTypes, string cacheKey)
    {
        var compilationStart = DateTime.UtcNow;
        
        try
        {
            var compiledDelegate = CompileMethodToDelegate(method, parameterTypes);
            
            var compiledMethod = new CompiledMethodInfo
            {
                CompiledDelegate = compiledDelegate,
                OriginalMethod = method,
                ParameterTypes = parameterTypes,
                ReturnType = method.ReturnType,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 1
            };

            // Ensure cache doesn't exceed maximum size
            EnsureCacheCapacity();
            
            _cache.TryAdd(cacheKey, compiledMethod);
            AddToAccessOrder(cacheKey);
            
            // Track compilation time
            var compilationTime = DateTime.UtcNow - compilationStart;
            lock (_statsLock)
            {
                _compilationTimes.Add(compilationTime);
                // Keep only recent compilation times for averaging
                if (_compilationTimes.Count > 100)
                {
                    _compilationTimes.RemoveAt(0);
                }
            }
            
            return compiledDelegate;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to compile method {method.DeclaringType?.Name}.{method.Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Compiles a MethodInfo to a strongly-typed delegate using expression trees.
    /// </summary>
    private static Delegate CompileMethodToDelegate(MethodInfo method, Type[] parameterTypes)
    {
        var parameters = new List<ParameterExpression>();
        
        // Create parameter expressions for the method call
        for (int i = 0; i < parameterTypes.Length; i++)
        {
            parameters.Add(Expression.Parameter(parameterTypes[i], $"arg{i}"));
        }

        Expression methodCall;
        
        if (method.IsStatic)
        {
            // Static method call
            methodCall = Expression.Call(method, parameters);
        }
        else
        {
            // Instance method call - first parameter is the instance
            if (parameters.Count == 0)
                throw new ArgumentException("Instance method requires at least one parameter for the instance");
                
            var instance = parameters[0];
            var methodParameters = parameters.Skip(1).ToArray();
            methodCall = Expression.Call(instance, method, methodParameters);
        }

        // Handle void return type
        if (method.ReturnType == typeof(void))
        {
            var actionType = GetActionType(parameterTypes);
            var lambda = Expression.Lambda(actionType, methodCall, parameters);
            return lambda.Compile();
        }
        else
        {
            var funcType = GetFuncType(parameterTypes, method.ReturnType);
            var lambda = Expression.Lambda(funcType, methodCall, parameters);
            return lambda.Compile();
        }
    }

    /// <summary>
    /// Gets the appropriate Action type for the given parameter types.
    /// </summary>
    private static Type GetActionType(Type[] parameterTypes)
    {
        return parameterTypes.Length switch
        {
            0 => typeof(Action),
            1 => typeof(Action<>).MakeGenericType(parameterTypes),
            2 => typeof(Action<,>).MakeGenericType(parameterTypes),
            3 => typeof(Action<,,>).MakeGenericType(parameterTypes),
            4 => typeof(Action<,,,>).MakeGenericType(parameterTypes),
            5 => typeof(Action<,,,,>).MakeGenericType(parameterTypes),
            6 => typeof(Action<,,,,,>).MakeGenericType(parameterTypes),
            7 => typeof(Action<,,,,,,>).MakeGenericType(parameterTypes),
            8 => typeof(Action<,,,,,,,>).MakeGenericType(parameterTypes),
            _ => throw new NotSupportedException($"Action with {parameterTypes.Length} parameters not supported")
        };
    }

    /// <summary>
    /// Gets the appropriate Func type for the given parameter types and return type.
    /// </summary>
    private static Type GetFuncType(Type[] parameterTypes, Type returnType)
    {
        var allTypes = parameterTypes.Concat(new[] { returnType }).ToArray();
        
        return parameterTypes.Length switch
        {
            0 => typeof(Func<>).MakeGenericType(returnType),
            1 => typeof(Func<,>).MakeGenericType(allTypes),
            2 => typeof(Func<,,>).MakeGenericType(allTypes),
            3 => typeof(Func<,,,>).MakeGenericType(allTypes),
            4 => typeof(Func<,,,,>).MakeGenericType(allTypes),
            5 => typeof(Func<,,,,,>).MakeGenericType(allTypes),
            6 => typeof(Func<,,,,,,>).MakeGenericType(allTypes),
            7 => typeof(Func<,,,,,,,>).MakeGenericType(allTypes),
            8 => typeof(Func<,,,,,,,,>).MakeGenericType(allTypes),
            _ => throw new NotSupportedException($"Func with {parameterTypes.Length} parameters not supported")
        };
    }

    /// <summary>
    /// Generates a unique cache key for the method and parameter types.
    /// </summary>
    private static string GenerateCacheKey(MethodInfo method, Type[] parameterTypes)
    {
        var key = $"{method.DeclaringType?.FullName}.{method.Name}";
        if (parameterTypes.Length > 0)
        {
            key += $"({string.Join(",", parameterTypes.Select(t => t.FullName))})";
        }
        
        if (method.IsGenericMethod)
        {
            var genericArgs = method.GetGenericArguments();
            key += $"<{string.Join(",", genericArgs.Select(t => t.FullName))}>";
        }
        
        return key;
    }

    /// <summary>
    /// Ensures the cache doesn't exceed the maximum capacity by evicting least recently used entries.
    /// </summary>
    private void EnsureCacheCapacity()
    {
        if (_cache.Count < _maxEntries) return;

        _accessOrderLock.EnterWriteLock();
        try
        {
            // Remove oldest entries until we're under the limit
            while (_cache.Count >= _maxEntries && _accessOrder.Count > 0)
            {
                var oldestKey = _accessOrder.First?.Value;
                if (oldestKey != null)
                {
                    _cache.TryRemove(oldestKey, out _);
                    _accessOrder.RemoveFirst();
                    Interlocked.Increment(ref _evictedEntries);
                }
            }
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Updates the access order for LRU eviction.
    /// </summary>
    private void UpdateAccessOrder(string key)
    {
        _accessOrderLock.EnterWriteLock();
        try
        {
            // Remove existing entry and add to end
            var node = _accessOrder.Find(key);
            if (node != null)
            {
                _accessOrder.Remove(node);
            }
            _accessOrder.AddLast(key);
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Adds a new key to the access order tracking.
    /// </summary>
    private void AddToAccessOrder(string key)
    {
        _accessOrderLock.EnterWriteLock();
        try
        {
            _accessOrder.AddLast(key);
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes a key from the access order tracking.
    /// </summary>
    private void RemoveFromAccessOrder(string key)
    {
        _accessOrderLock.EnterWriteLock();
        try
        {
            var node = _accessOrder.Find(key);
            if (node != null)
            {
                _accessOrder.Remove(node);
            }
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets comprehensive cache statistics for monitoring and optimization.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            var avgCompilationTime = _compilationTimes.Count > 0 
                ? TimeSpan.FromMilliseconds(_compilationTimes.Average(t => t.TotalMilliseconds))
                : TimeSpan.Zero;

            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                TotalRequests = _totalRequests,
                CacheHits = _cacheHits,
                CacheMisses = _cacheMisses,
                ExpiredEntries = _expiredEntries,
                EvictedEntries = _evictedEntries,
                AverageCompilationTime = avgCompilationTime
            };
        }
    }

    /// <summary>
    /// Clears all cached compiled methods.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        
        _accessOrderLock.EnterWriteLock();
        try
        {
            _accessOrder.Clear();
        }
        finally
        {
            _accessOrderLock.ExitWriteLock();
        }

        // Reset statistics
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
        Interlocked.Exchange(ref _expiredEntries, 0);
        Interlocked.Exchange(ref _evictedEntries, 0);
        
        lock (_statsLock)
        {
            _compilationTimes.Clear();
        }
    }

    /// <summary>
    /// Disposes of the cache and releases resources.
    /// </summary>
    public void Dispose()
    {
        _accessOrderLock?.Dispose();
    }
}