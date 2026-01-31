using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

/// <summary>
///     Generic entity source for benchmarks.
/// </summary>
public class BenchmarkEntitySource<T> : RowSource
{
    private readonly IEnumerable<T> _entities;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public BenchmarkEntitySource(
        IEnumerable<T> entities,
        IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    {
        _entities = entities;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _entities)
                yield return new BenchmarkEntityResolver<T>(entity, _nameToIndexMap, _indexToObjectAccessMap);
        }
    }
}
