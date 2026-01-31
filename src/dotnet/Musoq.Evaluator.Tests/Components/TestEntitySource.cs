using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Generic row source for test entities.
/// </summary>
public class TestEntitySource<T> : RowSource
{
    private readonly IEnumerable<T> _entities;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public TestEntitySource(
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
                yield return new EntityResolver<T>(entity, _nameToIndexMap, _indexToObjectAccessMap);
        }
    }
}
