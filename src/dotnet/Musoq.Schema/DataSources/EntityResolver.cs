using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Musoq.Schema.DataSources;
#if DEBUG
[DebuggerDisplay("{" + nameof(DebugString) + "()}")]
#endif
public class EntityResolver<T>(
    T entity,
    IReadOnlyDictionary<string, int> nameToIndexMap,
    IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    : IObjectResolver
{
    public object[] Contexts => [entity];

    object IObjectResolver.this[string name]
        => entity == null ? null : indexToObjectAccessMap[nameToIndexMap[name]](entity);

    object IObjectResolver.this[int index]
        => entity == null ? null : indexToObjectAccessMap[index](entity);

    public bool HasColumn(string name)
    {
        return nameToIndexMap.ContainsKey(name);
    }

#if DEBUG
    public string DebugString()
    {
        return $"{entity?.ToString() ?? "null"}";
    }
#endif
}
