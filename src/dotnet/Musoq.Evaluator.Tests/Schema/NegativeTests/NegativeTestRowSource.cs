using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class NegativeTestRowSource<T> : RowSourceBase<T>
{
    private readonly T[] _entities;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public NegativeTestRowSource(T[] entities, IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    {
        _entities = entities;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
    }

    protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
    {
        chunkedSource.Add(_entities.Select(entity =>
            new EntityResolver<T>(entity, _nameToIndexMap, _indexToObjectAccessMap)).ToList());
    }
}
