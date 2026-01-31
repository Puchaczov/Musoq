using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Generic object resolver for entities.
/// </summary>
public class EntityResolver<T> : IObjectResolver
{
    private static readonly object[] EmptyContexts = [];
    private readonly T _entity;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public EntityResolver(
        T entity,
        IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    {
        _entity = entity;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
    }

    public object[] Contexts => EmptyContexts;

    public object this[string name]
    {
        get
        {
            if (_nameToIndexMap.TryGetValue(name, out var index))
                return _indexToObjectAccessMap[index](_entity);
            return null!;
        }
    }

    public object this[int index]
    {
        get
        {
            if (_indexToObjectAccessMap.TryGetValue(index, out var accessor))
                return accessor(_entity);
            return null!;
        }
    }

    public bool HasColumn(string name)
    {
        return _nameToIndexMap.ContainsKey(name);
    }
}
