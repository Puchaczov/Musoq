using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Performance;

/// <summary>
/// Pooled object resolver that can be reused to reduce allocations
/// Part of Phase 3 memory management optimization
/// </summary>
public class PooledObjectResolver : IObjectResolver, IReadOnlyRow
{
    private readonly Dictionary<string, object?> _values;
    private readonly Dictionary<int, object?> _indexedValues;
    private bool _isDisposed;

    public PooledObjectResolver()
    {
        _values = new Dictionary<string, object?>();
        _indexedValues = new Dictionary<int, object?>();
        _isDisposed = false;
    }

    public object[] Contexts => Array.Empty<object>(); // Simple implementation for pooled resolver

    public object this[string name]
    {
        get
        {
            if (_isDisposed) 
                throw new ObjectDisposedException(nameof(PooledObjectResolver));
                
            return _values.TryGetValue(name, out var value) ? value : null;
        }
        set
        {
            if (_isDisposed) 
                throw new ObjectDisposedException(nameof(PooledObjectResolver));
                
            _values[name] = value;
        }
    }

    public object this[int index]
    {
        get
        {
            if (_isDisposed) 
                throw new ObjectDisposedException(nameof(PooledObjectResolver));
                
            return _indexedValues.TryGetValue(index, out var value) ? value : null;
        }
        set
        {
            if (_isDisposed) 
                throw new ObjectDisposedException(nameof(PooledObjectResolver));
                
            _indexedValues[index] = value;
        }
    }

    public bool HasColumn(string name)
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledObjectResolver));
            
        return _values.ContainsKey(name);
    }

    /// <summary>
    /// Reset the resolver for reuse
    /// </summary>
    public void Reset()
    {
        Clear();
        _isDisposed = false;
    }

    /// <summary>
    /// Clear all values but keep the resolver for reuse
    /// </summary>
    public void Clear()
    {
        _values.Clear();
        _indexedValues.Clear();
    }

    /// <summary>
    /// Set multiple values efficiently
    /// </summary>
    public void SetValues(IEnumerable<KeyValuePair<string, object?>> values)
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledObjectResolver));
            
        foreach (var kvp in values)
        {
            _values[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Set multiple indexed values efficiently
    /// </summary>
    public void SetIndexedValues(IEnumerable<KeyValuePair<int, object?>> values)
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledObjectResolver));
            
        foreach (var kvp in values)
        {
            _indexedValues[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Check if an indexed value exists
    /// </summary>
    public bool HasValue(int index)
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledObjectResolver));
            
        return _indexedValues.ContainsKey(index);
    }

    /// <summary>
    /// Get all stored values
    /// </summary>
    public IEnumerable<KeyValuePair<string, object?>> GetNamedValues()
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledObjectResolver));
            
        return _values;
    }

    /// <summary>
    /// Get all indexed values
    /// </summary>
    public IEnumerable<KeyValuePair<int, object?>> GetIndexedValues()
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledObjectResolver));
            
        return _indexedValues;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        Clear();
        _isDisposed = true;
        
        // Return this resolver to the pool
        MemoryPool.ReturnResolver(this);
    }
}