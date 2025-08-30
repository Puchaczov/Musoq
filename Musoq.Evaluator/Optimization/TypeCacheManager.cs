using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Musoq.Evaluator.Optimization;

/// <summary>
/// Manages caching of reflection operations to reduce runtime overhead.
/// Targets 30-50% reduction in reflection-related performance costs.
/// </summary>
public static class TypeCacheManager
{
    private static readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private static readonly ConcurrentDictionary<(Type, string), MethodInfo> _methodCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> _propertyCache = new();
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> _constructorCache = new();
    private static readonly ConcurrentDictionary<string, string> _castableTypeCache = new();
    
    // Statistics for monitoring cache effectiveness
    private static long _typeCacheHits = 0;
    private static long _typeCacheMisses = 0;
    private static long _methodCacheHits = 0;
    private static long _methodCacheMisses = 0;
    private static long _propertyCacheHits = 0;
    private static long _propertyCacheMisses = 0;

    /// <summary>
    /// Gets or caches a Type by its full name.
    /// </summary>
    public static Type GetCachedType(string typeName)
    {
        if (_typeCache.TryGetValue(typeName, out var cachedType))
        {
            Interlocked.Increment(ref _typeCacheHits);
            return cachedType;
        }

        Interlocked.Increment(ref _typeCacheMisses);
        var type = Type.GetType(typeName) ?? AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == typeName);
            
        if (type != null)
        {
            _typeCache.TryAdd(typeName, type);
        }
        
        return type;
    }

    /// <summary>
    /// Gets or caches a MethodInfo for the specified type and method name.
    /// </summary>
    public static MethodInfo GetCachedMethod(Type type, string methodName, Type[] parameterTypes = null)
    {
        var key = (type, methodName);
        
        if (_methodCache.TryGetValue(key, out var cachedMethod))
        {
            Interlocked.Increment(ref _methodCacheHits);
            return cachedMethod;
        }

        Interlocked.Increment(ref _methodCacheMisses);
        MethodInfo method;
        
        if (parameterTypes != null)
        {
            method = type.GetMethod(methodName, parameterTypes);
        }
        else
        {
            method = type.GetMethod(methodName);
        }
        
        if (method != null)
        {
            _methodCache.TryAdd(key, method);
        }
        
        return method;
    }

    /// <summary>
    /// Gets or caches a PropertyInfo for the specified type and property name.
    /// </summary>
    public static PropertyInfo GetCachedProperty(Type type, string propertyName)
    {
        var key = (type, propertyName);
        
        if (_propertyCache.TryGetValue(key, out var cachedProperty))
        {
            Interlocked.Increment(ref _propertyCacheHits);
            return cachedProperty;
        }

        Interlocked.Increment(ref _propertyCacheMisses);
        var property = type.GetProperty(propertyName);
        
        if (property != null)
        {
            _propertyCache.TryAdd(key, property);
        }
        
        return property;
    }

    /// <summary>
    /// Gets or caches the default constructor for the specified type.
    /// </summary>
    public static ConstructorInfo GetCachedDefaultConstructor(Type type)
    {
        if (_constructorCache.TryGetValue(type, out var cachedConstructor))
        {
            return cachedConstructor;
        }

        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor != null)
        {
            _constructorCache.TryAdd(type, constructor);
        }
        
        return constructor;
    }

    /// <summary>
    /// Gets or caches the castable type name for code generation.
    /// Optimizes the frequent EvaluationHelper.GetCastableType calls.
    /// </summary>
    public static string GetCachedCastableTypeName(Type type)
    {
        var typeName = type.FullName ?? type.Name;
        
        if (_castableTypeCache.TryGetValue(typeName, out var cachedCastableName))
        {
            return cachedCastableName;
        }

        string castableName;
        if (type == typeof(string))
            castableName = "string";
        else if (type == typeof(int))
            castableName = "int";
        else if (type == typeof(long))
            castableName = "long";
        else if (type == typeof(double))
            castableName = "double";
        else if (type == typeof(bool))
            castableName = "bool";
        else if (type == typeof(decimal))
            castableName = "decimal";
        else if (type == typeof(DateTime))
            castableName = "System.DateTime";
        else if (type == typeof(object))
            castableName = "object";
        else
            castableName = type.FullName ?? type.Name;

        _castableTypeCache.TryAdd(typeName, castableName);
        return castableName;
    }

    /// <summary>
    /// Gets cache statistics for monitoring and performance analysis.
    /// </summary>
    public static CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TypeCacheSize = _typeCache.Count,
            TypeCacheHits = _typeCacheHits,
            TypeCacheMisses = _typeCacheMisses,
            TypeCacheHitRatio = _typeCacheHits + _typeCacheMisses > 0 
                ? (double)_typeCacheHits / (_typeCacheHits + _typeCacheMisses) 
                : 0,
            
            MethodCacheSize = _methodCache.Count,
            MethodCacheHits = _methodCacheHits,
            MethodCacheMisses = _methodCacheMisses,
            MethodCacheHitRatio = _methodCacheHits + _methodCacheMisses > 0 
                ? (double)_methodCacheHits / (_methodCacheHits + _methodCacheMisses) 
                : 0,
            
            PropertyCacheSize = _propertyCache.Count,
            PropertyCacheHits = _propertyCacheHits,
            PropertyCacheMisses = _propertyCacheMisses,
            PropertyCacheHitRatio = _propertyCacheHits + _propertyCacheMisses > 0 
                ? (double)_propertyCacheHits / (_propertyCacheHits + _propertyCacheMisses) 
                : 0,
            
            ConstructorCacheSize = _constructorCache.Count,
            CastableTypeCacheSize = _castableTypeCache.Count
        };
    }

    /// <summary>
    /// Clears all caches. Primarily for testing purposes.
    /// </summary>
    public static void ClearCaches()
    {
        _typeCache.Clear();
        _methodCache.Clear();
        _propertyCache.Clear();
        _constructorCache.Clear();
        _castableTypeCache.Clear();
        
        // Reset statistics
        _typeCacheHits = 0;
        _typeCacheMisses = 0;
        _methodCacheHits = 0;
        _methodCacheMisses = 0;
        _propertyCacheHits = 0;
        _propertyCacheMisses = 0;
    }

    /// <summary>
    /// Pre-warms the cache with commonly used types.
    /// </summary>
    public static void PreWarmCache()
    {
        // Pre-warm with common .NET types
        var commonTypes = new[]
        {
            typeof(string), typeof(int), typeof(long), typeof(double), 
            typeof(bool), typeof(decimal), typeof(DateTime), typeof(object),
            typeof(Guid), typeof(TimeSpan), typeof(byte), typeof(short),
            typeof(float), typeof(char), typeof(sbyte), typeof(uint),
            typeof(ulong), typeof(ushort)
        };

        foreach (var type in commonTypes)
        {
            _typeCache.TryAdd(type.FullName, type);
            GetCachedCastableTypeName(type);
        }
    }
}

/// <summary>
/// Statistics for cache performance monitoring.
/// </summary>
public class CacheStatistics
{
    public int TypeCacheSize { get; set; }
    public long TypeCacheHits { get; set; }
    public long TypeCacheMisses { get; set; }
    public double TypeCacheHitRatio { get; set; }
    
    public int MethodCacheSize { get; set; }
    public long MethodCacheHits { get; set; }
    public long MethodCacheMisses { get; set; }
    public double MethodCacheHitRatio { get; set; }
    
    public int PropertyCacheSize { get; set; }
    public long PropertyCacheHits { get; set; }
    public long PropertyCacheMisses { get; set; }
    public double PropertyCacheHitRatio { get; set; }
    
    public int ConstructorCacheSize { get; set; }
    public int CastableTypeCacheSize { get; set; }

    public override string ToString()
    {
        return $"Type Cache: {TypeCacheSize} entries, {TypeCacheHitRatio:P1} hit ratio\n" +
               $"Method Cache: {MethodCacheSize} entries, {MethodCacheHitRatio:P1} hit ratio\n" +
               $"Property Cache: {PropertyCacheSize} entries, {PropertyCacheHitRatio:P1} hit ratio\n" +
               $"Constructor Cache: {ConstructorCacheSize} entries\n" +
               $"Castable Type Cache: {CastableTypeCacheSize} entries";
    }
}