using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Musoq.Schema.DataSources;
#if DEBUG
[DebuggerDisplay("{" + nameof(DebugString) + "()}")]
#endif
public class EntityResolver<T> : IObjectResolver
{
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
        Contexts = [entity];
    }

    public object[] Contexts { get; }

    object IObjectResolver.this[string name]
        => _entity == null ? null : _indexToObjectAccessMap[_nameToIndexMap[name]](_entity);

    object IObjectResolver.this[int index]
        => _entity == null ? null : _indexToObjectAccessMap[index](_entity);

    public bool HasColumn(string name)
    {
        return _nameToIndexMap.ContainsKey(name);
    }

#if DEBUG
    public string DebugString()
    {
        return $"{_entity?.ToString() ?? "null"}";
    }
#endif
}
