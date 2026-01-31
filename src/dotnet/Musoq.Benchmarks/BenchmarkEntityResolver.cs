using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

/// <summary>
///     Generic entity resolver for benchmarks.
/// </summary>
public class BenchmarkEntityResolver<T> : IObjectResolver
{
    private static readonly object[] EmptyContexts = [];
    private readonly T _entity;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public BenchmarkEntityResolver(
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
