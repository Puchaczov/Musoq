using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.DataSources;

public class EntitySource<T> : RowSource
{
    private readonly IEnumerable<T> _entities;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public EntitySource(IEnumerable<T> entities, IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    {
        _entities = entities;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get { return _entities.Select(item => new EntityResolver<T>(item, _nameToIndexMap, _indexToObjectAccessMap)); }
    }
}